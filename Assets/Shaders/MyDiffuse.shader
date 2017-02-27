// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Simplified Diffuse shader. Differences from regular Diffuse one:
// - no Main Color
// - fully supports only 1 directional light. Other lights can affect it, but it will be per-vertex/SH.

Shader "Custom/My Diffuse Shader" {
Properties {
	_Color("Color", Color) = (1,1,1,1)
	_MainTex ("Base (RGB)", 2D) = "white" {}
}
SubShader {
	Tags { "RenderType"="Opaque"}
	LOD 200

CGPROGRAM
#pragma surface surf MyBlinnPhong nolightmap noforwardadd vertex:vert


sampler2D _MainTex;
sampler2D_half _PersDepthTex;
half4x4 _XRAMatrix2;
half4x4 _WorldToPersCam;
half4 _PersCamPos;
half4 _PersCamFwdXZ;
half4 _Color;
half4 _PlyrColor;
half tolerance;
int _HearSpotCount;
half4 _HearSpotPos[8];

float sightRange;
float onePerSightRange;
float minHearRange;
float maxHearRange;
float onePerMaxHearRange;
float sightEdgeBlur;
float onePerSightEdgeBlur;

struct MySurfaceOutput {
    fixed3 Albedo;
    fixed3 Normal;
    fixed3 Emission;
	fixed3 lightDir;
    half Specular;
    fixed Gloss;
    fixed Alpha;
    fixed Intensity;
};

inline fixed4 LightingMyBlinnPhong (MySurfaceOutput s, fixed3 lightDir, fixed3 halfDir, fixed atten)
{
	fixed diff = max (0, dot (s.Normal, s.lightDir));
	fixed nh = max (0, dot (s.Normal, halfDir));
	fixed spec = pow (nh, s.Specular*128) * s.Gloss;
	
	fixed4 c;
	c.rgb = (s.Albedo * _LightColor0.rgb * diff + _LightColor0.rgb * spec) * atten;
	UNITY_OPAQUE_ALPHA(c.a);
	return c;
}

struct Input {
	float2 uv_MainTex;
	float4 worldPosX;
};


void vert (inout appdata_base v, out Input o){

    UNITY_INITIALIZE_OUTPUT(Input,o);

	//o.worldPosX = mul(unity_ObjectToWorld, float4(v.vertex.xyz * 0.965f, v.vertex.w));
	o.worldPosX = mul(unity_ObjectToWorld, v.vertex);

}

void surf (Input IN, inout MySurfaceOutput o) {
	fixed4 c = tex2D(_MainTex, IN.uv_MainTex);



	half4 D = mul(_WorldToPersCam, IN.worldPosX);
	D = D / D.w;

	half myDot2 = dot (_PersCamFwdXZ.xz, IN.worldPosX.xz - _PersCamPos.xz);

	//half4 newUV = half4(clamp(D.xy * 0.5 + 0.5, 0.0, 1.0), 0, 0);
	half2 newUV = half2(clamp(D.xy * 0.5 + 0.5, 0.0, 1.0));
	half XRAdepth2 = dot(tex2D(_PersDepthTex, newUV), half4(1.0, 1/255.0, 1/65025.0, 1/16581375.0));

	#if defined(SHADER_API_OPENGL)
	half4 XRAH2 = half4(newUV.x * 2.0 - 1.0, newUV.y * 2.0 - 1.0, XRAdepth2 * 2.0 - 1.0, 1.0);
	#else
	half4 XRAH2 = half4(newUV.x * 2.0 - 1.0, newUV.y * 2.0 - 1.0, XRAdepth2, 1.0);
	#endif

	half4 XRAD2 = mul(_XRAMatrix2, XRAH2);

	half4 worldPos2 = XRAD2 / XRAD2.w;
	half distReal = distance(_PersCamPos.xyz, IN.worldPosX.xyz);
	half distReal2 = distance(_PersCamPos.xyz, worldPos2.xyz);

	//half dist2D = distance(_PersCamPos.xz, IN.worldPosX.xz);
	//half ret = ceil(clamp(myDot2, 0, 1)) *(1 - ceil(clamp(abs(D.x) - 1, 0, 1))) * (1 - clamp(distReal - distReal2 - tolerance , 0, 1)) *  (1 - clamp(distReal - sightRange, 0, sightRange) * onePerSightRange);
	//half ret = ceil(clamp(myDot2, 0, 1)) * (1 - clamp(abs(D.x) - sightEdgeBlur, 0, sightEdgeBlur) * onePerSightEdgeBlur) * (1 - clamp(distReal - distReal2 - tolerance , 0, 1)) *  (1 - clamp(distReal - sightRange, 0, sightRange) * onePerSightRange);
	half ret = ceil(clamp(myDot2, 0, 1)) * (1 - clamp(sqrt(D.x * D.x + D.y * D.y) - sightEdgeBlur, 0, sightEdgeBlur) * onePerSightEdgeBlur) * (1 - clamp(distReal - distReal2 - tolerance , 0, 1)) *  (1 - clamp(distReal - sightRange, 0, sightRange) * onePerSightRange);
	//half ret = ceil(clamp(myDot2, 0, 1)) * (1 - clamp(D.x * D.x + D.y * D.y - sightEdgeBlur, 0, sightEdgeBlur) * onePerSightEdgeBlur) * (1 - clamp(distReal - distReal2 - tolerance , 0, 1)) *  (1 - clamp(distReal - sightRange, 0, sightRange) * onePerSightRange);
	//half alphaMultiplier = clamp(ret + (1 - clamp(distReal - minHearRange, 0, maxHearRange) * onePerMaxHearRange) * (1 - clamp(abs(deltaY) - fadeHeight, 0, fadeHeight) * onePerFadeHeight), 0, 1);


	half alphaMul2 = (1 - clamp(distReal - minHearRange, 0, maxHearRange) * onePerMaxHearRange);

	o.lightDir = normalize(_PersCamPos.xyz - IN.worldPosX.xyz) * clamp(alphaMul2, 0.0001, 1);
	o.lightDir += -_PersCamFwdXZ * ret;

	for (int a = 0; a < _HearSpotCount; a++){
		half reta = (1 - clamp(distance(_HearSpotPos[a], IN.worldPosX.xyz) - minHearRange, 0, maxHearRange) * onePerMaxHearRange);
		o.lightDir += normalize(_HearSpotPos[a] - IN.worldPosX.xyz) * reta;
		alphaMul2 += reta;
	}
	o.lightDir = normalize(o.lightDir);

	alphaMul2 = clamp(alphaMul2 - ret, 0, 1);

	half plainMul =0.25 * clamp((IN.worldPosX.y - _PersCamPos.y) * 0.125 - ret - alphaMul2, 0, 1) + alphaMul2;

	//o.Albedo = c.rgb * _Color.rgb *  plainMul + c.rgb * _PlyrColor * ret;
	half lum = dot (c.rgb * _Color.rgb, half3(0.22, 0.707, 0.071)) * plainMul;
	half3 one3 = half3(1,1,1);
	//o.Albedo = half3(lum, lum, lum) + c.rgb * (one3 - (one3 - _Color.rgb) * (one3 - _PlyrColor.rgb)) * ret;
	o.Albedo = half3(lum, lum, lum) + clamp(c.rgb * (_Color.rgb + _PlyrColor.rgb) * ret, 0, 1);
}
ENDCG
}

Fallback "Mobile/VertexLit"
}
