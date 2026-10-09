Shader "NatureManufacture Shaders/Water/Standard Specular Wet"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("MainTex ", 2D) = "white" {}
		_WetColor("Wet Color", Color) = (0.6691177,0.6691177,0.6691177,1)
		_SmoothnessPower("Smoothness Power", Range( 0 , 2)) = 1
		[NoScaleOffset]_BumpMap("BumpMap", 2D) = "bump" {}
		_BumpScale("BumpScale", Range( 0 , 2)) = 1
		[NoScaleOffset]_SpecularRGBSmoothnessA("Specular (RGB) Smoothness (A)", 2D) = "white" {}
		_SpecularPower("Specular Power", Range( 0 , 2)) = 1
		_WetSmoothness("Wet Smoothness", Range( 0 , 100)) = 0
		[NoScaleOffset]_AmbientOcclusionG("Ambient Occlusion (G)", 2D) = "white" {}
		_AmbientOcclusionPower("Ambient Occlusion Power", Range( 0 , 1)) = 1
		_DetailMask("DetailMask", 2D) = "white" {}
		_DetailAlbedoPower("Detail Albedo Power", Range( 0 , 2)) = 0
		_DetailMapAlbedoRNyGNxA("Detail Map Albedo(R) Ny(G) Nx(A)", 2D) = "black" {}
		_DetailNormalMapScale("DetailNormalMapScale", Range( 0 , 5)) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }
		Cull Back
		ZTest LEqual
		CGPROGRAM
		#include "UnityStandardUtils.cginc"
		#pragma target 3.0
		#pragma multi_compile_instancing
		#include "NM_indirect.cginc"
		#pragma multi_compile GPU_FRUSTUM_ON __
		#pragma instancing_options procedural:setup
		#pragma surface surf StandardSpecular keepalpha addshadow fullforwardshadows dithercrossfade 
		struct Input
		{
			float2 uv_texcoord;
			float4 vertexColor : COLOR;
		};

		uniform float _BumpScale;
		uniform sampler2D _BumpMap;
		uniform sampler2D _MainTex;
		uniform float4 _MainTex_ST;
		uniform sampler2D _DetailMapAlbedoRNyGNxA;
		uniform float4 _DetailMapAlbedoRNyGNxA_ST;
		uniform float _DetailNormalMapScale;
		uniform sampler2D _DetailMask;
		uniform float4 _DetailMask_ST;
		uniform float4 _Color;
		uniform float _DetailAlbedoPower;
		uniform float4 _WetColor;
		uniform sampler2D _SpecularRGBSmoothnessA;
		uniform float _SpecularPower;
		uniform float _SmoothnessPower;
		uniform float _WetSmoothness;
		uniform sampler2D _AmbientOcclusionG;
		uniform float _AmbientOcclusionPower;

		void surf( Input i , inout SurfaceOutputStandardSpecular o )
		{
			float2 uv0_MainTex = i.uv_texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
			float3 tex2DNode4 = UnpackScaleNormal( tex2D( _BumpMap, uv0_MainTex ), _BumpScale );
			float2 uv0_DetailMapAlbedoRNyGNxA = i.uv_texcoord * _DetailMapAlbedoRNyGNxA_ST.xy + _DetailMapAlbedoRNyGNxA_ST.zw;
			float4 tex2DNode205 = tex2D( _DetailMapAlbedoRNyGNxA, uv0_DetailMapAlbedoRNyGNxA );
			float2 appendResult11_g1 = (float2(tex2DNode205.a , tex2DNode205.g));
			float2 temp_output_4_0_g1 = ( ( ( appendResult11_g1 * float2( 2,2 ) ) + float2( -1,-1 ) ) * _DetailNormalMapScale );
			float2 break8_g1 = temp_output_4_0_g1;
			float dotResult5_g1 = dot( temp_output_4_0_g1 , temp_output_4_0_g1 );
			float temp_output_9_0_g1 = sqrt( ( 1.0 - saturate( dotResult5_g1 ) ) );
			float3 appendResult20_g1 = (float3(break8_g1.x , break8_g1.y , temp_output_9_0_g1));
			float3 normalizeResult202 = normalize( BlendNormals( tex2DNode4 , appendResult20_g1 ) );
			float2 uv_DetailMask = i.uv_texcoord * _DetailMask_ST.xy + _DetailMask_ST.zw;
			float4 tex2DNode195 = tex2D( _DetailMask, uv_DetailMask );
			float3 lerpResult193 = lerp( tex2DNode4 , normalizeResult202 , tex2DNode195.a);
			o.Normal = lerpResult193;
			float4 temp_output_44_0 = ( tex2D( _MainTex, uv0_MainTex ) * _Color );
			float4 temp_cast_0 = (( _DetailAlbedoPower * tex2DNode205.r )).xxxx;
			float4 blendOpSrc189 = temp_output_44_0;
			float4 blendOpDest189 = temp_cast_0;
			float4 lerpResult192 = lerp( temp_output_44_0 , (( blendOpDest189 > 0.5 ) ? ( 1.0 - ( 1.0 - 2.0 * ( blendOpDest189 - 0.5 ) ) * ( 1.0 - blendOpSrc189 ) ) : ( 2.0 * blendOpDest189 * blendOpSrc189 ) ) , ( _DetailAlbedoPower * tex2DNode195.a ));
			float temp_output_261_0 = ( 1.0 - ( i.vertexColor / float4( 1,1,1,1 ) ).r );
			float4 lerpResult272 = lerp( lerpResult192 , ( lerpResult192 * _WetColor ) , temp_output_261_0);
			o.Albedo = lerpResult272.rgb;
			float4 tex2DNode29 = tex2D( _SpecularRGBSmoothnessA, uv0_MainTex );
			o.Specular = ( tex2DNode29 * _SpecularPower ).rgb;
			float lerpResult269 = lerp( ( tex2DNode29.a * _SmoothnessPower ) , _WetSmoothness , temp_output_261_0);
			o.Smoothness = lerpResult269;
			float clampResult67 = clamp( tex2D( _AmbientOcclusionG, uv0_MainTex ).g , ( 1.0 - _AmbientOcclusionPower ) , 1.0 );
			o.Occlusion = clampResult67;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
}