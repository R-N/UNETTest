// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Unlit/WorldPosShad2"
{
	Properties
	{
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		Blend one zero
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
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float4 localPos : LOCAL_POSITION;
			};

			float4x4 _WorldToLocalWP2;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.localPos = mul(_WorldToLocalWP2, mul(unity_ObjectToWorld, v.vertex));
				return o;
			}
			
			float4 frag (v2f i) : SV_Target
			{
				return float4(clamp(i.localPos.x * 0.01 + 0.5, 0, 1),clamp(i.localPos.y * 0.01 + 0.5, 0, 1),clamp(i.localPos.z * 0.01 + 0.5, 0, 1),clamp(i.localPos.w * 0.01 + 0.5, 0, 1));
			}
			ENDCG
		}
	}
}
