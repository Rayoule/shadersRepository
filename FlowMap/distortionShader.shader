Shader "Custom/distortionShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_FlowTex ("FlowTexture", 2D) = "black" {}
		_UJump ("U jump per phase", Range (-0.25, 0.25)) = 0.25
		_VJump ("V jump per phase", Range (-0.25, 0.25)) = 0.25
		_FlowIntensity ("Intensity of Flow", Range(0, 1)) = 0
		_Tiling ("tiling", float) = 1
		_Speed ("Speed of Animation", float) = 1
		_Levels ("Levels : X = min; Y = max", Vector) = (0,0,0,0)
		_ToonTex ("Colors levels Texture", 2D) = "white" {}
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
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex, _FlowTex, _ToonTex;
			float4 _MainTex_ST, _Levels;
			float _FlowIntensity, _Tiling, _Speed, _UJump, _VJump;

			//////////////// CALCULATE FLOW ////////////////
			float3 FlowUVW (float2 uv, float2 flowVector, float2 jump, float time, bool flowB)
				{
					float phaseOffset = flowB ? 0.5 : 0; // offseted ?
					time *= _Speed;
					float progress = frac(time + phaseOffset); // get progress from 0 to 1
					float3 uvw;

					// get UV from flow
					uvw.xy = uv - (flowVector * _FlowIntensity) * progress;
					uvw.xy += (time - progress) * jump;
					uvw.xy *= _Tiling;
					uvw.z = 1 - abs(1 - 2 * progress);
					// uvw.z will be 0 when flow "jumps" (go from 0 to 1 in a instant)
					return uvw;
				}
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// get flow from red and green
				float2 flow = tex2D(_FlowTex, i.uv).rg * 2 - 1;
				// get "time" from alpha
				float time = tex2D(_FlowTex, i.uv).a + _Time.y;

				float2 jump = float2(_UJump, _VJump);

				// process flow
				float3 newUVA = FlowUVW(i.uv, flow, jump, time, true);
				float3 newUVB = FlowUVW(i.uv, flow, jump, time, false);

				// Get values depending on flow
				fixed4 texA = tex2D(_MainTex, newUVA.xy) * newUVA.z;
				fixed4 texB = tex2D(_MainTex, newUVB.xy) * newUVB.z;

				// Apply
				fixed4 col = texA + texB;
				float clamped = (col.r - _Levels.x) / (_Levels.y - _Levels.x); // set levels
				fixed4 toonCol = tex2D(_ToonTex, float2(clamped, 0));

				return toonCol;
			}
			ENDCG
		}
	}
}
