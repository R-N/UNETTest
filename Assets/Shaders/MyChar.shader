// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Derived from diffuse shader
// Linearch

Shader "Custom/My Char Shader" {
Properties {
	_Color("Color", Color) = (1,1,1,1)
	_MainTex ("Base (RGB)", 2D) = "white" {}
}
SubShader {
	Tags { "RenderType"="Transparent" "Queue" = "Geometry+2"}
	LOD 200
	Blend One OneMinusSrcAlpha
	ColorMask RGBA
	Pass{
	ZWrite On
	ColorMask 0
	}
CGPROGRAM
#pragma surface surf MyLambert nolightmap noforwardadd vertex:vert keepalpha alpha

sampler2D _MainTex;
sampler2D_half _PersDepthTex;
half4x4 _XRAMatrix2;
half4x4 _WorldToPersCam;
half4 _PersCamPos;
half4 _PersCamFwdXZ;
half4 _Color;
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
float noiseMod;


struct Input {
	float2 uv_MainTex;
	float4 worldPosX;
	float noise;
};
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


inline fixed4 LightingMyLambert (MySurfaceOutput s, fixed3 lightDir, fixed atten)
{
	half NdotL = dot (s.Normal, s.lightDir);
	half4 c;
	c.rgb = (s.Albedo + s.Emission) * (NdotL * atten);
	c.a = s.Alpha;
	return c;
}

void vert (inout appdata_base v, out Input o){

    UNITY_INITIALIZE_OUTPUT(Input,o);

	o.worldPosX = mul(unity_ObjectToWorld, float4(v.vertex.xyz * 0.965f, v.vertex.w));
	//o.noise = 0.5 * frac(abs(sin(dot(v.vertex.xyz, float3(12.9898, 78.233, 45.5432)))) * 43758.5453);
	o.noise = 0.25 * (sin(dot(v.vertex.xyz, float3(12.9898, 78.233, 45.5432)) + noiseMod) + 1) ;
	//o.noise = 0.5 * (sin((o.worldPosX.x + o.worldPosX.y + o.worldPosX.z) * 43758.5453 + noiseMod) + 1) ;
}

void surf (Input IN, inout MySurfaceOutput o) {
	fixed4 c = tex2D(_MainTex, IN.uv_MainTex);

	half4 D = mul(_WorldToPersCam, IN.worldPosX);
	D = D / D.w;

	half myDot2 = dot (_PersCamFwdXZ.xz, IN.worldPosX.xz - _PersCamPos.xz);

	//half4 newUV = half4(clamp(D.xy * 0.5 + 0.5, 0.0, 1.0), 0, 0);
	//half XRAdepth2 = dot(tex2Dlod(_PersDepthTex, newUV), half4(1.0, 1/255.0, 1/65025.0, 1/16581375.0));
	half2 newUV = clamp(D.xy * 0.5 + 0.5, 0.0, 1.0);
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
	half ret = ceil(clamp(myDot2, 0, 1)) *(1 - clamp(sqrt(D.x * D.x + D.y * D.y) - sightEdgeBlur, 0, sightEdgeBlur) * onePerSightEdgeBlur) * (1 - clamp(distReal - distReal2 - tolerance , 0, 1)) *  (1 - clamp(distReal - sightRange, 0, sightRange) * onePerSightRange);
	half alphaMultiplier = (1 - clamp(distReal - minHearRange, 0, maxHearRange) * onePerMaxHearRange);

	o.lightDir = normalize(_PersCamPos.xyz - IN.worldPosX.xyz) * clamp(alphaMultiplier, 0.0001, 1);
	o.lightDir += -_PersCamFwdXZ * ret;

	alphaMultiplier -= IN.noise;
	for (int a = 0; a < _HearSpotCount; a++){
		half reta = (1 - clamp(distance(_HearSpotPos[a], IN.worldPosX.xyz) - minHearRange, 0, maxHearRange) * onePerMaxHearRange);
		o.lightDir += normalize(_HearSpotPos[a] - IN.worldPosX.xyz) * reta;
		alphaMultiplier += reta;
	}

	

	alphaMultiplier = clamp(alphaMultiplier + ret, ret, 1);

	o.Emission = c.rgb * _Color.rgb * ret;

	o.Alpha = c.a * alphaMultiplier * _Color.a;
}
ENDCG
}

Fallback "Mobile/VertexLit"
}
