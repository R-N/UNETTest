Shader "Custom/FieldOfView2" {
	Properties {
		_MainTex ("Main Texture", 2D) = "black" {}
	}
	
	SubShader {
		Pass {
			blend one zero
			CGPROGRAM
				#pragma vertex vert_img
				#pragma fragment frag
				#include "UnityCG.cginc"
			
				sampler2D_half _PersDepthTex;
				sampler2D_half _CameraDepthTexture2;
				half4x4 _WorldToPersCam;
				half4x4 _XRAMatrix;
				half4x4 _XRAMatrix2;
				//half4x4 _LocalToWorldWP;
				//half4x4 _LocalToWorldWP2;
				half4 _PersCamPos;
				half4 _PersCamFwdXZ;
			
				half4 frag(v2f_img IN) : COLOR 
				{

					//half XRAdepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture2, IN.uv);
					half XRAdepth = DecodeFloatRGBA(tex2D(_CameraDepthTexture2, IN.uv));

					#if defined(SHADER_API_OPENGL)
					half4 XRAH = half4(IN.uv.x * 2.0 - 1.0, IN.uv.y * 2.0 - 1.0, XRAdepth * 2.0 - 1.0, 1.0);
					#else
					half4 XRAH = half4(IN.uv.x * 2.0 - 1.0, IN.uv.y * 2.0 - 1.0, XRAdepth, 1.0);
					#endif

					half4 XRAD = mul(_XRAMatrix, XRAH);

					half4 worldPos = XRAD / XRAD.w;

					//half4 worldPos = mul(_LocalToWorldWP2, tex2D(_CameraDepthTexture2, IN.uv) * 100.0 - 50.0);

					half4 D = mul(_WorldToPersCam, worldPos);
					D = D / D.w;

					//half myDot = dot(_PersCamFwdXZ.xz, normalize(worldPos.xz - _PersCamPos.xz));
					half myDot2 = dot (_PersCamFwdXZ.xz, worldPos.xz - _PersCamPos.xz);

					half2 newUV = clamp(D.xy * 0.5 + 0.5, 0.0, 1.0);


					//half XRAdepth2 = SAMPLE_DEPTH_TEXTURE(_PersDepthTex, newUV);
					half XRAdepth2 = DecodeFloatRGBA(tex2D(_PersDepthTex, newUV));

					#if defined(SHADER_API_OPENGL)
					half4 XRAH2 = half4(newUV.x * 2.0 - 1.0, newUV.y * 2.0 - 1.0, XRAdepth2 * 2.0 - 1.0, 1.0);
					#else
					half4 XRAH2 = half4(newUV.x * 2.0 - 1.0, newUV.y * 2.0 - 1.0, XRAdepth2, 1.0);
					#endif

					half4 XRAD2 = mul(_XRAMatrix2, XRAH2);

					half4 worldPos2 = XRAD2 / XRAD2.w;

					//half4 worldPos2 = mul(_LocalToWorldWP, tex2D(_PersDepthTex, newUV) * 50.0 - 25.0);
					//half deltaDist = distance(worldPos.xyz, worldPos2.xyz);
					half distReal = distance(_PersCamPos.xyz, worldPos.xyz);
					half distReal2 = distance(_PersCamPos.xyz, worldPos2.xyz);

					half dist2D = distance(_PersCamPos.xz, worldPos.xz);
					half tolerance = 1;//0.15;
					half ret = ceil(clamp(myDot2, 0, 1)) *(1 - ceil(clamp(abs(D.x) - 1, 0, 1))) * (1 - ceil(clamp(distReal - distReal2 - tolerance , 0, 1))) *  (1 - clamp(dist2D - 10, 0, 10) * 0.1);
					//half ret = ceil(clamp(myDot - 0.866, 0, 1)) * (1 - ceil(clamp(distReal - distReal2 - tolerance , 0, 1))) *  (1 - clamp(dist2D - 10, 0, 10) * 0.1);

					half ret2 = clamp(ret + (1 - clamp(distReal - 2.5, 0, 5) * 0.2) * (1 - clamp(abs(worldPos.y - _PersCamPos.y) - 2.5, 0, 2.5) * 0.4), 0, 1);
					return half4(ret2, ret2, ret2, 1);
				}
			ENDCG
		}
	} 
}
