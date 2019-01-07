Shader "Unlit/triplanarMapping"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_worldUVW("Tiling UV coords in world (X,Y,Z)", Vector) = (0,0,0,0)
		_PanningUVW("Panning Offset of UVs (X,Y,Z)", Vector) = (0,0,0,0)
		_Rotation ("Rotation of Mapping Axis", Vector) = (0,0,0,0)
		_HorizTexX ("horizontal X", 2D) = "white" {}
		_HorizTexY ("horizontal Y", 2D) = "white" {}
		_HorizTexZ ("horizontal Z", 2D) = "white" {}
		_BlendFactor ("border blend factor", Range(0,5)) = 1
		_BlendColor ("Blend Color", Color) = (0,0,0,0)
		_Color1 ("Color 1", Color) = (1,1,1,1)
		_Color2 ("Color 2", Color) = (1,1,1,1)
		_Color3 ("Color 3", Color) = (1,1,1,1)
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float3 normal : NORMAL;
				float3 worldPos : TEXCOORD1;
			};

			sampler2D _MainTex, _HorizTexX, _HorizTexY, _HorizTexZ;
			float4 _MainTex_ST, _BlendColor;
			float3 _worldUVW, _Rotation, _PanningUVW;
			float _BlendFactor;
			float4 _Color1, _Color2, _Color3;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				o.normal = v.normal;
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{

				//ROTATION MATRIX
				float radZ = radians(_Rotation.z);
				float cosZ = cos(radZ);
				float sinZ = sin(radZ);

				float radX = radians(_Rotation.x);
				float cosX = cos(radX);
				float sinX = sin(radX);
				
				float radY = radians(_Rotation.y);
				float cosY = cos(radY);
				float sinY = sin(radY);

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
				// END of ROTATION MATRIX

				//INVERSE ROTATION MATRIX
				float icosZ = cos(-radZ);
				float isinZ = sin(-radZ);

				float icosX = cos(-radX);
				float isinX = sin(-radX);
				
				float icosY = cos(-radY);
				float isinY = sin(-radY);

				float3 ixAxis = float3(
					icosY * icosZ,
					icosX * isinZ + isinX * isinY * icosZ,
					isinX * isinZ - icosX * isinY * icosZ
				);
				float3 iyAxis = float3(
					-icosY * isinZ,
					icosX * icosZ - isinX * isinY * isinZ,
					isinX * icosZ + icosX * isinY * isinZ
				);
				float3 izAxis = float3(
					isinY,
					-isinX * icosY,
					icosX * icosY
				);
				// END of INVERSE ROTATION MATRIX


				// finding which axis the pixel is facing w/ dot product
				float dotX = dot(i.normal, xAxis);
				float dotY = dot(i.normal, yAxis);
				float dotZ = dot(i.normal, zAxis);

				float absDotX = abs(dotX);
				float absDotY = abs(dotY);
				float absDotZ = abs(dotZ);

				float maxX = step(absDotY, absDotX) * step(absDotZ, absDotX);
				float maxY = step(absDotX, absDotY) * step(absDotZ, absDotY);
				float maxZ = step(absDotY, absDotZ) * step(absDotX, absDotZ);

				// Get interpolated borders
				// X
				float equalXY =  1 - abs(absDotX / absDotY - 1);
				float equalXZ =  1 - abs(absDotX / absDotZ - 1);

				// Z
				float equalZY =  1 - abs(absDotZ / absDotY - 1);
				float equalZX =  1 - abs(absDotZ / absDotX - 1);

				// Y
				float equalYX =  1 - abs(absDotY / absDotX - 1);
				float equalYZ =  1 - abs(absDotY / absDotZ - 1);

				float borderX = max(equalXY, equalXZ) * maxX;
				float borderY = max(equalYX, equalYZ) * maxY;
				float borderZ = max(equalZY, equalZX) * maxZ;

				float borders = saturate(borderX + borderY + borderZ);
				borders = saturate(borders * _BlendFactor);

				// normalize the absDots so there addition never goes over 1
				float totalAbsDot = absDotX + absDotY + absDotZ;
				float normAbsDotX = absDotX / totalAbsDot;
				float normAbsDotY = absDotY / totalAbsDot;
				float normAbsDotZ = absDotZ / totalAbsDot;

				// Rotate WorldPos w/ inverse rotation matrix
				i.worldPos = ixAxis * i.worldPos.x + iyAxis * i.worldPos.y + izAxis * i.worldPos.z;

				// UVs of differents textures with world position with parameters applied
				float2 uvX = float2(
					(i.worldPos.z) / (_worldUVW.z) + _PanningUVW.z,
					(i.worldPos.y) / (_worldUVW.y) - _PanningUVW.y);
				float2 uvY = float2(
					(i.worldPos.x) / (_worldUVW.x) - _PanningUVW.x,
					(i.worldPos.z) / (_worldUVW.z) + _PanningUVW.z);
				float2 uvZ = float2(
					(i.worldPos.y) / (_worldUVW.y) + _PanningUVW.y,
					(i.worldPos.x) / (_worldUVW.x) - _PanningUVW.x);

				float3 colX = tex2D(_HorizTexX, uvX).rgb;
				colX *= _Color1.rgb;
				float3 colY = tex2D(_HorizTexY, uvY).rgb;
				colY *= _Color2.rgb;
				float3 colZ = tex2D(_HorizTexZ, uvZ).rgb;
				colZ *= _Color3.rgb;

				//processing the colors blended and separated
				fixed3 separateCol = float3(maxX * colX + maxY * colY + maxZ * colZ);
				fixed3 blendingCol = float3(normAbsDotX*colX + normAbsDotY*colY + normAbsDotZ*colZ) * _BlendColor;

				// mixing cols
				fixed3 col = ((1 - borders) * separateCol) + (borders * blendingCol);
				return fixed4(col, 1);
			}
			ENDCG
		}
	}
}
