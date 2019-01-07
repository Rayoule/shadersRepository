Shader "Custom/CustomArtyHeightMapping"
{
	Properties
	{
		_MainTex ("Radius Map", 2D) = "white" {}
		_RadiusMapStrength ("Strength of Radius Map", float) = 1
		_ColorMapTex ("Texture", 2D) = "white" {}
		_MaxRadius ("Maximum Radius", Range(-0.03, 0.03)) = 0
		_MinRadius ("Minimum Radius", Range(-0.03, 0.03)) = 0
		_MinPosVertex ("Minimum vertex position", Range(-0.03, 0.03)) = 0
		_CutOff("Cut off Alpha", float) = 0.1
	}
	SubShader
	{
		Tags {"Queue"="Transparent" "RenderType"="Transparent"}
		LOD 100

		ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float level : TEXCOORD1;
			};

			sampler2D _MainTex, _ColorMapTex;
			float4 _MainTex_ST;
			float _MaxRadius, _MinRadius, _RadiusMapStrength, _CutOff, _MinPosVertex;
			
			v2f vert (appdata v)
			{
				// Get Radius
				float radiusMapOne = tex2Dlod(_MainTex, float4(v.uv + _Time.y/3, 0, 1)).r;
				float radiusMapTwo = tex2Dlod(_MainTex, float4(v.uv*2 - _Time.y/6, 0, 1)).r;
				float radiusMap = radiusMapOne * radiusMapTwo;
				// Add to vertex w/ calculations
				v.vertex += v.vertex * radiusMap * _RadiusMapStrength;
				v.vertex = float4 (clamp(length(v.vertex.xyz), _MinPosVertex, 1000) * normalize(v.vertex.xyz), v.vertex.w);
				float pointRadius = length(v.vertex.xyz);
				float level = smoothstep(_MinRadius, _MaxRadius, pointRadius);
				level = sqrt(level);


				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.level = level;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float2 UVs = float2(i.level, 0.0);
				fixed4 col = tex2D(_ColorMapTex, UVs);
				clip(col.a - _CutOff);
				return fixed4(col);
			}
			ENDCG
		}
	}
}
