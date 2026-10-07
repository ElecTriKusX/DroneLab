Shader "NatureManufacture Shaders/Water/Standard Metallic Wet"
{
	Properties
	{
		_WetColor("Wet Color", Color) = (0.6691177,0.6691177,0.6691177,1)
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("MainTex ", 2D) = "white" {}
		_SmoothnessPower("Smoothness Power", Range( 0 , 2)) = 1
		[NoScaleOffset]_BumpMap("BumpMap", 2D) = "bump" {}
		_BumpScale("BumpScale", Range( 0 , 5)) = 0
		[NoScaleOffset]_MetalicRAmbientOcclusionGSmoothnessA("Metalic (R) Ambient Occlusion (G) Smoothness (A)", 2D) = "white" {}
		_MetallicPower("Metallic Power", Range( 0 , 2)) = 1
		_AmbientOcclusionPower("Ambient Occlusion Power", Range( 0 , 1)) = 1
		_WetSmoothness("Wet Smoothness", Range( 0 , 0.99)) = 0.67
		_DetailMask("DetailMask (A)", 2D) = "white" {}
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
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows dithercrossfade 
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
		uniform sampler2D _MetalicRAmbientOcclusionGSmoothnessA;
		uniform float _MetallicPower;
		uniform float _SmoothnessPower;
		uniform float _WetSmoothness;
		uniform float _AmbientOcclusionPower;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv0_MainTex = i.uv_texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
			float3 tex2DNode4 = UnpackScaleNormal( tex2D( _BumpMap, uv0_MainTex ), _BumpScale );
			float2 uv0_DetailMapAlbedoRNyGNxA = i.uv_texcoord * _DetailMapAlbedoRNyGNxA_ST.xy + _DetailMapAlbedoRNyGNxA_ST.zw;
			float4 tex2DNode486 = tex2D( _DetailMapAlbedoRNyGNxA, uv0_DetailMapAlbedoRNyGNxA );
			float2 appendResult11_g1 = (float2(tex2DNode486.a , tex2DNode486.g));
			float2 temp_output_4_0_g1 = ( ( ( appendResult11_g1 * float2( 2,2 ) ) + float2( -1,-1 ) ) * _DetailNormalMapScale );
			float2 break8_g1 = temp_output_4_0_g1;
			float dotResult5_g1 = dot( temp_output_4_0_g1 , temp_output_4_0_g1 );
			float temp_output_9_0_g1 = sqrt( ( 1.0 - saturate( dotResult5_g1 ) ) );
			float3 appendResult20_g1 = (float3(break8_g1.x , break8_g1.y , temp_output_9_0_g1));
			float2 uv_DetailMask = i.uv_texcoord * _DetailMask_ST.xy + _DetailMask_ST.zw;
			float4 tex2DNode481 = tex2D( _DetailMask, uv_DetailMask );
			float3 lerpResult479 = lerp( tex2DNode4 , BlendNormals( tex2DNode4 , appendResult20_g1 ) , tex2DNode481.a);
			o.Normal = lerpResult479;
			float4 temp_output_77_0 = ( tex2D( _MainTex, uv0_MainTex ) * _Color );
			float4 temp_cast_0 = (( _DetailAlbedoPower * tex2DNode486.r )).xxxx;
			float4 blendOpSrc474 = temp_output_77_0;
			float4 blendOpDest474 = temp_cast_0;
			float4 lerpResult480 = lerp( temp_output_77_0 , (( blendOpDest474 > 0.5 ) ? ( 1.0 - ( 1.0 - 2.0 * ( blendOpDest474 - 0.5 ) ) * ( 1.0 - blendOpSrc474 ) ) : ( 2.0 * blendOpDest474 * blendOpSrc474 ) ) , ( _DetailAlbedoPower * tex2DNode481.a ));
			float temp_output_522_0 = ( 1.0 - ( i.vertexColor / float4( 1,1,1,1 ) ).r );
			float4 lerpResult541 = lerp( lerpResult480 , ( lerpResult480 * _WetColor ) , temp_output_522_0);
			o.Albedo = lerpResult541.rgb;
			float4 tex2DNode2 = tex2D( _MetalicRAmbientOcclusionGSmoothnessA, uv0_MainTex );
			o.Metallic = ( tex2DNode2.r * _MetallicPower );
			float lerpResult540 = lerp( ( tex2DNode2.a * _SmoothnessPower ) , _WetSmoothness , temp_output_522_0);
			o.Smoothness = lerpResult540;
			float clampResult96 = clamp( tex2DNode2.g , ( 1.0 - _AmbientOcclusionPower ) , 1.0 );
			o.Occlusion = clampResult96;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
}