Shader "Standard/ToonLit" {

	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_ShadowColor ("Shadow Color", Color) = (0,0,0,1)
		_RampTex ("Ramp Texture", 2D) = "white" {}
		_ShadowRampTex ("Shadow Ramp Texture", 2D) = "white" {}
		_EmissiveTex ("Emissive Texture", 2D) = "black" {}
		_EmissiveColor ("Emissive Color", Color) = (0,0,0,0)
		_EmissionIntensity ("Emission Intensity", float) = 1
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		#include "UnityPBSLighting.cginc"
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf ToonLitStandard finalcolor:shadowColor fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _RampTex, _ShadowRampTex;
		fixed4 _ShadowColor;
		float lightingInfo;

		struct ToonLitOutput
		{
			fixed3 Albedo;
			fixed3 Normal;
			fixed3 Emission;
			fixed Alpha;
		};


		//////////// LIGHT CALCULATION ////////////////
		half4 LightingToonLitStandard (ToonLitOutput s, half3 lightDir, half atten) {

			// Light Process
            half NdotL = dot (s.Normal, lightDir);
			NdotL = (NdotL * 0.5) + 0.5;
			half toon = tex2D(_RampTex, half2(NdotL, 0)).r; // Toon lighting

			// Shadow Process
			half shadowToon = tex2D(_ShadowRampTex, half2(atten, 0)).r; // Toon Shadows

			// Lighting atten
			float lighting =  min(shadowToon, toon); // Keep Lower Shadows
			
			// Apply
			half4 c;
    		c.rgb = s.Albedo * _LightColor0.rgb * lighting;
            c.a = s.Alpha;

            return c;
        }

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
		};

		sampler2D _EmissiveTex;
		fixed4 _Color;
		float4 _EmissiveColor;
		float _EmissionIntensity;

		// Make GPU Instancing work
		UNITY_INSTANCING_BUFFER_START(Props)
		UNITY_INSTANCING_BUFFER_END(Props)


		//////////// TEXTURES CALCULATION ////////////////
		void surf (Input IN, inout ToonLitOutput o) {
			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;

			//Emissive
			float3 emissive = tex2D(_EmissiveTex, IN.uv_MainTex).rgb * _EmissiveColor;
			o.Emission = emissive * _EmissionIntensity;

			o.Alpha = c.a;
		}

		// Add Shadow Color (a base color more present where the object is the less lit)
		void shadowColor (Input IN, ToonLitOutput o, inout fixed4 color) {

			fixed3 newColor;
			fixed baseValue = max(max(color.r, color.g), color.b);
			newColor = _ShadowColor * (1 - baseValue);

			newColor.r = max(color.r, newColor.r);
			newColor.g = max(color.g, newColor.g);
			newColor.b = max(color.b, newColor.b);

			color.rgb = newColor;
		}

		ENDCG
	}
	FallBack "Diffuse"
}
