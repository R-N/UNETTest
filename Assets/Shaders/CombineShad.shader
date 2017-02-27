Shader "Custom/FieldOfView2" {
	Properties {
		_MainTex ("Main Texture", 2D) = "black" {}
	}
	
	SubShader {
		Pass{
			blend one zero
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
				#include "UnityCG.cginc"
			sampler2D _MainTex;
			sampler2D _DarkTex;
			sampler2D _MaskTex;
			float4 frag(v2f_img IN) : COLOR 
				{
					float mask = tex2D(_MaskTex, IN.uv).r;
					return float4(tex2D(_MainTex, IN.uv).rgb * mask + tex2D(_DarkTex, IN.uv).rgb * (1 - mask) * 0.25f, 1);
				}
			ENDCG
		}

	} 
}
