//// This Shader is using VERTEX COLOR RED CHANNEL to display


Shader "Mandragora/handShader"
{
	Properties
	{
		_LimitBloom ("Limitation Bloom", Range(0,1)) = 0
		_StarsCubeMap ("Stars CubeMap", Cube) = "" {}
		_FlowTex ("Flow Textures", 2D) = "white" {}
		_Tiling ("Textures Tiling", float) = 1
		_FlowIntensity ("Flow Intensity", float) = 1
		_StarsColor1 ("(1) Stars Color", Color) = (1,1,1,1)
		_StarsColor2 ("(2) Stars Color", Color) = (1,1,1,1)
		_StarsColor3 ("(3) Stars Color", Color) = (1,1,1,1)
		_VertexColorMultiply ("Vertex Color Multiplicator", float) = 1
		_VertexColorPower ("Vertex Color Power", float) = 1
		_FadeColor ("FadeColor", Color) = (1,1,1,1)
		_FadeOpacity ("Fade Opacity", Range(0,1)) = 1
		_FresnelPow ("Fresnel Power", float) = 1
		_FresnelFlowIntensity ("Fresnel Flow Intensity", float) = 1
		_ReflexionIntensity ("Reflexion Intensity", Range(0,1)) = 0.1
		_ReflexionPower ("Reflexion Power", float) = 2
	}
	SubShader
	{
		Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
		LOD 100

		ZWrite Off
		Cull Off

		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma geometry geo
			
			#include "UnityCG.cginc"
			#include "Lighting.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 color : COLOR;
			};

			struct v2g
			{
				float4 vertex : SV_POSITION;
				float3 flatNormal : TEXCOORD3;
				float3 color : COLOR;
				float3 worldVertex : TEXCOORD2;
				float4 screenPos : TEXCOORD1;
			};

			struct g2f
			{
				v2g data;
			};

			sampler2D _StarsTex, _FlowTex;
			samplerCUBE _StarsCubeMap;
			float _Tiling, _FlowIntensity, _VertexColorMultiply, _VertexColorPower;
			float _FresnelFlowIntensity, _FresnelPow, _FadeOpacity;
			float _ReflexionIntensity, _ReflexionPower;
			float _LimitBloom;
			fixed4 _StarsColor1, _StarsColor2, _StarsColor3, _FadeColor;
			
			v2g vert (appdata v)
			{
				v2g o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldVertex = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.screenPos = ComputeScreenPos(o.vertex);
				o.color = v.color;
				o.flatNormal = float3(0,0,0);
				return o;
			}


			///////////// GEOMETRY SHADER //////////////////
			[maxvertexcount(3)]
			void geo (triangle v2g i[3], inout TriangleStream<g2f> stream)
			{
				float3 p0 = i[0].worldVertex.xyz;
				float3 p1 = i[1].worldVertex.xyz;
				float3 p2 = i[2].worldVertex.xyz;

				float3 triangleNormal = normalize(cross(p1 - p0, p2 - p0)); // get the normal of the face
				// store it in flatNormal
				i[0].flatNormal = triangleNormal;
				i[1].flatNormal = triangleNormal;
				i[2].flatNormal = triangleNormal;

				// new vertices
				g2f g0, g1, g2;
				g0.data = i[0];
				g1.data = i[1];
				g2.data = i[2];

				// Apply
				stream.Append(g0);
				stream.Append(g1);
				stream.Append(g2);
			}

			// 3D rotation matrix
			float3 rotate4x4(float3 source, float3 angles) {
				float radX = radians(angles.x);
				float radY = radians(angles.y);
				float radZ = radians(angles.z);
				float sinX = sin(radX);
				float cosX = cos(radX);
				float sinY = sin(radY);
				float cosY = cos(radY);
				float sinZ = sin(radZ);
				float cosZ = cos(radZ);

				float3 xAxis = float3(
					cosY * cosZ,
					cosX * sinZ + sinX * sinY * cosZ,
					sinX * sinZ - cosX * sinY * cosZ
				);
				float3 yAxis = float3(
					-cosY * sinZ,
					cosX * cosZ - sinX * sinY * sinZ,
					sinX * cosZ + cosX * sinY * sinZ
				);
				float3 zAxis = float3(
					sinY,
					-sinX * cosY,
					cosX * cosY
				);

				return xAxis * source.x + yAxis * source.y + zAxis * source.z;
			}
			

			fixed4 frag (g2f i, fixed facing : VFACE) : SV_Target
			{
				// Get some variables
				float redVertexColor = pow(saturate(i.data.color.r), _VertexColorPower);
				float3 flatNormals = i.data.flatNormal;
				float3 worldPosition = i.data.worldVertex;
				float3 toCam = normalize(_WorldSpaceCameraPos - worldPosition);

				// Inverse Normal for VFACE
				float3 invertedNormal = -flatNormals;
				facing = step(1, facing);
				flatNormals = lerp(invertedNormal, flatNormals, facing);

				// Process light direction
				int lightID = _WorldSpaceLightPos0.w;
				float3 directionalLightDir = normalize(_WorldSpaceLightPos0.xyz);
				float3 pointLightDir = normalize(worldPosition - _WorldSpaceLightPos0.xyz);
				float3 lightDir = lerp(directionalLightDir, pointLightDir, lightID);

				// Process Reflection with this Light
				float3 H = normalize(lightDir + toCam);
				float NdotH = 1 - saturate(dot(flatNormals, H));
				NdotH = pow(NdotH, _ReflexionPower);
				float3 lightReflexion = NdotH * _LightColor0.rgb * _ReflexionIntensity * redVertexColor;
				lightReflexion *= facing;


				// Calculate Fresnel that multiply with Flow Texture
				float fresnel = dot(toCam, flatNormals);
				fresnel = 1 - fresnel;
				fresnel = pow(saturate(fresnel), _FresnelPow);
				fresnel *= _FresnelFlowIntensity;

				// Get Screen UVs
				float2 screenUv = (i.data.screenPos.xy/i.data.screenPos.w) * _Tiling;

				// Get FlowTex with fresnel Multiply
				float3 flowTex = tex2D(_FlowTex, screenUv * fresnel).rgb;
				flowTex *= _FlowIntensity;

				// Apply rotation offset
				float3 cubeSampleVector = rotate4x4(toCam, flowTex);
				// Sample Cubemap
				float3 starsTex = texCUBE(_StarsCubeMap, cubeSampleVector).rgb;

				// Process FadeColor
				float3 fadeColor = float3(_FadeColor.r * flowTex.b,
											_FadeColor.g * redVertexColor,
											_FadeColor.b);
				float3 finalFade = redVertexColor * _FadeColor.rgb;

				// Noise 3D
				float3 Noise = float3(0,0,0);
				Noise.x = sin(worldPosition.y*10 + _Time.z) * cos(worldPosition.z*2 + _Time.y);
				Noise.y = sin(worldPosition.z*5 + _Time.z) * cos(worldPosition.x + _Time.y);
				Noise.z = sin(worldPosition.x*20 + _Time.z) * cos(worldPosition.y*1.5 + _Time.y);
				Noise = Noise*0.5 + 0.5;
				Noise = pow(Noise, 3);

				// Apply All colors
				float3 starsCol = float3(0,0,0);
				starsCol += _StarsColor1.rgb * starsTex.r * Noise.x;
				starsCol += _StarsColor2.rgb * starsTex.g * Noise.y;
				starsCol += _StarsColor3.rgb * starsTex.b * Noise.z;
				// saturate and add the fade color that will glow
				starsCol = saturate(starsCol);
				starsCol += finalFade * _FadeOpacity;
				// add Vertex Color Multiply
				starsCol *= _VertexColorMultiply * redVertexColor;


				// Apply
				fixed4 col = fixed4(0,0,0,1);
				col.rgb = starsCol;
				col.rgb += lightReflexion;
				col.rgb = lerp(saturate(col.rgb), col.rgb, _LimitBloom);

				col.a = redVertexColor;
				
				return col;
			}
			ENDCG
		}
	}
}