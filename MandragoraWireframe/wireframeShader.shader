Shader "Mandragora/wireframeShader"
{
	Properties
	{
		_Color ("Color", Color) = (1,1,1,1)
		_EmissiveColor ("Emissive Color", Color) = (1,1,1,1)
		_Emissive("Emissive", float) = 1
		_WireframeWidth ("Wireframe Width", float) = 0.05
		_AlphaCutoff ("Alpha Cutoff", Range(0,1)) = 0.5
	}
	SubShader
	{
		Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" "IgnoreProjector"="True" }
		LOD 100

		Cull Off

		Pass
		{

			CGPROGRAM

			#pragma target 4.0
			#pragma vertex vert
			#pragma fragment frag
			#pragma geometry geo
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct InterpolatorsVertex {
                float4 vertex : SV_POSITION;
				float4 screenPos : TEXCOORD2;
             };

			// Uses Geometry Shader
			struct InterpolatorsGeometry {
				InterpolatorsVertex data;
				float2 barycentricCoordinates : TEXCOORD3;
			};

			float4 _Color, _EmissiveColor;
			float _WireframeWidth, _AlphaCutoff, _Emissive;
			
			InterpolatorsVertex vert (appdata v)
			{
				InterpolatorsVertex o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.screenPos = ComputeScreenPos(o.vertex);
				return o;
			}

			//////////// GEOMETRY SHADER /////////////////////
			[maxvertexcount(3)]
			void geo (triangle InterpolatorsVertex i[3], inout TriangleStream<InterpolatorsGeometry> stream)
			{

				InterpolatorsGeometry g0, g1, g2;
				g0.data = i[0];
				g1.data = i[1];
				g2.data = i[2];

				// Stores barycentric coordinates used to make wireframe
				g0.barycentricCoordinates = float2(1, 0);
				g1.barycentricCoordinates = float2(0, 1);
				g2.barycentricCoordinates = float2(0, 0);

				stream.Append(g0);
				stream.Append(g1);
				stream.Append(g2);
			}
			
			
			fixed4 frag (InterpolatorsGeometry i) : SV_Target
			{

				float2 screenWPos = i.data.screenPos.xy / i.data.screenPos.w;

				// Wireframe Process using Barycentric Coords
				float3 barys;
				barys.xy = i.barycentricCoordinates;
				barys.z = 1 - barys.x - barys.y; // Deduce z using x and y
				float minBary = min(barys.x, min(barys.y, barys.z)); // get the closer to an edge value
				float delta = fwidth(minBary) * _WireframeWidth / i.data.screenPos.z;
				minBary = smoothstep(0, delta, minBary);
				fixed3 wires = 1 - minBary;

				// Apply color
				fixed4 col;
				col.rgb = wires * _Color.rgb;
				col.rgb += _Emissive * _EmissiveColor.rgb;
				// Apply Alpha
				clip(wires - _AlphaCutoff);
				col.a = wires;
				return col;

			}
			ENDCG
		}
	}
}
