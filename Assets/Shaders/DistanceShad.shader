// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/DistanceShad"
{
	Properties
	{
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		Blend One Zero
		//Zwrite On
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag2
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 depth : TEXCOORD0;
			};
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.depth = o.vertex.zw;
				return o;
			}

			half4 frag2 (v2f i) :SV_Target{
				return EncodeFloatRGBA(i.depth.x / i.depth.y);
			}
			ENDCG
		}
	}
}
