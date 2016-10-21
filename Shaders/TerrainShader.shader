Shader "Custom/TerrainShader" {

		Properties {
			_GrassTex("Grass", 2D) = "white" {}
			_GrassNormal("Grass Normals", 2D) = "white" {}
			_SandTex("Sand", 2D) = "white" {}
			_DirtTex("Dirt", 2D) = "white" {}
			_SnowTex("Snow", 2D) = "white" {}
			_Glossiness("Smoothness", Range(0,1)) = 0.0
			_Metallic("Metallic", Range(0,1)) = 0.0
		}

	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Lambert vertex:vert

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		struct Input {
			float2 uv_GrassTex;
			float2 uv_SandTex;
			float4 vertexColor;
		};

		void vert(inout appdata_full v, out Input o)
		{
			UNITY_INITIALIZE_OUTPUT(Input, o);
			o.vertexColor = v.color; // Save the Vertex Color in the Input for the surf() method
		}

		half _Glossiness;
		half _Metallic;

		sampler2D _GrassTex;
		sampler2D _SandTex;
		sampler2D _SnowTex;
		sampler2D _DirtTex;
		sampler2D _GrassNormal;

		void surf(Input IN, inout SurfaceOutput o)
		{
			// Albedo comes from a texture tinted by color
			fixed4 c1 = tex2D(_SandTex, IN.uv_SandTex);
			fixed4 c2 = tex2D(_GrassTex, IN.uv_GrassTex);
			fixed4 c3 = tex2D(_DirtTex, IN.uv_SandTex);
			fixed4 c4 = tex2D(_SnowTex, IN.uv_SandTex);

			o.Albedo = c1.rgb * IN.vertexColor.x
				+ c2.rgb * IN.vertexColor.y
				+ c3.rgb * IN.vertexColor.z
				+ c4.rgb * IN.vertexColor.w;

			//o.Normal = UnpackNormal(tex2D(_GrassNormal, IN.uv_GrassTex));
			
			o.Alpha = 1;
		}

		ENDCG
	}
	FallBack "Diffuse"
}
