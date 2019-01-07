Shader "Mandragora/StarOutlineShader"
{
	Properties
	{
		_Color("Color", Color) = (1, 1, 1, 1)
		_Luminosity ("Luminosity", float) = 1
		_OutlineExtrusion("[S] Spikes Extrusion", float) = 0.19
		_OutlineColor("[S] Spikes Color", Color) = (1, 1, 1, 1)
		_Speed ("[S] Speed", float) = 0.73
		_PowDist ("[S] Length Visibility", float) = 18.6
		_PowFraction ("[S] Opacity smooth", float) = 4
		_MaxLocalDist ("Max Local Distance", float) = 20.88
		_MinLocalDist ("Min Local Distance", float) = 7.3
		// Scale depending on the mesh local vertices, SETUP IT FIRST
		_Scale ("[PAS TOUCHER] Scale", float) = 1
	}

	CGINCLUDE
		
		#include "UnityCG.cginc"

		struct vertexInput
			{
				float4 vertex : POSITION;
			};

			struct vertexOutput
			{
				float4 pos : SV_POSITION;
				float4 color : COLOR;
			};

		ENDCG

	SubShader
	{
		
		Pass
		{
			NAME "Base Pass"
            Tags { "RenderType"="Opaque" }

			// Write to Stencil buffer (so that outline pass can read)
			Stencil
			{
				Ref 4
				Comp always
				Pass replace
				ZFail keep
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			// Properties
			float4 _Color;
			float _Scale, _Luminosity;

			vertexOutput vert(vertexInput input)
			{
				vertexOutput output;
				float4 newVertex = input.vertex;
				newVertex *= 1 + cos(_Time.z + newVertex.x*1/_Scale + newVertex.y*1/_Scale + newVertex.z*1/_Scale)*0.2;
				output.pos = UnityObjectToClipPos(newVertex);
				output.color = _Color;

				return output;
			}

			float4 frag(vertexOutput input) : COLOR
			{
				return input.color * _Luminosity;
			}

			ENDCG
		}
		

		// Outline pass
		Pass
		{
			NAME "Spikes Pass"

			Tags {"Queue"="Transparent" "RenderType"="Transparent" }
			Blend SrcAlpha OneMinusSrcAlpha
			// Won't draw where it sees ref value 4
			Cull Off
			ZWrite Off
			ZTest On
			Stencil
			{
				Ref 4
				Comp notequal
				Fail keep
				Pass replace
			}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			
			// Properties
			float4 _OutlineColor;
			float _OutlineExtrusion, _MaxLocalDist, _MinLocalDist, _PowDist, _PowFraction, _Speed, _Scale;

			struct appdata
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float distFromCenter : TEXCOORD0;
				float fraction : TEXCOORD1;
				
			};

			v2f vert(appdata input)
			{
				v2f output;

				float4 newPos = input.vertex;
				float4 localPos = newPos;
				float time = _Time.y * _Speed; // set time speed


				_MinLocalDist *= _Scale; // Hard coded for a specific mesh
				_MaxLocalDist *= _Scale; // ""
				float dist = length(localPos.xyz); // take distance vertex/pivot
				dist = (dist - _MinLocalDist) / (_MaxLocalDist - _MinLocalDist); // make it between 0 - 1
				output.distFromCenter = saturate(dist);


				// normal extrusion technique
				float4 normal = normalize(float4(input.normal, 1));
				float noise = frac(time + localPos.x*(1/_Scale) + localPos.y*(1/_Scale) + localPos.z*(1/_Scale)); // desynchronize vertices
				float fractionNoise = 1 - (abs(0.5 - noise) * 2); // make frac between 0-1 and inverted

				output.fraction = pow(fractionNoise, _PowFraction); // give it more intensity
				newPos += float4(normal.xyz, 0.0) * _OutlineExtrusion * dist * noise; // apply everything


				// convert to world space
				output.pos = UnityObjectToClipPos(newPos);

				return output;
			}

			float4 frag(v2f input) : COLOR
			{
				float dist = input.distFromCenter;
				float frac = input.fraction;
				dist = pow(dist, _PowDist);
				dist *= frac;
				dist = saturate(dist);
				
				return float4(_OutlineColor.rgb, _OutlineColor.a * dist);
			}

			ENDCG
		}

		
		

		
	}
}