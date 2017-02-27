Shader "Custom/FieldOfView" {
	Properties {
		_MainTex ("Main Texture", 2D) = "black" {}
		_VisibleTex ("Visible Tex", 2D) = "black" {}
		_Mask ("Mask", 2D) = "black" {}
		_VisibleMask ("Visible Mask", 2D) = "black" {}
		_Mist ("Mist", 2D) = "black" {}
		_ArenaTex ("Arena Texture", 2D) = "black" {}
	}
	
	SubShader {

		// Empty 
		Pass {
			CGPROGRAM
				#pragma vertex vert_img
				#pragma fragment frag
				#pragma fragmentoption ARB_precision_hint_fastest
				#include "UnityCG.cginc"
			
				sampler2D _MainTex;
			
				fixed4 frag(v2f_img IN) : COLOR 
				{
					return tex2D (_MainTex, IN.uv);
				}
			ENDCG
		}
		
		// NormalFoV

		Pass {
			CGPROGRAM
				#pragma vertex vert_img
				#pragma fragment frag
				#pragma fragmentoption ARB_precision_hint_fastest
				#include "UnityCG.cginc"
			
				sampler2D _MainTex;
				sampler2D _VisibleTex;
				sampler2D _Mask;
				sampler2D _VisibleMask;
				sampler2D _ArenaTex;
			
				fixed4 frag(v2f_img IN) : COLOR 
				{
					fixed4 mask = tex2D(_Mask, IN.uv);
					fixed myA = mask.r * 0.75 + 0.25;
					fixed4 vis = tex2D(_VisibleTex, IN.uv);
					fixed4 visMask = tex2D(_VisibleMask, IN.uv);
					fixed4 arn = tex2D (_ArenaTex, IN.uv);
					fixed myA2 = vis.a * visMask.r;
					arn = fixed4(arn.rgb * 0.25 * (1 - myA2), arn.a * (1 - myA2));
					fixed4 grn = tex2D(_MainTex, IN.uv);
					return fixed4(vis.rgb * myA2 + arn + grn.rgb * myA * (1 - arn.a - myA2), 1);
				}
			ENDCG
		}
		
		// FoV with mist

		Pass {
			CGPROGRAM
				#pragma vertex vert_img
				#pragma fragment frag
				#pragma fragmentoption ARB_precision_hint_fastest
				#include "UnityCG.cginc"
			
				sampler2D _MainTex;
				sampler2D _VisibleTex;
				sampler2D _Mask;
				sampler2D _VisibleMask;
				sampler2D _Mist;
				sampler2D _ArenaTex;
			
				fixed4 frag(v2f_img IN) : COLOR 
				{
					fixed4 mask = tex2D(_Mask, IN.uv);
					fixed myA = mask.r * 0.75 + 0.25;
					fixed4 vis = tex2D(_VisibleTex, IN.uv);
					fixed4 visMask = tex2D(_VisibleMask, IN.uv);
					fixed4 arn = tex2D (_ArenaTex, IN.uv);
					fixed myA2 = vis.a * visMask.r;
					arn = fixed4(arn.rgb * 0.25 * (1 - myA2), arn.a * (1 - myA2));
					fixed4 grn = tex2D(_MainTex, IN.uv);
					fixed3 nonMist = vis.rgb * myA2 + arn + grn.rgb * myA * (1 - arn.a - myA2);
					fixed4 mist = tex2D(_Mist, IN.uv);
					return fixed4 (mist.rgb * mist.r + nonMist.rgb * (1 - mist.r), 1);
				}
			ENDCG
		}
	} 
}
