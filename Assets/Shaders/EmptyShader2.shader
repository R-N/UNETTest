Shader "Custom/FieldOfView2" {
	Properties {
		_MainTex ("Main Texture", 2D) = "black" {}
	}
	
	SubShader {

		// Empty 
		Pass {
			CGPROGRAM
				#pragma vertex vert_img
				#pragma fragment frag
				#include "UnityCG.cginc"
			
				sampler2D _MainTex;
			
				fixed4 frag(v2f_img IN) : COLOR 
				{
					return SAMPLE_DEPTH_TEXTURE (_MainTex, IN.uv);
					//return tex2D(_MainTex, IN.uv);
				}
			ENDCG
		}

	} 
}
