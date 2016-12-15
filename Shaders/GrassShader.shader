Shader "Custom/FogShader" {
	Properties{
		_MainTex("Base (RGB)", 2D) = "white" {}
	_Color("Color", Color) = (1, 1, 1, 1)
		_Cutoff("Alpha cutoff", Range(0,1)) = 0.5
	}
		SubShader{
		Tags{ "Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "TransparentCutout" }
		Fog{ Mode  Off }
		LOD 200

		CGPROGRAM
#pragma surface surf Lambert vertex:fogVertex finalcolor:fogColor alphatest:_Cutoff

		sampler2D _MainTex;

#ifndef UNITY_APPLY_FOG
	half4 unity_FogColor;
	half4 unity_FogDensity;
#endif	

	//uniform half4 unity_FogColor;
	uniform half4 unity_FogStart;
	uniform half4 unity_FogEnd;
	//uniform half4 unity_FogDensity;
	uniform half4 _Color;

	struct Input {
		float2 uv_MainTex;
		half fogFactor;
	};

	void fogVertex(inout appdata_full v, out Input data)
	{
		UNITY_INITIALIZE_OUTPUT(Input, data);
		float cameraVertDist = length(mul(UNITY_MATRIX_MV, v.vertex).xyz);
		float f = cameraVertDist;
		data.fogFactor = saturate(1 / pow(2.71828,  f * f));
	}

	void fogColor(Input IN, SurfaceOutput o, inout fixed4 color)
	{
		color.rgb = lerp(unity_FogColor.rgb, color.rgb, IN.fogFactor);
	}

	void surf(Input IN, inout SurfaceOutput o) {
		half4 c = tex2D(_MainTex, IN.uv_MainTex);
		o.Albedo = c.rgb * _Color;
		o.Alpha = c.a;
	}
	ENDCG
	}
		FallBack "Diffuse"
}