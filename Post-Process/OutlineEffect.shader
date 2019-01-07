Shader "Custom/OutlineEffect"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_threshold ("Threshold of line", Range(0, 1)) = 0
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

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
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			sampler2D _MainTex;
			float _threshold;

			fixed4 frag (v2f i) : SV_Target
			{
				

				// Calculate Outline with normals (normals has to be drawed before with replacement shader)
				float2 pixelStep = float2(i.uv.x / _ScreenParams.x, i.uv.y / _ScreenParams.y);
				float3 thisCol = tex2D(_MainTex, i.uv).rgb;

				fixed4 colHC = tex2D(_MainTex, float2(i.uv.x, i.uv.y + pixelStep.y));
					float difHC = distance(thisCol, colHC.rgb);
				fixed4 colHD = tex2D(_MainTex, float2(i.uv.x + pixelStep.x, i.uv.y + pixelStep.y));
					float difHD = distance(thisCol, colHD.rgb);
				fixed4 colHG = tex2D(_MainTex, float2(i.uv.x - pixelStep.x, i.uv.y + pixelStep.y));
					float difHG = distance(thisCol, colHG.rgb);
				fixed4 colBC = tex2D(_MainTex, float2(i.uv.x, i.uv.y - pixelStep.y));
					float difBC = distance(thisCol, colBC.rgb);
				fixed4 colBD = tex2D(_MainTex, float2(i.uv.x + pixelStep.x, i.uv.y - pixelStep.y));
					float difBD = distance(thisCol, colBD.rgb);
				fixed4 colBG = tex2D(_MainTex, float2(i.uv.x - pixelStep.x, i.uv.y - pixelStep.y));
					float difBG = distance(thisCol, colBG.rgb);
				fixed4 colCD = tex2D(_MainTex, float2(i.uv.x + pixelStep.x, i.uv.y));
					float difCD = distance(thisCol, colCD.rgb);
				fixed4 colCG = tex2D(_MainTex, float2(i.uv.x - pixelStep.x, i.uv.y));
					float difCG = distance(thisCol, colCG.rgb);

				// keep the biggest difference
				float maxValue = max(max(max(difHC, difHD), max(difHG, difBC)), max(max(difBD, difBG), max(difCD, difCG)));

				maxValue = sqrt(maxValue);

				float minStep = step(_threshold, maxValue);
				maxValue *= minStep;

				// Apply
				fixed3 col = 1 - fixed3(maxValue, maxValue, maxValue);

				return fixed4(col, 1);
			}
			ENDCG
		}
	}
}