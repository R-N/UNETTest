// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Simplified Diffuse shader. Differences from regular Diffuse one:
// - no Main Color
// - fully supports only 1 directional light. Other lights can affect it, but it will be per-vertex/SH.

Shader "Custom/My Char Shader Always" {
Properties {
	_MainTex ("Base (RGB)", 2D) = "white" {}
}
SubShader {
	Tags { "RenderType"="Opaque" "Queue" = "Geometry"}
	LOD 200
	Blend One Zero 
CGPROGRAM
#pragma surface surf MyLambertLocal nolightmap noforwardadd vertex:vert 

sampler2D _MainTex;
half4 _PlyrColor;

struct Input {
	float2 uv_MainTex;
	float4 worldPosX;
};

inline fixed4 LightingMyLambertLocal (SurfaceOutput s, fixed3 lightDir, fixed atten)
{
	//half NdotL = dot (s.Normal, lightDir);
	half4 c;
	//c.rgb = (s.Albedo + s.Emission) * (NdotL * atten);
	c.rgb = s.Albedo + s.Emission;
	c.a = s.Alpha;
	return c;
}

void vert (inout appdata_base v, out Input o){

    UNITY_INITIALIZE_OUTPUT(Input,o);

	o.worldPosX = mul(unity_ObjectToWorld, v.vertex);
}

void surf (Input IN, inout SurfaceOutput o) {
	fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
	//o.Albedo = c.rgb * _PlyrColor.rgb;
	//o.Albedo = half4(0,0,0,1);
	o.Emission = c.rgb * _PlyrColor.rgb;
	o.Alpha = c.a * _PlyrColor.a;
}
ENDCG
}

Fallback "Mobile/VertexLit"
}
