Shader "NatureManufacture Shaders/Debug/Flowmap Direction"
{
    Properties
    {
        [NoScaleOffset]_Direction("Direction", 2D) = "white" {}
        _TextureSize("Texture Size", Vector) = (246, 246, 0, 0)
        [ToggleUI]_RotateUV("Rotate UV", Float) = 1
        [ToggleUI]Boolean_F2B1E91A("Boolean", Float) = 0
        [NonModifiableTextureData][NoScaleOffset]_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1("Texture2D", 2D) = "white" {}
        [HideInInspector]_EmissionColor("Color", Color) = (1, 1, 1, 1)
        [HideInInspector]_RenderQueueType("Float", Float) = 1
        [HideInInspector][ToggleUI]_AddPrecomputedVelocity("Boolean", Float) = 0
        [HideInInspector][ToggleUI]_DepthOffsetEnable("Boolean", Float) = 0
        [HideInInspector][ToggleUI]_ConservativeDepthOffsetEnable("Boolean", Float) = 0
        [HideInInspector][ToggleUI]_TransparentWritingMotionVec("Boolean", Float) = 0
        [HideInInspector][ToggleUI]_AlphaCutoffEnable("Boolean", Float) = 1
        [HideInInspector]_TransparentSortPriority("_TransparentSortPriority", Float) = 0
        [HideInInspector][ToggleUI]_UseShadowThreshold("Boolean", Float) = 0
        [HideInInspector][ToggleUI]_DoubleSidedEnable("Boolean", Float) = 0
        [HideInInspector][Enum(Flip, 0, Mirror, 1, None, 2)]_DoubleSidedNormalMode("Float", Float) = 2
        [HideInInspector]_DoubleSidedConstants("Vector4", Vector) = (1, 1, -1, 0)
        [HideInInspector][Enum(Auto, 0, On, 1, Off, 2)]_DoubleSidedGIMode("Float", Float) = 0
        [HideInInspector][ToggleUI]_TransparentDepthPrepassEnable("Boolean", Float) = 0
        [HideInInspector][ToggleUI]_TransparentDepthPostpassEnable("Boolean", Float) = 0
        [HideInInspector]_SurfaceType("Float", Float) = 0
        [HideInInspector]_BlendMode("Float", Float) = 0
        [HideInInspector]_SrcBlend("Float", Float) = 1
        [HideInInspector]_DstBlend("Float", Float) = 0
        [HideInInspector]_AlphaSrcBlend("Float", Float) = 1
        [HideInInspector]_AlphaDstBlend("Float", Float) = 0
        [HideInInspector][ToggleUI]_ZWrite("Boolean", Float) = 1
        [HideInInspector][ToggleUI]_TransparentZWrite("Boolean", Float) = 0
        [HideInInspector]_CullMode("Float", Float) = 2
        [HideInInspector][ToggleUI]_EnableFogOnTransparent("Boolean", Float) = 1
        [HideInInspector]_CullModeForward("Float", Float) = 2
        [HideInInspector][Enum(Front, 1, Back, 2)]_TransparentCullMode("Float", Float) = 2
        [HideInInspector][Enum(UnityEditor.Rendering.HighDefinition.OpaqueCullMode)]_OpaqueCullMode("Float", Float) = 2
        [HideInInspector]_ZTestDepthEqualForOpaque("Float", Int) = 4
        [HideInInspector][Enum(UnityEngine.Rendering.CompareFunction)]_ZTestTransparent("Float", Float) = 4
        [HideInInspector][ToggleUI]_TransparentBackfaceEnable("Boolean", Float) = 0
        [HideInInspector][ToggleUI]_EnableBlendModePreserveSpecularLighting("Boolean", Float) = 0
        [HideInInspector]_StencilRef("Float", Int) = 0
        [HideInInspector]_StencilWriteMask("Float", Int) = 6
        [HideInInspector]_StencilRefDepth("Float", Int) = 1
        [HideInInspector]_StencilWriteMaskDepth("Float", Int) = 9
        [HideInInspector]_StencilRefMV("Float", Int) = 33
        [HideInInspector]_StencilWriteMaskMV("Float", Int) = 41
        [HideInInspector]_StencilRefDistortionVec("Float", Int) = 4
        [HideInInspector]_StencilWriteMaskDistortionVec("Float", Int) = 4
        [HideInInspector]_StencilWriteMaskGBuffer("Float", Int) = 15
        [HideInInspector]_StencilRefGBuffer("Float", Int) = 2
        [HideInInspector]_ZTestGBuffer("Float", Int) = 4
        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="HDRenderPipeline"
            "RenderType"="HDUnlitShader"
            "Queue"="AlphaTest+0"
            "ShaderGraphShader"="true"
            "ShaderGraphTargetId"="HDUnlitSubTarget"
        }
        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }
        
            // Render State
            Cull [_CullMode]
        ZWrite On
        ColorMask 0
        ZClip [_ZClip]
        
            // Debug
            // <None>
        
            // --------------------------------------------------
            // Pass
        
            HLSLPROGRAM
        
            // Pragmas
            #pragma multi_compile _ DOTS_INSTANCING_ON
        #pragma instancing_options renderinglayer
        #pragma target 4.5
        #pragma vertex Vert
        #pragma fragment Frag
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
        #pragma multi_compile_instancing
        
            // Keywords
            #pragma shader_feature_local _ _ALPHATEST_ON
        #pragma shader_feature _ _SURFACE_TYPE_TRANSPARENT
        #pragma shader_feature_local _BLENDMODE_OFF _BLENDMODE_ALPHA _BLENDMODE_ADD _BLENDMODE_PRE_MULTIPLY
        #pragma shader_feature_local _ _ADD_PRECOMPUTED_VELOCITY
        #pragma shader_feature_local _ _TRANSPARENT_WRITES_MOTION_VEC
        #pragma shader_feature_local_fragment _ _ENABLE_FOG_ON_TRANSPARENT
            // GraphKeywords: <None>
        
            // For custom interpolators to inject a substruct definition before FragInputs definition,
            // allowing for FragInputs to capture CI's intended for ShaderGraph's SDI.
            struct CustomInterpolators
        {
        };
        #define USE_CUSTOMINTERP_SUBSTRUCT
        
        
        
            // TODO: Merge FragInputsVFX substruct with CustomInterpolators.
        	#ifdef HAVE_VFX_MODIFICATION
        	struct FragInputsVFX
            {
                /* WARNING: $splice Could not find named fragment 'FragInputsVFX' */
            };
            #endif
        
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GeometricTools.hlsl" // Required by Tessellation.hlsl
        	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Tessellation.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl" // Required to be include before we include properties as it define DECLARE_STACK_CB
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphHeader.hlsl" // Need to be here for Gradient struct definition
        
            // --------------------------------------------------
            // Defines
        
            // Attribute
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
        
            #define HAVE_MESH_MODIFICATION
        
            //Strip down the FragInputs.hlsl (on graphics), so we can only optimize the interpolators we use.
            //if by accident something requests contents of FragInputs.hlsl, it will be caught as a compiler error
            //Frag inputs stripping is only enabled when FRAG_INPUTS_ENABLE_STRIPPING is set
            #if !defined(SHADER_STAGE_RAY_TRACING) && SHADERPASS != SHADERPASS_RAYTRACING_GBUFFER && SHADERPASS != SHADERPASS_FULL_SCREEN_DEBUG
            #define FRAG_INPUTS_ENABLE_STRIPPING
            #endif
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
        
        
        
            #define SHADERPASS SHADERPASS_SHADOWS
        
        
            // Following two define are a workaround introduce in 10.1.x for RaytracingQualityNode
            // The ShaderGraph don't support correctly migration of this node as it serialize all the node data
            // in the json file making it impossible to uprgrade. Until we get a fix, we do a workaround here
            // to still allow us to rename the field and keyword of this node without breaking existing code.
            #ifdef RAYTRACING_SHADER_GRAPH_DEFAULT
            #define RAYTRACING_SHADER_GRAPH_HIGH
            #endif
        
            #ifdef RAYTRACING_SHADER_GRAPH_RAYTRACED
            #define RAYTRACING_SHADER_GRAPH_LOW
            #endif
            // end
        
            #ifndef SHADER_UNLIT
            // We need isFrontFace when using double sided - it is not required for unlit as in case of unlit double sided only drive the cullmode
            // VARYINGS_NEED_CULLFACE can be define by VaryingsMeshToPS.FaceSign input if a IsFrontFace Node is included in the shader graph.
            #if defined(_DOUBLESIDED_ON) && !defined(VARYINGS_NEED_CULLFACE)
                #define VARYINGS_NEED_CULLFACE
            #endif
            #endif
        
            // Specific Material Define
        // Setup a define to say we are an unlit shader
        #define SHADER_UNLIT
        
        // Following Macro are only used by Unlit material
        #if defined(_ENABLE_SHADOW_MATTE) && (SHADERPASS == SHADERPASS_FORWARD_UNLIT || SHADERPASS == SHADERPASS_PATH_TRACING)
        #define LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
        #define HAS_LIGHTLOOP
        #endif
            // Caution: we can use the define SHADER_UNLIT onlit after the above Material include as it is the Unlit template who define it
        
            // To handle SSR on transparent correctly with a possibility to enable/disable it per framesettings
            // we should have a code like this:
            // if !defined(_DISABLE_SSR_TRANSPARENT)
            // pragma multi_compile _ WRITE_NORMAL_BUFFER
            // endif
            // i.e we enable the multicompile only if we can receive SSR or not, and then C# code drive
            // it based on if SSR transparent in frame settings and not (and stripper can strip it).
            // this is currently not possible with our current preprocessor as _DISABLE_SSR_TRANSPARENT is a keyword not a define
            // so instead we used this and chose to pay the extra cost of normal write even if SSR transaprent is disabled.
            // Ideally the shader graph generator should handle it but condition below can't be handle correctly for now.
            #if SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_PREPASS
            #if !defined(_DISABLE_SSR_TRANSPARENT) && !defined(SHADER_UNLIT)
                #define WRITE_NORMAL_BUFFER
            #endif
            #endif
        
            #ifndef DEBUG_DISPLAY
                // In case of opaque we don't want to perform the alpha test, it is done in depth prepass and we use depth equal for ztest (setup from UI)
                // Don't do it with debug display mode as it is possible there is no depth prepass in this case
                #if !defined(_SURFACE_TYPE_TRANSPARENT)
                    #if SHADERPASS == SHADERPASS_FORWARD
                    #define SHADERPASS_FORWARD_BYPASS_ALPHA_TEST
                    #elif SHADERPASS == SHADERPASS_GBUFFER
                    #define SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST
                    #endif
                #endif
            #endif
        
            // Define _DEFERRED_CAPABLE_MATERIAL for shader capable to run in deferred pass
            #if defined(SHADER_LIT) && !defined(_SURFACE_TYPE_TRANSPARENT)
                #define _DEFERRED_CAPABLE_MATERIAL
            #endif
        
            // Translate transparent motion vector define
            #if defined(_TRANSPARENT_WRITES_MOTION_VEC) && defined(_SURFACE_TYPE_TRANSPARENT)
                #define _WRITE_TRANSPARENT_MOTION_VECTOR
            #endif
        
            // -- Graph Properties
            CBUFFER_START(UnityPerMaterial)
        float4 _TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1_TexelSize;
        float4 _Direction_TexelSize;
        float2 _TextureSize;
        float _RotateUV;
        float Boolean_F2B1E91A;
        float4 _EmissionColor;
        float _UseShadowThreshold;
        float4 _DoubleSidedConstants;
        float _BlendMode;
        float _EnableBlendModePreserveSpecularLighting;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1);
        SAMPLER(sampler_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1);
        TEXTURE2D(_Direction);
        SAMPLER(sampler_Direction);
        
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
        
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
        
            // Includes
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl"
        	// GraphIncludes: <None>
        
            // --------------------------------------------------
            // Structs and Packing
        
            struct AttributesMesh
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct VaryingsMeshToPS
        {
            SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
        };
        struct SurfaceDescriptionInputs
        {
        };
        struct PackedVaryingsMeshToPS
        {
            SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
        };
        
            PackedVaryingsMeshToPS PackVaryingsMeshToPS (VaryingsMeshToPS input)
        {
            PackedVaryingsMeshToPS output;
            ZERO_INITIALIZE(PackedVaryingsMeshToPS, output);
            output.positionCS = input.positionCS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            return output;
        }
        
        VaryingsMeshToPS UnpackVaryingsMeshToPS (PackedVaryingsMeshToPS input)
        {
            VaryingsMeshToPS output;
            output.positionCS = input.positionCS;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            return output;
        }
        
        
            // --------------------------------------------------
            // Graph
        
        
            // Graph Functions
            // GraphFunctions: <None>
        
            // Graph Vertex
            struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
            // Graph Pixel
            struct SurfaceDescription
        {
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            surface.Alpha = 1;
            surface.AlphaClipThreshold = 0.5;
            return surface;
        }
        
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES AttributesMesh
            #define VaryingsMeshType VaryingsMeshToPS
            #define VFX_SRP_VARYINGS VaryingsMeshType
            #define VFX_SRP_SURFACE_INPUTS FragInputs
            #endif
            
        VertexDescriptionInputs AttributesMeshToVertexDescriptionInputs(AttributesMesh input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
        
            return output;
        }
        
        VertexDescription GetVertexDescription(AttributesMesh input, float3 timeParameters
        #ifdef HAVE_VFX_MODIFICATION
            , AttributesElement element
        #endif
        )
        {
            // build graph inputs
            VertexDescriptionInputs vertexDescriptionInputs = AttributesMeshToVertexDescriptionInputs(input);
            // Override time parameters with used one (This is required to correctly handle motion vector for vertex animation based on time)
        
            // evaluate vertex graph
        #ifdef HAVE_VFX_MODIFICATION
            GraphProperties properties;
            ZERO_INITIALIZE(GraphProperties, properties);
        
            // Fetch the vertex graph properties for the particle instance.
            GetElementVertexProperties(element, properties);
        
            VertexDescription vertexDescription = VertexDescriptionFunction(vertexDescriptionInputs, properties);
        #else
            VertexDescription vertexDescription = VertexDescriptionFunction(vertexDescriptionInputs);
        #endif
            return vertexDescription;
        
        }
        
        AttributesMesh ApplyMeshModification(AttributesMesh input, float3 timeParameters
        #ifdef USE_CUSTOMINTERP_SUBSTRUCT
            #ifdef TESSELLATION_ON
            , inout VaryingsMeshToDS varyings
            #else
            , inout VaryingsMeshToPS varyings
            #endif
        #endif
        #ifdef HAVE_VFX_MODIFICATION
                , AttributesElement element
        #endif
            )
        {
            VertexDescription vertexDescription = GetVertexDescription(input, timeParameters
        #ifdef HAVE_VFX_MODIFICATION
                , element
        #endif
            );
        
            // copy graph output to the results
            input.positionOS = vertexDescription.Position;
            input.normalOS = vertexDescription.Normal;
            input.tangentOS.xyz = vertexDescription.Tangent;
        
            
        
            return input;
        }
        
        #if defined(_ADD_CUSTOM_VELOCITY) // For shader graph custom velocity
        // Return precomputed Velocity in object space
        float3 GetCustomVelocity(AttributesMesh input
        #ifdef HAVE_VFX_MODIFICATION
            , AttributesElement element
        #endif
        )
        {
            VertexDescription vertexDescription = GetVertexDescription(input, _TimeParameters.xyz
        #ifdef HAVE_VFX_MODIFICATION
                , element
        #endif
            );
            return vertexDescription.CustomVelocity;
        }
        #endif
        
        FragInputs BuildFragInputs(VaryingsMeshToPS input)
        {
            FragInputs output;
            ZERO_INITIALIZE(FragInputs, output);
        
            // Init to some default value to make the computer quiet (else it output 'divide by zero' warning even if value is not used).
            // TODO: this is a really poor workaround, but the variable is used in a bunch of places
            // to compute normals which are then passed on elsewhere to compute other values...
            output.tangentToWorld = k_identity3x3;
            output.positionSS = input.positionCS;       // input.positionCS is SV_Position
        
        
        #ifdef HAVE_VFX_MODIFICATION
            // FragInputs from VFX come from two places: Interpolator or CBuffer.
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            // splice point to copy custom interpolator fields from varyings to frag inputs
            
        
            return output;
        }
        
        // existing HDRP code uses the combined function to go directly from packed to frag inputs
        FragInputs UnpackVaryingsMeshToFragInputs(PackedVaryingsMeshToPS input)
        {
            UNITY_SETUP_INSTANCE_ID(input);
        #if defined(HAVE_VFX_MODIFICATION) && defined(UNITY_INSTANCING_ENABLED)
            unity_InstanceID = input.instanceID;
        #endif
            VaryingsMeshToPS unpacked = UnpackVaryingsMeshToPS(input);
            return BuildFragInputs(unpacked);
        }
            SurfaceDescriptionInputs FragInputsToSurfaceDescriptionInputs(FragInputs input, float3 viewWS)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
            #if defined(SHADER_STAGE_RAY_TRACING)
            #else
            #endif
        
        #if UNITY_UV_STARTS_AT_TOP
        #else
        #endif
        
        
        
            // splice point to copy frag inputs custom interpolator pack into the SDI
            
        
            return output;
        }
        
            // --------------------------------------------------
            // Build Surface Data (Specific Material)
        
        void BuildSurfaceData(FragInputs fragInputs, inout SurfaceDescription surfaceDescription, float3 V, PositionInputs posInput, out SurfaceData surfaceData)
        {
            // setup defaults -- these are used if the graph doesn't output a value
            ZERO_INITIALIZE(SurfaceData, surfaceData);
        
            // copy across graph values, if defined
        
            #ifdef WRITE_NORMAL_BUFFER
            // When we need to export the normal (in the depth prepass, we write the geometry one)
            surfaceData.normalWS = fragInputs.tangentToWorld[2];
            #endif
        
            #if defined(DEBUG_DISPLAY)
            if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
            {
                // TODO
            }
            #endif
        
            #ifdef _ENABLE_SHADOW_MATTE
        
                #if (SHADERPASS == SHADERPASS_FORWARD_UNLIT) || (SHADERPASS == SHADERPASS_RAYTRACING_GBUFFER) || (SHADERPASS == SHADERPASS_RAYTRACING_INDIRECT) || (SHADERPASS == SHADERPASS_RAYTRACING_FORWARD)
        
                    HDShadowContext shadowContext = InitShadowContext();
        
                    // Evaluate the shadow, the normal is guaranteed if shadow matte is enabled on this shader.
                    float3 shadow3;
                    ShadowLoopMin(shadowContext, posInput, normalize(fragInputs.tangentToWorld[2]), asuint(_ShadowMatteFilter), GetMeshRenderingLightLayer(), shadow3);
        
                    // Compute the average value in the fourth channel
                    float4 shadow = float4(shadow3, dot(shadow3, float3(1.0/3.0, 1.0/3.0, 1.0/3.0)));
        
                    float4 shadowColor = (1.0 - shadow) * surfaceDescription.ShadowTint.rgba;
                    float  localAlpha  = saturate(shadowColor.a + surfaceDescription.Alpha);
        
                    // Keep the nested lerp
                    // With no Color (bsdfData.color.rgb, bsdfData.color.a == 0.0f), just use ShadowColor*Color to avoid a ring of "white" around the shadow
                    // And mix color to consider the Color & ShadowColor alpha (from texture or/and color picker)
                    #ifdef _SURFACE_TYPE_TRANSPARENT
                        surfaceData.color = lerp(shadowColor.rgb * surfaceData.color, lerp(lerp(shadowColor.rgb, surfaceData.color, 1.0 - surfaceDescription.ShadowTint.a), surfaceData.color, shadow.rgb), surfaceDescription.Alpha);
                    #else
                        surfaceData.color = lerp(lerp(shadowColor.rgb, surfaceData.color, 1.0 - surfaceDescription.ShadowTint.a), surfaceData.color, shadow.rgb);
                    #endif
                    localAlpha = ApplyBlendMode(surfaceData.color, localAlpha).a;
        
                    surfaceDescription.Alpha = localAlpha;
        
                #elif SHADERPASS == SHADERPASS_PATH_TRACING
        
                    surfaceData.normalWS = fragInputs.tangentToWorld[2];
                    surfaceData.shadowTint = surfaceDescription.ShadowTint.rgba;
        
                #endif
        
            #endif // _ENABLE_SHADOW_MATTE
        }
        
            // --------------------------------------------------
            // Get Surface And BuiltinData
        
            void GetSurfaceAndBuiltinData(FragInputs fragInputs, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData RAY_TRACING_OPTIONAL_PARAMETERS)
            {
                // Don't dither if displaced tessellation (we're fading out the displacement instead to match the next LOD)
                #if !defined(SHADER_STAGE_RAY_TRACING) && !defined(_TESSELLATION_DISPLACEMENT)
                #ifdef LOD_FADE_CROSSFADE // enable dithering LOD transition if user select CrossFade transition in LOD group
                LODDitheringTransition(ComputeFadeMaskSeed(V, posInput.positionSS), unity_LODFade.x);
                #endif
                #endif
        
                #ifndef SHADER_UNLIT
                #ifdef _DOUBLESIDED_ON
                    float3 doubleSidedConstants = _DoubleSidedConstants.xyz;
                #else
                    float3 doubleSidedConstants = float3(1.0, 1.0, 1.0);
                #endif
        
                ApplyDoubleSidedFlipOrMirror(fragInputs, doubleSidedConstants); // Apply double sided flip on the vertex normal
                #endif // SHADER_UNLIT
        
                SurfaceDescriptionInputs surfaceDescriptionInputs = FragInputsToSurfaceDescriptionInputs(fragInputs, V);
        
                #if defined(HAVE_VFX_MODIFICATION)
                GraphProperties properties;
                ZERO_INITIALIZE(GraphProperties, properties);
        
                GetElementPixelProperties(fragInputs, properties);
        
                SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs, properties);
                #else
                SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);
                #endif
        
                // Perform alpha test very early to save performance (a killed pixel will not sample textures)
                // TODO: split graph evaluation to grab just alpha dependencies first? tricky..
                #ifdef _ALPHATEST_ON
                    float alphaCutoff = surfaceDescription.AlphaClipThreshold;
                    #if SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_PREPASS
                    // The TransparentDepthPrepass is also used with SSR transparent.
                    // If an artists enable transaprent SSR but not the TransparentDepthPrepass itself, then we use AlphaClipThreshold
                    // otherwise if TransparentDepthPrepass is enabled we use AlphaClipThresholdDepthPrepass
                    #elif SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_POSTPASS
                    // DepthPostpass always use its own alpha threshold
                    alphaCutoff = surfaceDescription.AlphaClipThresholdDepthPostpass;
                    #elif (SHADERPASS == SHADERPASS_SHADOWS) || (SHADERPASS == SHADERPASS_RAYTRACING_VISIBILITY)
                    // If use shadow threshold isn't enable we don't allow any test
                    #endif
        
                    GENERIC_ALPHA_TEST(surfaceDescription.Alpha, alphaCutoff);
                #endif
        
                #if !defined(SHADER_STAGE_RAY_TRACING) && _DEPTHOFFSET_ON
                ApplyDepthOffsetPositionInput(V, surfaceDescription.DepthOffset, GetViewForwardDir(), GetWorldToHClipMatrix(), posInput);
                #endif
        
                #ifndef SHADER_UNLIT
                float3 bentNormalWS;
                BuildSurfaceData(fragInputs, surfaceDescription, V, posInput, surfaceData, bentNormalWS);
        
                #ifdef FRAG_INPUTS_USE_TEXCOORD1
                    float4 lightmapTexCoord1 = fragInputs.texCoord1;
                #else
                    float4 lightmapTexCoord1 = float4(0,0,0,0);
                #endif
        
                #ifdef FRAG_INPUTS_USE_TEXCOORD2
                    float4 lightmapTexCoord2 = fragInputs.texCoord2;
                #else
                    float4 lightmapTexCoord2 = float4(0,0,0,0);
                #endif
        
                // Builtin Data
                // For back lighting we use the oposite vertex normal
                InitBuiltinData(posInput, surfaceDescription.Alpha, bentNormalWS, -fragInputs.tangentToWorld[2], lightmapTexCoord1, lightmapTexCoord2, builtinData);
        
                #else
                BuildSurfaceData(fragInputs, surfaceDescription, V, posInput, surfaceData);
        
                ZERO_BUILTIN_INITIALIZE(builtinData); // No call to InitBuiltinData as we don't have any lighting
                builtinData.opacity = surfaceDescription.Alpha;
        
                #if defined(DEBUG_DISPLAY)
                    // Light Layers are currently not used for the Unlit shader (because it is not lit)
                    // But Unlit objects do cast shadows according to their rendering layer mask, which is what we want to
                    // display in the light layers visualization mode, therefore we need the renderingLayers
                    builtinData.renderingLayers = GetMeshRenderingLightLayer();
                #endif
        
                #endif // SHADER_UNLIT
        
                #ifdef _ALPHATEST_ON
                    // Used for sharpening by alpha to mask - Alpha to covertage is only used with depth only and forward pass (no shadow pass, no transparent pass)
                    builtinData.alphaClipTreshold = alphaCutoff;
                #endif
        
                // override sampleBakedGI - not used by Unlit
        
        
                // Note this will not fully work on transparent surfaces (can check with _SURFACE_TYPE_TRANSPARENT define)
                // We will always overwrite vt feeback with the nearest. So behind transparent surfaces vt will not be resolved
                // This is a limitation of the current MRT approach.
                #ifdef UNITY_VIRTUAL_TEXTURING
                #endif
        
                #if _DEPTHOFFSET_ON
                builtinData.depthOffset = surfaceDescription.DepthOffset;
                #endif
        
                // TODO: We should generate distortion / distortionBlur for non distortion pass
                #if (SHADERPASS == SHADERPASS_DISTORTION)
                builtinData.distortion = surfaceDescription.Distortion;
                builtinData.distortionBlur = surfaceDescription.DistortionBlur;
                #endif
        
                #ifndef SHADER_UNLIT
                // PostInitBuiltinData call ApplyDebugToBuiltinData
                PostInitBuiltinData(V, posInput, surfaceData, builtinData);
                #else
                ApplyDebugToBuiltinData(builtinData);
                #endif
        
                RAY_TRACING_OPTIONAL_ALPHA_TEST_PASS
            }
        
            // --------------------------------------------------
            // Main
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"
        
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
        
        	#ifdef HAVE_VFX_MODIFICATION
        	#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/VisualEffectVertex.hlsl"
        	#endif
        
            ENDHLSL
        }
        Pass
        {
            Name "META"
            Tags
            {
                "LightMode" = "META"
            }
        
            // Render State
            Cull Off
        
            // Debug
            // <None>
        
            // --------------------------------------------------
            // Pass
        
            HLSLPROGRAM
        
            // Pragmas
            #pragma multi_compile _ DOTS_INSTANCING_ON
        #pragma instancing_options renderinglayer
        #pragma target 4.5
        #pragma vertex Vert
        #pragma fragment Frag
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
        #pragma multi_compile_instancing
        
            // Keywords
            #pragma shader_feature _ EDITOR_VISUALIZATION
        #pragma shader_feature_local _ _ALPHATEST_ON
        #pragma shader_feature _ _SURFACE_TYPE_TRANSPARENT
        #pragma shader_feature_local _BLENDMODE_OFF _BLENDMODE_ALPHA _BLENDMODE_ADD _BLENDMODE_PRE_MULTIPLY
        #pragma shader_feature_local _ _ADD_PRECOMPUTED_VELOCITY
        #pragma shader_feature_local _ _TRANSPARENT_WRITES_MOTION_VEC
        #pragma shader_feature_local_fragment _ _ENABLE_FOG_ON_TRANSPARENT
            // GraphKeywords: <None>
        
            // For custom interpolators to inject a substruct definition before FragInputs definition,
            // allowing for FragInputs to capture CI's intended for ShaderGraph's SDI.
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreInclude' */
        
        
            // TODO: Merge FragInputsVFX substruct with CustomInterpolators.
        	#ifdef HAVE_VFX_MODIFICATION
        	struct FragInputsVFX
            {
                /* WARNING: $splice Could not find named fragment 'FragInputsVFX' */
            };
            #endif
        
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GeometricTools.hlsl" // Required by Tessellation.hlsl
        	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Tessellation.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl" // Required to be include before we include properties as it define DECLARE_STACK_CB
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphHeader.hlsl" // Need to be here for Gradient struct definition
        
            // --------------------------------------------------
            // Defines
        
            // Attribute
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define ATTRIBUTES_NEED_TEXCOORD2
            #define ATTRIBUTES_NEED_TEXCOORD3
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_POSITIONPREDISPLACEMENT_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_TEXCOORD1
            #define VARYINGS_NEED_TEXCOORD2
            #define VARYINGS_NEED_TEXCOORD3
        
            #define HAVE_MESH_MODIFICATION
        
            //Strip down the FragInputs.hlsl (on graphics), so we can only optimize the interpolators we use.
            //if by accident something requests contents of FragInputs.hlsl, it will be caught as a compiler error
            //Frag inputs stripping is only enabled when FRAG_INPUTS_ENABLE_STRIPPING is set
            #if !defined(SHADER_STAGE_RAY_TRACING) && SHADERPASS != SHADERPASS_RAYTRACING_GBUFFER && SHADERPASS != SHADERPASS_FULL_SCREEN_DEBUG
            #define FRAG_INPUTS_ENABLE_STRIPPING
            #endif
            #define FRAG_INPUTS_USE_TEXCOORD0
            #define FRAG_INPUTS_USE_TEXCOORD1
            #define FRAG_INPUTS_USE_TEXCOORD2
            #define FRAG_INPUTS_USE_TEXCOORD3
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
        
        
        
            #define SHADERPASS SHADERPASS_LIGHT_TRANSPORT
        #define RAYTRACING_SHADER_GRAPH_DEFAULT
        
        
            // Following two define are a workaround introduce in 10.1.x for RaytracingQualityNode
            // The ShaderGraph don't support correctly migration of this node as it serialize all the node data
            // in the json file making it impossible to uprgrade. Until we get a fix, we do a workaround here
            // to still allow us to rename the field and keyword of this node without breaking existing code.
            #ifdef RAYTRACING_SHADER_GRAPH_DEFAULT
            #define RAYTRACING_SHADER_GRAPH_HIGH
            #endif
        
            #ifdef RAYTRACING_SHADER_GRAPH_RAYTRACED
            #define RAYTRACING_SHADER_GRAPH_LOW
            #endif
            // end
        
            #ifndef SHADER_UNLIT
            // We need isFrontFace when using double sided - it is not required for unlit as in case of unlit double sided only drive the cullmode
            // VARYINGS_NEED_CULLFACE can be define by VaryingsMeshToPS.FaceSign input if a IsFrontFace Node is included in the shader graph.
            #if defined(_DOUBLESIDED_ON) && !defined(VARYINGS_NEED_CULLFACE)
                #define VARYINGS_NEED_CULLFACE
            #endif
            #endif
        
            // Specific Material Define
        // Setup a define to say we are an unlit shader
        #define SHADER_UNLIT
        
        // Following Macro are only used by Unlit material
        #if defined(_ENABLE_SHADOW_MATTE) && (SHADERPASS == SHADERPASS_FORWARD_UNLIT || SHADERPASS == SHADERPASS_PATH_TRACING)
        #define LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
        #define HAS_LIGHTLOOP
        #endif
            // Caution: we can use the define SHADER_UNLIT onlit after the above Material include as it is the Unlit template who define it
        
            // To handle SSR on transparent correctly with a possibility to enable/disable it per framesettings
            // we should have a code like this:
            // if !defined(_DISABLE_SSR_TRANSPARENT)
            // pragma multi_compile _ WRITE_NORMAL_BUFFER
            // endif
            // i.e we enable the multicompile only if we can receive SSR or not, and then C# code drive
            // it based on if SSR transparent in frame settings and not (and stripper can strip it).
            // this is currently not possible with our current preprocessor as _DISABLE_SSR_TRANSPARENT is a keyword not a define
            // so instead we used this and chose to pay the extra cost of normal write even if SSR transaprent is disabled.
            // Ideally the shader graph generator should handle it but condition below can't be handle correctly for now.
            #if SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_PREPASS
            #if !defined(_DISABLE_SSR_TRANSPARENT) && !defined(SHADER_UNLIT)
                #define WRITE_NORMAL_BUFFER
            #endif
            #endif
        
            #ifndef DEBUG_DISPLAY
                // In case of opaque we don't want to perform the alpha test, it is done in depth prepass and we use depth equal for ztest (setup from UI)
                // Don't do it with debug display mode as it is possible there is no depth prepass in this case
                #if !defined(_SURFACE_TYPE_TRANSPARENT)
                    #if SHADERPASS == SHADERPASS_FORWARD
                    #define SHADERPASS_FORWARD_BYPASS_ALPHA_TEST
                    #elif SHADERPASS == SHADERPASS_GBUFFER
                    #define SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST
                    #endif
                #endif
            #endif
        
            // Define _DEFERRED_CAPABLE_MATERIAL for shader capable to run in deferred pass
            #if defined(SHADER_LIT) && !defined(_SURFACE_TYPE_TRANSPARENT)
                #define _DEFERRED_CAPABLE_MATERIAL
            #endif
        
            // Translate transparent motion vector define
            #if defined(_TRANSPARENT_WRITES_MOTION_VEC) && defined(_SURFACE_TYPE_TRANSPARENT)
                #define _WRITE_TRANSPARENT_MOTION_VECTOR
            #endif
        
            // -- Graph Properties
            CBUFFER_START(UnityPerMaterial)
        float4 _TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1_TexelSize;
        float4 _Direction_TexelSize;
        float2 _TextureSize;
        float _RotateUV;
        float Boolean_F2B1E91A;
        float4 _EmissionColor;
        float _UseShadowThreshold;
        float4 _DoubleSidedConstants;
        float _BlendMode;
        float _EnableBlendModePreserveSpecularLighting;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1);
        SAMPLER(sampler_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1);
        TEXTURE2D(_Direction);
        SAMPLER(sampler_Direction);
        
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
        
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
        
            // Includes
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl"
        	// GraphIncludes: <None>
        
            // --------------------------------------------------
            // Structs and Packing
        
            struct AttributesMesh
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 uv0 : TEXCOORD0;
             float4 uv1 : TEXCOORD1;
             float4 uv2 : TEXCOORD2;
             float4 uv3 : TEXCOORD3;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct VaryingsMeshToPS
        {
            SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
             float3 positionRWS;
             float3 positionPredisplacementRWS;
             float4 texCoord0;
             float4 texCoord1;
             float4 texCoord2;
             float4 texCoord3;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
        };
        struct VertexDescriptionInputs
        {
        };
        struct SurfaceDescriptionInputs
        {
             float4 uv0;
             float4 uv1;
             float4 uv2;
             float4 uv3;
        };
        struct PackedVaryingsMeshToPS
        {
            SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
             float3 interp0 : INTERP0;
             float3 interp1 : INTERP1;
             float4 interp2 : INTERP2;
             float4 interp3 : INTERP3;
             float4 interp4 : INTERP4;
             float4 interp5 : INTERP5;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
        };
        
            PackedVaryingsMeshToPS PackVaryingsMeshToPS (VaryingsMeshToPS input)
        {
            PackedVaryingsMeshToPS output;
            ZERO_INITIALIZE(PackedVaryingsMeshToPS, output);
            output.positionCS = input.positionCS;
            output.interp0.xyz =  input.positionRWS;
            output.interp1.xyz =  input.positionPredisplacementRWS;
            output.interp2.xyzw =  input.texCoord0;
            output.interp3.xyzw =  input.texCoord1;
            output.interp4.xyzw =  input.texCoord2;
            output.interp5.xyzw =  input.texCoord3;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            return output;
        }
        
        VaryingsMeshToPS UnpackVaryingsMeshToPS (PackedVaryingsMeshToPS input)
        {
            VaryingsMeshToPS output;
            output.positionCS = input.positionCS;
            output.positionRWS = input.interp0.xyz;
            output.positionPredisplacementRWS = input.interp1.xyz;
            output.texCoord0 = input.interp2.xyzw;
            output.texCoord1 = input.interp3.xyzw;
            output.texCoord2 = input.interp4.xyzw;
            output.texCoord3 = input.interp5.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            return output;
        }
        
        
            // --------------------------------------------------
            // Graph
        
        
            // Graph Functions
            
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A / B;
        }
        
        void Unity_Fraction_float2(float2 In, out float2 Out)
        {
            Out = frac(In);
        }
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A * B;
        }
        
        void Unity_Subtract_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A - B;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_Comparison_Greater_float(float A, float B, out float Out)
        {
            Out = A > B ? 1 : 0;
        }
        
        void Unity_Divide_float(float A, float B, out float Out)
        {
            Out = A / B;
        }
        
        void Unity_Arctangent_float(float In, out float Out)
        {
            Out = atan(In);
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Branch_float(float Predicate, float True, float False, out float Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Rotate_Radians_float(float2 UV, float2 Center, float Rotation, out float2 Out)
        {
            //rotation matrix
            UV -= Center;
            float s = sin(Rotation);
            float c = cos(Rotation);
        
            //center rotation matrix
            float2x2 rMatrix = float2x2(c, -s, s, c);
            rMatrix *= 0.5;
            rMatrix += 0.5;
            rMatrix = rMatrix*2 - 1;
        
            //multiply the UVs by the rotation matrix
            UV.xy = mul(UV.xy, rMatrix);
            UV += Center;
        
            Out = UV;
        }
        
        void Unity_Distance_float2(float2 A, float2 B, out float Out)
        {
            Out = distance(A, B);
        }
        
        void Unity_Clamp_float(float In, float Min, float Max, out float Out)
        {
            Out = clamp(In, Min, Max);
        }
        
        void Unity_Subtract_float(float A, float B, out float Out)
        {
            Out = A - B;
        }
        
        void Unity_Absolute_float(float In, out float Out)
        {
            Out = abs(In);
        }
        
        void Unity_Power_float(float A, float B, out float Out)
        {
            Out = pow(A, B);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A * B;
        }
        
            // Graph Vertex
            struct VertexDescription
        {
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            return description;
        }
        
            // Graph Pixel
            struct SurfaceDescription
        {
            float3 BaseColor;
            float3 Emission;
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0 = UnityBuildTexture2DStructNoScale(_Direction);
            float4 _UV_7c8a457b34527181924b2b4a71350de9_Out_0 = IN.uv0;
            float2 _TilingAndOffset_67d58b7a8092828aaece7f3acfe6e8a9_Out_3;
            Unity_TilingAndOffset_float((_UV_7c8a457b34527181924b2b4a71350de9_Out_0.xy), float2 (2, 2), float2 (0, 0), _TilingAndOffset_67d58b7a8092828aaece7f3acfe6e8a9_Out_3);
            float2 _Vector2_9a3e446bd0dfc3868e3db6f9bf101ead_Out_0 = float2(1, 1);
            float2 _Vector2_8355e4dbd796b188ba1e500dcabf0c0f_Out_0 = float2(30, 30);
            float2 _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2;
            Unity_Divide_float2(_Vector2_9a3e446bd0dfc3868e3db6f9bf101ead_Out_0, _Vector2_8355e4dbd796b188ba1e500dcabf0c0f_Out_0, _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2);
            float2 _Divide_5d0f49ab4bcab689894f976217114a79_Out_2;
            Unity_Divide_float2(_TilingAndOffset_67d58b7a8092828aaece7f3acfe6e8a9_Out_3, _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2, _Divide_5d0f49ab4bcab689894f976217114a79_Out_2);
            float2 _Fraction_e4f0f05fb76fd3809f5ec855b54a4595_Out_1;
            Unity_Fraction_float2(_Divide_5d0f49ab4bcab689894f976217114a79_Out_2, _Fraction_e4f0f05fb76fd3809f5ec855b54a4595_Out_1);
            float2 _Multiply_cbcb0de832b1d08288916e46aa8d4df2_Out_2;
            Unity_Multiply_float2_float2(_Fraction_e4f0f05fb76fd3809f5ec855b54a4595_Out_1, _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2, _Multiply_cbcb0de832b1d08288916e46aa8d4df2_Out_2);
            float2 _Vector2_e6d2595367ed0380b42c7a74855838a5_Out_0 = float2(246, 246);
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_Width_0 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.z;
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_Height_2 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.w;
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_TexelWidth_3 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.x;
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_TexelHeight_4 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.y;
            float2 _Vector2_e8728bfbf194e5818f010927b4ba8d79_Out_0 = float2(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Width_0, _TexelSize_18bb9f416e386f87bee91aa46804bc56_Height_2);
            float2 _Multiply_561a2890964f4081a8cf5d16bccb889f_Out_2;
            Unity_Multiply_float2_float2(_Vector2_e6d2595367ed0380b42c7a74855838a5_Out_0, _Vector2_e8728bfbf194e5818f010927b4ba8d79_Out_0, _Multiply_561a2890964f4081a8cf5d16bccb889f_Out_2);
            float2 _Vector2_97c81bcbfd26f08385a101ac3da412f3_Out_0 = float2(10, 10);
            float2 _Multiply_bccda05b3809138d8da15484dd6a7b4e_Out_2;
            Unity_Multiply_float2_float2(_Vector2_e8728bfbf194e5818f010927b4ba8d79_Out_0, _Vector2_97c81bcbfd26f08385a101ac3da412f3_Out_0, _Multiply_bccda05b3809138d8da15484dd6a7b4e_Out_2);
            float2 _Subtract_710d4672e66f7780ad90ca6e19a733bd_Out_2;
            Unity_Subtract_float2(_Multiply_561a2890964f4081a8cf5d16bccb889f_Out_2, _Multiply_bccda05b3809138d8da15484dd6a7b4e_Out_2, _Subtract_710d4672e66f7780ad90ca6e19a733bd_Out_2);
            float2 _Multiply_cd542324eceadd8daaca18ab78d3fbf6_Out_2;
            Unity_Multiply_float2_float2(_Multiply_cbcb0de832b1d08288916e46aa8d4df2_Out_2, _Subtract_710d4672e66f7780ad90ca6e19a733bd_Out_2, _Multiply_cd542324eceadd8daaca18ab78d3fbf6_Out_2);
            float2 _Vector2_df86361e9c36e7828e3097cc1fa8a75b_Out_0 = float2(0.1, 0.1);
            float2 _Multiply_8e351d40d36634829f2bb0905b9b9342_Out_2;
            Unity_Multiply_float2_float2(_Multiply_cd542324eceadd8daaca18ab78d3fbf6_Out_2, _Vector2_df86361e9c36e7828e3097cc1fa8a75b_Out_0, _Multiply_8e351d40d36634829f2bb0905b9b9342_Out_2);
            float4 _UV_1ca38e193be034888b8a707af9d47210_Out_0 = IN.uv3;
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_R_1 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[0];
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[1];
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_B_3 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[2];
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_A_4 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[3];
            float _Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2;
            Unity_Multiply_float_float(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2, -1, _Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2);
            float _Comparison_e67d9847aa86858490dd0c37e68e7505_Out_2;
            Unity_Comparison_Greater_float(_Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2, 0, _Comparison_e67d9847aa86858490dd0c37e68e7505_Out_2);
            float _Divide_2b884a92fd783d82a9012917c2b83115_Out_2;
            Unity_Divide_float(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_R_1, _Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2, _Divide_2b884a92fd783d82a9012917c2b83115_Out_2);
            float _Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1;
            Unity_Arctangent_float(_Divide_2b884a92fd783d82a9012917c2b83115_Out_2, _Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1);
            float _Add_cf6b7aee4a487f8ca16b9171b64a8f0d_Out_2;
            Unity_Add_float(_Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1, 3.141592, _Add_cf6b7aee4a487f8ca16b9171b64a8f0d_Out_2);
            float _Branch_9de100b31215ba89bb19e5b89012a25a_Out_3;
            Unity_Branch_float(_Comparison_e67d9847aa86858490dd0c37e68e7505_Out_2, _Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1, _Add_cf6b7aee4a487f8ca16b9171b64a8f0d_Out_2, _Branch_9de100b31215ba89bb19e5b89012a25a_Out_3);
            float2 _Rotate_ff1406ef744b188ca79c6d3bf0b8bbd7_Out_3;
            Unity_Rotate_Radians_float(_Multiply_8e351d40d36634829f2bb0905b9b9342_Out_2, float2 (0.5, 0.5), _Branch_9de100b31215ba89bb19e5b89012a25a_Out_3, _Rotate_ff1406ef744b188ca79c6d3bf0b8bbd7_Out_3);
            #if defined(SHADER_API_GLES) && (SHADER_TARGET < 30)
              float4 _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0 = float4(0.0f, 0.0f, 0.0f, 1.0f);
            #else
              float4 _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0 = SAMPLE_TEXTURE2D_LOD(_Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0.tex, _Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0.samplerstate, _Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0.GetTransformedUV(_Rotate_ff1406ef744b188ca79c6d3bf0b8bbd7_Out_3), 0);
            #endif
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_R_5 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.r;
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_G_6 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.g;
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_B_7 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.b;
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_A_8 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.a;
            float _Comparison_1a93f0ca7c913a8f81659b20b8d59f2b_Out_2;
            Unity_Comparison_Greater_float(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2, 0.001, _Comparison_1a93f0ca7c913a8f81659b20b8d59f2b_Out_2);
            float3 _Vector3_37ab63b41d52bb8d8eba9088ed71bc11_Out_0 = float3(0, 0, 0);
            float2 _Vector2_4aeecd1cb271dd8e96c2fb12309b9dd0_Out_0 = float2(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_R_1, _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2);
            float _Distance_47e812ff6269a181b6e0faa017faee3b_Out_2;
            Unity_Distance_float2(_Vector2_4aeecd1cb271dd8e96c2fb12309b9dd0_Out_0, float2(0, 0), _Distance_47e812ff6269a181b6e0faa017faee3b_Out_2);
            float _Multiply_e5102f48c5e759879293e06062894e0c_Out_2;
            Unity_Multiply_float_float(_Distance_47e812ff6269a181b6e0faa017faee3b_Out_2, 1.6, _Multiply_e5102f48c5e759879293e06062894e0c_Out_2);
            float _Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3;
            Unity_Clamp_float(_Multiply_e5102f48c5e759879293e06062894e0c_Out_2, 0, 1, _Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3);
            float _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2;
            Unity_Subtract_float(1, _Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3, _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2);
            float3 _Vector3_aeff4a868e81c68f85dfe4c9662da9ce_Out_0 = float3(1, _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2, 0);
            float _Absolute_5573b6b2c1616e819aa4eeb67caec457_Out_1;
            Unity_Absolute_float(_Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3, _Absolute_5573b6b2c1616e819aa4eeb67caec457_Out_1);
            float _Power_33dedbf009cacf848c403a58667a66c6_Out_2;
            Unity_Power_float(_Absolute_5573b6b2c1616e819aa4eeb67caec457_Out_1, 0.16, _Power_33dedbf009cacf848c403a58667a66c6_Out_2);
            float _Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3;
            Unity_Clamp_float(_Power_33dedbf009cacf848c403a58667a66c6_Out_2, 0, 1, _Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3);
            float3 _Lerp_e6000a54e83a8f8e8c5bc16419b360ca_Out_3;
            Unity_Lerp_float3(_Vector3_37ab63b41d52bb8d8eba9088ed71bc11_Out_0, _Vector3_aeff4a868e81c68f85dfe4c9662da9ce_Out_0, (_Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3.xxx), _Lerp_e6000a54e83a8f8e8c5bc16419b360ca_Out_3);
            float3 _Vector3_484f28086891e98d9589d1f65774f6f4_Out_0 = float3(0, _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2, 1);
            float3 _Lerp_93469a90f98ccf80a7121c0b2bf05f48_Out_3;
            Unity_Lerp_float3(_Vector3_37ab63b41d52bb8d8eba9088ed71bc11_Out_0, _Vector3_484f28086891e98d9589d1f65774f6f4_Out_0, (_Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3.xxx), _Lerp_93469a90f98ccf80a7121c0b2bf05f48_Out_3);
            float3 _Branch_365bf259ae64c386a18e2d6997727100_Out_3;
            Unity_Branch_float3(_Comparison_1a93f0ca7c913a8f81659b20b8d59f2b_Out_2, _Lerp_e6000a54e83a8f8e8c5bc16419b360ca_Out_3, _Lerp_93469a90f98ccf80a7121c0b2bf05f48_Out_3, _Branch_365bf259ae64c386a18e2d6997727100_Out_3);
            float3 _Multiply_0563147e8bc9868b8c2a4f2c442c349f_Out_2;
            Unity_Multiply_float3_float3((_SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_A_8.xxx), _Branch_365bf259ae64c386a18e2d6997727100_Out_3, _Multiply_0563147e8bc9868b8c2a4f2c442c349f_Out_2);
            surface.BaseColor = _Multiply_0563147e8bc9868b8c2a4f2c442c349f_Out_2;
            surface.Emission = float3(0, 0, 0);
            surface.Alpha = 1;
            surface.AlphaClipThreshold = 0.5;
            return surface;
        }
        
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES AttributesMesh
            #define VaryingsMeshType VaryingsMeshToPS
            #define VFX_SRP_VARYINGS VaryingsMeshType
            #define VFX_SRP_SURFACE_INPUTS FragInputs
            #endif
            
        VertexDescriptionInputs AttributesMeshToVertexDescriptionInputs(AttributesMesh input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
        
            return output;
        }
        
        VertexDescription GetVertexDescription(AttributesMesh input, float3 timeParameters
        #ifdef HAVE_VFX_MODIFICATION
            , AttributesElement element
        #endif
        )
        {
            // build graph inputs
            VertexDescriptionInputs vertexDescriptionInputs = AttributesMeshToVertexDescriptionInputs(input);
            // Override time parameters with used one (This is required to correctly handle motion vector for vertex animation based on time)
        
            // evaluate vertex graph
        #ifdef HAVE_VFX_MODIFICATION
            GraphProperties properties;
            ZERO_INITIALIZE(GraphProperties, properties);
        
            // Fetch the vertex graph properties for the particle instance.
            GetElementVertexProperties(element, properties);
        
            VertexDescription vertexDescription = VertexDescriptionFunction(vertexDescriptionInputs, properties);
        #else
            VertexDescription vertexDescription = VertexDescriptionFunction(vertexDescriptionInputs);
        #endif
            return vertexDescription;
        
        }
        
        AttributesMesh ApplyMeshModification(AttributesMesh input, float3 timeParameters
        #ifdef USE_CUSTOMINTERP_SUBSTRUCT
            #ifdef TESSELLATION_ON
            , inout VaryingsMeshToDS varyings
            #else
            , inout VaryingsMeshToPS varyings
            #endif
        #endif
        #ifdef HAVE_VFX_MODIFICATION
                , AttributesElement element
        #endif
            )
        {
            VertexDescription vertexDescription = GetVertexDescription(input, timeParameters
        #ifdef HAVE_VFX_MODIFICATION
                , element
        #endif
            );
        
            // copy graph output to the results
        
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorVertMeshCustomInterpolation' */
        
            return input;
        }
        
        #if defined(_ADD_CUSTOM_VELOCITY) // For shader graph custom velocity
        // Return precomputed Velocity in object space
        float3 GetCustomVelocity(AttributesMesh input
        #ifdef HAVE_VFX_MODIFICATION
            , AttributesElement element
        #endif
        )
        {
            VertexDescription vertexDescription = GetVertexDescription(input, _TimeParameters.xyz
        #ifdef HAVE_VFX_MODIFICATION
                , element
        #endif
            );
            return vertexDescription.CustomVelocity;
        }
        #endif
        
        FragInputs BuildFragInputs(VaryingsMeshToPS input)
        {
            FragInputs output;
            ZERO_INITIALIZE(FragInputs, output);
        
            // Init to some default value to make the computer quiet (else it output 'divide by zero' warning even if value is not used).
            // TODO: this is a really poor workaround, but the variable is used in a bunch of places
            // to compute normals which are then passed on elsewhere to compute other values...
            output.tangentToWorld = k_identity3x3;
            output.positionSS = input.positionCS;       // input.positionCS is SV_Position
        
            output.positionRWS =                input.positionRWS;
            output.positionPredisplacementRWS = input.positionPredisplacementRWS;
            output.texCoord0 =                  input.texCoord0;
            output.texCoord1 =                  input.texCoord1;
            output.texCoord2 =                  input.texCoord2;
            output.texCoord3 =                  input.texCoord3;
        
        #ifdef HAVE_VFX_MODIFICATION
            // FragInputs from VFX come from two places: Interpolator or CBuffer.
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            // splice point to copy custom interpolator fields from varyings to frag inputs
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorVaryingsToFragInputs' */
        
            return output;
        }
        
        // existing HDRP code uses the combined function to go directly from packed to frag inputs
        FragInputs UnpackVaryingsMeshToFragInputs(PackedVaryingsMeshToPS input)
        {
            UNITY_SETUP_INSTANCE_ID(input);
        #if defined(HAVE_VFX_MODIFICATION) && defined(UNITY_INSTANCING_ENABLED)
            unity_InstanceID = input.instanceID;
        #endif
            VaryingsMeshToPS unpacked = UnpackVaryingsMeshToPS(input);
            return BuildFragInputs(unpacked);
        }
            SurfaceDescriptionInputs FragInputsToSurfaceDescriptionInputs(FragInputs input, float3 viewWS)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
            #if defined(SHADER_STAGE_RAY_TRACING)
            #else
            #endif
        
        #if UNITY_UV_STARTS_AT_TOP
        #else
        #endif
        
        
            output.uv0 =                                        input.texCoord0;
            output.uv1 =                                        input.texCoord1;
            output.uv2 =                                        input.texCoord2;
            output.uv3 =                                        input.texCoord3;
        
            // splice point to copy frag inputs custom interpolator pack into the SDI
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorCopyToSDI' */
        
            return output;
        }
        
            // --------------------------------------------------
            // Build Surface Data (Specific Material)
        
        void BuildSurfaceData(FragInputs fragInputs, inout SurfaceDescription surfaceDescription, float3 V, PositionInputs posInput, out SurfaceData surfaceData)
        {
            // setup defaults -- these are used if the graph doesn't output a value
            ZERO_INITIALIZE(SurfaceData, surfaceData);
        
            // copy across graph values, if defined
            surfaceData.color = surfaceDescription.BaseColor;
        
            #ifdef WRITE_NORMAL_BUFFER
            // When we need to export the normal (in the depth prepass, we write the geometry one)
            surfaceData.normalWS = fragInputs.tangentToWorld[2];
            #endif
        
            #if defined(DEBUG_DISPLAY)
            if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
            {
                // TODO
            }
            #endif
        
            #ifdef _ENABLE_SHADOW_MATTE
        
                #if (SHADERPASS == SHADERPASS_FORWARD_UNLIT) || (SHADERPASS == SHADERPASS_RAYTRACING_GBUFFER) || (SHADERPASS == SHADERPASS_RAYTRACING_INDIRECT) || (SHADERPASS == SHADERPASS_RAYTRACING_FORWARD)
        
                    HDShadowContext shadowContext = InitShadowContext();
        
                    // Evaluate the shadow, the normal is guaranteed if shadow matte is enabled on this shader.
                    float3 shadow3;
                    ShadowLoopMin(shadowContext, posInput, normalize(fragInputs.tangentToWorld[2]), asuint(_ShadowMatteFilter), GetMeshRenderingLightLayer(), shadow3);
        
                    // Compute the average value in the fourth channel
                    float4 shadow = float4(shadow3, dot(shadow3, float3(1.0/3.0, 1.0/3.0, 1.0/3.0)));
        
                    float4 shadowColor = (1.0 - shadow) * surfaceDescription.ShadowTint.rgba;
                    float  localAlpha  = saturate(shadowColor.a + surfaceDescription.Alpha);
        
                    // Keep the nested lerp
                    // With no Color (bsdfData.color.rgb, bsdfData.color.a == 0.0f), just use ShadowColor*Color to avoid a ring of "white" around the shadow
                    // And mix color to consider the Color & ShadowColor alpha (from texture or/and color picker)
                    #ifdef _SURFACE_TYPE_TRANSPARENT
                        surfaceData.color = lerp(shadowColor.rgb * surfaceData.color, lerp(lerp(shadowColor.rgb, surfaceData.color, 1.0 - surfaceDescription.ShadowTint.a), surfaceData.color, shadow.rgb), surfaceDescription.Alpha);
                    #else
                        surfaceData.color = lerp(lerp(shadowColor.rgb, surfaceData.color, 1.0 - surfaceDescription.ShadowTint.a), surfaceData.color, shadow.rgb);
                    #endif
                    localAlpha = ApplyBlendMode(surfaceData.color, localAlpha).a;
        
                    surfaceDescription.Alpha = localAlpha;
        
                #elif SHADERPASS == SHADERPASS_PATH_TRACING
        
                    surfaceData.normalWS = fragInputs.tangentToWorld[2];
                    surfaceData.shadowTint = surfaceDescription.ShadowTint.rgba;
        
                #endif
        
            #endif // _ENABLE_SHADOW_MATTE
        }
        
            // --------------------------------------------------
            // Get Surface And BuiltinData
        
            void GetSurfaceAndBuiltinData(FragInputs fragInputs, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData RAY_TRACING_OPTIONAL_PARAMETERS)
            {
                // Don't dither if displaced tessellation (we're fading out the displacement instead to match the next LOD)
                #if !defined(SHADER_STAGE_RAY_TRACING) && !defined(_TESSELLATION_DISPLACEMENT)
                #ifdef LOD_FADE_CROSSFADE // enable dithering LOD transition if user select CrossFade transition in LOD group
                LODDitheringTransition(ComputeFadeMaskSeed(V, posInput.positionSS), unity_LODFade.x);
                #endif
                #endif
        
                #ifndef SHADER_UNLIT
                #ifdef _DOUBLESIDED_ON
                    float3 doubleSidedConstants = _DoubleSidedConstants.xyz;
                #else
                    float3 doubleSidedConstants = float3(1.0, 1.0, 1.0);
                #endif
        
                ApplyDoubleSidedFlipOrMirror(fragInputs, doubleSidedConstants); // Apply double sided flip on the vertex normal
                #endif // SHADER_UNLIT
        
                SurfaceDescriptionInputs surfaceDescriptionInputs = FragInputsToSurfaceDescriptionInputs(fragInputs, V);
        
                #if defined(HAVE_VFX_MODIFICATION)
                GraphProperties properties;
                ZERO_INITIALIZE(GraphProperties, properties);
        
                GetElementPixelProperties(fragInputs, properties);
        
                SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs, properties);
                #else
                SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);
                #endif
        
                // Perform alpha test very early to save performance (a killed pixel will not sample textures)
                // TODO: split graph evaluation to grab just alpha dependencies first? tricky..
                #ifdef _ALPHATEST_ON
                    float alphaCutoff = surfaceDescription.AlphaClipThreshold;
                    #if SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_PREPASS
                    // The TransparentDepthPrepass is also used with SSR transparent.
                    // If an artists enable transaprent SSR but not the TransparentDepthPrepass itself, then we use AlphaClipThreshold
                    // otherwise if TransparentDepthPrepass is enabled we use AlphaClipThresholdDepthPrepass
                    #elif SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_POSTPASS
                    // DepthPostpass always use its own alpha threshold
                    alphaCutoff = surfaceDescription.AlphaClipThresholdDepthPostpass;
                    #elif (SHADERPASS == SHADERPASS_SHADOWS) || (SHADERPASS == SHADERPASS_RAYTRACING_VISIBILITY)
                    // If use shadow threshold isn't enable we don't allow any test
                    #endif
        
                    GENERIC_ALPHA_TEST(surfaceDescription.Alpha, alphaCutoff);
                #endif
        
                #if !defined(SHADER_STAGE_RAY_TRACING) && _DEPTHOFFSET_ON
                ApplyDepthOffsetPositionInput(V, surfaceDescription.DepthOffset, GetViewForwardDir(), GetWorldToHClipMatrix(), posInput);
                #endif
        
                #ifndef SHADER_UNLIT
                float3 bentNormalWS;
                BuildSurfaceData(fragInputs, surfaceDescription, V, posInput, surfaceData, bentNormalWS);
        
                #ifdef FRAG_INPUTS_USE_TEXCOORD1
                    float4 lightmapTexCoord1 = fragInputs.texCoord1;
                #else
                    float4 lightmapTexCoord1 = float4(0,0,0,0);
                #endif
        
                #ifdef FRAG_INPUTS_USE_TEXCOORD2
                    float4 lightmapTexCoord2 = fragInputs.texCoord2;
                #else
                    float4 lightmapTexCoord2 = float4(0,0,0,0);
                #endif
        
                // Builtin Data
                // For back lighting we use the oposite vertex normal
                InitBuiltinData(posInput, surfaceDescription.Alpha, bentNormalWS, -fragInputs.tangentToWorld[2], lightmapTexCoord1, lightmapTexCoord2, builtinData);
        
                #else
                BuildSurfaceData(fragInputs, surfaceDescription, V, posInput, surfaceData);
        
                ZERO_BUILTIN_INITIALIZE(builtinData); // No call to InitBuiltinData as we don't have any lighting
                builtinData.opacity = surfaceDescription.Alpha;
        
                #if defined(DEBUG_DISPLAY)
                    // Light Layers are currently not used for the Unlit shader (because it is not lit)
                    // But Unlit objects do cast shadows according to their rendering layer mask, which is what we want to
                    // display in the light layers visualization mode, therefore we need the renderingLayers
                    builtinData.renderingLayers = GetMeshRenderingLightLayer();
                #endif
        
                #endif // SHADER_UNLIT
        
                #ifdef _ALPHATEST_ON
                    // Used for sharpening by alpha to mask - Alpha to covertage is only used with depth only and forward pass (no shadow pass, no transparent pass)
                    builtinData.alphaClipTreshold = alphaCutoff;
                #endif
        
                // override sampleBakedGI - not used by Unlit
        
                builtinData.emissiveColor = surfaceDescription.Emission;
        
                // Note this will not fully work on transparent surfaces (can check with _SURFACE_TYPE_TRANSPARENT define)
                // We will always overwrite vt feeback with the nearest. So behind transparent surfaces vt will not be resolved
                // This is a limitation of the current MRT approach.
                #ifdef UNITY_VIRTUAL_TEXTURING
                #endif
        
                #if _DEPTHOFFSET_ON
                builtinData.depthOffset = surfaceDescription.DepthOffset;
                #endif
        
                // TODO: We should generate distortion / distortionBlur for non distortion pass
                #if (SHADERPASS == SHADERPASS_DISTORTION)
                builtinData.distortion = surfaceDescription.Distortion;
                builtinData.distortionBlur = surfaceDescription.DistortionBlur;
                #endif
        
                #ifndef SHADER_UNLIT
                // PostInitBuiltinData call ApplyDebugToBuiltinData
                PostInitBuiltinData(V, posInput, surfaceData, builtinData);
                #else
                ApplyDebugToBuiltinData(builtinData);
                #endif
        
                RAY_TRACING_OPTIONAL_ALPHA_TEST_PASS
            }
        
            // --------------------------------------------------
            // Main
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassLightTransport.hlsl"
        
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
        
        	#ifdef HAVE_VFX_MODIFICATION
        	#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/VisualEffectVertex.hlsl"
        	#endif
        
            ENDHLSL
        }
        Pass
        {
            Name "ScenePickingPass"
            Tags
            {
                "LightMode" = "Picking"
            }
        
            // Render State
            Cull [_CullMode]
        
            // Debug
            // <None>
        
            // --------------------------------------------------
            // Pass
        
            HLSLPROGRAM
        
            // Pragmas
            #pragma multi_compile _ DOTS_INSTANCING_ON
        #pragma editor_sync_compilation
        #pragma instancing_options renderinglayer
        #pragma target 4.5
        #pragma vertex Vert
        #pragma fragment Frag
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
        #pragma multi_compile_instancing
        
            // Keywords
            #pragma shader_feature_local _ _ALPHATEST_ON
        #pragma shader_feature _ _SURFACE_TYPE_TRANSPARENT
        #pragma shader_feature_local _BLENDMODE_OFF _BLENDMODE_ALPHA _BLENDMODE_ADD _BLENDMODE_PRE_MULTIPLY
        #pragma shader_feature_local _ _ADD_PRECOMPUTED_VELOCITY
        #pragma shader_feature_local _ _TRANSPARENT_WRITES_MOTION_VEC
        #pragma shader_feature_local_fragment _ _ENABLE_FOG_ON_TRANSPARENT
            // GraphKeywords: <None>
        
            // For custom interpolators to inject a substruct definition before FragInputs definition,
            // allowing for FragInputs to capture CI's intended for ShaderGraph's SDI.
            struct CustomInterpolators
        {
        };
        #define USE_CUSTOMINTERP_SUBSTRUCT
        
        
        
            // TODO: Merge FragInputsVFX substruct with CustomInterpolators.
        	#ifdef HAVE_VFX_MODIFICATION
        	struct FragInputsVFX
            {
                /* WARNING: $splice Could not find named fragment 'FragInputsVFX' */
            };
            #endif
        
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GeometricTools.hlsl" // Required by Tessellation.hlsl
        	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Tessellation.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl" // Required to be include before we include properties as it define DECLARE_STACK_CB
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphHeader.hlsl" // Need to be here for Gradient struct definition
        
            // --------------------------------------------------
            // Defines
        
            // Attribute
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define ATTRIBUTES_NEED_TEXCOORD2
            #define ATTRIBUTES_NEED_TEXCOORD3
            #define VARYINGS_NEED_TANGENT_TO_WORLD
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_TEXCOORD1
            #define VARYINGS_NEED_TEXCOORD2
            #define VARYINGS_NEED_TEXCOORD3
        
            #define HAVE_MESH_MODIFICATION
        
            //Strip down the FragInputs.hlsl (on graphics), so we can only optimize the interpolators we use.
            //if by accident something requests contents of FragInputs.hlsl, it will be caught as a compiler error
            //Frag inputs stripping is only enabled when FRAG_INPUTS_ENABLE_STRIPPING is set
            #if !defined(SHADER_STAGE_RAY_TRACING) && SHADERPASS != SHADERPASS_RAYTRACING_GBUFFER && SHADERPASS != SHADERPASS_FULL_SCREEN_DEBUG
            #define FRAG_INPUTS_ENABLE_STRIPPING
            #endif
            #define FRAG_INPUTS_USE_TEXCOORD0
            #define FRAG_INPUTS_USE_TEXCOORD1
            #define FRAG_INPUTS_USE_TEXCOORD2
            #define FRAG_INPUTS_USE_TEXCOORD3
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
        
        
        
            #define SHADERPASS SHADERPASS_DEPTH_ONLY
        #define SCENEPICKINGPASS 1
        
        
            // Following two define are a workaround introduce in 10.1.x for RaytracingQualityNode
            // The ShaderGraph don't support correctly migration of this node as it serialize all the node data
            // in the json file making it impossible to uprgrade. Until we get a fix, we do a workaround here
            // to still allow us to rename the field and keyword of this node without breaking existing code.
            #ifdef RAYTRACING_SHADER_GRAPH_DEFAULT
            #define RAYTRACING_SHADER_GRAPH_HIGH
            #endif
        
            #ifdef RAYTRACING_SHADER_GRAPH_RAYTRACED
            #define RAYTRACING_SHADER_GRAPH_LOW
            #endif
            // end
        
            #ifndef SHADER_UNLIT
            // We need isFrontFace when using double sided - it is not required for unlit as in case of unlit double sided only drive the cullmode
            // VARYINGS_NEED_CULLFACE can be define by VaryingsMeshToPS.FaceSign input if a IsFrontFace Node is included in the shader graph.
            #if defined(_DOUBLESIDED_ON) && !defined(VARYINGS_NEED_CULLFACE)
                #define VARYINGS_NEED_CULLFACE
            #endif
            #endif
        
            // Specific Material Define
        // Setup a define to say we are an unlit shader
        #define SHADER_UNLIT
        
        // Following Macro are only used by Unlit material
        #if defined(_ENABLE_SHADOW_MATTE) && (SHADERPASS == SHADERPASS_FORWARD_UNLIT || SHADERPASS == SHADERPASS_PATH_TRACING)
        #define LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
        #define HAS_LIGHTLOOP
        #endif
            // Caution: we can use the define SHADER_UNLIT onlit after the above Material include as it is the Unlit template who define it
        
            // To handle SSR on transparent correctly with a possibility to enable/disable it per framesettings
            // we should have a code like this:
            // if !defined(_DISABLE_SSR_TRANSPARENT)
            // pragma multi_compile _ WRITE_NORMAL_BUFFER
            // endif
            // i.e we enable the multicompile only if we can receive SSR or not, and then C# code drive
            // it based on if SSR transparent in frame settings and not (and stripper can strip it).
            // this is currently not possible with our current preprocessor as _DISABLE_SSR_TRANSPARENT is a keyword not a define
            // so instead we used this and chose to pay the extra cost of normal write even if SSR transaprent is disabled.
            // Ideally the shader graph generator should handle it but condition below can't be handle correctly for now.
            #if SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_PREPASS
            #if !defined(_DISABLE_SSR_TRANSPARENT) && !defined(SHADER_UNLIT)
                #define WRITE_NORMAL_BUFFER
            #endif
            #endif
        
            #ifndef DEBUG_DISPLAY
                // In case of opaque we don't want to perform the alpha test, it is done in depth prepass and we use depth equal for ztest (setup from UI)
                // Don't do it with debug display mode as it is possible there is no depth prepass in this case
                #if !defined(_SURFACE_TYPE_TRANSPARENT)
                    #if SHADERPASS == SHADERPASS_FORWARD
                    #define SHADERPASS_FORWARD_BYPASS_ALPHA_TEST
                    #elif SHADERPASS == SHADERPASS_GBUFFER
                    #define SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST
                    #endif
                #endif
            #endif
        
            // Define _DEFERRED_CAPABLE_MATERIAL for shader capable to run in deferred pass
            #if defined(SHADER_LIT) && !defined(_SURFACE_TYPE_TRANSPARENT)
                #define _DEFERRED_CAPABLE_MATERIAL
            #endif
        
            // Translate transparent motion vector define
            #if defined(_TRANSPARENT_WRITES_MOTION_VEC) && defined(_SURFACE_TYPE_TRANSPARENT)
                #define _WRITE_TRANSPARENT_MOTION_VECTOR
            #endif
        
            // -- Graph Properties
            CBUFFER_START(UnityPerMaterial)
        float4 _TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1_TexelSize;
        float4 _Direction_TexelSize;
        float2 _TextureSize;
        float _RotateUV;
        float Boolean_F2B1E91A;
        float4 _EmissionColor;
        float _UseShadowThreshold;
        float4 _DoubleSidedConstants;
        float _BlendMode;
        float _EnableBlendModePreserveSpecularLighting;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1);
        SAMPLER(sampler_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1);
        TEXTURE2D(_Direction);
        SAMPLER(sampler_Direction);
        
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
        
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
        
            // Includes
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/PickingSpaceTransforms.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl"
        	// GraphIncludes: <None>
        
            // --------------------------------------------------
            // Structs and Packing
        
            struct AttributesMesh
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
             float4 uv1 : TEXCOORD1;
             float4 uv2 : TEXCOORD2;
             float4 uv3 : TEXCOORD3;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct VaryingsMeshToPS
        {
            SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
             float3 normalWS;
             float4 tangentWS;
             float4 texCoord0;
             float4 texCoord1;
             float4 texCoord2;
             float4 texCoord3;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
        };
        struct SurfaceDescriptionInputs
        {
             float4 uv0;
             float4 uv1;
             float4 uv2;
             float4 uv3;
        };
        struct PackedVaryingsMeshToPS
        {
            SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
             float3 interp0 : INTERP0;
             float4 interp1 : INTERP1;
             float4 interp2 : INTERP2;
             float4 interp3 : INTERP3;
             float4 interp4 : INTERP4;
             float4 interp5 : INTERP5;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
        };
        
            PackedVaryingsMeshToPS PackVaryingsMeshToPS (VaryingsMeshToPS input)
        {
            PackedVaryingsMeshToPS output;
            ZERO_INITIALIZE(PackedVaryingsMeshToPS, output);
            output.positionCS = input.positionCS;
            output.interp0.xyz =  input.normalWS;
            output.interp1.xyzw =  input.tangentWS;
            output.interp2.xyzw =  input.texCoord0;
            output.interp3.xyzw =  input.texCoord1;
            output.interp4.xyzw =  input.texCoord2;
            output.interp5.xyzw =  input.texCoord3;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            return output;
        }
        
        VaryingsMeshToPS UnpackVaryingsMeshToPS (PackedVaryingsMeshToPS input)
        {
            VaryingsMeshToPS output;
            output.positionCS = input.positionCS;
            output.normalWS = input.interp0.xyz;
            output.tangentWS = input.interp1.xyzw;
            output.texCoord0 = input.interp2.xyzw;
            output.texCoord1 = input.interp3.xyzw;
            output.texCoord2 = input.interp4.xyzw;
            output.texCoord3 = input.interp5.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            return output;
        }
        
        
            // --------------------------------------------------
            // Graph
        
        
            // Graph Functions
            
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A / B;
        }
        
        void Unity_Fraction_float2(float2 In, out float2 Out)
        {
            Out = frac(In);
        }
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A * B;
        }
        
        void Unity_Subtract_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A - B;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_Comparison_Greater_float(float A, float B, out float Out)
        {
            Out = A > B ? 1 : 0;
        }
        
        void Unity_Divide_float(float A, float B, out float Out)
        {
            Out = A / B;
        }
        
        void Unity_Arctangent_float(float In, out float Out)
        {
            Out = atan(In);
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Branch_float(float Predicate, float True, float False, out float Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Rotate_Radians_float(float2 UV, float2 Center, float Rotation, out float2 Out)
        {
            //rotation matrix
            UV -= Center;
            float s = sin(Rotation);
            float c = cos(Rotation);
        
            //center rotation matrix
            float2x2 rMatrix = float2x2(c, -s, s, c);
            rMatrix *= 0.5;
            rMatrix += 0.5;
            rMatrix = rMatrix*2 - 1;
        
            //multiply the UVs by the rotation matrix
            UV.xy = mul(UV.xy, rMatrix);
            UV += Center;
        
            Out = UV;
        }
        
        void Unity_Distance_float2(float2 A, float2 B, out float Out)
        {
            Out = distance(A, B);
        }
        
        void Unity_Clamp_float(float In, float Min, float Max, out float Out)
        {
            Out = clamp(In, Min, Max);
        }
        
        void Unity_Subtract_float(float A, float B, out float Out)
        {
            Out = A - B;
        }
        
        void Unity_Absolute_float(float In, out float Out)
        {
            Out = abs(In);
        }
        
        void Unity_Power_float(float A, float B, out float Out)
        {
            Out = pow(A, B);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A * B;
        }
        
            // Graph Vertex
            struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
            // Graph Pixel
            struct SurfaceDescription
        {
            float3 BaseColor;
            float3 Emission;
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0 = UnityBuildTexture2DStructNoScale(_Direction);
            float4 _UV_7c8a457b34527181924b2b4a71350de9_Out_0 = IN.uv0;
            float2 _TilingAndOffset_67d58b7a8092828aaece7f3acfe6e8a9_Out_3;
            Unity_TilingAndOffset_float((_UV_7c8a457b34527181924b2b4a71350de9_Out_0.xy), float2 (2, 2), float2 (0, 0), _TilingAndOffset_67d58b7a8092828aaece7f3acfe6e8a9_Out_3);
            float2 _Vector2_9a3e446bd0dfc3868e3db6f9bf101ead_Out_0 = float2(1, 1);
            float2 _Vector2_8355e4dbd796b188ba1e500dcabf0c0f_Out_0 = float2(30, 30);
            float2 _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2;
            Unity_Divide_float2(_Vector2_9a3e446bd0dfc3868e3db6f9bf101ead_Out_0, _Vector2_8355e4dbd796b188ba1e500dcabf0c0f_Out_0, _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2);
            float2 _Divide_5d0f49ab4bcab689894f976217114a79_Out_2;
            Unity_Divide_float2(_TilingAndOffset_67d58b7a8092828aaece7f3acfe6e8a9_Out_3, _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2, _Divide_5d0f49ab4bcab689894f976217114a79_Out_2);
            float2 _Fraction_e4f0f05fb76fd3809f5ec855b54a4595_Out_1;
            Unity_Fraction_float2(_Divide_5d0f49ab4bcab689894f976217114a79_Out_2, _Fraction_e4f0f05fb76fd3809f5ec855b54a4595_Out_1);
            float2 _Multiply_cbcb0de832b1d08288916e46aa8d4df2_Out_2;
            Unity_Multiply_float2_float2(_Fraction_e4f0f05fb76fd3809f5ec855b54a4595_Out_1, _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2, _Multiply_cbcb0de832b1d08288916e46aa8d4df2_Out_2);
            float2 _Vector2_e6d2595367ed0380b42c7a74855838a5_Out_0 = float2(246, 246);
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_Width_0 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.z;
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_Height_2 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.w;
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_TexelWidth_3 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.x;
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_TexelHeight_4 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.y;
            float2 _Vector2_e8728bfbf194e5818f010927b4ba8d79_Out_0 = float2(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Width_0, _TexelSize_18bb9f416e386f87bee91aa46804bc56_Height_2);
            float2 _Multiply_561a2890964f4081a8cf5d16bccb889f_Out_2;
            Unity_Multiply_float2_float2(_Vector2_e6d2595367ed0380b42c7a74855838a5_Out_0, _Vector2_e8728bfbf194e5818f010927b4ba8d79_Out_0, _Multiply_561a2890964f4081a8cf5d16bccb889f_Out_2);
            float2 _Vector2_97c81bcbfd26f08385a101ac3da412f3_Out_0 = float2(10, 10);
            float2 _Multiply_bccda05b3809138d8da15484dd6a7b4e_Out_2;
            Unity_Multiply_float2_float2(_Vector2_e8728bfbf194e5818f010927b4ba8d79_Out_0, _Vector2_97c81bcbfd26f08385a101ac3da412f3_Out_0, _Multiply_bccda05b3809138d8da15484dd6a7b4e_Out_2);
            float2 _Subtract_710d4672e66f7780ad90ca6e19a733bd_Out_2;
            Unity_Subtract_float2(_Multiply_561a2890964f4081a8cf5d16bccb889f_Out_2, _Multiply_bccda05b3809138d8da15484dd6a7b4e_Out_2, _Subtract_710d4672e66f7780ad90ca6e19a733bd_Out_2);
            float2 _Multiply_cd542324eceadd8daaca18ab78d3fbf6_Out_2;
            Unity_Multiply_float2_float2(_Multiply_cbcb0de832b1d08288916e46aa8d4df2_Out_2, _Subtract_710d4672e66f7780ad90ca6e19a733bd_Out_2, _Multiply_cd542324eceadd8daaca18ab78d3fbf6_Out_2);
            float2 _Vector2_df86361e9c36e7828e3097cc1fa8a75b_Out_0 = float2(0.1, 0.1);
            float2 _Multiply_8e351d40d36634829f2bb0905b9b9342_Out_2;
            Unity_Multiply_float2_float2(_Multiply_cd542324eceadd8daaca18ab78d3fbf6_Out_2, _Vector2_df86361e9c36e7828e3097cc1fa8a75b_Out_0, _Multiply_8e351d40d36634829f2bb0905b9b9342_Out_2);
            float4 _UV_1ca38e193be034888b8a707af9d47210_Out_0 = IN.uv3;
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_R_1 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[0];
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[1];
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_B_3 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[2];
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_A_4 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[3];
            float _Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2;
            Unity_Multiply_float_float(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2, -1, _Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2);
            float _Comparison_e67d9847aa86858490dd0c37e68e7505_Out_2;
            Unity_Comparison_Greater_float(_Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2, 0, _Comparison_e67d9847aa86858490dd0c37e68e7505_Out_2);
            float _Divide_2b884a92fd783d82a9012917c2b83115_Out_2;
            Unity_Divide_float(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_R_1, _Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2, _Divide_2b884a92fd783d82a9012917c2b83115_Out_2);
            float _Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1;
            Unity_Arctangent_float(_Divide_2b884a92fd783d82a9012917c2b83115_Out_2, _Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1);
            float _Add_cf6b7aee4a487f8ca16b9171b64a8f0d_Out_2;
            Unity_Add_float(_Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1, 3.141592, _Add_cf6b7aee4a487f8ca16b9171b64a8f0d_Out_2);
            float _Branch_9de100b31215ba89bb19e5b89012a25a_Out_3;
            Unity_Branch_float(_Comparison_e67d9847aa86858490dd0c37e68e7505_Out_2, _Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1, _Add_cf6b7aee4a487f8ca16b9171b64a8f0d_Out_2, _Branch_9de100b31215ba89bb19e5b89012a25a_Out_3);
            float2 _Rotate_ff1406ef744b188ca79c6d3bf0b8bbd7_Out_3;
            Unity_Rotate_Radians_float(_Multiply_8e351d40d36634829f2bb0905b9b9342_Out_2, float2 (0.5, 0.5), _Branch_9de100b31215ba89bb19e5b89012a25a_Out_3, _Rotate_ff1406ef744b188ca79c6d3bf0b8bbd7_Out_3);
            #if defined(SHADER_API_GLES) && (SHADER_TARGET < 30)
              float4 _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0 = float4(0.0f, 0.0f, 0.0f, 1.0f);
            #else
              float4 _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0 = SAMPLE_TEXTURE2D_LOD(_Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0.tex, _Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0.samplerstate, _Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0.GetTransformedUV(_Rotate_ff1406ef744b188ca79c6d3bf0b8bbd7_Out_3), 0);
            #endif
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_R_5 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.r;
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_G_6 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.g;
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_B_7 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.b;
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_A_8 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.a;
            float _Comparison_1a93f0ca7c913a8f81659b20b8d59f2b_Out_2;
            Unity_Comparison_Greater_float(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2, 0.001, _Comparison_1a93f0ca7c913a8f81659b20b8d59f2b_Out_2);
            float3 _Vector3_37ab63b41d52bb8d8eba9088ed71bc11_Out_0 = float3(0, 0, 0);
            float2 _Vector2_4aeecd1cb271dd8e96c2fb12309b9dd0_Out_0 = float2(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_R_1, _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2);
            float _Distance_47e812ff6269a181b6e0faa017faee3b_Out_2;
            Unity_Distance_float2(_Vector2_4aeecd1cb271dd8e96c2fb12309b9dd0_Out_0, float2(0, 0), _Distance_47e812ff6269a181b6e0faa017faee3b_Out_2);
            float _Multiply_e5102f48c5e759879293e06062894e0c_Out_2;
            Unity_Multiply_float_float(_Distance_47e812ff6269a181b6e0faa017faee3b_Out_2, 1.6, _Multiply_e5102f48c5e759879293e06062894e0c_Out_2);
            float _Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3;
            Unity_Clamp_float(_Multiply_e5102f48c5e759879293e06062894e0c_Out_2, 0, 1, _Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3);
            float _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2;
            Unity_Subtract_float(1, _Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3, _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2);
            float3 _Vector3_aeff4a868e81c68f85dfe4c9662da9ce_Out_0 = float3(1, _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2, 0);
            float _Absolute_5573b6b2c1616e819aa4eeb67caec457_Out_1;
            Unity_Absolute_float(_Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3, _Absolute_5573b6b2c1616e819aa4eeb67caec457_Out_1);
            float _Power_33dedbf009cacf848c403a58667a66c6_Out_2;
            Unity_Power_float(_Absolute_5573b6b2c1616e819aa4eeb67caec457_Out_1, 0.16, _Power_33dedbf009cacf848c403a58667a66c6_Out_2);
            float _Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3;
            Unity_Clamp_float(_Power_33dedbf009cacf848c403a58667a66c6_Out_2, 0, 1, _Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3);
            float3 _Lerp_e6000a54e83a8f8e8c5bc16419b360ca_Out_3;
            Unity_Lerp_float3(_Vector3_37ab63b41d52bb8d8eba9088ed71bc11_Out_0, _Vector3_aeff4a868e81c68f85dfe4c9662da9ce_Out_0, (_Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3.xxx), _Lerp_e6000a54e83a8f8e8c5bc16419b360ca_Out_3);
            float3 _Vector3_484f28086891e98d9589d1f65774f6f4_Out_0 = float3(0, _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2, 1);
            float3 _Lerp_93469a90f98ccf80a7121c0b2bf05f48_Out_3;
            Unity_Lerp_float3(_Vector3_37ab63b41d52bb8d8eba9088ed71bc11_Out_0, _Vector3_484f28086891e98d9589d1f65774f6f4_Out_0, (_Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3.xxx), _Lerp_93469a90f98ccf80a7121c0b2bf05f48_Out_3);
            float3 _Branch_365bf259ae64c386a18e2d6997727100_Out_3;
            Unity_Branch_float3(_Comparison_1a93f0ca7c913a8f81659b20b8d59f2b_Out_2, _Lerp_e6000a54e83a8f8e8c5bc16419b360ca_Out_3, _Lerp_93469a90f98ccf80a7121c0b2bf05f48_Out_3, _Branch_365bf259ae64c386a18e2d6997727100_Out_3);
            float3 _Multiply_0563147e8bc9868b8c2a4f2c442c349f_Out_2;
            Unity_Multiply_float3_float3((_SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_A_8.xxx), _Branch_365bf259ae64c386a18e2d6997727100_Out_3, _Multiply_0563147e8bc9868b8c2a4f2c442c349f_Out_2);
            surface.BaseColor = _Multiply_0563147e8bc9868b8c2a4f2c442c349f_Out_2;
            surface.Emission = float3(0, 0, 0);
            surface.Alpha = 1;
            surface.AlphaClipThreshold = 0.5;
            return surface;
        }
        
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES AttributesMesh
            #define VaryingsMeshType VaryingsMeshToPS
            #define VFX_SRP_VARYINGS VaryingsMeshType
            #define VFX_SRP_SURFACE_INPUTS FragInputs
            #endif
            
        VertexDescriptionInputs AttributesMeshToVertexDescriptionInputs(AttributesMesh input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
        
            return output;
        }
        
        VertexDescription GetVertexDescription(AttributesMesh input, float3 timeParameters
        #ifdef HAVE_VFX_MODIFICATION
            , AttributesElement element
        #endif
        )
        {
            // build graph inputs
            VertexDescriptionInputs vertexDescriptionInputs = AttributesMeshToVertexDescriptionInputs(input);
            // Override time parameters with used one (This is required to correctly handle motion vector for vertex animation based on time)
        
            // evaluate vertex graph
        #ifdef HAVE_VFX_MODIFICATION
            GraphProperties properties;
            ZERO_INITIALIZE(GraphProperties, properties);
        
            // Fetch the vertex graph properties for the particle instance.
            GetElementVertexProperties(element, properties);
        
            VertexDescription vertexDescription = VertexDescriptionFunction(vertexDescriptionInputs, properties);
        #else
            VertexDescription vertexDescription = VertexDescriptionFunction(vertexDescriptionInputs);
        #endif
            return vertexDescription;
        
        }
        
        AttributesMesh ApplyMeshModification(AttributesMesh input, float3 timeParameters
        #ifdef USE_CUSTOMINTERP_SUBSTRUCT
            #ifdef TESSELLATION_ON
            , inout VaryingsMeshToDS varyings
            #else
            , inout VaryingsMeshToPS varyings
            #endif
        #endif
        #ifdef HAVE_VFX_MODIFICATION
                , AttributesElement element
        #endif
            )
        {
            VertexDescription vertexDescription = GetVertexDescription(input, timeParameters
        #ifdef HAVE_VFX_MODIFICATION
                , element
        #endif
            );
        
            // copy graph output to the results
            input.positionOS = vertexDescription.Position;
            input.normalOS = vertexDescription.Normal;
            input.tangentOS.xyz = vertexDescription.Tangent;
        
            
        
            return input;
        }
        
        #if defined(_ADD_CUSTOM_VELOCITY) // For shader graph custom velocity
        // Return precomputed Velocity in object space
        float3 GetCustomVelocity(AttributesMesh input
        #ifdef HAVE_VFX_MODIFICATION
            , AttributesElement element
        #endif
        )
        {
            VertexDescription vertexDescription = GetVertexDescription(input, _TimeParameters.xyz
        #ifdef HAVE_VFX_MODIFICATION
                , element
        #endif
            );
            return vertexDescription.CustomVelocity;
        }
        #endif
        
        FragInputs BuildFragInputs(VaryingsMeshToPS input)
        {
            FragInputs output;
            ZERO_INITIALIZE(FragInputs, output);
        
            // Init to some default value to make the computer quiet (else it output 'divide by zero' warning even if value is not used).
            // TODO: this is a really poor workaround, but the variable is used in a bunch of places
            // to compute normals which are then passed on elsewhere to compute other values...
            output.tangentToWorld = k_identity3x3;
            output.positionSS = input.positionCS;       // input.positionCS is SV_Position
        
            output.tangentToWorld =             BuildTangentToWorld(input.tangentWS, input.normalWS);
            output.texCoord0 =                  input.texCoord0;
            output.texCoord1 =                  input.texCoord1;
            output.texCoord2 =                  input.texCoord2;
            output.texCoord3 =                  input.texCoord3;
        
        #ifdef HAVE_VFX_MODIFICATION
            // FragInputs from VFX come from two places: Interpolator or CBuffer.
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            // splice point to copy custom interpolator fields from varyings to frag inputs
            
        
            return output;
        }
        
        // existing HDRP code uses the combined function to go directly from packed to frag inputs
        FragInputs UnpackVaryingsMeshToFragInputs(PackedVaryingsMeshToPS input)
        {
            UNITY_SETUP_INSTANCE_ID(input);
        #if defined(HAVE_VFX_MODIFICATION) && defined(UNITY_INSTANCING_ENABLED)
            unity_InstanceID = input.instanceID;
        #endif
            VaryingsMeshToPS unpacked = UnpackVaryingsMeshToPS(input);
            return BuildFragInputs(unpacked);
        }
            SurfaceDescriptionInputs FragInputsToSurfaceDescriptionInputs(FragInputs input, float3 viewWS)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
            #if defined(SHADER_STAGE_RAY_TRACING)
            #else
            #endif
        
        #if UNITY_UV_STARTS_AT_TOP
        #else
        #endif
        
        
            output.uv0 =                                        input.texCoord0;
            output.uv1 =                                        input.texCoord1;
            output.uv2 =                                        input.texCoord2;
            output.uv3 =                                        input.texCoord3;
        
            // splice point to copy frag inputs custom interpolator pack into the SDI
            
        
            return output;
        }
        
            // --------------------------------------------------
            // Build Surface Data (Specific Material)
        
        void BuildSurfaceData(FragInputs fragInputs, inout SurfaceDescription surfaceDescription, float3 V, PositionInputs posInput, out SurfaceData surfaceData)
        {
            // setup defaults -- these are used if the graph doesn't output a value
            ZERO_INITIALIZE(SurfaceData, surfaceData);
        
            // copy across graph values, if defined
            surfaceData.color = surfaceDescription.BaseColor;
        
            #ifdef WRITE_NORMAL_BUFFER
            // When we need to export the normal (in the depth prepass, we write the geometry one)
            surfaceData.normalWS = fragInputs.tangentToWorld[2];
            #endif
        
            #if defined(DEBUG_DISPLAY)
            if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
            {
                // TODO
            }
            #endif
        
            #ifdef _ENABLE_SHADOW_MATTE
        
                #if (SHADERPASS == SHADERPASS_FORWARD_UNLIT) || (SHADERPASS == SHADERPASS_RAYTRACING_GBUFFER) || (SHADERPASS == SHADERPASS_RAYTRACING_INDIRECT) || (SHADERPASS == SHADERPASS_RAYTRACING_FORWARD)
        
                    HDShadowContext shadowContext = InitShadowContext();
        
                    // Evaluate the shadow, the normal is guaranteed if shadow matte is enabled on this shader.
                    float3 shadow3;
                    ShadowLoopMin(shadowContext, posInput, normalize(fragInputs.tangentToWorld[2]), asuint(_ShadowMatteFilter), GetMeshRenderingLightLayer(), shadow3);
        
                    // Compute the average value in the fourth channel
                    float4 shadow = float4(shadow3, dot(shadow3, float3(1.0/3.0, 1.0/3.0, 1.0/3.0)));
        
                    float4 shadowColor = (1.0 - shadow) * surfaceDescription.ShadowTint.rgba;
                    float  localAlpha  = saturate(shadowColor.a + surfaceDescription.Alpha);
        
                    // Keep the nested lerp
                    // With no Color (bsdfData.color.rgb, bsdfData.color.a == 0.0f), just use ShadowColor*Color to avoid a ring of "white" around the shadow
                    // And mix color to consider the Color & ShadowColor alpha (from texture or/and color picker)
                    #ifdef _SURFACE_TYPE_TRANSPARENT
                        surfaceData.color = lerp(shadowColor.rgb * surfaceData.color, lerp(lerp(shadowColor.rgb, surfaceData.color, 1.0 - surfaceDescription.ShadowTint.a), surfaceData.color, shadow.rgb), surfaceDescription.Alpha);
                    #else
                        surfaceData.color = lerp(lerp(shadowColor.rgb, surfaceData.color, 1.0 - surfaceDescription.ShadowTint.a), surfaceData.color, shadow.rgb);
                    #endif
                    localAlpha = ApplyBlendMode(surfaceData.color, localAlpha).a;
        
                    surfaceDescription.Alpha = localAlpha;
        
                #elif SHADERPASS == SHADERPASS_PATH_TRACING
        
                    surfaceData.normalWS = fragInputs.tangentToWorld[2];
                    surfaceData.shadowTint = surfaceDescription.ShadowTint.rgba;
        
                #endif
        
            #endif // _ENABLE_SHADOW_MATTE
        }
        
            // --------------------------------------------------
            // Get Surface And BuiltinData
        
            void GetSurfaceAndBuiltinData(FragInputs fragInputs, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData RAY_TRACING_OPTIONAL_PARAMETERS)
            {
                // Don't dither if displaced tessellation (we're fading out the displacement instead to match the next LOD)
                #if !defined(SHADER_STAGE_RAY_TRACING) && !defined(_TESSELLATION_DISPLACEMENT)
                #ifdef LOD_FADE_CROSSFADE // enable dithering LOD transition if user select CrossFade transition in LOD group
                LODDitheringTransition(ComputeFadeMaskSeed(V, posInput.positionSS), unity_LODFade.x);
                #endif
                #endif
        
                #ifndef SHADER_UNLIT
                #ifdef _DOUBLESIDED_ON
                    float3 doubleSidedConstants = _DoubleSidedConstants.xyz;
                #else
                    float3 doubleSidedConstants = float3(1.0, 1.0, 1.0);
                #endif
        
                ApplyDoubleSidedFlipOrMirror(fragInputs, doubleSidedConstants); // Apply double sided flip on the vertex normal
                #endif // SHADER_UNLIT
        
                SurfaceDescriptionInputs surfaceDescriptionInputs = FragInputsToSurfaceDescriptionInputs(fragInputs, V);
        
                #if defined(HAVE_VFX_MODIFICATION)
                GraphProperties properties;
                ZERO_INITIALIZE(GraphProperties, properties);
        
                GetElementPixelProperties(fragInputs, properties);
        
                SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs, properties);
                #else
                SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);
                #endif
        
                // Perform alpha test very early to save performance (a killed pixel will not sample textures)
                // TODO: split graph evaluation to grab just alpha dependencies first? tricky..
                #ifdef _ALPHATEST_ON
                    float alphaCutoff = surfaceDescription.AlphaClipThreshold;
                    #if SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_PREPASS
                    // The TransparentDepthPrepass is also used with SSR transparent.
                    // If an artists enable transaprent SSR but not the TransparentDepthPrepass itself, then we use AlphaClipThreshold
                    // otherwise if TransparentDepthPrepass is enabled we use AlphaClipThresholdDepthPrepass
                    #elif SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_POSTPASS
                    // DepthPostpass always use its own alpha threshold
                    alphaCutoff = surfaceDescription.AlphaClipThresholdDepthPostpass;
                    #elif (SHADERPASS == SHADERPASS_SHADOWS) || (SHADERPASS == SHADERPASS_RAYTRACING_VISIBILITY)
                    // If use shadow threshold isn't enable we don't allow any test
                    #endif
        
                    GENERIC_ALPHA_TEST(surfaceDescription.Alpha, alphaCutoff);
                #endif
        
                #if !defined(SHADER_STAGE_RAY_TRACING) && _DEPTHOFFSET_ON
                ApplyDepthOffsetPositionInput(V, surfaceDescription.DepthOffset, GetViewForwardDir(), GetWorldToHClipMatrix(), posInput);
                #endif
        
                #ifndef SHADER_UNLIT
                float3 bentNormalWS;
                BuildSurfaceData(fragInputs, surfaceDescription, V, posInput, surfaceData, bentNormalWS);
        
                #ifdef FRAG_INPUTS_USE_TEXCOORD1
                    float4 lightmapTexCoord1 = fragInputs.texCoord1;
                #else
                    float4 lightmapTexCoord1 = float4(0,0,0,0);
                #endif
        
                #ifdef FRAG_INPUTS_USE_TEXCOORD2
                    float4 lightmapTexCoord2 = fragInputs.texCoord2;
                #else
                    float4 lightmapTexCoord2 = float4(0,0,0,0);
                #endif
        
                // Builtin Data
                // For back lighting we use the oposite vertex normal
                InitBuiltinData(posInput, surfaceDescription.Alpha, bentNormalWS, -fragInputs.tangentToWorld[2], lightmapTexCoord1, lightmapTexCoord2, builtinData);
        
                #else
                BuildSurfaceData(fragInputs, surfaceDescription, V, posInput, surfaceData);
        
                ZERO_BUILTIN_INITIALIZE(builtinData); // No call to InitBuiltinData as we don't have any lighting
                builtinData.opacity = surfaceDescription.Alpha;
        
                #if defined(DEBUG_DISPLAY)
                    // Light Layers are currently not used for the Unlit shader (because it is not lit)
                    // But Unlit objects do cast shadows according to their rendering layer mask, which is what we want to
                    // display in the light layers visualization mode, therefore we need the renderingLayers
                    builtinData.renderingLayers = GetMeshRenderingLightLayer();
                #endif
        
                #endif // SHADER_UNLIT
        
                #ifdef _ALPHATEST_ON
                    // Used for sharpening by alpha to mask - Alpha to covertage is only used with depth only and forward pass (no shadow pass, no transparent pass)
                    builtinData.alphaClipTreshold = alphaCutoff;
                #endif
        
                // override sampleBakedGI - not used by Unlit
        
                builtinData.emissiveColor = surfaceDescription.Emission;
        
                // Note this will not fully work on transparent surfaces (can check with _SURFACE_TYPE_TRANSPARENT define)
                // We will always overwrite vt feeback with the nearest. So behind transparent surfaces vt will not be resolved
                // This is a limitation of the current MRT approach.
                #ifdef UNITY_VIRTUAL_TEXTURING
                #endif
        
                #if _DEPTHOFFSET_ON
                builtinData.depthOffset = surfaceDescription.DepthOffset;
                #endif
        
                // TODO: We should generate distortion / distortionBlur for non distortion pass
                #if (SHADERPASS == SHADERPASS_DISTORTION)
                builtinData.distortion = surfaceDescription.Distortion;
                builtinData.distortionBlur = surfaceDescription.DistortionBlur;
                #endif
        
                #ifndef SHADER_UNLIT
                // PostInitBuiltinData call ApplyDebugToBuiltinData
                PostInitBuiltinData(V, posInput, surfaceData, builtinData);
                #else
                ApplyDebugToBuiltinData(builtinData);
                #endif
        
                RAY_TRACING_OPTIONAL_ALPHA_TEST_PASS
            }
        
            // --------------------------------------------------
            // Main
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"
        
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
        
        	#ifdef HAVE_VFX_MODIFICATION
        	#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/VisualEffectVertex.hlsl"
        	#endif
        
            ENDHLSL
        }
        Pass
        {
            Name "SceneSelectionPass"
            Tags
            {
                "LightMode" = "SceneSelectionPass"
            }
        
            // Render State
            Cull Off
        
            // Debug
            // <None>
        
            // --------------------------------------------------
            // Pass
        
            HLSLPROGRAM
        
            // Pragmas
            #pragma multi_compile _ DOTS_INSTANCING_ON
        #pragma editor_sync_compilation
        #pragma instancing_options renderinglayer
        #pragma target 4.5
        #pragma vertex Vert
        #pragma fragment Frag
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
        #pragma multi_compile_instancing
        
            // Keywords
            #pragma shader_feature_local _ _ALPHATEST_ON
        #pragma shader_feature _ _SURFACE_TYPE_TRANSPARENT
        #pragma shader_feature_local _BLENDMODE_OFF _BLENDMODE_ALPHA _BLENDMODE_ADD _BLENDMODE_PRE_MULTIPLY
        #pragma shader_feature_local _ _ADD_PRECOMPUTED_VELOCITY
        #pragma shader_feature_local _ _TRANSPARENT_WRITES_MOTION_VEC
        #pragma shader_feature_local_fragment _ _ENABLE_FOG_ON_TRANSPARENT
            // GraphKeywords: <None>
        
            // For custom interpolators to inject a substruct definition before FragInputs definition,
            // allowing for FragInputs to capture CI's intended for ShaderGraph's SDI.
            struct CustomInterpolators
        {
        };
        #define USE_CUSTOMINTERP_SUBSTRUCT
        
        
        
            // TODO: Merge FragInputsVFX substruct with CustomInterpolators.
        	#ifdef HAVE_VFX_MODIFICATION
        	struct FragInputsVFX
            {
                /* WARNING: $splice Could not find named fragment 'FragInputsVFX' */
            };
            #endif
        
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GeometricTools.hlsl" // Required by Tessellation.hlsl
        	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Tessellation.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl" // Required to be include before we include properties as it define DECLARE_STACK_CB
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphHeader.hlsl" // Need to be here for Gradient struct definition
        
            // --------------------------------------------------
            // Defines
        
            // Attribute
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define ATTRIBUTES_NEED_TEXCOORD2
            #define ATTRIBUTES_NEED_TEXCOORD3
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_TEXCOORD1
            #define VARYINGS_NEED_TEXCOORD2
            #define VARYINGS_NEED_TEXCOORD3
        
            #define HAVE_MESH_MODIFICATION
        
            //Strip down the FragInputs.hlsl (on graphics), so we can only optimize the interpolators we use.
            //if by accident something requests contents of FragInputs.hlsl, it will be caught as a compiler error
            //Frag inputs stripping is only enabled when FRAG_INPUTS_ENABLE_STRIPPING is set
            #if !defined(SHADER_STAGE_RAY_TRACING) && SHADERPASS != SHADERPASS_RAYTRACING_GBUFFER && SHADERPASS != SHADERPASS_FULL_SCREEN_DEBUG
            #define FRAG_INPUTS_ENABLE_STRIPPING
            #endif
            #define FRAG_INPUTS_USE_TEXCOORD0
            #define FRAG_INPUTS_USE_TEXCOORD1
            #define FRAG_INPUTS_USE_TEXCOORD2
            #define FRAG_INPUTS_USE_TEXCOORD3
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
        
        
        
            #define SHADERPASS SHADERPASS_DEPTH_ONLY
        #define RAYTRACING_SHADER_GRAPH_DEFAULT
        #define SCENESELECTIONPASS 1
        
        
            // Following two define are a workaround introduce in 10.1.x for RaytracingQualityNode
            // The ShaderGraph don't support correctly migration of this node as it serialize all the node data
            // in the json file making it impossible to uprgrade. Until we get a fix, we do a workaround here
            // to still allow us to rename the field and keyword of this node without breaking existing code.
            #ifdef RAYTRACING_SHADER_GRAPH_DEFAULT
            #define RAYTRACING_SHADER_GRAPH_HIGH
            #endif
        
            #ifdef RAYTRACING_SHADER_GRAPH_RAYTRACED
            #define RAYTRACING_SHADER_GRAPH_LOW
            #endif
            // end
        
            #ifndef SHADER_UNLIT
            // We need isFrontFace when using double sided - it is not required for unlit as in case of unlit double sided only drive the cullmode
            // VARYINGS_NEED_CULLFACE can be define by VaryingsMeshToPS.FaceSign input if a IsFrontFace Node is included in the shader graph.
            #if defined(_DOUBLESIDED_ON) && !defined(VARYINGS_NEED_CULLFACE)
                #define VARYINGS_NEED_CULLFACE
            #endif
            #endif
        
            // Specific Material Define
        // Setup a define to say we are an unlit shader
        #define SHADER_UNLIT
        
        // Following Macro are only used by Unlit material
        #if defined(_ENABLE_SHADOW_MATTE) && (SHADERPASS == SHADERPASS_FORWARD_UNLIT || SHADERPASS == SHADERPASS_PATH_TRACING)
        #define LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
        #define HAS_LIGHTLOOP
        #endif
            // Caution: we can use the define SHADER_UNLIT onlit after the above Material include as it is the Unlit template who define it
        
            // To handle SSR on transparent correctly with a possibility to enable/disable it per framesettings
            // we should have a code like this:
            // if !defined(_DISABLE_SSR_TRANSPARENT)
            // pragma multi_compile _ WRITE_NORMAL_BUFFER
            // endif
            // i.e we enable the multicompile only if we can receive SSR or not, and then C# code drive
            // it based on if SSR transparent in frame settings and not (and stripper can strip it).
            // this is currently not possible with our current preprocessor as _DISABLE_SSR_TRANSPARENT is a keyword not a define
            // so instead we used this and chose to pay the extra cost of normal write even if SSR transaprent is disabled.
            // Ideally the shader graph generator should handle it but condition below can't be handle correctly for now.
            #if SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_PREPASS
            #if !defined(_DISABLE_SSR_TRANSPARENT) && !defined(SHADER_UNLIT)
                #define WRITE_NORMAL_BUFFER
            #endif
            #endif
        
            #ifndef DEBUG_DISPLAY
                // In case of opaque we don't want to perform the alpha test, it is done in depth prepass and we use depth equal for ztest (setup from UI)
                // Don't do it with debug display mode as it is possible there is no depth prepass in this case
                #if !defined(_SURFACE_TYPE_TRANSPARENT)
                    #if SHADERPASS == SHADERPASS_FORWARD
                    #define SHADERPASS_FORWARD_BYPASS_ALPHA_TEST
                    #elif SHADERPASS == SHADERPASS_GBUFFER
                    #define SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST
                    #endif
                #endif
            #endif
        
            // Define _DEFERRED_CAPABLE_MATERIAL for shader capable to run in deferred pass
            #if defined(SHADER_LIT) && !defined(_SURFACE_TYPE_TRANSPARENT)
                #define _DEFERRED_CAPABLE_MATERIAL
            #endif
        
            // Translate transparent motion vector define
            #if defined(_TRANSPARENT_WRITES_MOTION_VEC) && defined(_SURFACE_TYPE_TRANSPARENT)
                #define _WRITE_TRANSPARENT_MOTION_VECTOR
            #endif
        
            // -- Graph Properties
            CBUFFER_START(UnityPerMaterial)
        float4 _TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1_TexelSize;
        float4 _Direction_TexelSize;
        float2 _TextureSize;
        float _RotateUV;
        float Boolean_F2B1E91A;
        float4 _EmissionColor;
        float _UseShadowThreshold;
        float4 _DoubleSidedConstants;
        float _BlendMode;
        float _EnableBlendModePreserveSpecularLighting;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1);
        SAMPLER(sampler_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1);
        TEXTURE2D(_Direction);
        SAMPLER(sampler_Direction);
        
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
        
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
        
            // Includes
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/PickingSpaceTransforms.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl"
        	// GraphIncludes: <None>
        
            // --------------------------------------------------
            // Structs and Packing
        
            struct AttributesMesh
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
             float4 uv1 : TEXCOORD1;
             float4 uv2 : TEXCOORD2;
             float4 uv3 : TEXCOORD3;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct VaryingsMeshToPS
        {
            SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
             float4 texCoord0;
             float4 texCoord1;
             float4 texCoord2;
             float4 texCoord3;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
        };
        struct SurfaceDescriptionInputs
        {
             float4 uv0;
             float4 uv1;
             float4 uv2;
             float4 uv3;
        };
        struct PackedVaryingsMeshToPS
        {
            SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
             float4 interp0 : INTERP0;
             float4 interp1 : INTERP1;
             float4 interp2 : INTERP2;
             float4 interp3 : INTERP3;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
        };
        
            PackedVaryingsMeshToPS PackVaryingsMeshToPS (VaryingsMeshToPS input)
        {
            PackedVaryingsMeshToPS output;
            ZERO_INITIALIZE(PackedVaryingsMeshToPS, output);
            output.positionCS = input.positionCS;
            output.interp0.xyzw =  input.texCoord0;
            output.interp1.xyzw =  input.texCoord1;
            output.interp2.xyzw =  input.texCoord2;
            output.interp3.xyzw =  input.texCoord3;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            return output;
        }
        
        VaryingsMeshToPS UnpackVaryingsMeshToPS (PackedVaryingsMeshToPS input)
        {
            VaryingsMeshToPS output;
            output.positionCS = input.positionCS;
            output.texCoord0 = input.interp0.xyzw;
            output.texCoord1 = input.interp1.xyzw;
            output.texCoord2 = input.interp2.xyzw;
            output.texCoord3 = input.interp3.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            return output;
        }
        
        
            // --------------------------------------------------
            // Graph
        
        
            // Graph Functions
            
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A / B;
        }
        
        void Unity_Fraction_float2(float2 In, out float2 Out)
        {
            Out = frac(In);
        }
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A * B;
        }
        
        void Unity_Subtract_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A - B;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_Comparison_Greater_float(float A, float B, out float Out)
        {
            Out = A > B ? 1 : 0;
        }
        
        void Unity_Divide_float(float A, float B, out float Out)
        {
            Out = A / B;
        }
        
        void Unity_Arctangent_float(float In, out float Out)
        {
            Out = atan(In);
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Branch_float(float Predicate, float True, float False, out float Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Rotate_Radians_float(float2 UV, float2 Center, float Rotation, out float2 Out)
        {
            //rotation matrix
            UV -= Center;
            float s = sin(Rotation);
            float c = cos(Rotation);
        
            //center rotation matrix
            float2x2 rMatrix = float2x2(c, -s, s, c);
            rMatrix *= 0.5;
            rMatrix += 0.5;
            rMatrix = rMatrix*2 - 1;
        
            //multiply the UVs by the rotation matrix
            UV.xy = mul(UV.xy, rMatrix);
            UV += Center;
        
            Out = UV;
        }
        
        void Unity_Distance_float2(float2 A, float2 B, out float Out)
        {
            Out = distance(A, B);
        }
        
        void Unity_Clamp_float(float In, float Min, float Max, out float Out)
        {
            Out = clamp(In, Min, Max);
        }
        
        void Unity_Subtract_float(float A, float B, out float Out)
        {
            Out = A - B;
        }
        
        void Unity_Absolute_float(float In, out float Out)
        {
            Out = abs(In);
        }
        
        void Unity_Power_float(float A, float B, out float Out)
        {
            Out = pow(A, B);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A * B;
        }
        
            // Graph Vertex
            struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
            // Graph Pixel
            struct SurfaceDescription
        {
            float3 BaseColor;
            float3 Emission;
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0 = UnityBuildTexture2DStructNoScale(_Direction);
            float4 _UV_7c8a457b34527181924b2b4a71350de9_Out_0 = IN.uv0;
            float2 _TilingAndOffset_67d58b7a8092828aaece7f3acfe6e8a9_Out_3;
            Unity_TilingAndOffset_float((_UV_7c8a457b34527181924b2b4a71350de9_Out_0.xy), float2 (2, 2), float2 (0, 0), _TilingAndOffset_67d58b7a8092828aaece7f3acfe6e8a9_Out_3);
            float2 _Vector2_9a3e446bd0dfc3868e3db6f9bf101ead_Out_0 = float2(1, 1);
            float2 _Vector2_8355e4dbd796b188ba1e500dcabf0c0f_Out_0 = float2(30, 30);
            float2 _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2;
            Unity_Divide_float2(_Vector2_9a3e446bd0dfc3868e3db6f9bf101ead_Out_0, _Vector2_8355e4dbd796b188ba1e500dcabf0c0f_Out_0, _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2);
            float2 _Divide_5d0f49ab4bcab689894f976217114a79_Out_2;
            Unity_Divide_float2(_TilingAndOffset_67d58b7a8092828aaece7f3acfe6e8a9_Out_3, _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2, _Divide_5d0f49ab4bcab689894f976217114a79_Out_2);
            float2 _Fraction_e4f0f05fb76fd3809f5ec855b54a4595_Out_1;
            Unity_Fraction_float2(_Divide_5d0f49ab4bcab689894f976217114a79_Out_2, _Fraction_e4f0f05fb76fd3809f5ec855b54a4595_Out_1);
            float2 _Multiply_cbcb0de832b1d08288916e46aa8d4df2_Out_2;
            Unity_Multiply_float2_float2(_Fraction_e4f0f05fb76fd3809f5ec855b54a4595_Out_1, _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2, _Multiply_cbcb0de832b1d08288916e46aa8d4df2_Out_2);
            float2 _Vector2_e6d2595367ed0380b42c7a74855838a5_Out_0 = float2(246, 246);
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_Width_0 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.z;
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_Height_2 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.w;
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_TexelWidth_3 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.x;
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_TexelHeight_4 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.y;
            float2 _Vector2_e8728bfbf194e5818f010927b4ba8d79_Out_0 = float2(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Width_0, _TexelSize_18bb9f416e386f87bee91aa46804bc56_Height_2);
            float2 _Multiply_561a2890964f4081a8cf5d16bccb889f_Out_2;
            Unity_Multiply_float2_float2(_Vector2_e6d2595367ed0380b42c7a74855838a5_Out_0, _Vector2_e8728bfbf194e5818f010927b4ba8d79_Out_0, _Multiply_561a2890964f4081a8cf5d16bccb889f_Out_2);
            float2 _Vector2_97c81bcbfd26f08385a101ac3da412f3_Out_0 = float2(10, 10);
            float2 _Multiply_bccda05b3809138d8da15484dd6a7b4e_Out_2;
            Unity_Multiply_float2_float2(_Vector2_e8728bfbf194e5818f010927b4ba8d79_Out_0, _Vector2_97c81bcbfd26f08385a101ac3da412f3_Out_0, _Multiply_bccda05b3809138d8da15484dd6a7b4e_Out_2);
            float2 _Subtract_710d4672e66f7780ad90ca6e19a733bd_Out_2;
            Unity_Subtract_float2(_Multiply_561a2890964f4081a8cf5d16bccb889f_Out_2, _Multiply_bccda05b3809138d8da15484dd6a7b4e_Out_2, _Subtract_710d4672e66f7780ad90ca6e19a733bd_Out_2);
            float2 _Multiply_cd542324eceadd8daaca18ab78d3fbf6_Out_2;
            Unity_Multiply_float2_float2(_Multiply_cbcb0de832b1d08288916e46aa8d4df2_Out_2, _Subtract_710d4672e66f7780ad90ca6e19a733bd_Out_2, _Multiply_cd542324eceadd8daaca18ab78d3fbf6_Out_2);
            float2 _Vector2_df86361e9c36e7828e3097cc1fa8a75b_Out_0 = float2(0.1, 0.1);
            float2 _Multiply_8e351d40d36634829f2bb0905b9b9342_Out_2;
            Unity_Multiply_float2_float2(_Multiply_cd542324eceadd8daaca18ab78d3fbf6_Out_2, _Vector2_df86361e9c36e7828e3097cc1fa8a75b_Out_0, _Multiply_8e351d40d36634829f2bb0905b9b9342_Out_2);
            float4 _UV_1ca38e193be034888b8a707af9d47210_Out_0 = IN.uv3;
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_R_1 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[0];
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[1];
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_B_3 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[2];
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_A_4 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[3];
            float _Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2;
            Unity_Multiply_float_float(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2, -1, _Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2);
            float _Comparison_e67d9847aa86858490dd0c37e68e7505_Out_2;
            Unity_Comparison_Greater_float(_Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2, 0, _Comparison_e67d9847aa86858490dd0c37e68e7505_Out_2);
            float _Divide_2b884a92fd783d82a9012917c2b83115_Out_2;
            Unity_Divide_float(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_R_1, _Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2, _Divide_2b884a92fd783d82a9012917c2b83115_Out_2);
            float _Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1;
            Unity_Arctangent_float(_Divide_2b884a92fd783d82a9012917c2b83115_Out_2, _Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1);
            float _Add_cf6b7aee4a487f8ca16b9171b64a8f0d_Out_2;
            Unity_Add_float(_Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1, 3.141592, _Add_cf6b7aee4a487f8ca16b9171b64a8f0d_Out_2);
            float _Branch_9de100b31215ba89bb19e5b89012a25a_Out_3;
            Unity_Branch_float(_Comparison_e67d9847aa86858490dd0c37e68e7505_Out_2, _Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1, _Add_cf6b7aee4a487f8ca16b9171b64a8f0d_Out_2, _Branch_9de100b31215ba89bb19e5b89012a25a_Out_3);
            float2 _Rotate_ff1406ef744b188ca79c6d3bf0b8bbd7_Out_3;
            Unity_Rotate_Radians_float(_Multiply_8e351d40d36634829f2bb0905b9b9342_Out_2, float2 (0.5, 0.5), _Branch_9de100b31215ba89bb19e5b89012a25a_Out_3, _Rotate_ff1406ef744b188ca79c6d3bf0b8bbd7_Out_3);
            #if defined(SHADER_API_GLES) && (SHADER_TARGET < 30)
              float4 _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0 = float4(0.0f, 0.0f, 0.0f, 1.0f);
            #else
              float4 _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0 = SAMPLE_TEXTURE2D_LOD(_Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0.tex, _Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0.samplerstate, _Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0.GetTransformedUV(_Rotate_ff1406ef744b188ca79c6d3bf0b8bbd7_Out_3), 0);
            #endif
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_R_5 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.r;
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_G_6 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.g;
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_B_7 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.b;
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_A_8 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.a;
            float _Comparison_1a93f0ca7c913a8f81659b20b8d59f2b_Out_2;
            Unity_Comparison_Greater_float(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2, 0.001, _Comparison_1a93f0ca7c913a8f81659b20b8d59f2b_Out_2);
            float3 _Vector3_37ab63b41d52bb8d8eba9088ed71bc11_Out_0 = float3(0, 0, 0);
            float2 _Vector2_4aeecd1cb271dd8e96c2fb12309b9dd0_Out_0 = float2(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_R_1, _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2);
            float _Distance_47e812ff6269a181b6e0faa017faee3b_Out_2;
            Unity_Distance_float2(_Vector2_4aeecd1cb271dd8e96c2fb12309b9dd0_Out_0, float2(0, 0), _Distance_47e812ff6269a181b6e0faa017faee3b_Out_2);
            float _Multiply_e5102f48c5e759879293e06062894e0c_Out_2;
            Unity_Multiply_float_float(_Distance_47e812ff6269a181b6e0faa017faee3b_Out_2, 1.6, _Multiply_e5102f48c5e759879293e06062894e0c_Out_2);
            float _Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3;
            Unity_Clamp_float(_Multiply_e5102f48c5e759879293e06062894e0c_Out_2, 0, 1, _Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3);
            float _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2;
            Unity_Subtract_float(1, _Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3, _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2);
            float3 _Vector3_aeff4a868e81c68f85dfe4c9662da9ce_Out_0 = float3(1, _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2, 0);
            float _Absolute_5573b6b2c1616e819aa4eeb67caec457_Out_1;
            Unity_Absolute_float(_Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3, _Absolute_5573b6b2c1616e819aa4eeb67caec457_Out_1);
            float _Power_33dedbf009cacf848c403a58667a66c6_Out_2;
            Unity_Power_float(_Absolute_5573b6b2c1616e819aa4eeb67caec457_Out_1, 0.16, _Power_33dedbf009cacf848c403a58667a66c6_Out_2);
            float _Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3;
            Unity_Clamp_float(_Power_33dedbf009cacf848c403a58667a66c6_Out_2, 0, 1, _Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3);
            float3 _Lerp_e6000a54e83a8f8e8c5bc16419b360ca_Out_3;
            Unity_Lerp_float3(_Vector3_37ab63b41d52bb8d8eba9088ed71bc11_Out_0, _Vector3_aeff4a868e81c68f85dfe4c9662da9ce_Out_0, (_Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3.xxx), _Lerp_e6000a54e83a8f8e8c5bc16419b360ca_Out_3);
            float3 _Vector3_484f28086891e98d9589d1f65774f6f4_Out_0 = float3(0, _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2, 1);
            float3 _Lerp_93469a90f98ccf80a7121c0b2bf05f48_Out_3;
            Unity_Lerp_float3(_Vector3_37ab63b41d52bb8d8eba9088ed71bc11_Out_0, _Vector3_484f28086891e98d9589d1f65774f6f4_Out_0, (_Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3.xxx), _Lerp_93469a90f98ccf80a7121c0b2bf05f48_Out_3);
            float3 _Branch_365bf259ae64c386a18e2d6997727100_Out_3;
            Unity_Branch_float3(_Comparison_1a93f0ca7c913a8f81659b20b8d59f2b_Out_2, _Lerp_e6000a54e83a8f8e8c5bc16419b360ca_Out_3, _Lerp_93469a90f98ccf80a7121c0b2bf05f48_Out_3, _Branch_365bf259ae64c386a18e2d6997727100_Out_3);
            float3 _Multiply_0563147e8bc9868b8c2a4f2c442c349f_Out_2;
            Unity_Multiply_float3_float3((_SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_A_8.xxx), _Branch_365bf259ae64c386a18e2d6997727100_Out_3, _Multiply_0563147e8bc9868b8c2a4f2c442c349f_Out_2);
            surface.BaseColor = _Multiply_0563147e8bc9868b8c2a4f2c442c349f_Out_2;
            surface.Emission = float3(0, 0, 0);
            surface.Alpha = 1;
            surface.AlphaClipThreshold = 0.5;
            return surface;
        }
        
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES AttributesMesh
            #define VaryingsMeshType VaryingsMeshToPS
            #define VFX_SRP_VARYINGS VaryingsMeshType
            #define VFX_SRP_SURFACE_INPUTS FragInputs
            #endif
            
        VertexDescriptionInputs AttributesMeshToVertexDescriptionInputs(AttributesMesh input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
        
            return output;
        }
        
        VertexDescription GetVertexDescription(AttributesMesh input, float3 timeParameters
        #ifdef HAVE_VFX_MODIFICATION
            , AttributesElement element
        #endif
        )
        {
            // build graph inputs
            VertexDescriptionInputs vertexDescriptionInputs = AttributesMeshToVertexDescriptionInputs(input);
            // Override time parameters with used one (This is required to correctly handle motion vector for vertex animation based on time)
        
            // evaluate vertex graph
        #ifdef HAVE_VFX_MODIFICATION
            GraphProperties properties;
            ZERO_INITIALIZE(GraphProperties, properties);
        
            // Fetch the vertex graph properties for the particle instance.
            GetElementVertexProperties(element, properties);
        
            VertexDescription vertexDescription = VertexDescriptionFunction(vertexDescriptionInputs, properties);
        #else
            VertexDescription vertexDescription = VertexDescriptionFunction(vertexDescriptionInputs);
        #endif
            return vertexDescription;
        
        }
        
        AttributesMesh ApplyMeshModification(AttributesMesh input, float3 timeParameters
        #ifdef USE_CUSTOMINTERP_SUBSTRUCT
            #ifdef TESSELLATION_ON
            , inout VaryingsMeshToDS varyings
            #else
            , inout VaryingsMeshToPS varyings
            #endif
        #endif
        #ifdef HAVE_VFX_MODIFICATION
                , AttributesElement element
        #endif
            )
        {
            VertexDescription vertexDescription = GetVertexDescription(input, timeParameters
        #ifdef HAVE_VFX_MODIFICATION
                , element
        #endif
            );
        
            // copy graph output to the results
            input.positionOS = vertexDescription.Position;
            input.normalOS = vertexDescription.Normal;
            input.tangentOS.xyz = vertexDescription.Tangent;
        
            
        
            return input;
        }
        
        #if defined(_ADD_CUSTOM_VELOCITY) // For shader graph custom velocity
        // Return precomputed Velocity in object space
        float3 GetCustomVelocity(AttributesMesh input
        #ifdef HAVE_VFX_MODIFICATION
            , AttributesElement element
        #endif
        )
        {
            VertexDescription vertexDescription = GetVertexDescription(input, _TimeParameters.xyz
        #ifdef HAVE_VFX_MODIFICATION
                , element
        #endif
            );
            return vertexDescription.CustomVelocity;
        }
        #endif
        
        FragInputs BuildFragInputs(VaryingsMeshToPS input)
        {
            FragInputs output;
            ZERO_INITIALIZE(FragInputs, output);
        
            // Init to some default value to make the computer quiet (else it output 'divide by zero' warning even if value is not used).
            // TODO: this is a really poor workaround, but the variable is used in a bunch of places
            // to compute normals which are then passed on elsewhere to compute other values...
            output.tangentToWorld = k_identity3x3;
            output.positionSS = input.positionCS;       // input.positionCS is SV_Position
        
            output.texCoord0 =                  input.texCoord0;
            output.texCoord1 =                  input.texCoord1;
            output.texCoord2 =                  input.texCoord2;
            output.texCoord3 =                  input.texCoord3;
        
        #ifdef HAVE_VFX_MODIFICATION
            // FragInputs from VFX come from two places: Interpolator or CBuffer.
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            // splice point to copy custom interpolator fields from varyings to frag inputs
            
        
            return output;
        }
        
        // existing HDRP code uses the combined function to go directly from packed to frag inputs
        FragInputs UnpackVaryingsMeshToFragInputs(PackedVaryingsMeshToPS input)
        {
            UNITY_SETUP_INSTANCE_ID(input);
        #if defined(HAVE_VFX_MODIFICATION) && defined(UNITY_INSTANCING_ENABLED)
            unity_InstanceID = input.instanceID;
        #endif
            VaryingsMeshToPS unpacked = UnpackVaryingsMeshToPS(input);
            return BuildFragInputs(unpacked);
        }
            SurfaceDescriptionInputs FragInputsToSurfaceDescriptionInputs(FragInputs input, float3 viewWS)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
            #if defined(SHADER_STAGE_RAY_TRACING)
            #else
            #endif
        
        #if UNITY_UV_STARTS_AT_TOP
        #else
        #endif
        
        
            output.uv0 =                                        input.texCoord0;
            output.uv1 =                                        input.texCoord1;
            output.uv2 =                                        input.texCoord2;
            output.uv3 =                                        input.texCoord3;
        
            // splice point to copy frag inputs custom interpolator pack into the SDI
            
        
            return output;
        }
        
            // --------------------------------------------------
            // Build Surface Data (Specific Material)
        
        void BuildSurfaceData(FragInputs fragInputs, inout SurfaceDescription surfaceDescription, float3 V, PositionInputs posInput, out SurfaceData surfaceData)
        {
            // setup defaults -- these are used if the graph doesn't output a value
            ZERO_INITIALIZE(SurfaceData, surfaceData);
        
            // copy across graph values, if defined
            surfaceData.color = surfaceDescription.BaseColor;
        
            #ifdef WRITE_NORMAL_BUFFER
            // When we need to export the normal (in the depth prepass, we write the geometry one)
            surfaceData.normalWS = fragInputs.tangentToWorld[2];
            #endif
        
            #if defined(DEBUG_DISPLAY)
            if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
            {
                // TODO
            }
            #endif
        
            #ifdef _ENABLE_SHADOW_MATTE
        
                #if (SHADERPASS == SHADERPASS_FORWARD_UNLIT) || (SHADERPASS == SHADERPASS_RAYTRACING_GBUFFER) || (SHADERPASS == SHADERPASS_RAYTRACING_INDIRECT) || (SHADERPASS == SHADERPASS_RAYTRACING_FORWARD)
        
                    HDShadowContext shadowContext = InitShadowContext();
        
                    // Evaluate the shadow, the normal is guaranteed if shadow matte is enabled on this shader.
                    float3 shadow3;
                    ShadowLoopMin(shadowContext, posInput, normalize(fragInputs.tangentToWorld[2]), asuint(_ShadowMatteFilter), GetMeshRenderingLightLayer(), shadow3);
        
                    // Compute the average value in the fourth channel
                    float4 shadow = float4(shadow3, dot(shadow3, float3(1.0/3.0, 1.0/3.0, 1.0/3.0)));
        
                    float4 shadowColor = (1.0 - shadow) * surfaceDescription.ShadowTint.rgba;
                    float  localAlpha  = saturate(shadowColor.a + surfaceDescription.Alpha);
        
                    // Keep the nested lerp
                    // With no Color (bsdfData.color.rgb, bsdfData.color.a == 0.0f), just use ShadowColor*Color to avoid a ring of "white" around the shadow
                    // And mix color to consider the Color & ShadowColor alpha (from texture or/and color picker)
                    #ifdef _SURFACE_TYPE_TRANSPARENT
                        surfaceData.color = lerp(shadowColor.rgb * surfaceData.color, lerp(lerp(shadowColor.rgb, surfaceData.color, 1.0 - surfaceDescription.ShadowTint.a), surfaceData.color, shadow.rgb), surfaceDescription.Alpha);
                    #else
                        surfaceData.color = lerp(lerp(shadowColor.rgb, surfaceData.color, 1.0 - surfaceDescription.ShadowTint.a), surfaceData.color, shadow.rgb);
                    #endif
                    localAlpha = ApplyBlendMode(surfaceData.color, localAlpha).a;
        
                    surfaceDescription.Alpha = localAlpha;
        
                #elif SHADERPASS == SHADERPASS_PATH_TRACING
        
                    surfaceData.normalWS = fragInputs.tangentToWorld[2];
                    surfaceData.shadowTint = surfaceDescription.ShadowTint.rgba;
        
                #endif
        
            #endif // _ENABLE_SHADOW_MATTE
        }
        
            // --------------------------------------------------
            // Get Surface And BuiltinData
        
            void GetSurfaceAndBuiltinData(FragInputs fragInputs, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData RAY_TRACING_OPTIONAL_PARAMETERS)
            {
                // Don't dither if displaced tessellation (we're fading out the displacement instead to match the next LOD)
                #if !defined(SHADER_STAGE_RAY_TRACING) && !defined(_TESSELLATION_DISPLACEMENT)
                #ifdef LOD_FADE_CROSSFADE // enable dithering LOD transition if user select CrossFade transition in LOD group
                LODDitheringTransition(ComputeFadeMaskSeed(V, posInput.positionSS), unity_LODFade.x);
                #endif
                #endif
        
                #ifndef SHADER_UNLIT
                #ifdef _DOUBLESIDED_ON
                    float3 doubleSidedConstants = _DoubleSidedConstants.xyz;
                #else
                    float3 doubleSidedConstants = float3(1.0, 1.0, 1.0);
                #endif
        
                ApplyDoubleSidedFlipOrMirror(fragInputs, doubleSidedConstants); // Apply double sided flip on the vertex normal
                #endif // SHADER_UNLIT
        
                SurfaceDescriptionInputs surfaceDescriptionInputs = FragInputsToSurfaceDescriptionInputs(fragInputs, V);
        
                #if defined(HAVE_VFX_MODIFICATION)
                GraphProperties properties;
                ZERO_INITIALIZE(GraphProperties, properties);
        
                GetElementPixelProperties(fragInputs, properties);
        
                SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs, properties);
                #else
                SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);
                #endif
        
                // Perform alpha test very early to save performance (a killed pixel will not sample textures)
                // TODO: split graph evaluation to grab just alpha dependencies first? tricky..
                #ifdef _ALPHATEST_ON
                    float alphaCutoff = surfaceDescription.AlphaClipThreshold;
                    #if SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_PREPASS
                    // The TransparentDepthPrepass is also used with SSR transparent.
                    // If an artists enable transaprent SSR but not the TransparentDepthPrepass itself, then we use AlphaClipThreshold
                    // otherwise if TransparentDepthPrepass is enabled we use AlphaClipThresholdDepthPrepass
                    #elif SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_POSTPASS
                    // DepthPostpass always use its own alpha threshold
                    alphaCutoff = surfaceDescription.AlphaClipThresholdDepthPostpass;
                    #elif (SHADERPASS == SHADERPASS_SHADOWS) || (SHADERPASS == SHADERPASS_RAYTRACING_VISIBILITY)
                    // If use shadow threshold isn't enable we don't allow any test
                    #endif
        
                    GENERIC_ALPHA_TEST(surfaceDescription.Alpha, alphaCutoff);
                #endif
        
                #if !defined(SHADER_STAGE_RAY_TRACING) && _DEPTHOFFSET_ON
                ApplyDepthOffsetPositionInput(V, surfaceDescription.DepthOffset, GetViewForwardDir(), GetWorldToHClipMatrix(), posInput);
                #endif
        
                #ifndef SHADER_UNLIT
                float3 bentNormalWS;
                BuildSurfaceData(fragInputs, surfaceDescription, V, posInput, surfaceData, bentNormalWS);
        
                #ifdef FRAG_INPUTS_USE_TEXCOORD1
                    float4 lightmapTexCoord1 = fragInputs.texCoord1;
                #else
                    float4 lightmapTexCoord1 = float4(0,0,0,0);
                #endif
        
                #ifdef FRAG_INPUTS_USE_TEXCOORD2
                    float4 lightmapTexCoord2 = fragInputs.texCoord2;
                #else
                    float4 lightmapTexCoord2 = float4(0,0,0,0);
                #endif
        
                // Builtin Data
                // For back lighting we use the oposite vertex normal
                InitBuiltinData(posInput, surfaceDescription.Alpha, bentNormalWS, -fragInputs.tangentToWorld[2], lightmapTexCoord1, lightmapTexCoord2, builtinData);
        
                #else
                BuildSurfaceData(fragInputs, surfaceDescription, V, posInput, surfaceData);
        
                ZERO_BUILTIN_INITIALIZE(builtinData); // No call to InitBuiltinData as we don't have any lighting
                builtinData.opacity = surfaceDescription.Alpha;
        
                #if defined(DEBUG_DISPLAY)
                    // Light Layers are currently not used for the Unlit shader (because it is not lit)
                    // But Unlit objects do cast shadows according to their rendering layer mask, which is what we want to
                    // display in the light layers visualization mode, therefore we need the renderingLayers
                    builtinData.renderingLayers = GetMeshRenderingLightLayer();
                #endif
        
                #endif // SHADER_UNLIT
        
                #ifdef _ALPHATEST_ON
                    // Used for sharpening by alpha to mask - Alpha to covertage is only used with depth only and forward pass (no shadow pass, no transparent pass)
                    builtinData.alphaClipTreshold = alphaCutoff;
                #endif
        
                // override sampleBakedGI - not used by Unlit
        
                builtinData.emissiveColor = surfaceDescription.Emission;
        
                // Note this will not fully work on transparent surfaces (can check with _SURFACE_TYPE_TRANSPARENT define)
                // We will always overwrite vt feeback with the nearest. So behind transparent surfaces vt will not be resolved
                // This is a limitation of the current MRT approach.
                #ifdef UNITY_VIRTUAL_TEXTURING
                #endif
        
                #if _DEPTHOFFSET_ON
                builtinData.depthOffset = surfaceDescription.DepthOffset;
                #endif
        
                // TODO: We should generate distortion / distortionBlur for non distortion pass
                #if (SHADERPASS == SHADERPASS_DISTORTION)
                builtinData.distortion = surfaceDescription.Distortion;
                builtinData.distortionBlur = surfaceDescription.DistortionBlur;
                #endif
        
                #ifndef SHADER_UNLIT
                // PostInitBuiltinData call ApplyDebugToBuiltinData
                PostInitBuiltinData(V, posInput, surfaceData, builtinData);
                #else
                ApplyDebugToBuiltinData(builtinData);
                #endif
        
                RAY_TRACING_OPTIONAL_ALPHA_TEST_PASS
            }
        
            // --------------------------------------------------
            // Main
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"
        
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
        
        	#ifdef HAVE_VFX_MODIFICATION
        	#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/VisualEffectVertex.hlsl"
        	#endif
        
            ENDHLSL
        }
        Pass
        {
            Name "MotionVectors"
            Tags
            {
                "LightMode" = "MotionVectors"
            }
        
            // Render State
            Cull [_CullMode]
        ZWrite On
        Stencil
        {
        WriteMask [_StencilWriteMaskMV]
        Ref [_StencilRefMV]
        CompFront Always
        PassFront Replace
        CompBack Always
        PassBack Replace
        }
        AlphaToMask [_AlphaCutoffEnable]
        
            // Debug
            // <None>
        
            // --------------------------------------------------
            // Pass
        
            HLSLPROGRAM
        
            // Pragmas
            #pragma multi_compile _ DOTS_INSTANCING_ON
        #pragma instancing_options renderinglayer
        #pragma target 4.5
        #pragma vertex Vert
        #pragma fragment Frag
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
        #pragma multi_compile_instancing
        
            // Keywords
            #pragma shader_feature_local _ _ALPHATEST_ON
        #pragma multi_compile_fragment _ WRITE_MSAA_DEPTH
        #pragma shader_feature _ _SURFACE_TYPE_TRANSPARENT
        #pragma shader_feature_local _BLENDMODE_OFF _BLENDMODE_ALPHA _BLENDMODE_ADD _BLENDMODE_PRE_MULTIPLY
        #pragma shader_feature_local _ _ADD_PRECOMPUTED_VELOCITY
        #pragma shader_feature_local _ _TRANSPARENT_WRITES_MOTION_VEC
        #pragma shader_feature_local_fragment _ _ENABLE_FOG_ON_TRANSPARENT
            // GraphKeywords: <None>
        
            // For custom interpolators to inject a substruct definition before FragInputs definition,
            // allowing for FragInputs to capture CI's intended for ShaderGraph's SDI.
            struct CustomInterpolators
        {
        };
        #define USE_CUSTOMINTERP_SUBSTRUCT
        
        
        
            // TODO: Merge FragInputsVFX substruct with CustomInterpolators.
        	#ifdef HAVE_VFX_MODIFICATION
        	struct FragInputsVFX
            {
                /* WARNING: $splice Could not find named fragment 'FragInputsVFX' */
            };
            #endif
        
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GeometricTools.hlsl" // Required by Tessellation.hlsl
        	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Tessellation.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl" // Required to be include before we include properties as it define DECLARE_STACK_CB
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphHeader.hlsl" // Need to be here for Gradient struct definition
        
            // --------------------------------------------------
            // Defines
        
            // Attribute
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define ATTRIBUTES_NEED_TEXCOORD2
            #define ATTRIBUTES_NEED_TEXCOORD3
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_TANGENT_TO_WORLD
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_TEXCOORD1
            #define VARYINGS_NEED_TEXCOORD2
            #define VARYINGS_NEED_TEXCOORD3
        
            #define HAVE_MESH_MODIFICATION
        
            //Strip down the FragInputs.hlsl (on graphics), so we can only optimize the interpolators we use.
            //if by accident something requests contents of FragInputs.hlsl, it will be caught as a compiler error
            //Frag inputs stripping is only enabled when FRAG_INPUTS_ENABLE_STRIPPING is set
            #if !defined(SHADER_STAGE_RAY_TRACING) && SHADERPASS != SHADERPASS_RAYTRACING_GBUFFER && SHADERPASS != SHADERPASS_FULL_SCREEN_DEBUG
            #define FRAG_INPUTS_ENABLE_STRIPPING
            #endif
            #define FRAG_INPUTS_USE_TEXCOORD0
            #define FRAG_INPUTS_USE_TEXCOORD1
            #define FRAG_INPUTS_USE_TEXCOORD2
            #define FRAG_INPUTS_USE_TEXCOORD3
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
        
        
        
            #define SHADERPASS SHADERPASS_MOTION_VECTORS
        
        
            // Following two define are a workaround introduce in 10.1.x for RaytracingQualityNode
            // The ShaderGraph don't support correctly migration of this node as it serialize all the node data
            // in the json file making it impossible to uprgrade. Until we get a fix, we do a workaround here
            // to still allow us to rename the field and keyword of this node without breaking existing code.
            #ifdef RAYTRACING_SHADER_GRAPH_DEFAULT
            #define RAYTRACING_SHADER_GRAPH_HIGH
            #endif
        
            #ifdef RAYTRACING_SHADER_GRAPH_RAYTRACED
            #define RAYTRACING_SHADER_GRAPH_LOW
            #endif
            // end
        
            #ifndef SHADER_UNLIT
            // We need isFrontFace when using double sided - it is not required for unlit as in case of unlit double sided only drive the cullmode
            // VARYINGS_NEED_CULLFACE can be define by VaryingsMeshToPS.FaceSign input if a IsFrontFace Node is included in the shader graph.
            #if defined(_DOUBLESIDED_ON) && !defined(VARYINGS_NEED_CULLFACE)
                #define VARYINGS_NEED_CULLFACE
            #endif
            #endif
        
            // Specific Material Define
        // Setup a define to say we are an unlit shader
        #define SHADER_UNLIT
        
        // Following Macro are only used by Unlit material
        #if defined(_ENABLE_SHADOW_MATTE) && (SHADERPASS == SHADERPASS_FORWARD_UNLIT || SHADERPASS == SHADERPASS_PATH_TRACING)
        #define LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
        #define HAS_LIGHTLOOP
        #endif
            // Caution: we can use the define SHADER_UNLIT onlit after the above Material include as it is the Unlit template who define it
        
            // To handle SSR on transparent correctly with a possibility to enable/disable it per framesettings
            // we should have a code like this:
            // if !defined(_DISABLE_SSR_TRANSPARENT)
            // pragma multi_compile _ WRITE_NORMAL_BUFFER
            // endif
            // i.e we enable the multicompile only if we can receive SSR or not, and then C# code drive
            // it based on if SSR transparent in frame settings and not (and stripper can strip it).
            // this is currently not possible with our current preprocessor as _DISABLE_SSR_TRANSPARENT is a keyword not a define
            // so instead we used this and chose to pay the extra cost of normal write even if SSR transaprent is disabled.
            // Ideally the shader graph generator should handle it but condition below can't be handle correctly for now.
            #if SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_PREPASS
            #if !defined(_DISABLE_SSR_TRANSPARENT) && !defined(SHADER_UNLIT)
                #define WRITE_NORMAL_BUFFER
            #endif
            #endif
        
            #ifndef DEBUG_DISPLAY
                // In case of opaque we don't want to perform the alpha test, it is done in depth prepass and we use depth equal for ztest (setup from UI)
                // Don't do it with debug display mode as it is possible there is no depth prepass in this case
                #if !defined(_SURFACE_TYPE_TRANSPARENT)
                    #if SHADERPASS == SHADERPASS_FORWARD
                    #define SHADERPASS_FORWARD_BYPASS_ALPHA_TEST
                    #elif SHADERPASS == SHADERPASS_GBUFFER
                    #define SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST
                    #endif
                #endif
            #endif
        
            // Define _DEFERRED_CAPABLE_MATERIAL for shader capable to run in deferred pass
            #if defined(SHADER_LIT) && !defined(_SURFACE_TYPE_TRANSPARENT)
                #define _DEFERRED_CAPABLE_MATERIAL
            #endif
        
            // Translate transparent motion vector define
            #if defined(_TRANSPARENT_WRITES_MOTION_VEC) && defined(_SURFACE_TYPE_TRANSPARENT)
                #define _WRITE_TRANSPARENT_MOTION_VECTOR
            #endif
        
            // -- Graph Properties
            CBUFFER_START(UnityPerMaterial)
        float4 _TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1_TexelSize;
        float4 _Direction_TexelSize;
        float2 _TextureSize;
        float _RotateUV;
        float Boolean_F2B1E91A;
        float4 _EmissionColor;
        float _UseShadowThreshold;
        float4 _DoubleSidedConstants;
        float _BlendMode;
        float _EnableBlendModePreserveSpecularLighting;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1);
        SAMPLER(sampler_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1);
        TEXTURE2D(_Direction);
        SAMPLER(sampler_Direction);
        
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
        
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
        
            // Includes
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl"
        	// GraphIncludes: <None>
        
            // --------------------------------------------------
            // Structs and Packing
        
            struct AttributesMesh
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
             float4 uv1 : TEXCOORD1;
             float4 uv2 : TEXCOORD2;
             float4 uv3 : TEXCOORD3;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct VaryingsMeshToPS
        {
            SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
             float3 positionRWS;
             float3 normalWS;
             float4 tangentWS;
             float4 texCoord0;
             float4 texCoord1;
             float4 texCoord2;
             float4 texCoord3;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
        };
        struct SurfaceDescriptionInputs
        {
             float4 uv0;
             float4 uv1;
             float4 uv2;
             float4 uv3;
        };
        struct PackedVaryingsMeshToPS
        {
            SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
             float3 interp0 : INTERP0;
             float3 interp1 : INTERP1;
             float4 interp2 : INTERP2;
             float4 interp3 : INTERP3;
             float4 interp4 : INTERP4;
             float4 interp5 : INTERP5;
             float4 interp6 : INTERP6;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
        };
        
            PackedVaryingsMeshToPS PackVaryingsMeshToPS (VaryingsMeshToPS input)
        {
            PackedVaryingsMeshToPS output;
            ZERO_INITIALIZE(PackedVaryingsMeshToPS, output);
            output.positionCS = input.positionCS;
            output.interp0.xyz =  input.positionRWS;
            output.interp1.xyz =  input.normalWS;
            output.interp2.xyzw =  input.tangentWS;
            output.interp3.xyzw =  input.texCoord0;
            output.interp4.xyzw =  input.texCoord1;
            output.interp5.xyzw =  input.texCoord2;
            output.interp6.xyzw =  input.texCoord3;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            return output;
        }
        
        VaryingsMeshToPS UnpackVaryingsMeshToPS (PackedVaryingsMeshToPS input)
        {
            VaryingsMeshToPS output;
            output.positionCS = input.positionCS;
            output.positionRWS = input.interp0.xyz;
            output.normalWS = input.interp1.xyz;
            output.tangentWS = input.interp2.xyzw;
            output.texCoord0 = input.interp3.xyzw;
            output.texCoord1 = input.interp4.xyzw;
            output.texCoord2 = input.interp5.xyzw;
            output.texCoord3 = input.interp6.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            return output;
        }
        
        
            // --------------------------------------------------
            // Graph
        
        
            // Graph Functions
            
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A / B;
        }
        
        void Unity_Fraction_float2(float2 In, out float2 Out)
        {
            Out = frac(In);
        }
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A * B;
        }
        
        void Unity_Subtract_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A - B;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_Comparison_Greater_float(float A, float B, out float Out)
        {
            Out = A > B ? 1 : 0;
        }
        
        void Unity_Divide_float(float A, float B, out float Out)
        {
            Out = A / B;
        }
        
        void Unity_Arctangent_float(float In, out float Out)
        {
            Out = atan(In);
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Branch_float(float Predicate, float True, float False, out float Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Rotate_Radians_float(float2 UV, float2 Center, float Rotation, out float2 Out)
        {
            //rotation matrix
            UV -= Center;
            float s = sin(Rotation);
            float c = cos(Rotation);
        
            //center rotation matrix
            float2x2 rMatrix = float2x2(c, -s, s, c);
            rMatrix *= 0.5;
            rMatrix += 0.5;
            rMatrix = rMatrix*2 - 1;
        
            //multiply the UVs by the rotation matrix
            UV.xy = mul(UV.xy, rMatrix);
            UV += Center;
        
            Out = UV;
        }
        
        void Unity_Distance_float2(float2 A, float2 B, out float Out)
        {
            Out = distance(A, B);
        }
        
        void Unity_Clamp_float(float In, float Min, float Max, out float Out)
        {
            Out = clamp(In, Min, Max);
        }
        
        void Unity_Subtract_float(float A, float B, out float Out)
        {
            Out = A - B;
        }
        
        void Unity_Absolute_float(float In, out float Out)
        {
            Out = abs(In);
        }
        
        void Unity_Power_float(float A, float B, out float Out)
        {
            Out = pow(A, B);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A * B;
        }
        
            // Graph Vertex
            struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
            // Graph Pixel
            struct SurfaceDescription
        {
            float3 BaseColor;
            float3 Emission;
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0 = UnityBuildTexture2DStructNoScale(_Direction);
            float4 _UV_7c8a457b34527181924b2b4a71350de9_Out_0 = IN.uv0;
            float2 _TilingAndOffset_67d58b7a8092828aaece7f3acfe6e8a9_Out_3;
            Unity_TilingAndOffset_float((_UV_7c8a457b34527181924b2b4a71350de9_Out_0.xy), float2 (2, 2), float2 (0, 0), _TilingAndOffset_67d58b7a8092828aaece7f3acfe6e8a9_Out_3);
            float2 _Vector2_9a3e446bd0dfc3868e3db6f9bf101ead_Out_0 = float2(1, 1);
            float2 _Vector2_8355e4dbd796b188ba1e500dcabf0c0f_Out_0 = float2(30, 30);
            float2 _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2;
            Unity_Divide_float2(_Vector2_9a3e446bd0dfc3868e3db6f9bf101ead_Out_0, _Vector2_8355e4dbd796b188ba1e500dcabf0c0f_Out_0, _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2);
            float2 _Divide_5d0f49ab4bcab689894f976217114a79_Out_2;
            Unity_Divide_float2(_TilingAndOffset_67d58b7a8092828aaece7f3acfe6e8a9_Out_3, _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2, _Divide_5d0f49ab4bcab689894f976217114a79_Out_2);
            float2 _Fraction_e4f0f05fb76fd3809f5ec855b54a4595_Out_1;
            Unity_Fraction_float2(_Divide_5d0f49ab4bcab689894f976217114a79_Out_2, _Fraction_e4f0f05fb76fd3809f5ec855b54a4595_Out_1);
            float2 _Multiply_cbcb0de832b1d08288916e46aa8d4df2_Out_2;
            Unity_Multiply_float2_float2(_Fraction_e4f0f05fb76fd3809f5ec855b54a4595_Out_1, _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2, _Multiply_cbcb0de832b1d08288916e46aa8d4df2_Out_2);
            float2 _Vector2_e6d2595367ed0380b42c7a74855838a5_Out_0 = float2(246, 246);
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_Width_0 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.z;
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_Height_2 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.w;
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_TexelWidth_3 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.x;
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_TexelHeight_4 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.y;
            float2 _Vector2_e8728bfbf194e5818f010927b4ba8d79_Out_0 = float2(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Width_0, _TexelSize_18bb9f416e386f87bee91aa46804bc56_Height_2);
            float2 _Multiply_561a2890964f4081a8cf5d16bccb889f_Out_2;
            Unity_Multiply_float2_float2(_Vector2_e6d2595367ed0380b42c7a74855838a5_Out_0, _Vector2_e8728bfbf194e5818f010927b4ba8d79_Out_0, _Multiply_561a2890964f4081a8cf5d16bccb889f_Out_2);
            float2 _Vector2_97c81bcbfd26f08385a101ac3da412f3_Out_0 = float2(10, 10);
            float2 _Multiply_bccda05b3809138d8da15484dd6a7b4e_Out_2;
            Unity_Multiply_float2_float2(_Vector2_e8728bfbf194e5818f010927b4ba8d79_Out_0, _Vector2_97c81bcbfd26f08385a101ac3da412f3_Out_0, _Multiply_bccda05b3809138d8da15484dd6a7b4e_Out_2);
            float2 _Subtract_710d4672e66f7780ad90ca6e19a733bd_Out_2;
            Unity_Subtract_float2(_Multiply_561a2890964f4081a8cf5d16bccb889f_Out_2, _Multiply_bccda05b3809138d8da15484dd6a7b4e_Out_2, _Subtract_710d4672e66f7780ad90ca6e19a733bd_Out_2);
            float2 _Multiply_cd542324eceadd8daaca18ab78d3fbf6_Out_2;
            Unity_Multiply_float2_float2(_Multiply_cbcb0de832b1d08288916e46aa8d4df2_Out_2, _Subtract_710d4672e66f7780ad90ca6e19a733bd_Out_2, _Multiply_cd542324eceadd8daaca18ab78d3fbf6_Out_2);
            float2 _Vector2_df86361e9c36e7828e3097cc1fa8a75b_Out_0 = float2(0.1, 0.1);
            float2 _Multiply_8e351d40d36634829f2bb0905b9b9342_Out_2;
            Unity_Multiply_float2_float2(_Multiply_cd542324eceadd8daaca18ab78d3fbf6_Out_2, _Vector2_df86361e9c36e7828e3097cc1fa8a75b_Out_0, _Multiply_8e351d40d36634829f2bb0905b9b9342_Out_2);
            float4 _UV_1ca38e193be034888b8a707af9d47210_Out_0 = IN.uv3;
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_R_1 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[0];
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[1];
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_B_3 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[2];
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_A_4 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[3];
            float _Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2;
            Unity_Multiply_float_float(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2, -1, _Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2);
            float _Comparison_e67d9847aa86858490dd0c37e68e7505_Out_2;
            Unity_Comparison_Greater_float(_Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2, 0, _Comparison_e67d9847aa86858490dd0c37e68e7505_Out_2);
            float _Divide_2b884a92fd783d82a9012917c2b83115_Out_2;
            Unity_Divide_float(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_R_1, _Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2, _Divide_2b884a92fd783d82a9012917c2b83115_Out_2);
            float _Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1;
            Unity_Arctangent_float(_Divide_2b884a92fd783d82a9012917c2b83115_Out_2, _Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1);
            float _Add_cf6b7aee4a487f8ca16b9171b64a8f0d_Out_2;
            Unity_Add_float(_Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1, 3.141592, _Add_cf6b7aee4a487f8ca16b9171b64a8f0d_Out_2);
            float _Branch_9de100b31215ba89bb19e5b89012a25a_Out_3;
            Unity_Branch_float(_Comparison_e67d9847aa86858490dd0c37e68e7505_Out_2, _Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1, _Add_cf6b7aee4a487f8ca16b9171b64a8f0d_Out_2, _Branch_9de100b31215ba89bb19e5b89012a25a_Out_3);
            float2 _Rotate_ff1406ef744b188ca79c6d3bf0b8bbd7_Out_3;
            Unity_Rotate_Radians_float(_Multiply_8e351d40d36634829f2bb0905b9b9342_Out_2, float2 (0.5, 0.5), _Branch_9de100b31215ba89bb19e5b89012a25a_Out_3, _Rotate_ff1406ef744b188ca79c6d3bf0b8bbd7_Out_3);
            #if defined(SHADER_API_GLES) && (SHADER_TARGET < 30)
              float4 _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0 = float4(0.0f, 0.0f, 0.0f, 1.0f);
            #else
              float4 _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0 = SAMPLE_TEXTURE2D_LOD(_Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0.tex, _Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0.samplerstate, _Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0.GetTransformedUV(_Rotate_ff1406ef744b188ca79c6d3bf0b8bbd7_Out_3), 0);
            #endif
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_R_5 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.r;
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_G_6 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.g;
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_B_7 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.b;
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_A_8 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.a;
            float _Comparison_1a93f0ca7c913a8f81659b20b8d59f2b_Out_2;
            Unity_Comparison_Greater_float(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2, 0.001, _Comparison_1a93f0ca7c913a8f81659b20b8d59f2b_Out_2);
            float3 _Vector3_37ab63b41d52bb8d8eba9088ed71bc11_Out_0 = float3(0, 0, 0);
            float2 _Vector2_4aeecd1cb271dd8e96c2fb12309b9dd0_Out_0 = float2(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_R_1, _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2);
            float _Distance_47e812ff6269a181b6e0faa017faee3b_Out_2;
            Unity_Distance_float2(_Vector2_4aeecd1cb271dd8e96c2fb12309b9dd0_Out_0, float2(0, 0), _Distance_47e812ff6269a181b6e0faa017faee3b_Out_2);
            float _Multiply_e5102f48c5e759879293e06062894e0c_Out_2;
            Unity_Multiply_float_float(_Distance_47e812ff6269a181b6e0faa017faee3b_Out_2, 1.6, _Multiply_e5102f48c5e759879293e06062894e0c_Out_2);
            float _Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3;
            Unity_Clamp_float(_Multiply_e5102f48c5e759879293e06062894e0c_Out_2, 0, 1, _Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3);
            float _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2;
            Unity_Subtract_float(1, _Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3, _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2);
            float3 _Vector3_aeff4a868e81c68f85dfe4c9662da9ce_Out_0 = float3(1, _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2, 0);
            float _Absolute_5573b6b2c1616e819aa4eeb67caec457_Out_1;
            Unity_Absolute_float(_Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3, _Absolute_5573b6b2c1616e819aa4eeb67caec457_Out_1);
            float _Power_33dedbf009cacf848c403a58667a66c6_Out_2;
            Unity_Power_float(_Absolute_5573b6b2c1616e819aa4eeb67caec457_Out_1, 0.16, _Power_33dedbf009cacf848c403a58667a66c6_Out_2);
            float _Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3;
            Unity_Clamp_float(_Power_33dedbf009cacf848c403a58667a66c6_Out_2, 0, 1, _Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3);
            float3 _Lerp_e6000a54e83a8f8e8c5bc16419b360ca_Out_3;
            Unity_Lerp_float3(_Vector3_37ab63b41d52bb8d8eba9088ed71bc11_Out_0, _Vector3_aeff4a868e81c68f85dfe4c9662da9ce_Out_0, (_Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3.xxx), _Lerp_e6000a54e83a8f8e8c5bc16419b360ca_Out_3);
            float3 _Vector3_484f28086891e98d9589d1f65774f6f4_Out_0 = float3(0, _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2, 1);
            float3 _Lerp_93469a90f98ccf80a7121c0b2bf05f48_Out_3;
            Unity_Lerp_float3(_Vector3_37ab63b41d52bb8d8eba9088ed71bc11_Out_0, _Vector3_484f28086891e98d9589d1f65774f6f4_Out_0, (_Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3.xxx), _Lerp_93469a90f98ccf80a7121c0b2bf05f48_Out_3);
            float3 _Branch_365bf259ae64c386a18e2d6997727100_Out_3;
            Unity_Branch_float3(_Comparison_1a93f0ca7c913a8f81659b20b8d59f2b_Out_2, _Lerp_e6000a54e83a8f8e8c5bc16419b360ca_Out_3, _Lerp_93469a90f98ccf80a7121c0b2bf05f48_Out_3, _Branch_365bf259ae64c386a18e2d6997727100_Out_3);
            float3 _Multiply_0563147e8bc9868b8c2a4f2c442c349f_Out_2;
            Unity_Multiply_float3_float3((_SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_A_8.xxx), _Branch_365bf259ae64c386a18e2d6997727100_Out_3, _Multiply_0563147e8bc9868b8c2a4f2c442c349f_Out_2);
            surface.BaseColor = _Multiply_0563147e8bc9868b8c2a4f2c442c349f_Out_2;
            surface.Emission = float3(0, 0, 0);
            surface.Alpha = 1;
            surface.AlphaClipThreshold = 0.5;
            return surface;
        }
        
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES AttributesMesh
            #define VaryingsMeshType VaryingsMeshToPS
            #define VFX_SRP_VARYINGS VaryingsMeshType
            #define VFX_SRP_SURFACE_INPUTS FragInputs
            #endif
            
        VertexDescriptionInputs AttributesMeshToVertexDescriptionInputs(AttributesMesh input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
        
            return output;
        }
        
        VertexDescription GetVertexDescription(AttributesMesh input, float3 timeParameters
        #ifdef HAVE_VFX_MODIFICATION
            , AttributesElement element
        #endif
        )
        {
            // build graph inputs
            VertexDescriptionInputs vertexDescriptionInputs = AttributesMeshToVertexDescriptionInputs(input);
            // Override time parameters with used one (This is required to correctly handle motion vector for vertex animation based on time)
        
            // evaluate vertex graph
        #ifdef HAVE_VFX_MODIFICATION
            GraphProperties properties;
            ZERO_INITIALIZE(GraphProperties, properties);
        
            // Fetch the vertex graph properties for the particle instance.
            GetElementVertexProperties(element, properties);
        
            VertexDescription vertexDescription = VertexDescriptionFunction(vertexDescriptionInputs, properties);
        #else
            VertexDescription vertexDescription = VertexDescriptionFunction(vertexDescriptionInputs);
        #endif
            return vertexDescription;
        
        }
        
        AttributesMesh ApplyMeshModification(AttributesMesh input, float3 timeParameters
        #ifdef USE_CUSTOMINTERP_SUBSTRUCT
            #ifdef TESSELLATION_ON
            , inout VaryingsMeshToDS varyings
            #else
            , inout VaryingsMeshToPS varyings
            #endif
        #endif
        #ifdef HAVE_VFX_MODIFICATION
                , AttributesElement element
        #endif
            )
        {
            VertexDescription vertexDescription = GetVertexDescription(input, timeParameters
        #ifdef HAVE_VFX_MODIFICATION
                , element
        #endif
            );
        
            // copy graph output to the results
            input.positionOS = vertexDescription.Position;
            input.normalOS = vertexDescription.Normal;
            input.tangentOS.xyz = vertexDescription.Tangent;
        
            
        
            return input;
        }
        
        #if defined(_ADD_CUSTOM_VELOCITY) // For shader graph custom velocity
        // Return precomputed Velocity in object space
        float3 GetCustomVelocity(AttributesMesh input
        #ifdef HAVE_VFX_MODIFICATION
            , AttributesElement element
        #endif
        )
        {
            VertexDescription vertexDescription = GetVertexDescription(input, _TimeParameters.xyz
        #ifdef HAVE_VFX_MODIFICATION
                , element
        #endif
            );
            return vertexDescription.CustomVelocity;
        }
        #endif
        
        FragInputs BuildFragInputs(VaryingsMeshToPS input)
        {
            FragInputs output;
            ZERO_INITIALIZE(FragInputs, output);
        
            // Init to some default value to make the computer quiet (else it output 'divide by zero' warning even if value is not used).
            // TODO: this is a really poor workaround, but the variable is used in a bunch of places
            // to compute normals which are then passed on elsewhere to compute other values...
            output.tangentToWorld = k_identity3x3;
            output.positionSS = input.positionCS;       // input.positionCS is SV_Position
        
            output.positionRWS =                input.positionRWS;
            output.tangentToWorld =             BuildTangentToWorld(input.tangentWS, input.normalWS);
            output.texCoord0 =                  input.texCoord0;
            output.texCoord1 =                  input.texCoord1;
            output.texCoord2 =                  input.texCoord2;
            output.texCoord3 =                  input.texCoord3;
        
        #ifdef HAVE_VFX_MODIFICATION
            // FragInputs from VFX come from two places: Interpolator or CBuffer.
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            // splice point to copy custom interpolator fields from varyings to frag inputs
            
        
            return output;
        }
        
        // existing HDRP code uses the combined function to go directly from packed to frag inputs
        FragInputs UnpackVaryingsMeshToFragInputs(PackedVaryingsMeshToPS input)
        {
            UNITY_SETUP_INSTANCE_ID(input);
        #if defined(HAVE_VFX_MODIFICATION) && defined(UNITY_INSTANCING_ENABLED)
            unity_InstanceID = input.instanceID;
        #endif
            VaryingsMeshToPS unpacked = UnpackVaryingsMeshToPS(input);
            return BuildFragInputs(unpacked);
        }
            SurfaceDescriptionInputs FragInputsToSurfaceDescriptionInputs(FragInputs input, float3 viewWS)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
            #if defined(SHADER_STAGE_RAY_TRACING)
            #else
            #endif
        
        #if UNITY_UV_STARTS_AT_TOP
        #else
        #endif
        
        
            output.uv0 =                                        input.texCoord0;
            output.uv1 =                                        input.texCoord1;
            output.uv2 =                                        input.texCoord2;
            output.uv3 =                                        input.texCoord3;
        
            // splice point to copy frag inputs custom interpolator pack into the SDI
            
        
            return output;
        }
        
            // --------------------------------------------------
            // Build Surface Data (Specific Material)
        
        void BuildSurfaceData(FragInputs fragInputs, inout SurfaceDescription surfaceDescription, float3 V, PositionInputs posInput, out SurfaceData surfaceData)
        {
            // setup defaults -- these are used if the graph doesn't output a value
            ZERO_INITIALIZE(SurfaceData, surfaceData);
        
            // copy across graph values, if defined
            surfaceData.color = surfaceDescription.BaseColor;
        
            #ifdef WRITE_NORMAL_BUFFER
            // When we need to export the normal (in the depth prepass, we write the geometry one)
            surfaceData.normalWS = fragInputs.tangentToWorld[2];
            #endif
        
            #if defined(DEBUG_DISPLAY)
            if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
            {
                // TODO
            }
            #endif
        
            #ifdef _ENABLE_SHADOW_MATTE
        
                #if (SHADERPASS == SHADERPASS_FORWARD_UNLIT) || (SHADERPASS == SHADERPASS_RAYTRACING_GBUFFER) || (SHADERPASS == SHADERPASS_RAYTRACING_INDIRECT) || (SHADERPASS == SHADERPASS_RAYTRACING_FORWARD)
        
                    HDShadowContext shadowContext = InitShadowContext();
        
                    // Evaluate the shadow, the normal is guaranteed if shadow matte is enabled on this shader.
                    float3 shadow3;
                    ShadowLoopMin(shadowContext, posInput, normalize(fragInputs.tangentToWorld[2]), asuint(_ShadowMatteFilter), GetMeshRenderingLightLayer(), shadow3);
        
                    // Compute the average value in the fourth channel
                    float4 shadow = float4(shadow3, dot(shadow3, float3(1.0/3.0, 1.0/3.0, 1.0/3.0)));
        
                    float4 shadowColor = (1.0 - shadow) * surfaceDescription.ShadowTint.rgba;
                    float  localAlpha  = saturate(shadowColor.a + surfaceDescription.Alpha);
        
                    // Keep the nested lerp
                    // With no Color (bsdfData.color.rgb, bsdfData.color.a == 0.0f), just use ShadowColor*Color to avoid a ring of "white" around the shadow
                    // And mix color to consider the Color & ShadowColor alpha (from texture or/and color picker)
                    #ifdef _SURFACE_TYPE_TRANSPARENT
                        surfaceData.color = lerp(shadowColor.rgb * surfaceData.color, lerp(lerp(shadowColor.rgb, surfaceData.color, 1.0 - surfaceDescription.ShadowTint.a), surfaceData.color, shadow.rgb), surfaceDescription.Alpha);
                    #else
                        surfaceData.color = lerp(lerp(shadowColor.rgb, surfaceData.color, 1.0 - surfaceDescription.ShadowTint.a), surfaceData.color, shadow.rgb);
                    #endif
                    localAlpha = ApplyBlendMode(surfaceData.color, localAlpha).a;
        
                    surfaceDescription.Alpha = localAlpha;
        
                #elif SHADERPASS == SHADERPASS_PATH_TRACING
        
                    surfaceData.normalWS = fragInputs.tangentToWorld[2];
                    surfaceData.shadowTint = surfaceDescription.ShadowTint.rgba;
        
                #endif
        
            #endif // _ENABLE_SHADOW_MATTE
        }
        
            // --------------------------------------------------
            // Get Surface And BuiltinData
        
            void GetSurfaceAndBuiltinData(FragInputs fragInputs, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData RAY_TRACING_OPTIONAL_PARAMETERS)
            {
                // Don't dither if displaced tessellation (we're fading out the displacement instead to match the next LOD)
                #if !defined(SHADER_STAGE_RAY_TRACING) && !defined(_TESSELLATION_DISPLACEMENT)
                #ifdef LOD_FADE_CROSSFADE // enable dithering LOD transition if user select CrossFade transition in LOD group
                LODDitheringTransition(ComputeFadeMaskSeed(V, posInput.positionSS), unity_LODFade.x);
                #endif
                #endif
        
                #ifndef SHADER_UNLIT
                #ifdef _DOUBLESIDED_ON
                    float3 doubleSidedConstants = _DoubleSidedConstants.xyz;
                #else
                    float3 doubleSidedConstants = float3(1.0, 1.0, 1.0);
                #endif
        
                ApplyDoubleSidedFlipOrMirror(fragInputs, doubleSidedConstants); // Apply double sided flip on the vertex normal
                #endif // SHADER_UNLIT
        
                SurfaceDescriptionInputs surfaceDescriptionInputs = FragInputsToSurfaceDescriptionInputs(fragInputs, V);
        
                #if defined(HAVE_VFX_MODIFICATION)
                GraphProperties properties;
                ZERO_INITIALIZE(GraphProperties, properties);
        
                GetElementPixelProperties(fragInputs, properties);
        
                SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs, properties);
                #else
                SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);
                #endif
        
                // Perform alpha test very early to save performance (a killed pixel will not sample textures)
                // TODO: split graph evaluation to grab just alpha dependencies first? tricky..
                #ifdef _ALPHATEST_ON
                    float alphaCutoff = surfaceDescription.AlphaClipThreshold;
                    #if SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_PREPASS
                    // The TransparentDepthPrepass is also used with SSR transparent.
                    // If an artists enable transaprent SSR but not the TransparentDepthPrepass itself, then we use AlphaClipThreshold
                    // otherwise if TransparentDepthPrepass is enabled we use AlphaClipThresholdDepthPrepass
                    #elif SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_POSTPASS
                    // DepthPostpass always use its own alpha threshold
                    alphaCutoff = surfaceDescription.AlphaClipThresholdDepthPostpass;
                    #elif (SHADERPASS == SHADERPASS_SHADOWS) || (SHADERPASS == SHADERPASS_RAYTRACING_VISIBILITY)
                    // If use shadow threshold isn't enable we don't allow any test
                    #endif
        
                    GENERIC_ALPHA_TEST(surfaceDescription.Alpha, alphaCutoff);
                #endif
        
                #if !defined(SHADER_STAGE_RAY_TRACING) && _DEPTHOFFSET_ON
                ApplyDepthOffsetPositionInput(V, surfaceDescription.DepthOffset, GetViewForwardDir(), GetWorldToHClipMatrix(), posInput);
                #endif
        
                #ifndef SHADER_UNLIT
                float3 bentNormalWS;
                BuildSurfaceData(fragInputs, surfaceDescription, V, posInput, surfaceData, bentNormalWS);
        
                #ifdef FRAG_INPUTS_USE_TEXCOORD1
                    float4 lightmapTexCoord1 = fragInputs.texCoord1;
                #else
                    float4 lightmapTexCoord1 = float4(0,0,0,0);
                #endif
        
                #ifdef FRAG_INPUTS_USE_TEXCOORD2
                    float4 lightmapTexCoord2 = fragInputs.texCoord2;
                #else
                    float4 lightmapTexCoord2 = float4(0,0,0,0);
                #endif
        
                // Builtin Data
                // For back lighting we use the oposite vertex normal
                InitBuiltinData(posInput, surfaceDescription.Alpha, bentNormalWS, -fragInputs.tangentToWorld[2], lightmapTexCoord1, lightmapTexCoord2, builtinData);
        
                #else
                BuildSurfaceData(fragInputs, surfaceDescription, V, posInput, surfaceData);
        
                ZERO_BUILTIN_INITIALIZE(builtinData); // No call to InitBuiltinData as we don't have any lighting
                builtinData.opacity = surfaceDescription.Alpha;
        
                #if defined(DEBUG_DISPLAY)
                    // Light Layers are currently not used for the Unlit shader (because it is not lit)
                    // But Unlit objects do cast shadows according to their rendering layer mask, which is what we want to
                    // display in the light layers visualization mode, therefore we need the renderingLayers
                    builtinData.renderingLayers = GetMeshRenderingLightLayer();
                #endif
        
                #endif // SHADER_UNLIT
        
                #ifdef _ALPHATEST_ON
                    // Used for sharpening by alpha to mask - Alpha to covertage is only used with depth only and forward pass (no shadow pass, no transparent pass)
                    builtinData.alphaClipTreshold = alphaCutoff;
                #endif
        
                // override sampleBakedGI - not used by Unlit
        
                builtinData.emissiveColor = surfaceDescription.Emission;
        
                // Note this will not fully work on transparent surfaces (can check with _SURFACE_TYPE_TRANSPARENT define)
                // We will always overwrite vt feeback with the nearest. So behind transparent surfaces vt will not be resolved
                // This is a limitation of the current MRT approach.
                #ifdef UNITY_VIRTUAL_TEXTURING
                #endif
        
                #if _DEPTHOFFSET_ON
                builtinData.depthOffset = surfaceDescription.DepthOffset;
                #endif
        
                // TODO: We should generate distortion / distortionBlur for non distortion pass
                #if (SHADERPASS == SHADERPASS_DISTORTION)
                builtinData.distortion = surfaceDescription.Distortion;
                builtinData.distortionBlur = surfaceDescription.DistortionBlur;
                #endif
        
                #ifndef SHADER_UNLIT
                // PostInitBuiltinData call ApplyDebugToBuiltinData
                PostInitBuiltinData(V, posInput, surfaceData, builtinData);
                #else
                ApplyDebugToBuiltinData(builtinData);
                #endif
        
                RAY_TRACING_OPTIONAL_ALPHA_TEST_PASS
            }
        
            // --------------------------------------------------
            // Main
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassMotionVectors.hlsl"
        
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
        
        	#ifdef HAVE_VFX_MODIFICATION
        	#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/VisualEffectVertex.hlsl"
        	#endif
        
            ENDHLSL
        }
        Pass
        {
            Name "DepthForwardOnly"
            Tags
            {
                "LightMode" = "DepthForwardOnly"
            }
        
            // Render State
            Cull [_CullMode]
        ZWrite On
        Stencil
        {
        WriteMask [_StencilWriteMaskDepth]
        Ref [_StencilRefDepth]
        CompFront Always
        PassFront Replace
        CompBack Always
        PassBack Replace
        }
        AlphaToMask [_AlphaCutoffEnable]
        
            // Debug
            // <None>
        
            // --------------------------------------------------
            // Pass
        
            HLSLPROGRAM
        
            // Pragmas
            #pragma multi_compile _ DOTS_INSTANCING_ON
        #pragma instancing_options renderinglayer
        #pragma target 4.5
        #pragma vertex Vert
        #pragma fragment Frag
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
        #pragma multi_compile_instancing
        
            // Keywords
            #pragma shader_feature_local _ _ALPHATEST_ON
        #pragma multi_compile_fragment _ WRITE_MSAA_DEPTH
        #pragma shader_feature _ _SURFACE_TYPE_TRANSPARENT
        #pragma shader_feature_local _BLENDMODE_OFF _BLENDMODE_ALPHA _BLENDMODE_ADD _BLENDMODE_PRE_MULTIPLY
        #pragma shader_feature_local _ _ADD_PRECOMPUTED_VELOCITY
        #pragma shader_feature_local _ _TRANSPARENT_WRITES_MOTION_VEC
        #pragma shader_feature_local_fragment _ _ENABLE_FOG_ON_TRANSPARENT
            // GraphKeywords: <None>
        
            // For custom interpolators to inject a substruct definition before FragInputs definition,
            // allowing for FragInputs to capture CI's intended for ShaderGraph's SDI.
            struct CustomInterpolators
        {
        };
        #define USE_CUSTOMINTERP_SUBSTRUCT
        
        
        
            // TODO: Merge FragInputsVFX substruct with CustomInterpolators.
        	#ifdef HAVE_VFX_MODIFICATION
        	struct FragInputsVFX
            {
                /* WARNING: $splice Could not find named fragment 'FragInputsVFX' */
            };
            #endif
        
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GeometricTools.hlsl" // Required by Tessellation.hlsl
        	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Tessellation.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl" // Required to be include before we include properties as it define DECLARE_STACK_CB
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphHeader.hlsl" // Need to be here for Gradient struct definition
        
            // --------------------------------------------------
            // Defines
        
            // Attribute
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define ATTRIBUTES_NEED_TEXCOORD2
            #define ATTRIBUTES_NEED_TEXCOORD3
            #define VARYINGS_NEED_TANGENT_TO_WORLD
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_TEXCOORD1
            #define VARYINGS_NEED_TEXCOORD2
            #define VARYINGS_NEED_TEXCOORD3
        
            #define HAVE_MESH_MODIFICATION
        
            //Strip down the FragInputs.hlsl (on graphics), so we can only optimize the interpolators we use.
            //if by accident something requests contents of FragInputs.hlsl, it will be caught as a compiler error
            //Frag inputs stripping is only enabled when FRAG_INPUTS_ENABLE_STRIPPING is set
            #if !defined(SHADER_STAGE_RAY_TRACING) && SHADERPASS != SHADERPASS_RAYTRACING_GBUFFER && SHADERPASS != SHADERPASS_FULL_SCREEN_DEBUG
            #define FRAG_INPUTS_ENABLE_STRIPPING
            #endif
            #define FRAG_INPUTS_USE_TEXCOORD0
            #define FRAG_INPUTS_USE_TEXCOORD1
            #define FRAG_INPUTS_USE_TEXCOORD2
            #define FRAG_INPUTS_USE_TEXCOORD3
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
        
        
        
            #define SHADERPASS SHADERPASS_DEPTH_ONLY
        
        
            // Following two define are a workaround introduce in 10.1.x for RaytracingQualityNode
            // The ShaderGraph don't support correctly migration of this node as it serialize all the node data
            // in the json file making it impossible to uprgrade. Until we get a fix, we do a workaround here
            // to still allow us to rename the field and keyword of this node without breaking existing code.
            #ifdef RAYTRACING_SHADER_GRAPH_DEFAULT
            #define RAYTRACING_SHADER_GRAPH_HIGH
            #endif
        
            #ifdef RAYTRACING_SHADER_GRAPH_RAYTRACED
            #define RAYTRACING_SHADER_GRAPH_LOW
            #endif
            // end
        
            #ifndef SHADER_UNLIT
            // We need isFrontFace when using double sided - it is not required for unlit as in case of unlit double sided only drive the cullmode
            // VARYINGS_NEED_CULLFACE can be define by VaryingsMeshToPS.FaceSign input if a IsFrontFace Node is included in the shader graph.
            #if defined(_DOUBLESIDED_ON) && !defined(VARYINGS_NEED_CULLFACE)
                #define VARYINGS_NEED_CULLFACE
            #endif
            #endif
        
            // Specific Material Define
        // Setup a define to say we are an unlit shader
        #define SHADER_UNLIT
        
        // Following Macro are only used by Unlit material
        #if defined(_ENABLE_SHADOW_MATTE) && (SHADERPASS == SHADERPASS_FORWARD_UNLIT || SHADERPASS == SHADERPASS_PATH_TRACING)
        #define LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
        #define HAS_LIGHTLOOP
        #endif
            // Caution: we can use the define SHADER_UNLIT onlit after the above Material include as it is the Unlit template who define it
        
            // To handle SSR on transparent correctly with a possibility to enable/disable it per framesettings
            // we should have a code like this:
            // if !defined(_DISABLE_SSR_TRANSPARENT)
            // pragma multi_compile _ WRITE_NORMAL_BUFFER
            // endif
            // i.e we enable the multicompile only if we can receive SSR or not, and then C# code drive
            // it based on if SSR transparent in frame settings and not (and stripper can strip it).
            // this is currently not possible with our current preprocessor as _DISABLE_SSR_TRANSPARENT is a keyword not a define
            // so instead we used this and chose to pay the extra cost of normal write even if SSR transaprent is disabled.
            // Ideally the shader graph generator should handle it but condition below can't be handle correctly for now.
            #if SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_PREPASS
            #if !defined(_DISABLE_SSR_TRANSPARENT) && !defined(SHADER_UNLIT)
                #define WRITE_NORMAL_BUFFER
            #endif
            #endif
        
            #ifndef DEBUG_DISPLAY
                // In case of opaque we don't want to perform the alpha test, it is done in depth prepass and we use depth equal for ztest (setup from UI)
                // Don't do it with debug display mode as it is possible there is no depth prepass in this case
                #if !defined(_SURFACE_TYPE_TRANSPARENT)
                    #if SHADERPASS == SHADERPASS_FORWARD
                    #define SHADERPASS_FORWARD_BYPASS_ALPHA_TEST
                    #elif SHADERPASS == SHADERPASS_GBUFFER
                    #define SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST
                    #endif
                #endif
            #endif
        
            // Define _DEFERRED_CAPABLE_MATERIAL for shader capable to run in deferred pass
            #if defined(SHADER_LIT) && !defined(_SURFACE_TYPE_TRANSPARENT)
                #define _DEFERRED_CAPABLE_MATERIAL
            #endif
        
            // Translate transparent motion vector define
            #if defined(_TRANSPARENT_WRITES_MOTION_VEC) && defined(_SURFACE_TYPE_TRANSPARENT)
                #define _WRITE_TRANSPARENT_MOTION_VECTOR
            #endif
        
            // -- Graph Properties
            CBUFFER_START(UnityPerMaterial)
        float4 _TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1_TexelSize;
        float4 _Direction_TexelSize;
        float2 _TextureSize;
        float _RotateUV;
        float Boolean_F2B1E91A;
        float4 _EmissionColor;
        float _UseShadowThreshold;
        float4 _DoubleSidedConstants;
        float _BlendMode;
        float _EnableBlendModePreserveSpecularLighting;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1);
        SAMPLER(sampler_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1);
        TEXTURE2D(_Direction);
        SAMPLER(sampler_Direction);
        
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
        
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
        
            // Includes
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl"
        	// GraphIncludes: <None>
        
            // --------------------------------------------------
            // Structs and Packing
        
            struct AttributesMesh
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
             float4 uv1 : TEXCOORD1;
             float4 uv2 : TEXCOORD2;
             float4 uv3 : TEXCOORD3;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct VaryingsMeshToPS
        {
            SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
             float3 normalWS;
             float4 tangentWS;
             float4 texCoord0;
             float4 texCoord1;
             float4 texCoord2;
             float4 texCoord3;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
        };
        struct SurfaceDescriptionInputs
        {
             float4 uv0;
             float4 uv1;
             float4 uv2;
             float4 uv3;
        };
        struct PackedVaryingsMeshToPS
        {
            SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
             float3 interp0 : INTERP0;
             float4 interp1 : INTERP1;
             float4 interp2 : INTERP2;
             float4 interp3 : INTERP3;
             float4 interp4 : INTERP4;
             float4 interp5 : INTERP5;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
        };
        
            PackedVaryingsMeshToPS PackVaryingsMeshToPS (VaryingsMeshToPS input)
        {
            PackedVaryingsMeshToPS output;
            ZERO_INITIALIZE(PackedVaryingsMeshToPS, output);
            output.positionCS = input.positionCS;
            output.interp0.xyz =  input.normalWS;
            output.interp1.xyzw =  input.tangentWS;
            output.interp2.xyzw =  input.texCoord0;
            output.interp3.xyzw =  input.texCoord1;
            output.interp4.xyzw =  input.texCoord2;
            output.interp5.xyzw =  input.texCoord3;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            return output;
        }
        
        VaryingsMeshToPS UnpackVaryingsMeshToPS (PackedVaryingsMeshToPS input)
        {
            VaryingsMeshToPS output;
            output.positionCS = input.positionCS;
            output.normalWS = input.interp0.xyz;
            output.tangentWS = input.interp1.xyzw;
            output.texCoord0 = input.interp2.xyzw;
            output.texCoord1 = input.interp3.xyzw;
            output.texCoord2 = input.interp4.xyzw;
            output.texCoord3 = input.interp5.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            return output;
        }
        
        
            // --------------------------------------------------
            // Graph
        
        
            // Graph Functions
            
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A / B;
        }
        
        void Unity_Fraction_float2(float2 In, out float2 Out)
        {
            Out = frac(In);
        }
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A * B;
        }
        
        void Unity_Subtract_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A - B;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_Comparison_Greater_float(float A, float B, out float Out)
        {
            Out = A > B ? 1 : 0;
        }
        
        void Unity_Divide_float(float A, float B, out float Out)
        {
            Out = A / B;
        }
        
        void Unity_Arctangent_float(float In, out float Out)
        {
            Out = atan(In);
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Branch_float(float Predicate, float True, float False, out float Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Rotate_Radians_float(float2 UV, float2 Center, float Rotation, out float2 Out)
        {
            //rotation matrix
            UV -= Center;
            float s = sin(Rotation);
            float c = cos(Rotation);
        
            //center rotation matrix
            float2x2 rMatrix = float2x2(c, -s, s, c);
            rMatrix *= 0.5;
            rMatrix += 0.5;
            rMatrix = rMatrix*2 - 1;
        
            //multiply the UVs by the rotation matrix
            UV.xy = mul(UV.xy, rMatrix);
            UV += Center;
        
            Out = UV;
        }
        
        void Unity_Distance_float2(float2 A, float2 B, out float Out)
        {
            Out = distance(A, B);
        }
        
        void Unity_Clamp_float(float In, float Min, float Max, out float Out)
        {
            Out = clamp(In, Min, Max);
        }
        
        void Unity_Subtract_float(float A, float B, out float Out)
        {
            Out = A - B;
        }
        
        void Unity_Absolute_float(float In, out float Out)
        {
            Out = abs(In);
        }
        
        void Unity_Power_float(float A, float B, out float Out)
        {
            Out = pow(A, B);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A * B;
        }
        
            // Graph Vertex
            struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
            // Graph Pixel
            struct SurfaceDescription
        {
            float3 BaseColor;
            float3 Emission;
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0 = UnityBuildTexture2DStructNoScale(_Direction);
            float4 _UV_7c8a457b34527181924b2b4a71350de9_Out_0 = IN.uv0;
            float2 _TilingAndOffset_67d58b7a8092828aaece7f3acfe6e8a9_Out_3;
            Unity_TilingAndOffset_float((_UV_7c8a457b34527181924b2b4a71350de9_Out_0.xy), float2 (2, 2), float2 (0, 0), _TilingAndOffset_67d58b7a8092828aaece7f3acfe6e8a9_Out_3);
            float2 _Vector2_9a3e446bd0dfc3868e3db6f9bf101ead_Out_0 = float2(1, 1);
            float2 _Vector2_8355e4dbd796b188ba1e500dcabf0c0f_Out_0 = float2(30, 30);
            float2 _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2;
            Unity_Divide_float2(_Vector2_9a3e446bd0dfc3868e3db6f9bf101ead_Out_0, _Vector2_8355e4dbd796b188ba1e500dcabf0c0f_Out_0, _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2);
            float2 _Divide_5d0f49ab4bcab689894f976217114a79_Out_2;
            Unity_Divide_float2(_TilingAndOffset_67d58b7a8092828aaece7f3acfe6e8a9_Out_3, _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2, _Divide_5d0f49ab4bcab689894f976217114a79_Out_2);
            float2 _Fraction_e4f0f05fb76fd3809f5ec855b54a4595_Out_1;
            Unity_Fraction_float2(_Divide_5d0f49ab4bcab689894f976217114a79_Out_2, _Fraction_e4f0f05fb76fd3809f5ec855b54a4595_Out_1);
            float2 _Multiply_cbcb0de832b1d08288916e46aa8d4df2_Out_2;
            Unity_Multiply_float2_float2(_Fraction_e4f0f05fb76fd3809f5ec855b54a4595_Out_1, _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2, _Multiply_cbcb0de832b1d08288916e46aa8d4df2_Out_2);
            float2 _Vector2_e6d2595367ed0380b42c7a74855838a5_Out_0 = float2(246, 246);
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_Width_0 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.z;
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_Height_2 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.w;
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_TexelWidth_3 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.x;
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_TexelHeight_4 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.y;
            float2 _Vector2_e8728bfbf194e5818f010927b4ba8d79_Out_0 = float2(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Width_0, _TexelSize_18bb9f416e386f87bee91aa46804bc56_Height_2);
            float2 _Multiply_561a2890964f4081a8cf5d16bccb889f_Out_2;
            Unity_Multiply_float2_float2(_Vector2_e6d2595367ed0380b42c7a74855838a5_Out_0, _Vector2_e8728bfbf194e5818f010927b4ba8d79_Out_0, _Multiply_561a2890964f4081a8cf5d16bccb889f_Out_2);
            float2 _Vector2_97c81bcbfd26f08385a101ac3da412f3_Out_0 = float2(10, 10);
            float2 _Multiply_bccda05b3809138d8da15484dd6a7b4e_Out_2;
            Unity_Multiply_float2_float2(_Vector2_e8728bfbf194e5818f010927b4ba8d79_Out_0, _Vector2_97c81bcbfd26f08385a101ac3da412f3_Out_0, _Multiply_bccda05b3809138d8da15484dd6a7b4e_Out_2);
            float2 _Subtract_710d4672e66f7780ad90ca6e19a733bd_Out_2;
            Unity_Subtract_float2(_Multiply_561a2890964f4081a8cf5d16bccb889f_Out_2, _Multiply_bccda05b3809138d8da15484dd6a7b4e_Out_2, _Subtract_710d4672e66f7780ad90ca6e19a733bd_Out_2);
            float2 _Multiply_cd542324eceadd8daaca18ab78d3fbf6_Out_2;
            Unity_Multiply_float2_float2(_Multiply_cbcb0de832b1d08288916e46aa8d4df2_Out_2, _Subtract_710d4672e66f7780ad90ca6e19a733bd_Out_2, _Multiply_cd542324eceadd8daaca18ab78d3fbf6_Out_2);
            float2 _Vector2_df86361e9c36e7828e3097cc1fa8a75b_Out_0 = float2(0.1, 0.1);
            float2 _Multiply_8e351d40d36634829f2bb0905b9b9342_Out_2;
            Unity_Multiply_float2_float2(_Multiply_cd542324eceadd8daaca18ab78d3fbf6_Out_2, _Vector2_df86361e9c36e7828e3097cc1fa8a75b_Out_0, _Multiply_8e351d40d36634829f2bb0905b9b9342_Out_2);
            float4 _UV_1ca38e193be034888b8a707af9d47210_Out_0 = IN.uv3;
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_R_1 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[0];
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[1];
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_B_3 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[2];
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_A_4 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[3];
            float _Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2;
            Unity_Multiply_float_float(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2, -1, _Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2);
            float _Comparison_e67d9847aa86858490dd0c37e68e7505_Out_2;
            Unity_Comparison_Greater_float(_Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2, 0, _Comparison_e67d9847aa86858490dd0c37e68e7505_Out_2);
            float _Divide_2b884a92fd783d82a9012917c2b83115_Out_2;
            Unity_Divide_float(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_R_1, _Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2, _Divide_2b884a92fd783d82a9012917c2b83115_Out_2);
            float _Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1;
            Unity_Arctangent_float(_Divide_2b884a92fd783d82a9012917c2b83115_Out_2, _Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1);
            float _Add_cf6b7aee4a487f8ca16b9171b64a8f0d_Out_2;
            Unity_Add_float(_Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1, 3.141592, _Add_cf6b7aee4a487f8ca16b9171b64a8f0d_Out_2);
            float _Branch_9de100b31215ba89bb19e5b89012a25a_Out_3;
            Unity_Branch_float(_Comparison_e67d9847aa86858490dd0c37e68e7505_Out_2, _Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1, _Add_cf6b7aee4a487f8ca16b9171b64a8f0d_Out_2, _Branch_9de100b31215ba89bb19e5b89012a25a_Out_3);
            float2 _Rotate_ff1406ef744b188ca79c6d3bf0b8bbd7_Out_3;
            Unity_Rotate_Radians_float(_Multiply_8e351d40d36634829f2bb0905b9b9342_Out_2, float2 (0.5, 0.5), _Branch_9de100b31215ba89bb19e5b89012a25a_Out_3, _Rotate_ff1406ef744b188ca79c6d3bf0b8bbd7_Out_3);
            #if defined(SHADER_API_GLES) && (SHADER_TARGET < 30)
              float4 _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0 = float4(0.0f, 0.0f, 0.0f, 1.0f);
            #else
              float4 _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0 = SAMPLE_TEXTURE2D_LOD(_Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0.tex, _Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0.samplerstate, _Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0.GetTransformedUV(_Rotate_ff1406ef744b188ca79c6d3bf0b8bbd7_Out_3), 0);
            #endif
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_R_5 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.r;
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_G_6 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.g;
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_B_7 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.b;
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_A_8 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.a;
            float _Comparison_1a93f0ca7c913a8f81659b20b8d59f2b_Out_2;
            Unity_Comparison_Greater_float(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2, 0.001, _Comparison_1a93f0ca7c913a8f81659b20b8d59f2b_Out_2);
            float3 _Vector3_37ab63b41d52bb8d8eba9088ed71bc11_Out_0 = float3(0, 0, 0);
            float2 _Vector2_4aeecd1cb271dd8e96c2fb12309b9dd0_Out_0 = float2(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_R_1, _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2);
            float _Distance_47e812ff6269a181b6e0faa017faee3b_Out_2;
            Unity_Distance_float2(_Vector2_4aeecd1cb271dd8e96c2fb12309b9dd0_Out_0, float2(0, 0), _Distance_47e812ff6269a181b6e0faa017faee3b_Out_2);
            float _Multiply_e5102f48c5e759879293e06062894e0c_Out_2;
            Unity_Multiply_float_float(_Distance_47e812ff6269a181b6e0faa017faee3b_Out_2, 1.6, _Multiply_e5102f48c5e759879293e06062894e0c_Out_2);
            float _Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3;
            Unity_Clamp_float(_Multiply_e5102f48c5e759879293e06062894e0c_Out_2, 0, 1, _Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3);
            float _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2;
            Unity_Subtract_float(1, _Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3, _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2);
            float3 _Vector3_aeff4a868e81c68f85dfe4c9662da9ce_Out_0 = float3(1, _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2, 0);
            float _Absolute_5573b6b2c1616e819aa4eeb67caec457_Out_1;
            Unity_Absolute_float(_Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3, _Absolute_5573b6b2c1616e819aa4eeb67caec457_Out_1);
            float _Power_33dedbf009cacf848c403a58667a66c6_Out_2;
            Unity_Power_float(_Absolute_5573b6b2c1616e819aa4eeb67caec457_Out_1, 0.16, _Power_33dedbf009cacf848c403a58667a66c6_Out_2);
            float _Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3;
            Unity_Clamp_float(_Power_33dedbf009cacf848c403a58667a66c6_Out_2, 0, 1, _Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3);
            float3 _Lerp_e6000a54e83a8f8e8c5bc16419b360ca_Out_3;
            Unity_Lerp_float3(_Vector3_37ab63b41d52bb8d8eba9088ed71bc11_Out_0, _Vector3_aeff4a868e81c68f85dfe4c9662da9ce_Out_0, (_Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3.xxx), _Lerp_e6000a54e83a8f8e8c5bc16419b360ca_Out_3);
            float3 _Vector3_484f28086891e98d9589d1f65774f6f4_Out_0 = float3(0, _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2, 1);
            float3 _Lerp_93469a90f98ccf80a7121c0b2bf05f48_Out_3;
            Unity_Lerp_float3(_Vector3_37ab63b41d52bb8d8eba9088ed71bc11_Out_0, _Vector3_484f28086891e98d9589d1f65774f6f4_Out_0, (_Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3.xxx), _Lerp_93469a90f98ccf80a7121c0b2bf05f48_Out_3);
            float3 _Branch_365bf259ae64c386a18e2d6997727100_Out_3;
            Unity_Branch_float3(_Comparison_1a93f0ca7c913a8f81659b20b8d59f2b_Out_2, _Lerp_e6000a54e83a8f8e8c5bc16419b360ca_Out_3, _Lerp_93469a90f98ccf80a7121c0b2bf05f48_Out_3, _Branch_365bf259ae64c386a18e2d6997727100_Out_3);
            float3 _Multiply_0563147e8bc9868b8c2a4f2c442c349f_Out_2;
            Unity_Multiply_float3_float3((_SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_A_8.xxx), _Branch_365bf259ae64c386a18e2d6997727100_Out_3, _Multiply_0563147e8bc9868b8c2a4f2c442c349f_Out_2);
            surface.BaseColor = _Multiply_0563147e8bc9868b8c2a4f2c442c349f_Out_2;
            surface.Emission = float3(0, 0, 0);
            surface.Alpha = 1;
            surface.AlphaClipThreshold = 0.5;
            return surface;
        }
        
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES AttributesMesh
            #define VaryingsMeshType VaryingsMeshToPS
            #define VFX_SRP_VARYINGS VaryingsMeshType
            #define VFX_SRP_SURFACE_INPUTS FragInputs
            #endif
            
        VertexDescriptionInputs AttributesMeshToVertexDescriptionInputs(AttributesMesh input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
        
            return output;
        }
        
        VertexDescription GetVertexDescription(AttributesMesh input, float3 timeParameters
        #ifdef HAVE_VFX_MODIFICATION
            , AttributesElement element
        #endif
        )
        {
            // build graph inputs
            VertexDescriptionInputs vertexDescriptionInputs = AttributesMeshToVertexDescriptionInputs(input);
            // Override time parameters with used one (This is required to correctly handle motion vector for vertex animation based on time)
        
            // evaluate vertex graph
        #ifdef HAVE_VFX_MODIFICATION
            GraphProperties properties;
            ZERO_INITIALIZE(GraphProperties, properties);
        
            // Fetch the vertex graph properties for the particle instance.
            GetElementVertexProperties(element, properties);
        
            VertexDescription vertexDescription = VertexDescriptionFunction(vertexDescriptionInputs, properties);
        #else
            VertexDescription vertexDescription = VertexDescriptionFunction(vertexDescriptionInputs);
        #endif
            return vertexDescription;
        
        }
        
        AttributesMesh ApplyMeshModification(AttributesMesh input, float3 timeParameters
        #ifdef USE_CUSTOMINTERP_SUBSTRUCT
            #ifdef TESSELLATION_ON
            , inout VaryingsMeshToDS varyings
            #else
            , inout VaryingsMeshToPS varyings
            #endif
        #endif
        #ifdef HAVE_VFX_MODIFICATION
                , AttributesElement element
        #endif
            )
        {
            VertexDescription vertexDescription = GetVertexDescription(input, timeParameters
        #ifdef HAVE_VFX_MODIFICATION
                , element
        #endif
            );
        
            // copy graph output to the results
            input.positionOS = vertexDescription.Position;
            input.normalOS = vertexDescription.Normal;
            input.tangentOS.xyz = vertexDescription.Tangent;
        
            
        
            return input;
        }
        
        #if defined(_ADD_CUSTOM_VELOCITY) // For shader graph custom velocity
        // Return precomputed Velocity in object space
        float3 GetCustomVelocity(AttributesMesh input
        #ifdef HAVE_VFX_MODIFICATION
            , AttributesElement element
        #endif
        )
        {
            VertexDescription vertexDescription = GetVertexDescription(input, _TimeParameters.xyz
        #ifdef HAVE_VFX_MODIFICATION
                , element
        #endif
            );
            return vertexDescription.CustomVelocity;
        }
        #endif
        
        FragInputs BuildFragInputs(VaryingsMeshToPS input)
        {
            FragInputs output;
            ZERO_INITIALIZE(FragInputs, output);
        
            // Init to some default value to make the computer quiet (else it output 'divide by zero' warning even if value is not used).
            // TODO: this is a really poor workaround, but the variable is used in a bunch of places
            // to compute normals which are then passed on elsewhere to compute other values...
            output.tangentToWorld = k_identity3x3;
            output.positionSS = input.positionCS;       // input.positionCS is SV_Position
        
            output.tangentToWorld =             BuildTangentToWorld(input.tangentWS, input.normalWS);
            output.texCoord0 =                  input.texCoord0;
            output.texCoord1 =                  input.texCoord1;
            output.texCoord2 =                  input.texCoord2;
            output.texCoord3 =                  input.texCoord3;
        
        #ifdef HAVE_VFX_MODIFICATION
            // FragInputs from VFX come from two places: Interpolator or CBuffer.
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            // splice point to copy custom interpolator fields from varyings to frag inputs
            
        
            return output;
        }
        
        // existing HDRP code uses the combined function to go directly from packed to frag inputs
        FragInputs UnpackVaryingsMeshToFragInputs(PackedVaryingsMeshToPS input)
        {
            UNITY_SETUP_INSTANCE_ID(input);
        #if defined(HAVE_VFX_MODIFICATION) && defined(UNITY_INSTANCING_ENABLED)
            unity_InstanceID = input.instanceID;
        #endif
            VaryingsMeshToPS unpacked = UnpackVaryingsMeshToPS(input);
            return BuildFragInputs(unpacked);
        }
            SurfaceDescriptionInputs FragInputsToSurfaceDescriptionInputs(FragInputs input, float3 viewWS)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
            #if defined(SHADER_STAGE_RAY_TRACING)
            #else
            #endif
        
        #if UNITY_UV_STARTS_AT_TOP
        #else
        #endif
        
        
            output.uv0 =                                        input.texCoord0;
            output.uv1 =                                        input.texCoord1;
            output.uv2 =                                        input.texCoord2;
            output.uv3 =                                        input.texCoord3;
        
            // splice point to copy frag inputs custom interpolator pack into the SDI
            
        
            return output;
        }
        
            // --------------------------------------------------
            // Build Surface Data (Specific Material)
        
        void BuildSurfaceData(FragInputs fragInputs, inout SurfaceDescription surfaceDescription, float3 V, PositionInputs posInput, out SurfaceData surfaceData)
        {
            // setup defaults -- these are used if the graph doesn't output a value
            ZERO_INITIALIZE(SurfaceData, surfaceData);
        
            // copy across graph values, if defined
            surfaceData.color = surfaceDescription.BaseColor;
        
            #ifdef WRITE_NORMAL_BUFFER
            // When we need to export the normal (in the depth prepass, we write the geometry one)
            surfaceData.normalWS = fragInputs.tangentToWorld[2];
            #endif
        
            #if defined(DEBUG_DISPLAY)
            if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
            {
                // TODO
            }
            #endif
        
            #ifdef _ENABLE_SHADOW_MATTE
        
                #if (SHADERPASS == SHADERPASS_FORWARD_UNLIT) || (SHADERPASS == SHADERPASS_RAYTRACING_GBUFFER) || (SHADERPASS == SHADERPASS_RAYTRACING_INDIRECT) || (SHADERPASS == SHADERPASS_RAYTRACING_FORWARD)
        
                    HDShadowContext shadowContext = InitShadowContext();
        
                    // Evaluate the shadow, the normal is guaranteed if shadow matte is enabled on this shader.
                    float3 shadow3;
                    ShadowLoopMin(shadowContext, posInput, normalize(fragInputs.tangentToWorld[2]), asuint(_ShadowMatteFilter), GetMeshRenderingLightLayer(), shadow3);
        
                    // Compute the average value in the fourth channel
                    float4 shadow = float4(shadow3, dot(shadow3, float3(1.0/3.0, 1.0/3.0, 1.0/3.0)));
        
                    float4 shadowColor = (1.0 - shadow) * surfaceDescription.ShadowTint.rgba;
                    float  localAlpha  = saturate(shadowColor.a + surfaceDescription.Alpha);
        
                    // Keep the nested lerp
                    // With no Color (bsdfData.color.rgb, bsdfData.color.a == 0.0f), just use ShadowColor*Color to avoid a ring of "white" around the shadow
                    // And mix color to consider the Color & ShadowColor alpha (from texture or/and color picker)
                    #ifdef _SURFACE_TYPE_TRANSPARENT
                        surfaceData.color = lerp(shadowColor.rgb * surfaceData.color, lerp(lerp(shadowColor.rgb, surfaceData.color, 1.0 - surfaceDescription.ShadowTint.a), surfaceData.color, shadow.rgb), surfaceDescription.Alpha);
                    #else
                        surfaceData.color = lerp(lerp(shadowColor.rgb, surfaceData.color, 1.0 - surfaceDescription.ShadowTint.a), surfaceData.color, shadow.rgb);
                    #endif
                    localAlpha = ApplyBlendMode(surfaceData.color, localAlpha).a;
        
                    surfaceDescription.Alpha = localAlpha;
        
                #elif SHADERPASS == SHADERPASS_PATH_TRACING
        
                    surfaceData.normalWS = fragInputs.tangentToWorld[2];
                    surfaceData.shadowTint = surfaceDescription.ShadowTint.rgba;
        
                #endif
        
            #endif // _ENABLE_SHADOW_MATTE
        }
        
            // --------------------------------------------------
            // Get Surface And BuiltinData
        
            void GetSurfaceAndBuiltinData(FragInputs fragInputs, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData RAY_TRACING_OPTIONAL_PARAMETERS)
            {
                // Don't dither if displaced tessellation (we're fading out the displacement instead to match the next LOD)
                #if !defined(SHADER_STAGE_RAY_TRACING) && !defined(_TESSELLATION_DISPLACEMENT)
                #ifdef LOD_FADE_CROSSFADE // enable dithering LOD transition if user select CrossFade transition in LOD group
                LODDitheringTransition(ComputeFadeMaskSeed(V, posInput.positionSS), unity_LODFade.x);
                #endif
                #endif
        
                #ifndef SHADER_UNLIT
                #ifdef _DOUBLESIDED_ON
                    float3 doubleSidedConstants = _DoubleSidedConstants.xyz;
                #else
                    float3 doubleSidedConstants = float3(1.0, 1.0, 1.0);
                #endif
        
                ApplyDoubleSidedFlipOrMirror(fragInputs, doubleSidedConstants); // Apply double sided flip on the vertex normal
                #endif // SHADER_UNLIT
        
                SurfaceDescriptionInputs surfaceDescriptionInputs = FragInputsToSurfaceDescriptionInputs(fragInputs, V);
        
                #if defined(HAVE_VFX_MODIFICATION)
                GraphProperties properties;
                ZERO_INITIALIZE(GraphProperties, properties);
        
                GetElementPixelProperties(fragInputs, properties);
        
                SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs, properties);
                #else
                SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);
                #endif
        
                // Perform alpha test very early to save performance (a killed pixel will not sample textures)
                // TODO: split graph evaluation to grab just alpha dependencies first? tricky..
                #ifdef _ALPHATEST_ON
                    float alphaCutoff = surfaceDescription.AlphaClipThreshold;
                    #if SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_PREPASS
                    // The TransparentDepthPrepass is also used with SSR transparent.
                    // If an artists enable transaprent SSR but not the TransparentDepthPrepass itself, then we use AlphaClipThreshold
                    // otherwise if TransparentDepthPrepass is enabled we use AlphaClipThresholdDepthPrepass
                    #elif SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_POSTPASS
                    // DepthPostpass always use its own alpha threshold
                    alphaCutoff = surfaceDescription.AlphaClipThresholdDepthPostpass;
                    #elif (SHADERPASS == SHADERPASS_SHADOWS) || (SHADERPASS == SHADERPASS_RAYTRACING_VISIBILITY)
                    // If use shadow threshold isn't enable we don't allow any test
                    #endif
        
                    GENERIC_ALPHA_TEST(surfaceDescription.Alpha, alphaCutoff);
                #endif
        
                #if !defined(SHADER_STAGE_RAY_TRACING) && _DEPTHOFFSET_ON
                ApplyDepthOffsetPositionInput(V, surfaceDescription.DepthOffset, GetViewForwardDir(), GetWorldToHClipMatrix(), posInput);
                #endif
        
                #ifndef SHADER_UNLIT
                float3 bentNormalWS;
                BuildSurfaceData(fragInputs, surfaceDescription, V, posInput, surfaceData, bentNormalWS);
        
                #ifdef FRAG_INPUTS_USE_TEXCOORD1
                    float4 lightmapTexCoord1 = fragInputs.texCoord1;
                #else
                    float4 lightmapTexCoord1 = float4(0,0,0,0);
                #endif
        
                #ifdef FRAG_INPUTS_USE_TEXCOORD2
                    float4 lightmapTexCoord2 = fragInputs.texCoord2;
                #else
                    float4 lightmapTexCoord2 = float4(0,0,0,0);
                #endif
        
                // Builtin Data
                // For back lighting we use the oposite vertex normal
                InitBuiltinData(posInput, surfaceDescription.Alpha, bentNormalWS, -fragInputs.tangentToWorld[2], lightmapTexCoord1, lightmapTexCoord2, builtinData);
        
                #else
                BuildSurfaceData(fragInputs, surfaceDescription, V, posInput, surfaceData);
        
                ZERO_BUILTIN_INITIALIZE(builtinData); // No call to InitBuiltinData as we don't have any lighting
                builtinData.opacity = surfaceDescription.Alpha;
        
                #if defined(DEBUG_DISPLAY)
                    // Light Layers are currently not used for the Unlit shader (because it is not lit)
                    // But Unlit objects do cast shadows according to their rendering layer mask, which is what we want to
                    // display in the light layers visualization mode, therefore we need the renderingLayers
                    builtinData.renderingLayers = GetMeshRenderingLightLayer();
                #endif
        
                #endif // SHADER_UNLIT
        
                #ifdef _ALPHATEST_ON
                    // Used for sharpening by alpha to mask - Alpha to covertage is only used with depth only and forward pass (no shadow pass, no transparent pass)
                    builtinData.alphaClipTreshold = alphaCutoff;
                #endif
        
                // override sampleBakedGI - not used by Unlit
        
                builtinData.emissiveColor = surfaceDescription.Emission;
        
                // Note this will not fully work on transparent surfaces (can check with _SURFACE_TYPE_TRANSPARENT define)
                // We will always overwrite vt feeback with the nearest. So behind transparent surfaces vt will not be resolved
                // This is a limitation of the current MRT approach.
                #ifdef UNITY_VIRTUAL_TEXTURING
                #endif
        
                #if _DEPTHOFFSET_ON
                builtinData.depthOffset = surfaceDescription.DepthOffset;
                #endif
        
                // TODO: We should generate distortion / distortionBlur for non distortion pass
                #if (SHADERPASS == SHADERPASS_DISTORTION)
                builtinData.distortion = surfaceDescription.Distortion;
                builtinData.distortionBlur = surfaceDescription.DistortionBlur;
                #endif
        
                #ifndef SHADER_UNLIT
                // PostInitBuiltinData call ApplyDebugToBuiltinData
                PostInitBuiltinData(V, posInput, surfaceData, builtinData);
                #else
                ApplyDebugToBuiltinData(builtinData);
                #endif
        
                RAY_TRACING_OPTIONAL_ALPHA_TEST_PASS
            }
        
            // --------------------------------------------------
            // Main
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassDepthOnly.hlsl"
        
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
        
        	#ifdef HAVE_VFX_MODIFICATION
        	#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/VisualEffectVertex.hlsl"
        	#endif
        
            ENDHLSL
        }
        Pass
        {
            Name "ForwardOnly"
            Tags
            {
                "LightMode" = "ForwardOnly"
            }
        
            // Render State
            Cull [_CullModeForward]
        Blend [_SrcBlend] [_DstBlend], [_AlphaSrcBlend] [_AlphaDstBlend]
        Blend 1 SrcAlpha OneMinusSrcAlpha
        ZTest [_ZTestDepthEqualForOpaque]
        ZWrite [_ZWrite]
        ColorMask [_ColorMaskTransparentVelOne] 1
        ColorMask [_ColorMaskTransparentVelTwo] 2
        Stencil
        {
        WriteMask [_StencilWriteMask]
        Ref [_StencilRef]
        CompFront Always
        PassFront Replace
        CompBack Always
        PassBack Replace
        }
        
            // Debug
            // <None>
        
            // --------------------------------------------------
            // Pass
        
            HLSLPROGRAM
        
            // Pragmas
            #pragma multi_compile _ DOTS_INSTANCING_ON
        #pragma instancing_options renderinglayer
        #pragma target 4.5
        #pragma vertex Vert
        #pragma fragment Frag
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
        #pragma multi_compile_instancing
        
            // Keywords
            #pragma shader_feature_local _ _ALPHATEST_ON
        #pragma shader_feature _ _SURFACE_TYPE_TRANSPARENT
        #pragma shader_feature_local _BLENDMODE_OFF _BLENDMODE_ALPHA _BLENDMODE_ADD _BLENDMODE_PRE_MULTIPLY
        #pragma shader_feature_local _ _ADD_PRECOMPUTED_VELOCITY
        #pragma shader_feature_local _ _TRANSPARENT_WRITES_MOTION_VEC
        #pragma shader_feature_local_fragment _ _ENABLE_FOG_ON_TRANSPARENT
        #pragma multi_compile _ DEBUG_DISPLAY
            // GraphKeywords: <None>
        
            // For custom interpolators to inject a substruct definition before FragInputs definition,
            // allowing for FragInputs to capture CI's intended for ShaderGraph's SDI.
            struct CustomInterpolators
        {
        };
        #define USE_CUSTOMINTERP_SUBSTRUCT
        
        
        
            // TODO: Merge FragInputsVFX substruct with CustomInterpolators.
        	#ifdef HAVE_VFX_MODIFICATION
        	struct FragInputsVFX
            {
                /* WARNING: $splice Could not find named fragment 'FragInputsVFX' */
            };
            #endif
        
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GeometricTools.hlsl" // Required by Tessellation.hlsl
        	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Tessellation.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl" // Required to be include before we include properties as it define DECLARE_STACK_CB
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphHeader.hlsl" // Need to be here for Gradient struct definition
        
            // --------------------------------------------------
            // Defines
        
            // Attribute
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define ATTRIBUTES_NEED_TEXCOORD2
            #define ATTRIBUTES_NEED_TEXCOORD3
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_TEXCOORD1
            #define VARYINGS_NEED_TEXCOORD2
            #define VARYINGS_NEED_TEXCOORD3
        
            #define HAVE_MESH_MODIFICATION
        
            //Strip down the FragInputs.hlsl (on graphics), so we can only optimize the interpolators we use.
            //if by accident something requests contents of FragInputs.hlsl, it will be caught as a compiler error
            //Frag inputs stripping is only enabled when FRAG_INPUTS_ENABLE_STRIPPING is set
            #if !defined(SHADER_STAGE_RAY_TRACING) && SHADERPASS != SHADERPASS_RAYTRACING_GBUFFER && SHADERPASS != SHADERPASS_FULL_SCREEN_DEBUG
            #define FRAG_INPUTS_ENABLE_STRIPPING
            #endif
            #define FRAG_INPUTS_USE_TEXCOORD0
            #define FRAG_INPUTS_USE_TEXCOORD1
            #define FRAG_INPUTS_USE_TEXCOORD2
            #define FRAG_INPUTS_USE_TEXCOORD3
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
        
        
        
            #define SHADERPASS SHADERPASS_FORWARD_UNLIT
        #define RAYTRACING_SHADER_GRAPH_DEFAULT
        
        
            // Following two define are a workaround introduce in 10.1.x for RaytracingQualityNode
            // The ShaderGraph don't support correctly migration of this node as it serialize all the node data
            // in the json file making it impossible to uprgrade. Until we get a fix, we do a workaround here
            // to still allow us to rename the field and keyword of this node without breaking existing code.
            #ifdef RAYTRACING_SHADER_GRAPH_DEFAULT
            #define RAYTRACING_SHADER_GRAPH_HIGH
            #endif
        
            #ifdef RAYTRACING_SHADER_GRAPH_RAYTRACED
            #define RAYTRACING_SHADER_GRAPH_LOW
            #endif
            // end
        
            #ifndef SHADER_UNLIT
            // We need isFrontFace when using double sided - it is not required for unlit as in case of unlit double sided only drive the cullmode
            // VARYINGS_NEED_CULLFACE can be define by VaryingsMeshToPS.FaceSign input if a IsFrontFace Node is included in the shader graph.
            #if defined(_DOUBLESIDED_ON) && !defined(VARYINGS_NEED_CULLFACE)
                #define VARYINGS_NEED_CULLFACE
            #endif
            #endif
        
            // Specific Material Define
        // Setup a define to say we are an unlit shader
        #define SHADER_UNLIT
        
        // Following Macro are only used by Unlit material
        #if defined(_ENABLE_SHADOW_MATTE) && (SHADERPASS == SHADERPASS_FORWARD_UNLIT || SHADERPASS == SHADERPASS_PATH_TRACING)
        #define LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
        #define HAS_LIGHTLOOP
        #endif
            // Caution: we can use the define SHADER_UNLIT onlit after the above Material include as it is the Unlit template who define it
        
            // To handle SSR on transparent correctly with a possibility to enable/disable it per framesettings
            // we should have a code like this:
            // if !defined(_DISABLE_SSR_TRANSPARENT)
            // pragma multi_compile _ WRITE_NORMAL_BUFFER
            // endif
            // i.e we enable the multicompile only if we can receive SSR or not, and then C# code drive
            // it based on if SSR transparent in frame settings and not (and stripper can strip it).
            // this is currently not possible with our current preprocessor as _DISABLE_SSR_TRANSPARENT is a keyword not a define
            // so instead we used this and chose to pay the extra cost of normal write even if SSR transaprent is disabled.
            // Ideally the shader graph generator should handle it but condition below can't be handle correctly for now.
            #if SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_PREPASS
            #if !defined(_DISABLE_SSR_TRANSPARENT) && !defined(SHADER_UNLIT)
                #define WRITE_NORMAL_BUFFER
            #endif
            #endif
        
            #ifndef DEBUG_DISPLAY
                // In case of opaque we don't want to perform the alpha test, it is done in depth prepass and we use depth equal for ztest (setup from UI)
                // Don't do it with debug display mode as it is possible there is no depth prepass in this case
                #if !defined(_SURFACE_TYPE_TRANSPARENT)
                    #if SHADERPASS == SHADERPASS_FORWARD
                    #define SHADERPASS_FORWARD_BYPASS_ALPHA_TEST
                    #elif SHADERPASS == SHADERPASS_GBUFFER
                    #define SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST
                    #endif
                #endif
            #endif
        
            // Define _DEFERRED_CAPABLE_MATERIAL for shader capable to run in deferred pass
            #if defined(SHADER_LIT) && !defined(_SURFACE_TYPE_TRANSPARENT)
                #define _DEFERRED_CAPABLE_MATERIAL
            #endif
        
            // Translate transparent motion vector define
            #if defined(_TRANSPARENT_WRITES_MOTION_VEC) && defined(_SURFACE_TYPE_TRANSPARENT)
                #define _WRITE_TRANSPARENT_MOTION_VECTOR
            #endif
        
            // -- Graph Properties
            CBUFFER_START(UnityPerMaterial)
        float4 _TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1_TexelSize;
        float4 _Direction_TexelSize;
        float2 _TextureSize;
        float _RotateUV;
        float Boolean_F2B1E91A;
        float4 _EmissionColor;
        float _UseShadowThreshold;
        float4 _DoubleSidedConstants;
        float _BlendMode;
        float _EnableBlendModePreserveSpecularLighting;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1);
        SAMPLER(sampler_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1);
        TEXTURE2D(_Direction);
        SAMPLER(sampler_Direction);
        
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
        
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
        
            // Includes
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl"
        	// GraphIncludes: <None>
        
            // --------------------------------------------------
            // Structs and Packing
        
            struct AttributesMesh
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
             float4 uv1 : TEXCOORD1;
             float4 uv2 : TEXCOORD2;
             float4 uv3 : TEXCOORD3;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct VaryingsMeshToPS
        {
            SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
             float3 positionRWS;
             float4 texCoord0;
             float4 texCoord1;
             float4 texCoord2;
             float4 texCoord3;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
        };
        struct SurfaceDescriptionInputs
        {
             float4 uv0;
             float4 uv1;
             float4 uv2;
             float4 uv3;
        };
        struct PackedVaryingsMeshToPS
        {
            SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
             float3 interp0 : INTERP0;
             float4 interp1 : INTERP1;
             float4 interp2 : INTERP2;
             float4 interp3 : INTERP3;
             float4 interp4 : INTERP4;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
        };
        
            PackedVaryingsMeshToPS PackVaryingsMeshToPS (VaryingsMeshToPS input)
        {
            PackedVaryingsMeshToPS output;
            ZERO_INITIALIZE(PackedVaryingsMeshToPS, output);
            output.positionCS = input.positionCS;
            output.interp0.xyz =  input.positionRWS;
            output.interp1.xyzw =  input.texCoord0;
            output.interp2.xyzw =  input.texCoord1;
            output.interp3.xyzw =  input.texCoord2;
            output.interp4.xyzw =  input.texCoord3;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            return output;
        }
        
        VaryingsMeshToPS UnpackVaryingsMeshToPS (PackedVaryingsMeshToPS input)
        {
            VaryingsMeshToPS output;
            output.positionCS = input.positionCS;
            output.positionRWS = input.interp0.xyz;
            output.texCoord0 = input.interp1.xyzw;
            output.texCoord1 = input.interp2.xyzw;
            output.texCoord2 = input.interp3.xyzw;
            output.texCoord3 = input.interp4.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            return output;
        }
        
        
            // --------------------------------------------------
            // Graph
        
        
            // Graph Functions
            
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A / B;
        }
        
        void Unity_Fraction_float2(float2 In, out float2 Out)
        {
            Out = frac(In);
        }
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A * B;
        }
        
        void Unity_Subtract_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A - B;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_Comparison_Greater_float(float A, float B, out float Out)
        {
            Out = A > B ? 1 : 0;
        }
        
        void Unity_Divide_float(float A, float B, out float Out)
        {
            Out = A / B;
        }
        
        void Unity_Arctangent_float(float In, out float Out)
        {
            Out = atan(In);
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Branch_float(float Predicate, float True, float False, out float Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Rotate_Radians_float(float2 UV, float2 Center, float Rotation, out float2 Out)
        {
            //rotation matrix
            UV -= Center;
            float s = sin(Rotation);
            float c = cos(Rotation);
        
            //center rotation matrix
            float2x2 rMatrix = float2x2(c, -s, s, c);
            rMatrix *= 0.5;
            rMatrix += 0.5;
            rMatrix = rMatrix*2 - 1;
        
            //multiply the UVs by the rotation matrix
            UV.xy = mul(UV.xy, rMatrix);
            UV += Center;
        
            Out = UV;
        }
        
        void Unity_Distance_float2(float2 A, float2 B, out float Out)
        {
            Out = distance(A, B);
        }
        
        void Unity_Clamp_float(float In, float Min, float Max, out float Out)
        {
            Out = clamp(In, Min, Max);
        }
        
        void Unity_Subtract_float(float A, float B, out float Out)
        {
            Out = A - B;
        }
        
        void Unity_Absolute_float(float In, out float Out)
        {
            Out = abs(In);
        }
        
        void Unity_Power_float(float A, float B, out float Out)
        {
            Out = pow(A, B);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A * B;
        }
        
            // Graph Vertex
            struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
            // Graph Pixel
            struct SurfaceDescription
        {
            float3 BaseColor;
            float3 Emission;
            float Alpha;
            float AlphaClipThreshold;
            float4 VTPackedFeedback;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0 = UnityBuildTexture2DStructNoScale(_Direction);
            float4 _UV_7c8a457b34527181924b2b4a71350de9_Out_0 = IN.uv0;
            float2 _TilingAndOffset_67d58b7a8092828aaece7f3acfe6e8a9_Out_3;
            Unity_TilingAndOffset_float((_UV_7c8a457b34527181924b2b4a71350de9_Out_0.xy), float2 (2, 2), float2 (0, 0), _TilingAndOffset_67d58b7a8092828aaece7f3acfe6e8a9_Out_3);
            float2 _Vector2_9a3e446bd0dfc3868e3db6f9bf101ead_Out_0 = float2(1, 1);
            float2 _Vector2_8355e4dbd796b188ba1e500dcabf0c0f_Out_0 = float2(30, 30);
            float2 _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2;
            Unity_Divide_float2(_Vector2_9a3e446bd0dfc3868e3db6f9bf101ead_Out_0, _Vector2_8355e4dbd796b188ba1e500dcabf0c0f_Out_0, _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2);
            float2 _Divide_5d0f49ab4bcab689894f976217114a79_Out_2;
            Unity_Divide_float2(_TilingAndOffset_67d58b7a8092828aaece7f3acfe6e8a9_Out_3, _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2, _Divide_5d0f49ab4bcab689894f976217114a79_Out_2);
            float2 _Fraction_e4f0f05fb76fd3809f5ec855b54a4595_Out_1;
            Unity_Fraction_float2(_Divide_5d0f49ab4bcab689894f976217114a79_Out_2, _Fraction_e4f0f05fb76fd3809f5ec855b54a4595_Out_1);
            float2 _Multiply_cbcb0de832b1d08288916e46aa8d4df2_Out_2;
            Unity_Multiply_float2_float2(_Fraction_e4f0f05fb76fd3809f5ec855b54a4595_Out_1, _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2, _Multiply_cbcb0de832b1d08288916e46aa8d4df2_Out_2);
            float2 _Vector2_e6d2595367ed0380b42c7a74855838a5_Out_0 = float2(246, 246);
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_Width_0 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.z;
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_Height_2 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.w;
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_TexelWidth_3 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.x;
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_TexelHeight_4 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.y;
            float2 _Vector2_e8728bfbf194e5818f010927b4ba8d79_Out_0 = float2(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Width_0, _TexelSize_18bb9f416e386f87bee91aa46804bc56_Height_2);
            float2 _Multiply_561a2890964f4081a8cf5d16bccb889f_Out_2;
            Unity_Multiply_float2_float2(_Vector2_e6d2595367ed0380b42c7a74855838a5_Out_0, _Vector2_e8728bfbf194e5818f010927b4ba8d79_Out_0, _Multiply_561a2890964f4081a8cf5d16bccb889f_Out_2);
            float2 _Vector2_97c81bcbfd26f08385a101ac3da412f3_Out_0 = float2(10, 10);
            float2 _Multiply_bccda05b3809138d8da15484dd6a7b4e_Out_2;
            Unity_Multiply_float2_float2(_Vector2_e8728bfbf194e5818f010927b4ba8d79_Out_0, _Vector2_97c81bcbfd26f08385a101ac3da412f3_Out_0, _Multiply_bccda05b3809138d8da15484dd6a7b4e_Out_2);
            float2 _Subtract_710d4672e66f7780ad90ca6e19a733bd_Out_2;
            Unity_Subtract_float2(_Multiply_561a2890964f4081a8cf5d16bccb889f_Out_2, _Multiply_bccda05b3809138d8da15484dd6a7b4e_Out_2, _Subtract_710d4672e66f7780ad90ca6e19a733bd_Out_2);
            float2 _Multiply_cd542324eceadd8daaca18ab78d3fbf6_Out_2;
            Unity_Multiply_float2_float2(_Multiply_cbcb0de832b1d08288916e46aa8d4df2_Out_2, _Subtract_710d4672e66f7780ad90ca6e19a733bd_Out_2, _Multiply_cd542324eceadd8daaca18ab78d3fbf6_Out_2);
            float2 _Vector2_df86361e9c36e7828e3097cc1fa8a75b_Out_0 = float2(0.1, 0.1);
            float2 _Multiply_8e351d40d36634829f2bb0905b9b9342_Out_2;
            Unity_Multiply_float2_float2(_Multiply_cd542324eceadd8daaca18ab78d3fbf6_Out_2, _Vector2_df86361e9c36e7828e3097cc1fa8a75b_Out_0, _Multiply_8e351d40d36634829f2bb0905b9b9342_Out_2);
            float4 _UV_1ca38e193be034888b8a707af9d47210_Out_0 = IN.uv3;
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_R_1 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[0];
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[1];
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_B_3 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[2];
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_A_4 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[3];
            float _Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2;
            Unity_Multiply_float_float(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2, -1, _Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2);
            float _Comparison_e67d9847aa86858490dd0c37e68e7505_Out_2;
            Unity_Comparison_Greater_float(_Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2, 0, _Comparison_e67d9847aa86858490dd0c37e68e7505_Out_2);
            float _Divide_2b884a92fd783d82a9012917c2b83115_Out_2;
            Unity_Divide_float(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_R_1, _Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2, _Divide_2b884a92fd783d82a9012917c2b83115_Out_2);
            float _Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1;
            Unity_Arctangent_float(_Divide_2b884a92fd783d82a9012917c2b83115_Out_2, _Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1);
            float _Add_cf6b7aee4a487f8ca16b9171b64a8f0d_Out_2;
            Unity_Add_float(_Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1, 3.141592, _Add_cf6b7aee4a487f8ca16b9171b64a8f0d_Out_2);
            float _Branch_9de100b31215ba89bb19e5b89012a25a_Out_3;
            Unity_Branch_float(_Comparison_e67d9847aa86858490dd0c37e68e7505_Out_2, _Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1, _Add_cf6b7aee4a487f8ca16b9171b64a8f0d_Out_2, _Branch_9de100b31215ba89bb19e5b89012a25a_Out_3);
            float2 _Rotate_ff1406ef744b188ca79c6d3bf0b8bbd7_Out_3;
            Unity_Rotate_Radians_float(_Multiply_8e351d40d36634829f2bb0905b9b9342_Out_2, float2 (0.5, 0.5), _Branch_9de100b31215ba89bb19e5b89012a25a_Out_3, _Rotate_ff1406ef744b188ca79c6d3bf0b8bbd7_Out_3);
            #if defined(SHADER_API_GLES) && (SHADER_TARGET < 30)
              float4 _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0 = float4(0.0f, 0.0f, 0.0f, 1.0f);
            #else
              float4 _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0 = SAMPLE_TEXTURE2D_LOD(_Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0.tex, _Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0.samplerstate, _Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0.GetTransformedUV(_Rotate_ff1406ef744b188ca79c6d3bf0b8bbd7_Out_3), 0);
            #endif
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_R_5 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.r;
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_G_6 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.g;
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_B_7 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.b;
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_A_8 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.a;
            float _Comparison_1a93f0ca7c913a8f81659b20b8d59f2b_Out_2;
            Unity_Comparison_Greater_float(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2, 0.001, _Comparison_1a93f0ca7c913a8f81659b20b8d59f2b_Out_2);
            float3 _Vector3_37ab63b41d52bb8d8eba9088ed71bc11_Out_0 = float3(0, 0, 0);
            float2 _Vector2_4aeecd1cb271dd8e96c2fb12309b9dd0_Out_0 = float2(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_R_1, _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2);
            float _Distance_47e812ff6269a181b6e0faa017faee3b_Out_2;
            Unity_Distance_float2(_Vector2_4aeecd1cb271dd8e96c2fb12309b9dd0_Out_0, float2(0, 0), _Distance_47e812ff6269a181b6e0faa017faee3b_Out_2);
            float _Multiply_e5102f48c5e759879293e06062894e0c_Out_2;
            Unity_Multiply_float_float(_Distance_47e812ff6269a181b6e0faa017faee3b_Out_2, 1.6, _Multiply_e5102f48c5e759879293e06062894e0c_Out_2);
            float _Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3;
            Unity_Clamp_float(_Multiply_e5102f48c5e759879293e06062894e0c_Out_2, 0, 1, _Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3);
            float _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2;
            Unity_Subtract_float(1, _Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3, _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2);
            float3 _Vector3_aeff4a868e81c68f85dfe4c9662da9ce_Out_0 = float3(1, _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2, 0);
            float _Absolute_5573b6b2c1616e819aa4eeb67caec457_Out_1;
            Unity_Absolute_float(_Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3, _Absolute_5573b6b2c1616e819aa4eeb67caec457_Out_1);
            float _Power_33dedbf009cacf848c403a58667a66c6_Out_2;
            Unity_Power_float(_Absolute_5573b6b2c1616e819aa4eeb67caec457_Out_1, 0.16, _Power_33dedbf009cacf848c403a58667a66c6_Out_2);
            float _Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3;
            Unity_Clamp_float(_Power_33dedbf009cacf848c403a58667a66c6_Out_2, 0, 1, _Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3);
            float3 _Lerp_e6000a54e83a8f8e8c5bc16419b360ca_Out_3;
            Unity_Lerp_float3(_Vector3_37ab63b41d52bb8d8eba9088ed71bc11_Out_0, _Vector3_aeff4a868e81c68f85dfe4c9662da9ce_Out_0, (_Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3.xxx), _Lerp_e6000a54e83a8f8e8c5bc16419b360ca_Out_3);
            float3 _Vector3_484f28086891e98d9589d1f65774f6f4_Out_0 = float3(0, _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2, 1);
            float3 _Lerp_93469a90f98ccf80a7121c0b2bf05f48_Out_3;
            Unity_Lerp_float3(_Vector3_37ab63b41d52bb8d8eba9088ed71bc11_Out_0, _Vector3_484f28086891e98d9589d1f65774f6f4_Out_0, (_Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3.xxx), _Lerp_93469a90f98ccf80a7121c0b2bf05f48_Out_3);
            float3 _Branch_365bf259ae64c386a18e2d6997727100_Out_3;
            Unity_Branch_float3(_Comparison_1a93f0ca7c913a8f81659b20b8d59f2b_Out_2, _Lerp_e6000a54e83a8f8e8c5bc16419b360ca_Out_3, _Lerp_93469a90f98ccf80a7121c0b2bf05f48_Out_3, _Branch_365bf259ae64c386a18e2d6997727100_Out_3);
            float3 _Multiply_0563147e8bc9868b8c2a4f2c442c349f_Out_2;
            Unity_Multiply_float3_float3((_SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_A_8.xxx), _Branch_365bf259ae64c386a18e2d6997727100_Out_3, _Multiply_0563147e8bc9868b8c2a4f2c442c349f_Out_2);
            surface.BaseColor = _Multiply_0563147e8bc9868b8c2a4f2c442c349f_Out_2;
            surface.Emission = float3(0, 0, 0);
            surface.Alpha = 1;
            surface.AlphaClipThreshold = 0.5;
            {
                surface.VTPackedFeedback = float4(1.0f,1.0f,1.0f,1.0f);
            }
            return surface;
        }
        
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES AttributesMesh
            #define VaryingsMeshType VaryingsMeshToPS
            #define VFX_SRP_VARYINGS VaryingsMeshType
            #define VFX_SRP_SURFACE_INPUTS FragInputs
            #endif
            
        VertexDescriptionInputs AttributesMeshToVertexDescriptionInputs(AttributesMesh input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
        
            return output;
        }
        
        VertexDescription GetVertexDescription(AttributesMesh input, float3 timeParameters
        #ifdef HAVE_VFX_MODIFICATION
            , AttributesElement element
        #endif
        )
        {
            // build graph inputs
            VertexDescriptionInputs vertexDescriptionInputs = AttributesMeshToVertexDescriptionInputs(input);
            // Override time parameters with used one (This is required to correctly handle motion vector for vertex animation based on time)
        
            // evaluate vertex graph
        #ifdef HAVE_VFX_MODIFICATION
            GraphProperties properties;
            ZERO_INITIALIZE(GraphProperties, properties);
        
            // Fetch the vertex graph properties for the particle instance.
            GetElementVertexProperties(element, properties);
        
            VertexDescription vertexDescription = VertexDescriptionFunction(vertexDescriptionInputs, properties);
        #else
            VertexDescription vertexDescription = VertexDescriptionFunction(vertexDescriptionInputs);
        #endif
            return vertexDescription;
        
        }
        
        AttributesMesh ApplyMeshModification(AttributesMesh input, float3 timeParameters
        #ifdef USE_CUSTOMINTERP_SUBSTRUCT
            #ifdef TESSELLATION_ON
            , inout VaryingsMeshToDS varyings
            #else
            , inout VaryingsMeshToPS varyings
            #endif
        #endif
        #ifdef HAVE_VFX_MODIFICATION
                , AttributesElement element
        #endif
            )
        {
            VertexDescription vertexDescription = GetVertexDescription(input, timeParameters
        #ifdef HAVE_VFX_MODIFICATION
                , element
        #endif
            );
        
            // copy graph output to the results
            input.positionOS = vertexDescription.Position;
            input.normalOS = vertexDescription.Normal;
            input.tangentOS.xyz = vertexDescription.Tangent;
        
            
        
            return input;
        }
        
        #if defined(_ADD_CUSTOM_VELOCITY) // For shader graph custom velocity
        // Return precomputed Velocity in object space
        float3 GetCustomVelocity(AttributesMesh input
        #ifdef HAVE_VFX_MODIFICATION
            , AttributesElement element
        #endif
        )
        {
            VertexDescription vertexDescription = GetVertexDescription(input, _TimeParameters.xyz
        #ifdef HAVE_VFX_MODIFICATION
                , element
        #endif
            );
            return vertexDescription.CustomVelocity;
        }
        #endif
        
        FragInputs BuildFragInputs(VaryingsMeshToPS input)
        {
            FragInputs output;
            ZERO_INITIALIZE(FragInputs, output);
        
            // Init to some default value to make the computer quiet (else it output 'divide by zero' warning even if value is not used).
            // TODO: this is a really poor workaround, but the variable is used in a bunch of places
            // to compute normals which are then passed on elsewhere to compute other values...
            output.tangentToWorld = k_identity3x3;
            output.positionSS = input.positionCS;       // input.positionCS is SV_Position
        
            output.positionRWS =                input.positionRWS;
            output.texCoord0 =                  input.texCoord0;
            output.texCoord1 =                  input.texCoord1;
            output.texCoord2 =                  input.texCoord2;
            output.texCoord3 =                  input.texCoord3;
        
        #ifdef HAVE_VFX_MODIFICATION
            // FragInputs from VFX come from two places: Interpolator or CBuffer.
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            // splice point to copy custom interpolator fields from varyings to frag inputs
            
        
            return output;
        }
        
        // existing HDRP code uses the combined function to go directly from packed to frag inputs
        FragInputs UnpackVaryingsMeshToFragInputs(PackedVaryingsMeshToPS input)
        {
            UNITY_SETUP_INSTANCE_ID(input);
        #if defined(HAVE_VFX_MODIFICATION) && defined(UNITY_INSTANCING_ENABLED)
            unity_InstanceID = input.instanceID;
        #endif
            VaryingsMeshToPS unpacked = UnpackVaryingsMeshToPS(input);
            return BuildFragInputs(unpacked);
        }
            SurfaceDescriptionInputs FragInputsToSurfaceDescriptionInputs(FragInputs input, float3 viewWS)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
            #if defined(SHADER_STAGE_RAY_TRACING)
            #else
            #endif
        
        #if UNITY_UV_STARTS_AT_TOP
        #else
        #endif
        
        
            output.uv0 =                                        input.texCoord0;
            output.uv1 =                                        input.texCoord1;
            output.uv2 =                                        input.texCoord2;
            output.uv3 =                                        input.texCoord3;
        
            // splice point to copy frag inputs custom interpolator pack into the SDI
            
        
            return output;
        }
        
            // --------------------------------------------------
            // Build Surface Data (Specific Material)
        
        void BuildSurfaceData(FragInputs fragInputs, inout SurfaceDescription surfaceDescription, float3 V, PositionInputs posInput, out SurfaceData surfaceData)
        {
            // setup defaults -- these are used if the graph doesn't output a value
            ZERO_INITIALIZE(SurfaceData, surfaceData);
        
            // copy across graph values, if defined
            surfaceData.color = surfaceDescription.BaseColor;
        
            #ifdef WRITE_NORMAL_BUFFER
            // When we need to export the normal (in the depth prepass, we write the geometry one)
            surfaceData.normalWS = fragInputs.tangentToWorld[2];
            #endif
        
            #if defined(DEBUG_DISPLAY)
            if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
            {
                // TODO
            }
            #endif
        
            #ifdef _ENABLE_SHADOW_MATTE
        
                #if (SHADERPASS == SHADERPASS_FORWARD_UNLIT) || (SHADERPASS == SHADERPASS_RAYTRACING_GBUFFER) || (SHADERPASS == SHADERPASS_RAYTRACING_INDIRECT) || (SHADERPASS == SHADERPASS_RAYTRACING_FORWARD)
        
                    HDShadowContext shadowContext = InitShadowContext();
        
                    // Evaluate the shadow, the normal is guaranteed if shadow matte is enabled on this shader.
                    float3 shadow3;
                    ShadowLoopMin(shadowContext, posInput, normalize(fragInputs.tangentToWorld[2]), asuint(_ShadowMatteFilter), GetMeshRenderingLightLayer(), shadow3);
        
                    // Compute the average value in the fourth channel
                    float4 shadow = float4(shadow3, dot(shadow3, float3(1.0/3.0, 1.0/3.0, 1.0/3.0)));
        
                    float4 shadowColor = (1.0 - shadow) * surfaceDescription.ShadowTint.rgba;
                    float  localAlpha  = saturate(shadowColor.a + surfaceDescription.Alpha);
        
                    // Keep the nested lerp
                    // With no Color (bsdfData.color.rgb, bsdfData.color.a == 0.0f), just use ShadowColor*Color to avoid a ring of "white" around the shadow
                    // And mix color to consider the Color & ShadowColor alpha (from texture or/and color picker)
                    #ifdef _SURFACE_TYPE_TRANSPARENT
                        surfaceData.color = lerp(shadowColor.rgb * surfaceData.color, lerp(lerp(shadowColor.rgb, surfaceData.color, 1.0 - surfaceDescription.ShadowTint.a), surfaceData.color, shadow.rgb), surfaceDescription.Alpha);
                    #else
                        surfaceData.color = lerp(lerp(shadowColor.rgb, surfaceData.color, 1.0 - surfaceDescription.ShadowTint.a), surfaceData.color, shadow.rgb);
                    #endif
                    localAlpha = ApplyBlendMode(surfaceData.color, localAlpha).a;
        
                    surfaceDescription.Alpha = localAlpha;
        
                #elif SHADERPASS == SHADERPASS_PATH_TRACING
        
                    surfaceData.normalWS = fragInputs.tangentToWorld[2];
                    surfaceData.shadowTint = surfaceDescription.ShadowTint.rgba;
        
                #endif
        
            #endif // _ENABLE_SHADOW_MATTE
        }
        
            // --------------------------------------------------
            // Get Surface And BuiltinData
        
            void GetSurfaceAndBuiltinData(FragInputs fragInputs, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData RAY_TRACING_OPTIONAL_PARAMETERS)
            {
                // Don't dither if displaced tessellation (we're fading out the displacement instead to match the next LOD)
                #if !defined(SHADER_STAGE_RAY_TRACING) && !defined(_TESSELLATION_DISPLACEMENT)
                #ifdef LOD_FADE_CROSSFADE // enable dithering LOD transition if user select CrossFade transition in LOD group
                LODDitheringTransition(ComputeFadeMaskSeed(V, posInput.positionSS), unity_LODFade.x);
                #endif
                #endif
        
                #ifndef SHADER_UNLIT
                #ifdef _DOUBLESIDED_ON
                    float3 doubleSidedConstants = _DoubleSidedConstants.xyz;
                #else
                    float3 doubleSidedConstants = float3(1.0, 1.0, 1.0);
                #endif
        
                ApplyDoubleSidedFlipOrMirror(fragInputs, doubleSidedConstants); // Apply double sided flip on the vertex normal
                #endif // SHADER_UNLIT
        
                SurfaceDescriptionInputs surfaceDescriptionInputs = FragInputsToSurfaceDescriptionInputs(fragInputs, V);
        
                #if defined(HAVE_VFX_MODIFICATION)
                GraphProperties properties;
                ZERO_INITIALIZE(GraphProperties, properties);
        
                GetElementPixelProperties(fragInputs, properties);
        
                SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs, properties);
                #else
                SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);
                #endif
        
                // Perform alpha test very early to save performance (a killed pixel will not sample textures)
                // TODO: split graph evaluation to grab just alpha dependencies first? tricky..
                #ifdef _ALPHATEST_ON
                    float alphaCutoff = surfaceDescription.AlphaClipThreshold;
                    #if SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_PREPASS
                    // The TransparentDepthPrepass is also used with SSR transparent.
                    // If an artists enable transaprent SSR but not the TransparentDepthPrepass itself, then we use AlphaClipThreshold
                    // otherwise if TransparentDepthPrepass is enabled we use AlphaClipThresholdDepthPrepass
                    #elif SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_POSTPASS
                    // DepthPostpass always use its own alpha threshold
                    alphaCutoff = surfaceDescription.AlphaClipThresholdDepthPostpass;
                    #elif (SHADERPASS == SHADERPASS_SHADOWS) || (SHADERPASS == SHADERPASS_RAYTRACING_VISIBILITY)
                    // If use shadow threshold isn't enable we don't allow any test
                    #endif
        
                    GENERIC_ALPHA_TEST(surfaceDescription.Alpha, alphaCutoff);
                #endif
        
                #if !defined(SHADER_STAGE_RAY_TRACING) && _DEPTHOFFSET_ON
                ApplyDepthOffsetPositionInput(V, surfaceDescription.DepthOffset, GetViewForwardDir(), GetWorldToHClipMatrix(), posInput);
                #endif
        
                #ifndef SHADER_UNLIT
                float3 bentNormalWS;
                BuildSurfaceData(fragInputs, surfaceDescription, V, posInput, surfaceData, bentNormalWS);
        
                #ifdef FRAG_INPUTS_USE_TEXCOORD1
                    float4 lightmapTexCoord1 = fragInputs.texCoord1;
                #else
                    float4 lightmapTexCoord1 = float4(0,0,0,0);
                #endif
        
                #ifdef FRAG_INPUTS_USE_TEXCOORD2
                    float4 lightmapTexCoord2 = fragInputs.texCoord2;
                #else
                    float4 lightmapTexCoord2 = float4(0,0,0,0);
                #endif
        
                // Builtin Data
                // For back lighting we use the oposite vertex normal
                InitBuiltinData(posInput, surfaceDescription.Alpha, bentNormalWS, -fragInputs.tangentToWorld[2], lightmapTexCoord1, lightmapTexCoord2, builtinData);
        
                #else
                BuildSurfaceData(fragInputs, surfaceDescription, V, posInput, surfaceData);
        
                ZERO_BUILTIN_INITIALIZE(builtinData); // No call to InitBuiltinData as we don't have any lighting
                builtinData.opacity = surfaceDescription.Alpha;
        
                #if defined(DEBUG_DISPLAY)
                    // Light Layers are currently not used for the Unlit shader (because it is not lit)
                    // But Unlit objects do cast shadows according to their rendering layer mask, which is what we want to
                    // display in the light layers visualization mode, therefore we need the renderingLayers
                    builtinData.renderingLayers = GetMeshRenderingLightLayer();
                #endif
        
                #endif // SHADER_UNLIT
        
                #ifdef _ALPHATEST_ON
                    // Used for sharpening by alpha to mask - Alpha to covertage is only used with depth only and forward pass (no shadow pass, no transparent pass)
                    builtinData.alphaClipTreshold = alphaCutoff;
                #endif
        
                // override sampleBakedGI - not used by Unlit
        
                builtinData.emissiveColor = surfaceDescription.Emission;
        
                // Note this will not fully work on transparent surfaces (can check with _SURFACE_TYPE_TRANSPARENT define)
                // We will always overwrite vt feeback with the nearest. So behind transparent surfaces vt will not be resolved
                // This is a limitation of the current MRT approach.
                #ifdef UNITY_VIRTUAL_TEXTURING
                builtinData.vtPackedFeedback = surfaceDescription.VTPackedFeedback;
                #endif
        
                #if _DEPTHOFFSET_ON
                builtinData.depthOffset = surfaceDescription.DepthOffset;
                #endif
        
                // TODO: We should generate distortion / distortionBlur for non distortion pass
                #if (SHADERPASS == SHADERPASS_DISTORTION)
                builtinData.distortion = surfaceDescription.Distortion;
                builtinData.distortionBlur = surfaceDescription.DistortionBlur;
                #endif
        
                #ifndef SHADER_UNLIT
                // PostInitBuiltinData call ApplyDebugToBuiltinData
                PostInitBuiltinData(V, posInput, surfaceData, builtinData);
                #else
                ApplyDebugToBuiltinData(builtinData);
                #endif
        
                RAY_TRACING_OPTIONAL_ALPHA_TEST_PASS
            }
        
            // --------------------------------------------------
            // Main
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassForwardUnlit.hlsl"
        
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
        
        	#ifdef HAVE_VFX_MODIFICATION
        	#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/VisualEffectVertex.hlsl"
        	#endif
        
            ENDHLSL
        }
        Pass
        {
            Name "FullScreenDebug"
            Tags
            {
                "LightMode" = "FullScreenDebug"
            }
        
            // Render State
            Cull [_CullMode]
        ZTest LEqual
        ZWrite Off
        
            // Debug
            // <None>
        
            // --------------------------------------------------
            // Pass
        
            HLSLPROGRAM
        
            // Pragmas
            #pragma multi_compile _ DOTS_INSTANCING_ON
        #pragma instancing_options renderinglayer
        #pragma target 4.5
        #pragma vertex Vert
        #pragma fragment Frag
        #pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal switch
        #pragma multi_compile_instancing
        
            // Keywords
            #pragma shader_feature_local _ _ALPHATEST_ON
        #pragma shader_feature _ _SURFACE_TYPE_TRANSPARENT
        #pragma shader_feature_local _BLENDMODE_OFF _BLENDMODE_ALPHA _BLENDMODE_ADD _BLENDMODE_PRE_MULTIPLY
        #pragma shader_feature_local _ _ADD_PRECOMPUTED_VELOCITY
        #pragma shader_feature_local _ _TRANSPARENT_WRITES_MOTION_VEC
        #pragma shader_feature_local_fragment _ _ENABLE_FOG_ON_TRANSPARENT
            // GraphKeywords: <None>
        
            // For custom interpolators to inject a substruct definition before FragInputs definition,
            // allowing for FragInputs to capture CI's intended for ShaderGraph's SDI.
            struct CustomInterpolators
        {
        };
        #define USE_CUSTOMINTERP_SUBSTRUCT
        
        
        
            // TODO: Merge FragInputsVFX substruct with CustomInterpolators.
        	#ifdef HAVE_VFX_MODIFICATION
        	struct FragInputsVFX
            {
                /* WARNING: $splice Could not find named fragment 'FragInputsVFX' */
            };
            #endif
        
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GeometricTools.hlsl" // Required by Tessellation.hlsl
        	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Tessellation.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl" // Required to be include before we include properties as it define DECLARE_STACK_CB
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphHeader.hlsl" // Need to be here for Gradient struct definition
        
            // --------------------------------------------------
            // Defines
        
            // Attribute
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define ATTRIBUTES_NEED_TEXCOORD2
            #define ATTRIBUTES_NEED_TEXCOORD3
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_TEXCOORD1
            #define VARYINGS_NEED_TEXCOORD2
            #define VARYINGS_NEED_TEXCOORD3
        
            #define HAVE_MESH_MODIFICATION
        
            //Strip down the FragInputs.hlsl (on graphics), so we can only optimize the interpolators we use.
            //if by accident something requests contents of FragInputs.hlsl, it will be caught as a compiler error
            //Frag inputs stripping is only enabled when FRAG_INPUTS_ENABLE_STRIPPING is set
            #if !defined(SHADER_STAGE_RAY_TRACING) && SHADERPASS != SHADERPASS_RAYTRACING_GBUFFER && SHADERPASS != SHADERPASS_FULL_SCREEN_DEBUG
            #define FRAG_INPUTS_ENABLE_STRIPPING
            #endif
            #define FRAG_INPUTS_USE_TEXCOORD0
            #define FRAG_INPUTS_USE_TEXCOORD1
            #define FRAG_INPUTS_USE_TEXCOORD2
            #define FRAG_INPUTS_USE_TEXCOORD3
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
        
        
        
            #define SHADERPASS SHADERPASS_FULL_SCREEN_DEBUG
        #define RAYTRACING_SHADER_GRAPH_DEFAULT
        
        
            // Following two define are a workaround introduce in 10.1.x for RaytracingQualityNode
            // The ShaderGraph don't support correctly migration of this node as it serialize all the node data
            // in the json file making it impossible to uprgrade. Until we get a fix, we do a workaround here
            // to still allow us to rename the field and keyword of this node without breaking existing code.
            #ifdef RAYTRACING_SHADER_GRAPH_DEFAULT
            #define RAYTRACING_SHADER_GRAPH_HIGH
            #endif
        
            #ifdef RAYTRACING_SHADER_GRAPH_RAYTRACED
            #define RAYTRACING_SHADER_GRAPH_LOW
            #endif
            // end
        
            #ifndef SHADER_UNLIT
            // We need isFrontFace when using double sided - it is not required for unlit as in case of unlit double sided only drive the cullmode
            // VARYINGS_NEED_CULLFACE can be define by VaryingsMeshToPS.FaceSign input if a IsFrontFace Node is included in the shader graph.
            #if defined(_DOUBLESIDED_ON) && !defined(VARYINGS_NEED_CULLFACE)
                #define VARYINGS_NEED_CULLFACE
            #endif
            #endif
        
            // Specific Material Define
        // Setup a define to say we are an unlit shader
        #define SHADER_UNLIT
        
        // Following Macro are only used by Unlit material
        #if defined(_ENABLE_SHADOW_MATTE) && (SHADERPASS == SHADERPASS_FORWARD_UNLIT || SHADERPASS == SHADERPASS_PATH_TRACING)
        #define LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
        #define HAS_LIGHTLOOP
        #endif
            // Caution: we can use the define SHADER_UNLIT onlit after the above Material include as it is the Unlit template who define it
        
            // To handle SSR on transparent correctly with a possibility to enable/disable it per framesettings
            // we should have a code like this:
            // if !defined(_DISABLE_SSR_TRANSPARENT)
            // pragma multi_compile _ WRITE_NORMAL_BUFFER
            // endif
            // i.e we enable the multicompile only if we can receive SSR or not, and then C# code drive
            // it based on if SSR transparent in frame settings and not (and stripper can strip it).
            // this is currently not possible with our current preprocessor as _DISABLE_SSR_TRANSPARENT is a keyword not a define
            // so instead we used this and chose to pay the extra cost of normal write even if SSR transaprent is disabled.
            // Ideally the shader graph generator should handle it but condition below can't be handle correctly for now.
            #if SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_PREPASS
            #if !defined(_DISABLE_SSR_TRANSPARENT) && !defined(SHADER_UNLIT)
                #define WRITE_NORMAL_BUFFER
            #endif
            #endif
        
            #ifndef DEBUG_DISPLAY
                // In case of opaque we don't want to perform the alpha test, it is done in depth prepass and we use depth equal for ztest (setup from UI)
                // Don't do it with debug display mode as it is possible there is no depth prepass in this case
                #if !defined(_SURFACE_TYPE_TRANSPARENT)
                    #if SHADERPASS == SHADERPASS_FORWARD
                    #define SHADERPASS_FORWARD_BYPASS_ALPHA_TEST
                    #elif SHADERPASS == SHADERPASS_GBUFFER
                    #define SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST
                    #endif
                #endif
            #endif
        
            // Define _DEFERRED_CAPABLE_MATERIAL for shader capable to run in deferred pass
            #if defined(SHADER_LIT) && !defined(_SURFACE_TYPE_TRANSPARENT)
                #define _DEFERRED_CAPABLE_MATERIAL
            #endif
        
            // Translate transparent motion vector define
            #if defined(_TRANSPARENT_WRITES_MOTION_VEC) && defined(_SURFACE_TYPE_TRANSPARENT)
                #define _WRITE_TRANSPARENT_MOTION_VECTOR
            #endif
        
            // -- Graph Properties
            CBUFFER_START(UnityPerMaterial)
        float4 _TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1_TexelSize;
        float4 _Direction_TexelSize;
        float2 _TextureSize;
        float _RotateUV;
        float Boolean_F2B1E91A;
        float4 _EmissionColor;
        float _UseShadowThreshold;
        float4 _DoubleSidedConstants;
        float _BlendMode;
        float _EnableBlendModePreserveSpecularLighting;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1);
        SAMPLER(sampler_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1);
        TEXTURE2D(_Direction);
        SAMPLER(sampler_Direction);
        
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
        
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
        
            // Includes
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Debug/DebugDisplay.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/NormalSurfaceGradient.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl"
        	// GraphIncludes: <None>
        
            // --------------------------------------------------
            // Structs and Packing
        
            struct AttributesMesh
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
             float4 uv1 : TEXCOORD1;
             float4 uv2 : TEXCOORD2;
             float4 uv3 : TEXCOORD3;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct VaryingsMeshToPS
        {
            SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
             float4 texCoord0;
             float4 texCoord1;
             float4 texCoord2;
             float4 texCoord3;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
        };
        struct SurfaceDescriptionInputs
        {
             float4 uv0;
             float4 uv1;
             float4 uv2;
             float4 uv3;
        };
        struct PackedVaryingsMeshToPS
        {
            SV_POSITION_QUALIFIERS float4 positionCS : SV_POSITION;
             float4 interp0 : INTERP0;
             float4 interp1 : INTERP1;
             float4 interp2 : INTERP2;
             float4 interp3 : INTERP3;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
        };
        
            PackedVaryingsMeshToPS PackVaryingsMeshToPS (VaryingsMeshToPS input)
        {
            PackedVaryingsMeshToPS output;
            ZERO_INITIALIZE(PackedVaryingsMeshToPS, output);
            output.positionCS = input.positionCS;
            output.interp0.xyzw =  input.texCoord0;
            output.interp1.xyzw =  input.texCoord1;
            output.interp2.xyzw =  input.texCoord2;
            output.interp3.xyzw =  input.texCoord3;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            return output;
        }
        
        VaryingsMeshToPS UnpackVaryingsMeshToPS (PackedVaryingsMeshToPS input)
        {
            VaryingsMeshToPS output;
            output.positionCS = input.positionCS;
            output.texCoord0 = input.interp0.xyzw;
            output.texCoord1 = input.interp1.xyzw;
            output.texCoord2 = input.interp2.xyzw;
            output.texCoord3 = input.interp3.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            return output;
        }
        
        
            // --------------------------------------------------
            // Graph
        
        
            // Graph Functions
            
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A / B;
        }
        
        void Unity_Fraction_float2(float2 In, out float2 Out)
        {
            Out = frac(In);
        }
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A * B;
        }
        
        void Unity_Subtract_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A - B;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_Comparison_Greater_float(float A, float B, out float Out)
        {
            Out = A > B ? 1 : 0;
        }
        
        void Unity_Divide_float(float A, float B, out float Out)
        {
            Out = A / B;
        }
        
        void Unity_Arctangent_float(float In, out float Out)
        {
            Out = atan(In);
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Branch_float(float Predicate, float True, float False, out float Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Rotate_Radians_float(float2 UV, float2 Center, float Rotation, out float2 Out)
        {
            //rotation matrix
            UV -= Center;
            float s = sin(Rotation);
            float c = cos(Rotation);
        
            //center rotation matrix
            float2x2 rMatrix = float2x2(c, -s, s, c);
            rMatrix *= 0.5;
            rMatrix += 0.5;
            rMatrix = rMatrix*2 - 1;
        
            //multiply the UVs by the rotation matrix
            UV.xy = mul(UV.xy, rMatrix);
            UV += Center;
        
            Out = UV;
        }
        
        void Unity_Distance_float2(float2 A, float2 B, out float Out)
        {
            Out = distance(A, B);
        }
        
        void Unity_Clamp_float(float In, float Min, float Max, out float Out)
        {
            Out = clamp(In, Min, Max);
        }
        
        void Unity_Subtract_float(float A, float B, out float Out)
        {
            Out = A - B;
        }
        
        void Unity_Absolute_float(float In, out float Out)
        {
            Out = abs(In);
        }
        
        void Unity_Power_float(float A, float B, out float Out)
        {
            Out = pow(A, B);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A * B;
        }
        
            // Graph Vertex
            struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
            // Graph Pixel
            struct SurfaceDescription
        {
            float3 BaseColor;
            float3 Emission;
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0 = UnityBuildTexture2DStructNoScale(_Direction);
            float4 _UV_7c8a457b34527181924b2b4a71350de9_Out_0 = IN.uv0;
            float2 _TilingAndOffset_67d58b7a8092828aaece7f3acfe6e8a9_Out_3;
            Unity_TilingAndOffset_float((_UV_7c8a457b34527181924b2b4a71350de9_Out_0.xy), float2 (2, 2), float2 (0, 0), _TilingAndOffset_67d58b7a8092828aaece7f3acfe6e8a9_Out_3);
            float2 _Vector2_9a3e446bd0dfc3868e3db6f9bf101ead_Out_0 = float2(1, 1);
            float2 _Vector2_8355e4dbd796b188ba1e500dcabf0c0f_Out_0 = float2(30, 30);
            float2 _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2;
            Unity_Divide_float2(_Vector2_9a3e446bd0dfc3868e3db6f9bf101ead_Out_0, _Vector2_8355e4dbd796b188ba1e500dcabf0c0f_Out_0, _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2);
            float2 _Divide_5d0f49ab4bcab689894f976217114a79_Out_2;
            Unity_Divide_float2(_TilingAndOffset_67d58b7a8092828aaece7f3acfe6e8a9_Out_3, _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2, _Divide_5d0f49ab4bcab689894f976217114a79_Out_2);
            float2 _Fraction_e4f0f05fb76fd3809f5ec855b54a4595_Out_1;
            Unity_Fraction_float2(_Divide_5d0f49ab4bcab689894f976217114a79_Out_2, _Fraction_e4f0f05fb76fd3809f5ec855b54a4595_Out_1);
            float2 _Multiply_cbcb0de832b1d08288916e46aa8d4df2_Out_2;
            Unity_Multiply_float2_float2(_Fraction_e4f0f05fb76fd3809f5ec855b54a4595_Out_1, _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2, _Multiply_cbcb0de832b1d08288916e46aa8d4df2_Out_2);
            float2 _Vector2_e6d2595367ed0380b42c7a74855838a5_Out_0 = float2(246, 246);
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_Width_0 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.z;
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_Height_2 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.w;
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_TexelWidth_3 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.x;
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_TexelHeight_4 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.y;
            float2 _Vector2_e8728bfbf194e5818f010927b4ba8d79_Out_0 = float2(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Width_0, _TexelSize_18bb9f416e386f87bee91aa46804bc56_Height_2);
            float2 _Multiply_561a2890964f4081a8cf5d16bccb889f_Out_2;
            Unity_Multiply_float2_float2(_Vector2_e6d2595367ed0380b42c7a74855838a5_Out_0, _Vector2_e8728bfbf194e5818f010927b4ba8d79_Out_0, _Multiply_561a2890964f4081a8cf5d16bccb889f_Out_2);
            float2 _Vector2_97c81bcbfd26f08385a101ac3da412f3_Out_0 = float2(10, 10);
            float2 _Multiply_bccda05b3809138d8da15484dd6a7b4e_Out_2;
            Unity_Multiply_float2_float2(_Vector2_e8728bfbf194e5818f010927b4ba8d79_Out_0, _Vector2_97c81bcbfd26f08385a101ac3da412f3_Out_0, _Multiply_bccda05b3809138d8da15484dd6a7b4e_Out_2);
            float2 _Subtract_710d4672e66f7780ad90ca6e19a733bd_Out_2;
            Unity_Subtract_float2(_Multiply_561a2890964f4081a8cf5d16bccb889f_Out_2, _Multiply_bccda05b3809138d8da15484dd6a7b4e_Out_2, _Subtract_710d4672e66f7780ad90ca6e19a733bd_Out_2);
            float2 _Multiply_cd542324eceadd8daaca18ab78d3fbf6_Out_2;
            Unity_Multiply_float2_float2(_Multiply_cbcb0de832b1d08288916e46aa8d4df2_Out_2, _Subtract_710d4672e66f7780ad90ca6e19a733bd_Out_2, _Multiply_cd542324eceadd8daaca18ab78d3fbf6_Out_2);
            float2 _Vector2_df86361e9c36e7828e3097cc1fa8a75b_Out_0 = float2(0.1, 0.1);
            float2 _Multiply_8e351d40d36634829f2bb0905b9b9342_Out_2;
            Unity_Multiply_float2_float2(_Multiply_cd542324eceadd8daaca18ab78d3fbf6_Out_2, _Vector2_df86361e9c36e7828e3097cc1fa8a75b_Out_0, _Multiply_8e351d40d36634829f2bb0905b9b9342_Out_2);
            float4 _UV_1ca38e193be034888b8a707af9d47210_Out_0 = IN.uv3;
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_R_1 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[0];
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[1];
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_B_3 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[2];
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_A_4 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[3];
            float _Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2;
            Unity_Multiply_float_float(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2, -1, _Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2);
            float _Comparison_e67d9847aa86858490dd0c37e68e7505_Out_2;
            Unity_Comparison_Greater_float(_Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2, 0, _Comparison_e67d9847aa86858490dd0c37e68e7505_Out_2);
            float _Divide_2b884a92fd783d82a9012917c2b83115_Out_2;
            Unity_Divide_float(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_R_1, _Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2, _Divide_2b884a92fd783d82a9012917c2b83115_Out_2);
            float _Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1;
            Unity_Arctangent_float(_Divide_2b884a92fd783d82a9012917c2b83115_Out_2, _Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1);
            float _Add_cf6b7aee4a487f8ca16b9171b64a8f0d_Out_2;
            Unity_Add_float(_Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1, 3.141592, _Add_cf6b7aee4a487f8ca16b9171b64a8f0d_Out_2);
            float _Branch_9de100b31215ba89bb19e5b89012a25a_Out_3;
            Unity_Branch_float(_Comparison_e67d9847aa86858490dd0c37e68e7505_Out_2, _Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1, _Add_cf6b7aee4a487f8ca16b9171b64a8f0d_Out_2, _Branch_9de100b31215ba89bb19e5b89012a25a_Out_3);
            float2 _Rotate_ff1406ef744b188ca79c6d3bf0b8bbd7_Out_3;
            Unity_Rotate_Radians_float(_Multiply_8e351d40d36634829f2bb0905b9b9342_Out_2, float2 (0.5, 0.5), _Branch_9de100b31215ba89bb19e5b89012a25a_Out_3, _Rotate_ff1406ef744b188ca79c6d3bf0b8bbd7_Out_3);
            #if defined(SHADER_API_GLES) && (SHADER_TARGET < 30)
              float4 _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0 = float4(0.0f, 0.0f, 0.0f, 1.0f);
            #else
              float4 _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0 = SAMPLE_TEXTURE2D_LOD(_Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0.tex, _Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0.samplerstate, _Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0.GetTransformedUV(_Rotate_ff1406ef744b188ca79c6d3bf0b8bbd7_Out_3), 0);
            #endif
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_R_5 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.r;
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_G_6 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.g;
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_B_7 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.b;
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_A_8 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.a;
            float _Comparison_1a93f0ca7c913a8f81659b20b8d59f2b_Out_2;
            Unity_Comparison_Greater_float(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2, 0.001, _Comparison_1a93f0ca7c913a8f81659b20b8d59f2b_Out_2);
            float3 _Vector3_37ab63b41d52bb8d8eba9088ed71bc11_Out_0 = float3(0, 0, 0);
            float2 _Vector2_4aeecd1cb271dd8e96c2fb12309b9dd0_Out_0 = float2(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_R_1, _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2);
            float _Distance_47e812ff6269a181b6e0faa017faee3b_Out_2;
            Unity_Distance_float2(_Vector2_4aeecd1cb271dd8e96c2fb12309b9dd0_Out_0, float2(0, 0), _Distance_47e812ff6269a181b6e0faa017faee3b_Out_2);
            float _Multiply_e5102f48c5e759879293e06062894e0c_Out_2;
            Unity_Multiply_float_float(_Distance_47e812ff6269a181b6e0faa017faee3b_Out_2, 1.6, _Multiply_e5102f48c5e759879293e06062894e0c_Out_2);
            float _Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3;
            Unity_Clamp_float(_Multiply_e5102f48c5e759879293e06062894e0c_Out_2, 0, 1, _Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3);
            float _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2;
            Unity_Subtract_float(1, _Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3, _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2);
            float3 _Vector3_aeff4a868e81c68f85dfe4c9662da9ce_Out_0 = float3(1, _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2, 0);
            float _Absolute_5573b6b2c1616e819aa4eeb67caec457_Out_1;
            Unity_Absolute_float(_Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3, _Absolute_5573b6b2c1616e819aa4eeb67caec457_Out_1);
            float _Power_33dedbf009cacf848c403a58667a66c6_Out_2;
            Unity_Power_float(_Absolute_5573b6b2c1616e819aa4eeb67caec457_Out_1, 0.16, _Power_33dedbf009cacf848c403a58667a66c6_Out_2);
            float _Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3;
            Unity_Clamp_float(_Power_33dedbf009cacf848c403a58667a66c6_Out_2, 0, 1, _Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3);
            float3 _Lerp_e6000a54e83a8f8e8c5bc16419b360ca_Out_3;
            Unity_Lerp_float3(_Vector3_37ab63b41d52bb8d8eba9088ed71bc11_Out_0, _Vector3_aeff4a868e81c68f85dfe4c9662da9ce_Out_0, (_Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3.xxx), _Lerp_e6000a54e83a8f8e8c5bc16419b360ca_Out_3);
            float3 _Vector3_484f28086891e98d9589d1f65774f6f4_Out_0 = float3(0, _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2, 1);
            float3 _Lerp_93469a90f98ccf80a7121c0b2bf05f48_Out_3;
            Unity_Lerp_float3(_Vector3_37ab63b41d52bb8d8eba9088ed71bc11_Out_0, _Vector3_484f28086891e98d9589d1f65774f6f4_Out_0, (_Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3.xxx), _Lerp_93469a90f98ccf80a7121c0b2bf05f48_Out_3);
            float3 _Branch_365bf259ae64c386a18e2d6997727100_Out_3;
            Unity_Branch_float3(_Comparison_1a93f0ca7c913a8f81659b20b8d59f2b_Out_2, _Lerp_e6000a54e83a8f8e8c5bc16419b360ca_Out_3, _Lerp_93469a90f98ccf80a7121c0b2bf05f48_Out_3, _Branch_365bf259ae64c386a18e2d6997727100_Out_3);
            float3 _Multiply_0563147e8bc9868b8c2a4f2c442c349f_Out_2;
            Unity_Multiply_float3_float3((_SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_A_8.xxx), _Branch_365bf259ae64c386a18e2d6997727100_Out_3, _Multiply_0563147e8bc9868b8c2a4f2c442c349f_Out_2);
            surface.BaseColor = _Multiply_0563147e8bc9868b8c2a4f2c442c349f_Out_2;
            surface.Emission = float3(0, 0, 0);
            surface.Alpha = 1;
            surface.AlphaClipThreshold = 0.5;
            return surface;
        }
        
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES AttributesMesh
            #define VaryingsMeshType VaryingsMeshToPS
            #define VFX_SRP_VARYINGS VaryingsMeshType
            #define VFX_SRP_SURFACE_INPUTS FragInputs
            #endif
            
        VertexDescriptionInputs AttributesMeshToVertexDescriptionInputs(AttributesMesh input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
        
            return output;
        }
        
        VertexDescription GetVertexDescription(AttributesMesh input, float3 timeParameters
        #ifdef HAVE_VFX_MODIFICATION
            , AttributesElement element
        #endif
        )
        {
            // build graph inputs
            VertexDescriptionInputs vertexDescriptionInputs = AttributesMeshToVertexDescriptionInputs(input);
            // Override time parameters with used one (This is required to correctly handle motion vector for vertex animation based on time)
        
            // evaluate vertex graph
        #ifdef HAVE_VFX_MODIFICATION
            GraphProperties properties;
            ZERO_INITIALIZE(GraphProperties, properties);
        
            // Fetch the vertex graph properties for the particle instance.
            GetElementVertexProperties(element, properties);
        
            VertexDescription vertexDescription = VertexDescriptionFunction(vertexDescriptionInputs, properties);
        #else
            VertexDescription vertexDescription = VertexDescriptionFunction(vertexDescriptionInputs);
        #endif
            return vertexDescription;
        
        }
        
        AttributesMesh ApplyMeshModification(AttributesMesh input, float3 timeParameters
        #ifdef USE_CUSTOMINTERP_SUBSTRUCT
            #ifdef TESSELLATION_ON
            , inout VaryingsMeshToDS varyings
            #else
            , inout VaryingsMeshToPS varyings
            #endif
        #endif
        #ifdef HAVE_VFX_MODIFICATION
                , AttributesElement element
        #endif
            )
        {
            VertexDescription vertexDescription = GetVertexDescription(input, timeParameters
        #ifdef HAVE_VFX_MODIFICATION
                , element
        #endif
            );
        
            // copy graph output to the results
            input.positionOS = vertexDescription.Position;
            input.normalOS = vertexDescription.Normal;
            input.tangentOS.xyz = vertexDescription.Tangent;
        
            
        
            return input;
        }
        
        #if defined(_ADD_CUSTOM_VELOCITY) // For shader graph custom velocity
        // Return precomputed Velocity in object space
        float3 GetCustomVelocity(AttributesMesh input
        #ifdef HAVE_VFX_MODIFICATION
            , AttributesElement element
        #endif
        )
        {
            VertexDescription vertexDescription = GetVertexDescription(input, _TimeParameters.xyz
        #ifdef HAVE_VFX_MODIFICATION
                , element
        #endif
            );
            return vertexDescription.CustomVelocity;
        }
        #endif
        
        FragInputs BuildFragInputs(VaryingsMeshToPS input)
        {
            FragInputs output;
            ZERO_INITIALIZE(FragInputs, output);
        
            // Init to some default value to make the computer quiet (else it output 'divide by zero' warning even if value is not used).
            // TODO: this is a really poor workaround, but the variable is used in a bunch of places
            // to compute normals which are then passed on elsewhere to compute other values...
            output.tangentToWorld = k_identity3x3;
            output.positionSS = input.positionCS;       // input.positionCS is SV_Position
        
            output.texCoord0 =                  input.texCoord0;
            output.texCoord1 =                  input.texCoord1;
            output.texCoord2 =                  input.texCoord2;
            output.texCoord3 =                  input.texCoord3;
        
        #ifdef HAVE_VFX_MODIFICATION
            // FragInputs from VFX come from two places: Interpolator or CBuffer.
            /* WARNING: $splice Could not find named fragment 'VFXSetFragInputs' */
        
        #endif
        
            // splice point to copy custom interpolator fields from varyings to frag inputs
            
        
            return output;
        }
        
        // existing HDRP code uses the combined function to go directly from packed to frag inputs
        FragInputs UnpackVaryingsMeshToFragInputs(PackedVaryingsMeshToPS input)
        {
            UNITY_SETUP_INSTANCE_ID(input);
        #if defined(HAVE_VFX_MODIFICATION) && defined(UNITY_INSTANCING_ENABLED)
            unity_InstanceID = input.instanceID;
        #endif
            VaryingsMeshToPS unpacked = UnpackVaryingsMeshToPS(input);
            return BuildFragInputs(unpacked);
        }
            SurfaceDescriptionInputs FragInputsToSurfaceDescriptionInputs(FragInputs input, float3 viewWS)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
            #if defined(SHADER_STAGE_RAY_TRACING)
            #else
            #endif
        
        #if UNITY_UV_STARTS_AT_TOP
        #else
        #endif
        
        
            output.uv0 =                                        input.texCoord0;
            output.uv1 =                                        input.texCoord1;
            output.uv2 =                                        input.texCoord2;
            output.uv3 =                                        input.texCoord3;
        
            // splice point to copy frag inputs custom interpolator pack into the SDI
            
        
            return output;
        }
        
            // --------------------------------------------------
            // Build Surface Data (Specific Material)
        
        void BuildSurfaceData(FragInputs fragInputs, inout SurfaceDescription surfaceDescription, float3 V, PositionInputs posInput, out SurfaceData surfaceData)
        {
            // setup defaults -- these are used if the graph doesn't output a value
            ZERO_INITIALIZE(SurfaceData, surfaceData);
        
            // copy across graph values, if defined
            surfaceData.color = surfaceDescription.BaseColor;
        
            #ifdef WRITE_NORMAL_BUFFER
            // When we need to export the normal (in the depth prepass, we write the geometry one)
            surfaceData.normalWS = fragInputs.tangentToWorld[2];
            #endif
        
            #if defined(DEBUG_DISPLAY)
            if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
            {
                // TODO
            }
            #endif
        
            #ifdef _ENABLE_SHADOW_MATTE
        
                #if (SHADERPASS == SHADERPASS_FORWARD_UNLIT) || (SHADERPASS == SHADERPASS_RAYTRACING_GBUFFER) || (SHADERPASS == SHADERPASS_RAYTRACING_INDIRECT) || (SHADERPASS == SHADERPASS_RAYTRACING_FORWARD)
        
                    HDShadowContext shadowContext = InitShadowContext();
        
                    // Evaluate the shadow, the normal is guaranteed if shadow matte is enabled on this shader.
                    float3 shadow3;
                    ShadowLoopMin(shadowContext, posInput, normalize(fragInputs.tangentToWorld[2]), asuint(_ShadowMatteFilter), GetMeshRenderingLightLayer(), shadow3);
        
                    // Compute the average value in the fourth channel
                    float4 shadow = float4(shadow3, dot(shadow3, float3(1.0/3.0, 1.0/3.0, 1.0/3.0)));
        
                    float4 shadowColor = (1.0 - shadow) * surfaceDescription.ShadowTint.rgba;
                    float  localAlpha  = saturate(shadowColor.a + surfaceDescription.Alpha);
        
                    // Keep the nested lerp
                    // With no Color (bsdfData.color.rgb, bsdfData.color.a == 0.0f), just use ShadowColor*Color to avoid a ring of "white" around the shadow
                    // And mix color to consider the Color & ShadowColor alpha (from texture or/and color picker)
                    #ifdef _SURFACE_TYPE_TRANSPARENT
                        surfaceData.color = lerp(shadowColor.rgb * surfaceData.color, lerp(lerp(shadowColor.rgb, surfaceData.color, 1.0 - surfaceDescription.ShadowTint.a), surfaceData.color, shadow.rgb), surfaceDescription.Alpha);
                    #else
                        surfaceData.color = lerp(lerp(shadowColor.rgb, surfaceData.color, 1.0 - surfaceDescription.ShadowTint.a), surfaceData.color, shadow.rgb);
                    #endif
                    localAlpha = ApplyBlendMode(surfaceData.color, localAlpha).a;
        
                    surfaceDescription.Alpha = localAlpha;
        
                #elif SHADERPASS == SHADERPASS_PATH_TRACING
        
                    surfaceData.normalWS = fragInputs.tangentToWorld[2];
                    surfaceData.shadowTint = surfaceDescription.ShadowTint.rgba;
        
                #endif
        
            #endif // _ENABLE_SHADOW_MATTE
        }
        
            // --------------------------------------------------
            // Get Surface And BuiltinData
        
            void GetSurfaceAndBuiltinData(FragInputs fragInputs, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData RAY_TRACING_OPTIONAL_PARAMETERS)
            {
                // Don't dither if displaced tessellation (we're fading out the displacement instead to match the next LOD)
                #if !defined(SHADER_STAGE_RAY_TRACING) && !defined(_TESSELLATION_DISPLACEMENT)
                #ifdef LOD_FADE_CROSSFADE // enable dithering LOD transition if user select CrossFade transition in LOD group
                LODDitheringTransition(ComputeFadeMaskSeed(V, posInput.positionSS), unity_LODFade.x);
                #endif
                #endif
        
                #ifndef SHADER_UNLIT
                #ifdef _DOUBLESIDED_ON
                    float3 doubleSidedConstants = _DoubleSidedConstants.xyz;
                #else
                    float3 doubleSidedConstants = float3(1.0, 1.0, 1.0);
                #endif
        
                ApplyDoubleSidedFlipOrMirror(fragInputs, doubleSidedConstants); // Apply double sided flip on the vertex normal
                #endif // SHADER_UNLIT
        
                SurfaceDescriptionInputs surfaceDescriptionInputs = FragInputsToSurfaceDescriptionInputs(fragInputs, V);
        
                #if defined(HAVE_VFX_MODIFICATION)
                GraphProperties properties;
                ZERO_INITIALIZE(GraphProperties, properties);
        
                GetElementPixelProperties(fragInputs, properties);
        
                SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs, properties);
                #else
                SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);
                #endif
        
                // Perform alpha test very early to save performance (a killed pixel will not sample textures)
                // TODO: split graph evaluation to grab just alpha dependencies first? tricky..
                #ifdef _ALPHATEST_ON
                    float alphaCutoff = surfaceDescription.AlphaClipThreshold;
                    #if SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_PREPASS
                    // The TransparentDepthPrepass is also used with SSR transparent.
                    // If an artists enable transaprent SSR but not the TransparentDepthPrepass itself, then we use AlphaClipThreshold
                    // otherwise if TransparentDepthPrepass is enabled we use AlphaClipThresholdDepthPrepass
                    #elif SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_POSTPASS
                    // DepthPostpass always use its own alpha threshold
                    alphaCutoff = surfaceDescription.AlphaClipThresholdDepthPostpass;
                    #elif (SHADERPASS == SHADERPASS_SHADOWS) || (SHADERPASS == SHADERPASS_RAYTRACING_VISIBILITY)
                    // If use shadow threshold isn't enable we don't allow any test
                    #endif
        
                    GENERIC_ALPHA_TEST(surfaceDescription.Alpha, alphaCutoff);
                #endif
        
                #if !defined(SHADER_STAGE_RAY_TRACING) && _DEPTHOFFSET_ON
                ApplyDepthOffsetPositionInput(V, surfaceDescription.DepthOffset, GetViewForwardDir(), GetWorldToHClipMatrix(), posInput);
                #endif
        
                #ifndef SHADER_UNLIT
                float3 bentNormalWS;
                BuildSurfaceData(fragInputs, surfaceDescription, V, posInput, surfaceData, bentNormalWS);
        
                #ifdef FRAG_INPUTS_USE_TEXCOORD1
                    float4 lightmapTexCoord1 = fragInputs.texCoord1;
                #else
                    float4 lightmapTexCoord1 = float4(0,0,0,0);
                #endif
        
                #ifdef FRAG_INPUTS_USE_TEXCOORD2
                    float4 lightmapTexCoord2 = fragInputs.texCoord2;
                #else
                    float4 lightmapTexCoord2 = float4(0,0,0,0);
                #endif
        
                // Builtin Data
                // For back lighting we use the oposite vertex normal
                InitBuiltinData(posInput, surfaceDescription.Alpha, bentNormalWS, -fragInputs.tangentToWorld[2], lightmapTexCoord1, lightmapTexCoord2, builtinData);
        
                #else
                BuildSurfaceData(fragInputs, surfaceDescription, V, posInput, surfaceData);
        
                ZERO_BUILTIN_INITIALIZE(builtinData); // No call to InitBuiltinData as we don't have any lighting
                builtinData.opacity = surfaceDescription.Alpha;
        
                #if defined(DEBUG_DISPLAY)
                    // Light Layers are currently not used for the Unlit shader (because it is not lit)
                    // But Unlit objects do cast shadows according to their rendering layer mask, which is what we want to
                    // display in the light layers visualization mode, therefore we need the renderingLayers
                    builtinData.renderingLayers = GetMeshRenderingLightLayer();
                #endif
        
                #endif // SHADER_UNLIT
        
                #ifdef _ALPHATEST_ON
                    // Used for sharpening by alpha to mask - Alpha to covertage is only used with depth only and forward pass (no shadow pass, no transparent pass)
                    builtinData.alphaClipTreshold = alphaCutoff;
                #endif
        
                // override sampleBakedGI - not used by Unlit
        
                builtinData.emissiveColor = surfaceDescription.Emission;
        
                // Note this will not fully work on transparent surfaces (can check with _SURFACE_TYPE_TRANSPARENT define)
                // We will always overwrite vt feeback with the nearest. So behind transparent surfaces vt will not be resolved
                // This is a limitation of the current MRT approach.
                #ifdef UNITY_VIRTUAL_TEXTURING
                #endif
        
                #if _DEPTHOFFSET_ON
                builtinData.depthOffset = surfaceDescription.DepthOffset;
                #endif
        
                // TODO: We should generate distortion / distortionBlur for non distortion pass
                #if (SHADERPASS == SHADERPASS_DISTORTION)
                builtinData.distortion = surfaceDescription.Distortion;
                builtinData.distortionBlur = surfaceDescription.DistortionBlur;
                #endif
        
                #ifndef SHADER_UNLIT
                // PostInitBuiltinData call ApplyDebugToBuiltinData
                PostInitBuiltinData(V, posInput, surfaceData, builtinData);
                #else
                ApplyDebugToBuiltinData(builtinData);
                #endif
        
                RAY_TRACING_OPTIONAL_ALPHA_TEST_PASS
            }
        
            // --------------------------------------------------
            // Main
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassFullScreenDebug.hlsl"
        
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
        
        	#ifdef HAVE_VFX_MODIFICATION
        	#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/VisualEffectVertex.hlsl"
        	#endif
        
            ENDHLSL
        }
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="HDRenderPipeline"
            "RenderType"="HDUnlitShader"
            "Queue"="AlphaTest+0"
            "ShaderGraphShader"="true"
            "ShaderGraphTargetId"="HDUnlitSubTarget"
        }
        Pass
        {
            Name "IndirectDXR"
            Tags
            {
                "LightMode" = "IndirectDXR"
            }
        
            // Render State
            // RenderState: <None>
        
            // Debug
            // <None>
        
            // --------------------------------------------------
            // Pass
        
            HLSLPROGRAM
        
            // Pragmas
            #pragma target 5.0
        #pragma raytracing surface_shader
        #pragma only_renderers d3d11 xboxseries ps5
        
            // Keywords
            #pragma shader_feature_local _ _ALPHATEST_ON
        #pragma shader_feature _ _SURFACE_TYPE_TRANSPARENT
        #pragma shader_feature_local _BLENDMODE_OFF _BLENDMODE_ALPHA _BLENDMODE_ADD _BLENDMODE_PRE_MULTIPLY
        #pragma shader_feature_local _ _ADD_PRECOMPUTED_VELOCITY
        #pragma shader_feature_local _ _TRANSPARENT_WRITES_MOTION_VEC
        #pragma shader_feature_local_fragment _ _ENABLE_FOG_ON_TRANSPARENT
        #pragma multi_compile _ DEBUG_DISPLAY
            // GraphKeywords: <None>
        
            // For custom interpolators to inject a substruct definition before FragInputs definition,
            // allowing for FragInputs to capture CI's intended for ShaderGraph's SDI.
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreInclude' */
        
        
            // TODO: Merge FragInputsVFX substruct with CustomInterpolators.
        	#ifdef HAVE_VFX_MODIFICATION
        	struct FragInputsVFX
            {
                /* WARNING: $splice Could not find named fragment 'FragInputsVFX' */
            };
            #endif
        
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GeometricTools.hlsl" // Required by Tessellation.hlsl
        	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Tessellation.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl" // Required to be include before we include properties as it define DECLARE_STACK_CB
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphHeader.hlsl" // Need to be here for Gradient struct definition
        
            // --------------------------------------------------
            // Defines
        
            // Attribute
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define ATTRIBUTES_NEED_TEXCOORD2
            #define ATTRIBUTES_NEED_TEXCOORD3
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_TEXCOORD1
            #define VARYINGS_NEED_TEXCOORD2
            #define VARYINGS_NEED_TEXCOORD3
        
        
            //Strip down the FragInputs.hlsl (on graphics), so we can only optimize the interpolators we use.
            //if by accident something requests contents of FragInputs.hlsl, it will be caught as a compiler error
            //Frag inputs stripping is only enabled when FRAG_INPUTS_ENABLE_STRIPPING is set
            #if !defined(SHADER_STAGE_RAY_TRACING) && SHADERPASS != SHADERPASS_RAYTRACING_GBUFFER && SHADERPASS != SHADERPASS_FULL_SCREEN_DEBUG
            #define FRAG_INPUTS_ENABLE_STRIPPING
            #endif
            #define FRAG_INPUTS_USE_TEXCOORD0
            #define FRAG_INPUTS_USE_TEXCOORD1
            #define FRAG_INPUTS_USE_TEXCOORD2
            #define FRAG_INPUTS_USE_TEXCOORD3
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
        
        
        
            #define SHADERPASS SHADERPASS_RAYTRACING_INDIRECT
        #define SHADOW_LOW
        #define RAYTRACING_SHADER_GRAPH_RAYTRACED
        
        
            // Following two define are a workaround introduce in 10.1.x for RaytracingQualityNode
            // The ShaderGraph don't support correctly migration of this node as it serialize all the node data
            // in the json file making it impossible to uprgrade. Until we get a fix, we do a workaround here
            // to still allow us to rename the field and keyword of this node without breaking existing code.
            #ifdef RAYTRACING_SHADER_GRAPH_DEFAULT
            #define RAYTRACING_SHADER_GRAPH_HIGH
            #endif
        
            #ifdef RAYTRACING_SHADER_GRAPH_RAYTRACED
            #define RAYTRACING_SHADER_GRAPH_LOW
            #endif
            // end
        
            #ifndef SHADER_UNLIT
            // We need isFrontFace when using double sided - it is not required for unlit as in case of unlit double sided only drive the cullmode
            // VARYINGS_NEED_CULLFACE can be define by VaryingsMeshToPS.FaceSign input if a IsFrontFace Node is included in the shader graph.
            #if defined(_DOUBLESIDED_ON) && !defined(VARYINGS_NEED_CULLFACE)
                #define VARYINGS_NEED_CULLFACE
            #endif
            #endif
        
            // Specific Material Define
        // Setup a define to say we are an unlit shader
        #define SHADER_UNLIT
        
        // Following Macro are only used by Unlit material
        #if defined(_ENABLE_SHADOW_MATTE) && (SHADERPASS == SHADERPASS_FORWARD_UNLIT || SHADERPASS == SHADERPASS_PATH_TRACING)
        #define LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
        #define HAS_LIGHTLOOP
        #endif
            // Caution: we can use the define SHADER_UNLIT onlit after the above Material include as it is the Unlit template who define it
        
            // To handle SSR on transparent correctly with a possibility to enable/disable it per framesettings
            // we should have a code like this:
            // if !defined(_DISABLE_SSR_TRANSPARENT)
            // pragma multi_compile _ WRITE_NORMAL_BUFFER
            // endif
            // i.e we enable the multicompile only if we can receive SSR or not, and then C# code drive
            // it based on if SSR transparent in frame settings and not (and stripper can strip it).
            // this is currently not possible with our current preprocessor as _DISABLE_SSR_TRANSPARENT is a keyword not a define
            // so instead we used this and chose to pay the extra cost of normal write even if SSR transaprent is disabled.
            // Ideally the shader graph generator should handle it but condition below can't be handle correctly for now.
            #if SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_PREPASS
            #if !defined(_DISABLE_SSR_TRANSPARENT) && !defined(SHADER_UNLIT)
                #define WRITE_NORMAL_BUFFER
            #endif
            #endif
        
            #ifndef DEBUG_DISPLAY
                // In case of opaque we don't want to perform the alpha test, it is done in depth prepass and we use depth equal for ztest (setup from UI)
                // Don't do it with debug display mode as it is possible there is no depth prepass in this case
                #if !defined(_SURFACE_TYPE_TRANSPARENT)
                    #if SHADERPASS == SHADERPASS_FORWARD
                    #define SHADERPASS_FORWARD_BYPASS_ALPHA_TEST
                    #elif SHADERPASS == SHADERPASS_GBUFFER
                    #define SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST
                    #endif
                #endif
            #endif
        
            // Define _DEFERRED_CAPABLE_MATERIAL for shader capable to run in deferred pass
            #if defined(SHADER_LIT) && !defined(_SURFACE_TYPE_TRANSPARENT)
                #define _DEFERRED_CAPABLE_MATERIAL
            #endif
        
            // Translate transparent motion vector define
            #if defined(_TRANSPARENT_WRITES_MOTION_VEC) && defined(_SURFACE_TYPE_TRANSPARENT)
                #define _WRITE_TRANSPARENT_MOTION_VECTOR
            #endif
        
            // -- Graph Properties
            CBUFFER_START(UnityPerMaterial)
        float4 _TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1_TexelSize;
        float4 _Direction_TexelSize;
        float2 _TextureSize;
        float _RotateUV;
        float Boolean_F2B1E91A;
        float4 _EmissionColor;
        float _UseShadowThreshold;
        float4 _DoubleSidedConstants;
        float _BlendMode;
        float _EnableBlendModePreserveSpecularLighting;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1);
        SAMPLER(sampler_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1);
        TEXTURE2D(_Direction);
        SAMPLER(sampler_Direction);
        
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
        
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
        
            // Includes
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracingLightLoop.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/UnlitRaytracing.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RayTracingCommon.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl"
        	// GraphIncludes: <None>
        
            // --------------------------------------------------
            // Structs and Packing
        
            struct SurfaceDescriptionInputs
        {
             float4 uv0;
             float4 uv1;
             float4 uv2;
             float4 uv3;
        };
        
            //Interpolator Packs: <None>
        
            // --------------------------------------------------
            // Graph
        
        
            // Graph Functions
            
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A / B;
        }
        
        void Unity_Fraction_float2(float2 In, out float2 Out)
        {
            Out = frac(In);
        }
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A * B;
        }
        
        void Unity_Subtract_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A - B;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_Comparison_Greater_float(float A, float B, out float Out)
        {
            Out = A > B ? 1 : 0;
        }
        
        void Unity_Divide_float(float A, float B, out float Out)
        {
            Out = A / B;
        }
        
        void Unity_Arctangent_float(float In, out float Out)
        {
            Out = atan(In);
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Branch_float(float Predicate, float True, float False, out float Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Rotate_Radians_float(float2 UV, float2 Center, float Rotation, out float2 Out)
        {
            //rotation matrix
            UV -= Center;
            float s = sin(Rotation);
            float c = cos(Rotation);
        
            //center rotation matrix
            float2x2 rMatrix = float2x2(c, -s, s, c);
            rMatrix *= 0.5;
            rMatrix += 0.5;
            rMatrix = rMatrix*2 - 1;
        
            //multiply the UVs by the rotation matrix
            UV.xy = mul(UV.xy, rMatrix);
            UV += Center;
        
            Out = UV;
        }
        
        void Unity_Distance_float2(float2 A, float2 B, out float Out)
        {
            Out = distance(A, B);
        }
        
        void Unity_Clamp_float(float In, float Min, float Max, out float Out)
        {
            Out = clamp(In, Min, Max);
        }
        
        void Unity_Subtract_float(float A, float B, out float Out)
        {
            Out = A - B;
        }
        
        void Unity_Absolute_float(float In, out float Out)
        {
            Out = abs(In);
        }
        
        void Unity_Power_float(float A, float B, out float Out)
        {
            Out = pow(A, B);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A * B;
        }
        
            // Graph Vertex
            // GraphVertex: <None>
        
            // Graph Pixel
            struct SurfaceDescription
        {
            float3 BaseColor;
            float3 Emission;
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0 = UnityBuildTexture2DStructNoScale(_Direction);
            float4 _UV_7c8a457b34527181924b2b4a71350de9_Out_0 = IN.uv0;
            float2 _TilingAndOffset_67d58b7a8092828aaece7f3acfe6e8a9_Out_3;
            Unity_TilingAndOffset_float((_UV_7c8a457b34527181924b2b4a71350de9_Out_0.xy), float2 (2, 2), float2 (0, 0), _TilingAndOffset_67d58b7a8092828aaece7f3acfe6e8a9_Out_3);
            float2 _Vector2_9a3e446bd0dfc3868e3db6f9bf101ead_Out_0 = float2(1, 1);
            float2 _Vector2_8355e4dbd796b188ba1e500dcabf0c0f_Out_0 = float2(30, 30);
            float2 _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2;
            Unity_Divide_float2(_Vector2_9a3e446bd0dfc3868e3db6f9bf101ead_Out_0, _Vector2_8355e4dbd796b188ba1e500dcabf0c0f_Out_0, _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2);
            float2 _Divide_5d0f49ab4bcab689894f976217114a79_Out_2;
            Unity_Divide_float2(_TilingAndOffset_67d58b7a8092828aaece7f3acfe6e8a9_Out_3, _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2, _Divide_5d0f49ab4bcab689894f976217114a79_Out_2);
            float2 _Fraction_e4f0f05fb76fd3809f5ec855b54a4595_Out_1;
            Unity_Fraction_float2(_Divide_5d0f49ab4bcab689894f976217114a79_Out_2, _Fraction_e4f0f05fb76fd3809f5ec855b54a4595_Out_1);
            float2 _Multiply_cbcb0de832b1d08288916e46aa8d4df2_Out_2;
            Unity_Multiply_float2_float2(_Fraction_e4f0f05fb76fd3809f5ec855b54a4595_Out_1, _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2, _Multiply_cbcb0de832b1d08288916e46aa8d4df2_Out_2);
            float2 _Vector2_e6d2595367ed0380b42c7a74855838a5_Out_0 = float2(246, 246);
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_Width_0 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.z;
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_Height_2 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.w;
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_TexelWidth_3 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.x;
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_TexelHeight_4 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.y;
            float2 _Vector2_e8728bfbf194e5818f010927b4ba8d79_Out_0 = float2(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Width_0, _TexelSize_18bb9f416e386f87bee91aa46804bc56_Height_2);
            float2 _Multiply_561a2890964f4081a8cf5d16bccb889f_Out_2;
            Unity_Multiply_float2_float2(_Vector2_e6d2595367ed0380b42c7a74855838a5_Out_0, _Vector2_e8728bfbf194e5818f010927b4ba8d79_Out_0, _Multiply_561a2890964f4081a8cf5d16bccb889f_Out_2);
            float2 _Vector2_97c81bcbfd26f08385a101ac3da412f3_Out_0 = float2(10, 10);
            float2 _Multiply_bccda05b3809138d8da15484dd6a7b4e_Out_2;
            Unity_Multiply_float2_float2(_Vector2_e8728bfbf194e5818f010927b4ba8d79_Out_0, _Vector2_97c81bcbfd26f08385a101ac3da412f3_Out_0, _Multiply_bccda05b3809138d8da15484dd6a7b4e_Out_2);
            float2 _Subtract_710d4672e66f7780ad90ca6e19a733bd_Out_2;
            Unity_Subtract_float2(_Multiply_561a2890964f4081a8cf5d16bccb889f_Out_2, _Multiply_bccda05b3809138d8da15484dd6a7b4e_Out_2, _Subtract_710d4672e66f7780ad90ca6e19a733bd_Out_2);
            float2 _Multiply_cd542324eceadd8daaca18ab78d3fbf6_Out_2;
            Unity_Multiply_float2_float2(_Multiply_cbcb0de832b1d08288916e46aa8d4df2_Out_2, _Subtract_710d4672e66f7780ad90ca6e19a733bd_Out_2, _Multiply_cd542324eceadd8daaca18ab78d3fbf6_Out_2);
            float2 _Vector2_df86361e9c36e7828e3097cc1fa8a75b_Out_0 = float2(0.1, 0.1);
            float2 _Multiply_8e351d40d36634829f2bb0905b9b9342_Out_2;
            Unity_Multiply_float2_float2(_Multiply_cd542324eceadd8daaca18ab78d3fbf6_Out_2, _Vector2_df86361e9c36e7828e3097cc1fa8a75b_Out_0, _Multiply_8e351d40d36634829f2bb0905b9b9342_Out_2);
            float4 _UV_1ca38e193be034888b8a707af9d47210_Out_0 = IN.uv3;
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_R_1 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[0];
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[1];
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_B_3 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[2];
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_A_4 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[3];
            float _Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2;
            Unity_Multiply_float_float(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2, -1, _Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2);
            float _Comparison_e67d9847aa86858490dd0c37e68e7505_Out_2;
            Unity_Comparison_Greater_float(_Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2, 0, _Comparison_e67d9847aa86858490dd0c37e68e7505_Out_2);
            float _Divide_2b884a92fd783d82a9012917c2b83115_Out_2;
            Unity_Divide_float(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_R_1, _Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2, _Divide_2b884a92fd783d82a9012917c2b83115_Out_2);
            float _Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1;
            Unity_Arctangent_float(_Divide_2b884a92fd783d82a9012917c2b83115_Out_2, _Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1);
            float _Add_cf6b7aee4a487f8ca16b9171b64a8f0d_Out_2;
            Unity_Add_float(_Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1, 3.141592, _Add_cf6b7aee4a487f8ca16b9171b64a8f0d_Out_2);
            float _Branch_9de100b31215ba89bb19e5b89012a25a_Out_3;
            Unity_Branch_float(_Comparison_e67d9847aa86858490dd0c37e68e7505_Out_2, _Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1, _Add_cf6b7aee4a487f8ca16b9171b64a8f0d_Out_2, _Branch_9de100b31215ba89bb19e5b89012a25a_Out_3);
            float2 _Rotate_ff1406ef744b188ca79c6d3bf0b8bbd7_Out_3;
            Unity_Rotate_Radians_float(_Multiply_8e351d40d36634829f2bb0905b9b9342_Out_2, float2 (0.5, 0.5), _Branch_9de100b31215ba89bb19e5b89012a25a_Out_3, _Rotate_ff1406ef744b188ca79c6d3bf0b8bbd7_Out_3);
            #if defined(SHADER_API_GLES) && (SHADER_TARGET < 30)
              float4 _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0 = float4(0.0f, 0.0f, 0.0f, 1.0f);
            #else
              float4 _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0 = SAMPLE_TEXTURE2D_LOD(_Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0.tex, _Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0.samplerstate, _Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0.GetTransformedUV(_Rotate_ff1406ef744b188ca79c6d3bf0b8bbd7_Out_3), 0);
            #endif
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_R_5 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.r;
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_G_6 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.g;
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_B_7 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.b;
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_A_8 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.a;
            float _Comparison_1a93f0ca7c913a8f81659b20b8d59f2b_Out_2;
            Unity_Comparison_Greater_float(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2, 0.001, _Comparison_1a93f0ca7c913a8f81659b20b8d59f2b_Out_2);
            float3 _Vector3_37ab63b41d52bb8d8eba9088ed71bc11_Out_0 = float3(0, 0, 0);
            float2 _Vector2_4aeecd1cb271dd8e96c2fb12309b9dd0_Out_0 = float2(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_R_1, _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2);
            float _Distance_47e812ff6269a181b6e0faa017faee3b_Out_2;
            Unity_Distance_float2(_Vector2_4aeecd1cb271dd8e96c2fb12309b9dd0_Out_0, float2(0, 0), _Distance_47e812ff6269a181b6e0faa017faee3b_Out_2);
            float _Multiply_e5102f48c5e759879293e06062894e0c_Out_2;
            Unity_Multiply_float_float(_Distance_47e812ff6269a181b6e0faa017faee3b_Out_2, 1.6, _Multiply_e5102f48c5e759879293e06062894e0c_Out_2);
            float _Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3;
            Unity_Clamp_float(_Multiply_e5102f48c5e759879293e06062894e0c_Out_2, 0, 1, _Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3);
            float _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2;
            Unity_Subtract_float(1, _Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3, _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2);
            float3 _Vector3_aeff4a868e81c68f85dfe4c9662da9ce_Out_0 = float3(1, _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2, 0);
            float _Absolute_5573b6b2c1616e819aa4eeb67caec457_Out_1;
            Unity_Absolute_float(_Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3, _Absolute_5573b6b2c1616e819aa4eeb67caec457_Out_1);
            float _Power_33dedbf009cacf848c403a58667a66c6_Out_2;
            Unity_Power_float(_Absolute_5573b6b2c1616e819aa4eeb67caec457_Out_1, 0.16, _Power_33dedbf009cacf848c403a58667a66c6_Out_2);
            float _Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3;
            Unity_Clamp_float(_Power_33dedbf009cacf848c403a58667a66c6_Out_2, 0, 1, _Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3);
            float3 _Lerp_e6000a54e83a8f8e8c5bc16419b360ca_Out_3;
            Unity_Lerp_float3(_Vector3_37ab63b41d52bb8d8eba9088ed71bc11_Out_0, _Vector3_aeff4a868e81c68f85dfe4c9662da9ce_Out_0, (_Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3.xxx), _Lerp_e6000a54e83a8f8e8c5bc16419b360ca_Out_3);
            float3 _Vector3_484f28086891e98d9589d1f65774f6f4_Out_0 = float3(0, _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2, 1);
            float3 _Lerp_93469a90f98ccf80a7121c0b2bf05f48_Out_3;
            Unity_Lerp_float3(_Vector3_37ab63b41d52bb8d8eba9088ed71bc11_Out_0, _Vector3_484f28086891e98d9589d1f65774f6f4_Out_0, (_Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3.xxx), _Lerp_93469a90f98ccf80a7121c0b2bf05f48_Out_3);
            float3 _Branch_365bf259ae64c386a18e2d6997727100_Out_3;
            Unity_Branch_float3(_Comparison_1a93f0ca7c913a8f81659b20b8d59f2b_Out_2, _Lerp_e6000a54e83a8f8e8c5bc16419b360ca_Out_3, _Lerp_93469a90f98ccf80a7121c0b2bf05f48_Out_3, _Branch_365bf259ae64c386a18e2d6997727100_Out_3);
            float3 _Multiply_0563147e8bc9868b8c2a4f2c442c349f_Out_2;
            Unity_Multiply_float3_float3((_SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_A_8.xxx), _Branch_365bf259ae64c386a18e2d6997727100_Out_3, _Multiply_0563147e8bc9868b8c2a4f2c442c349f_Out_2);
            surface.BaseColor = _Multiply_0563147e8bc9868b8c2a4f2c442c349f_Out_2;
            surface.Emission = float3(0, 0, 0);
            surface.Alpha = 1;
            surface.AlphaClipThreshold = 0.5;
            return surface;
        }
        
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES AttributesMesh
            #define VaryingsMeshType VaryingsMeshToPS
            #define VFX_SRP_VARYINGS VaryingsMeshType
            #define VFX_SRP_SURFACE_INPUTS FragInputs
            #endif
            SurfaceDescriptionInputs FragInputsToSurfaceDescriptionInputs(FragInputs input, float3 viewWS)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
            #if defined(SHADER_STAGE_RAY_TRACING)
            #else
            #endif
        
        #if UNITY_UV_STARTS_AT_TOP
        #else
        #endif
        
        
            output.uv0 =                                        input.texCoord0;
            output.uv1 =                                        input.texCoord1;
            output.uv2 =                                        input.texCoord2;
            output.uv3 =                                        input.texCoord3;
        
            // splice point to copy frag inputs custom interpolator pack into the SDI
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorCopyToSDI' */
        
            return output;
        }
        
            // --------------------------------------------------
            // Build Surface Data (Specific Material)
        
        void BuildSurfaceData(FragInputs fragInputs, inout SurfaceDescription surfaceDescription, float3 V, PositionInputs posInput, out SurfaceData surfaceData)
        {
            // setup defaults -- these are used if the graph doesn't output a value
            ZERO_INITIALIZE(SurfaceData, surfaceData);
        
            // copy across graph values, if defined
            surfaceData.color = surfaceDescription.BaseColor;
        
            #ifdef WRITE_NORMAL_BUFFER
            // When we need to export the normal (in the depth prepass, we write the geometry one)
            surfaceData.normalWS = fragInputs.tangentToWorld[2];
            #endif
        
            #if defined(DEBUG_DISPLAY)
            if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
            {
                // TODO
            }
            #endif
        
            #ifdef _ENABLE_SHADOW_MATTE
        
                #if (SHADERPASS == SHADERPASS_FORWARD_UNLIT) || (SHADERPASS == SHADERPASS_RAYTRACING_GBUFFER) || (SHADERPASS == SHADERPASS_RAYTRACING_INDIRECT) || (SHADERPASS == SHADERPASS_RAYTRACING_FORWARD)
        
                    HDShadowContext shadowContext = InitShadowContext();
        
                    // Evaluate the shadow, the normal is guaranteed if shadow matte is enabled on this shader.
                    float3 shadow3;
                    ShadowLoopMin(shadowContext, posInput, normalize(fragInputs.tangentToWorld[2]), asuint(_ShadowMatteFilter), GetMeshRenderingLightLayer(), shadow3);
        
                    // Compute the average value in the fourth channel
                    float4 shadow = float4(shadow3, dot(shadow3, float3(1.0/3.0, 1.0/3.0, 1.0/3.0)));
        
                    float4 shadowColor = (1.0 - shadow) * surfaceDescription.ShadowTint.rgba;
                    float  localAlpha  = saturate(shadowColor.a + surfaceDescription.Alpha);
        
                    // Keep the nested lerp
                    // With no Color (bsdfData.color.rgb, bsdfData.color.a == 0.0f), just use ShadowColor*Color to avoid a ring of "white" around the shadow
                    // And mix color to consider the Color & ShadowColor alpha (from texture or/and color picker)
                    #ifdef _SURFACE_TYPE_TRANSPARENT
                        surfaceData.color = lerp(shadowColor.rgb * surfaceData.color, lerp(lerp(shadowColor.rgb, surfaceData.color, 1.0 - surfaceDescription.ShadowTint.a), surfaceData.color, shadow.rgb), surfaceDescription.Alpha);
                    #else
                        surfaceData.color = lerp(lerp(shadowColor.rgb, surfaceData.color, 1.0 - surfaceDescription.ShadowTint.a), surfaceData.color, shadow.rgb);
                    #endif
                    localAlpha = ApplyBlendMode(surfaceData.color, localAlpha).a;
        
                    surfaceDescription.Alpha = localAlpha;
        
                #elif SHADERPASS == SHADERPASS_PATH_TRACING
        
                    surfaceData.normalWS = fragInputs.tangentToWorld[2];
                    surfaceData.shadowTint = surfaceDescription.ShadowTint.rgba;
        
                #endif
        
            #endif // _ENABLE_SHADOW_MATTE
        }
        
            // --------------------------------------------------
            // Get Surface And BuiltinData
        
            void GetSurfaceAndBuiltinData(FragInputs fragInputs, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData RAY_TRACING_OPTIONAL_PARAMETERS)
            {
                // Don't dither if displaced tessellation (we're fading out the displacement instead to match the next LOD)
                #if !defined(SHADER_STAGE_RAY_TRACING) && !defined(_TESSELLATION_DISPLACEMENT)
                #ifdef LOD_FADE_CROSSFADE // enable dithering LOD transition if user select CrossFade transition in LOD group
                LODDitheringTransition(ComputeFadeMaskSeed(V, posInput.positionSS), unity_LODFade.x);
                #endif
                #endif
        
                #ifndef SHADER_UNLIT
                #ifdef _DOUBLESIDED_ON
                    float3 doubleSidedConstants = _DoubleSidedConstants.xyz;
                #else
                    float3 doubleSidedConstants = float3(1.0, 1.0, 1.0);
                #endif
        
                ApplyDoubleSidedFlipOrMirror(fragInputs, doubleSidedConstants); // Apply double sided flip on the vertex normal
                #endif // SHADER_UNLIT
        
                SurfaceDescriptionInputs surfaceDescriptionInputs = FragInputsToSurfaceDescriptionInputs(fragInputs, V);
        
                #if defined(HAVE_VFX_MODIFICATION)
                GraphProperties properties;
                ZERO_INITIALIZE(GraphProperties, properties);
        
                GetElementPixelProperties(fragInputs, properties);
        
                SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs, properties);
                #else
                SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);
                #endif
        
                // Perform alpha test very early to save performance (a killed pixel will not sample textures)
                // TODO: split graph evaluation to grab just alpha dependencies first? tricky..
                #ifdef _ALPHATEST_ON
                    float alphaCutoff = surfaceDescription.AlphaClipThreshold;
                    #if SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_PREPASS
                    // The TransparentDepthPrepass is also used with SSR transparent.
                    // If an artists enable transaprent SSR but not the TransparentDepthPrepass itself, then we use AlphaClipThreshold
                    // otherwise if TransparentDepthPrepass is enabled we use AlphaClipThresholdDepthPrepass
                    #elif SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_POSTPASS
                    // DepthPostpass always use its own alpha threshold
                    alphaCutoff = surfaceDescription.AlphaClipThresholdDepthPostpass;
                    #elif (SHADERPASS == SHADERPASS_SHADOWS) || (SHADERPASS == SHADERPASS_RAYTRACING_VISIBILITY)
                    // If use shadow threshold isn't enable we don't allow any test
                    #endif
        
                    GENERIC_ALPHA_TEST(surfaceDescription.Alpha, alphaCutoff);
                #endif
        
                #if !defined(SHADER_STAGE_RAY_TRACING) && _DEPTHOFFSET_ON
                ApplyDepthOffsetPositionInput(V, surfaceDescription.DepthOffset, GetViewForwardDir(), GetWorldToHClipMatrix(), posInput);
                #endif
        
                #ifndef SHADER_UNLIT
                float3 bentNormalWS;
                BuildSurfaceData(fragInputs, surfaceDescription, V, posInput, surfaceData, bentNormalWS);
        
                #ifdef FRAG_INPUTS_USE_TEXCOORD1
                    float4 lightmapTexCoord1 = fragInputs.texCoord1;
                #else
                    float4 lightmapTexCoord1 = float4(0,0,0,0);
                #endif
        
                #ifdef FRAG_INPUTS_USE_TEXCOORD2
                    float4 lightmapTexCoord2 = fragInputs.texCoord2;
                #else
                    float4 lightmapTexCoord2 = float4(0,0,0,0);
                #endif
        
                // Builtin Data
                // For back lighting we use the oposite vertex normal
                InitBuiltinData(posInput, surfaceDescription.Alpha, bentNormalWS, -fragInputs.tangentToWorld[2], lightmapTexCoord1, lightmapTexCoord2, builtinData);
        
                #else
                BuildSurfaceData(fragInputs, surfaceDescription, V, posInput, surfaceData);
        
                ZERO_BUILTIN_INITIALIZE(builtinData); // No call to InitBuiltinData as we don't have any lighting
                builtinData.opacity = surfaceDescription.Alpha;
        
                #if defined(DEBUG_DISPLAY)
                    // Light Layers are currently not used for the Unlit shader (because it is not lit)
                    // But Unlit objects do cast shadows according to their rendering layer mask, which is what we want to
                    // display in the light layers visualization mode, therefore we need the renderingLayers
                    builtinData.renderingLayers = GetMeshRenderingLightLayer();
                #endif
        
                #endif // SHADER_UNLIT
        
                #ifdef _ALPHATEST_ON
                    // Used for sharpening by alpha to mask - Alpha to covertage is only used with depth only and forward pass (no shadow pass, no transparent pass)
                    builtinData.alphaClipTreshold = alphaCutoff;
                #endif
        
                // override sampleBakedGI - not used by Unlit
        
                builtinData.emissiveColor = surfaceDescription.Emission;
        
                // Note this will not fully work on transparent surfaces (can check with _SURFACE_TYPE_TRANSPARENT define)
                // We will always overwrite vt feeback with the nearest. So behind transparent surfaces vt will not be resolved
                // This is a limitation of the current MRT approach.
                #ifdef UNITY_VIRTUAL_TEXTURING
                #endif
        
                #if _DEPTHOFFSET_ON
                builtinData.depthOffset = surfaceDescription.DepthOffset;
                #endif
        
                // TODO: We should generate distortion / distortionBlur for non distortion pass
                #if (SHADERPASS == SHADERPASS_DISTORTION)
                builtinData.distortion = surfaceDescription.Distortion;
                builtinData.distortionBlur = surfaceDescription.DistortionBlur;
                #endif
        
                #ifndef SHADER_UNLIT
                // PostInitBuiltinData call ApplyDebugToBuiltinData
                PostInitBuiltinData(V, posInput, surfaceData, builtinData);
                #else
                ApplyDebugToBuiltinData(builtinData);
                #endif
        
                RAY_TRACING_OPTIONAL_ALPHA_TEST_PASS
            }
        
            // --------------------------------------------------
            // Main
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingIndirect.hlsl"
        
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
        
        	#ifdef HAVE_VFX_MODIFICATION
        	#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/VisualEffectVertex.hlsl"
        	#endif
        
            ENDHLSL
        }
        Pass
        {
            Name "VisibilityDXR"
            Tags
            {
                "LightMode" = "VisibilityDXR"
            }
        
            // Render State
            // RenderState: <None>
        
            // Debug
            // <None>
        
            // --------------------------------------------------
            // Pass
        
            HLSLPROGRAM
        
            // Pragmas
            #pragma target 5.0
        #pragma raytracing surface_shader
        #pragma only_renderers d3d11 xboxseries ps5
        
            // Keywords
            #pragma multi_compile _ TRANSPARENT_COLOR_SHADOW
        #pragma shader_feature_local _ _ALPHATEST_ON
        #pragma shader_feature _ _SURFACE_TYPE_TRANSPARENT
        #pragma shader_feature_local _BLENDMODE_OFF _BLENDMODE_ALPHA _BLENDMODE_ADD _BLENDMODE_PRE_MULTIPLY
        #pragma shader_feature_local _ _ADD_PRECOMPUTED_VELOCITY
        #pragma shader_feature_local _ _TRANSPARENT_WRITES_MOTION_VEC
        #pragma shader_feature_local_fragment _ _ENABLE_FOG_ON_TRANSPARENT
            // GraphKeywords: <None>
        
            // For custom interpolators to inject a substruct definition before FragInputs definition,
            // allowing for FragInputs to capture CI's intended for ShaderGraph's SDI.
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreInclude' */
        
        
            // TODO: Merge FragInputsVFX substruct with CustomInterpolators.
        	#ifdef HAVE_VFX_MODIFICATION
        	struct FragInputsVFX
            {
                /* WARNING: $splice Could not find named fragment 'FragInputsVFX' */
            };
            #endif
        
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GeometricTools.hlsl" // Required by Tessellation.hlsl
        	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Tessellation.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl" // Required to be include before we include properties as it define DECLARE_STACK_CB
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphHeader.hlsl" // Need to be here for Gradient struct definition
        
            // --------------------------------------------------
            // Defines
        
            // Attribute
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define ATTRIBUTES_NEED_TEXCOORD2
            #define ATTRIBUTES_NEED_TEXCOORD3
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_TEXCOORD1
            #define VARYINGS_NEED_TEXCOORD2
            #define VARYINGS_NEED_TEXCOORD3
        
        
            //Strip down the FragInputs.hlsl (on graphics), so we can only optimize the interpolators we use.
            //if by accident something requests contents of FragInputs.hlsl, it will be caught as a compiler error
            //Frag inputs stripping is only enabled when FRAG_INPUTS_ENABLE_STRIPPING is set
            #if !defined(SHADER_STAGE_RAY_TRACING) && SHADERPASS != SHADERPASS_RAYTRACING_GBUFFER && SHADERPASS != SHADERPASS_FULL_SCREEN_DEBUG
            #define FRAG_INPUTS_ENABLE_STRIPPING
            #endif
            #define FRAG_INPUTS_USE_TEXCOORD0
            #define FRAG_INPUTS_USE_TEXCOORD1
            #define FRAG_INPUTS_USE_TEXCOORD2
            #define FRAG_INPUTS_USE_TEXCOORD3
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
        
        
        
            #define SHADERPASS SHADERPASS_RAYTRACING_VISIBILITY
        
        
            // Following two define are a workaround introduce in 10.1.x for RaytracingQualityNode
            // The ShaderGraph don't support correctly migration of this node as it serialize all the node data
            // in the json file making it impossible to uprgrade. Until we get a fix, we do a workaround here
            // to still allow us to rename the field and keyword of this node without breaking existing code.
            #ifdef RAYTRACING_SHADER_GRAPH_DEFAULT
            #define RAYTRACING_SHADER_GRAPH_HIGH
            #endif
        
            #ifdef RAYTRACING_SHADER_GRAPH_RAYTRACED
            #define RAYTRACING_SHADER_GRAPH_LOW
            #endif
            // end
        
            #ifndef SHADER_UNLIT
            // We need isFrontFace when using double sided - it is not required for unlit as in case of unlit double sided only drive the cullmode
            // VARYINGS_NEED_CULLFACE can be define by VaryingsMeshToPS.FaceSign input if a IsFrontFace Node is included in the shader graph.
            #if defined(_DOUBLESIDED_ON) && !defined(VARYINGS_NEED_CULLFACE)
                #define VARYINGS_NEED_CULLFACE
            #endif
            #endif
        
            // Specific Material Define
        // Setup a define to say we are an unlit shader
        #define SHADER_UNLIT
        
        // Following Macro are only used by Unlit material
        #if defined(_ENABLE_SHADOW_MATTE) && (SHADERPASS == SHADERPASS_FORWARD_UNLIT || SHADERPASS == SHADERPASS_PATH_TRACING)
        #define LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
        #define HAS_LIGHTLOOP
        #endif
            // Caution: we can use the define SHADER_UNLIT onlit after the above Material include as it is the Unlit template who define it
        
            // To handle SSR on transparent correctly with a possibility to enable/disable it per framesettings
            // we should have a code like this:
            // if !defined(_DISABLE_SSR_TRANSPARENT)
            // pragma multi_compile _ WRITE_NORMAL_BUFFER
            // endif
            // i.e we enable the multicompile only if we can receive SSR or not, and then C# code drive
            // it based on if SSR transparent in frame settings and not (and stripper can strip it).
            // this is currently not possible with our current preprocessor as _DISABLE_SSR_TRANSPARENT is a keyword not a define
            // so instead we used this and chose to pay the extra cost of normal write even if SSR transaprent is disabled.
            // Ideally the shader graph generator should handle it but condition below can't be handle correctly for now.
            #if SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_PREPASS
            #if !defined(_DISABLE_SSR_TRANSPARENT) && !defined(SHADER_UNLIT)
                #define WRITE_NORMAL_BUFFER
            #endif
            #endif
        
            #ifndef DEBUG_DISPLAY
                // In case of opaque we don't want to perform the alpha test, it is done in depth prepass and we use depth equal for ztest (setup from UI)
                // Don't do it with debug display mode as it is possible there is no depth prepass in this case
                #if !defined(_SURFACE_TYPE_TRANSPARENT)
                    #if SHADERPASS == SHADERPASS_FORWARD
                    #define SHADERPASS_FORWARD_BYPASS_ALPHA_TEST
                    #elif SHADERPASS == SHADERPASS_GBUFFER
                    #define SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST
                    #endif
                #endif
            #endif
        
            // Define _DEFERRED_CAPABLE_MATERIAL for shader capable to run in deferred pass
            #if defined(SHADER_LIT) && !defined(_SURFACE_TYPE_TRANSPARENT)
                #define _DEFERRED_CAPABLE_MATERIAL
            #endif
        
            // Translate transparent motion vector define
            #if defined(_TRANSPARENT_WRITES_MOTION_VEC) && defined(_SURFACE_TYPE_TRANSPARENT)
                #define _WRITE_TRANSPARENT_MOTION_VECTOR
            #endif
        
            // -- Graph Properties
            CBUFFER_START(UnityPerMaterial)
        float4 _TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1_TexelSize;
        float4 _Direction_TexelSize;
        float2 _TextureSize;
        float _RotateUV;
        float Boolean_F2B1E91A;
        float4 _EmissionColor;
        float _UseShadowThreshold;
        float4 _DoubleSidedConstants;
        float _BlendMode;
        float _EnableBlendModePreserveSpecularLighting;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1);
        SAMPLER(sampler_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1);
        TEXTURE2D(_Direction);
        SAMPLER(sampler_Direction);
        
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
        
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
        
            // Includes
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracingLightLoop.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/UnlitRaytracing.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RayTracingCommon.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl"
        	// GraphIncludes: <None>
        
            // --------------------------------------------------
            // Structs and Packing
        
            struct SurfaceDescriptionInputs
        {
             float4 uv0;
             float4 uv1;
             float4 uv2;
             float4 uv3;
        };
        
            //Interpolator Packs: <None>
        
            // --------------------------------------------------
            // Graph
        
        
            // Graph Functions
            
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A / B;
        }
        
        void Unity_Fraction_float2(float2 In, out float2 Out)
        {
            Out = frac(In);
        }
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A * B;
        }
        
        void Unity_Subtract_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A - B;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_Comparison_Greater_float(float A, float B, out float Out)
        {
            Out = A > B ? 1 : 0;
        }
        
        void Unity_Divide_float(float A, float B, out float Out)
        {
            Out = A / B;
        }
        
        void Unity_Arctangent_float(float In, out float Out)
        {
            Out = atan(In);
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Branch_float(float Predicate, float True, float False, out float Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Rotate_Radians_float(float2 UV, float2 Center, float Rotation, out float2 Out)
        {
            //rotation matrix
            UV -= Center;
            float s = sin(Rotation);
            float c = cos(Rotation);
        
            //center rotation matrix
            float2x2 rMatrix = float2x2(c, -s, s, c);
            rMatrix *= 0.5;
            rMatrix += 0.5;
            rMatrix = rMatrix*2 - 1;
        
            //multiply the UVs by the rotation matrix
            UV.xy = mul(UV.xy, rMatrix);
            UV += Center;
        
            Out = UV;
        }
        
        void Unity_Distance_float2(float2 A, float2 B, out float Out)
        {
            Out = distance(A, B);
        }
        
        void Unity_Clamp_float(float In, float Min, float Max, out float Out)
        {
            Out = clamp(In, Min, Max);
        }
        
        void Unity_Subtract_float(float A, float B, out float Out)
        {
            Out = A - B;
        }
        
        void Unity_Absolute_float(float In, out float Out)
        {
            Out = abs(In);
        }
        
        void Unity_Power_float(float A, float B, out float Out)
        {
            Out = pow(A, B);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A * B;
        }
        
            // Graph Vertex
            // GraphVertex: <None>
        
            // Graph Pixel
            struct SurfaceDescription
        {
            float3 BaseColor;
            float3 Emission;
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0 = UnityBuildTexture2DStructNoScale(_Direction);
            float4 _UV_7c8a457b34527181924b2b4a71350de9_Out_0 = IN.uv0;
            float2 _TilingAndOffset_67d58b7a8092828aaece7f3acfe6e8a9_Out_3;
            Unity_TilingAndOffset_float((_UV_7c8a457b34527181924b2b4a71350de9_Out_0.xy), float2 (2, 2), float2 (0, 0), _TilingAndOffset_67d58b7a8092828aaece7f3acfe6e8a9_Out_3);
            float2 _Vector2_9a3e446bd0dfc3868e3db6f9bf101ead_Out_0 = float2(1, 1);
            float2 _Vector2_8355e4dbd796b188ba1e500dcabf0c0f_Out_0 = float2(30, 30);
            float2 _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2;
            Unity_Divide_float2(_Vector2_9a3e446bd0dfc3868e3db6f9bf101ead_Out_0, _Vector2_8355e4dbd796b188ba1e500dcabf0c0f_Out_0, _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2);
            float2 _Divide_5d0f49ab4bcab689894f976217114a79_Out_2;
            Unity_Divide_float2(_TilingAndOffset_67d58b7a8092828aaece7f3acfe6e8a9_Out_3, _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2, _Divide_5d0f49ab4bcab689894f976217114a79_Out_2);
            float2 _Fraction_e4f0f05fb76fd3809f5ec855b54a4595_Out_1;
            Unity_Fraction_float2(_Divide_5d0f49ab4bcab689894f976217114a79_Out_2, _Fraction_e4f0f05fb76fd3809f5ec855b54a4595_Out_1);
            float2 _Multiply_cbcb0de832b1d08288916e46aa8d4df2_Out_2;
            Unity_Multiply_float2_float2(_Fraction_e4f0f05fb76fd3809f5ec855b54a4595_Out_1, _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2, _Multiply_cbcb0de832b1d08288916e46aa8d4df2_Out_2);
            float2 _Vector2_e6d2595367ed0380b42c7a74855838a5_Out_0 = float2(246, 246);
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_Width_0 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.z;
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_Height_2 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.w;
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_TexelWidth_3 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.x;
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_TexelHeight_4 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.y;
            float2 _Vector2_e8728bfbf194e5818f010927b4ba8d79_Out_0 = float2(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Width_0, _TexelSize_18bb9f416e386f87bee91aa46804bc56_Height_2);
            float2 _Multiply_561a2890964f4081a8cf5d16bccb889f_Out_2;
            Unity_Multiply_float2_float2(_Vector2_e6d2595367ed0380b42c7a74855838a5_Out_0, _Vector2_e8728bfbf194e5818f010927b4ba8d79_Out_0, _Multiply_561a2890964f4081a8cf5d16bccb889f_Out_2);
            float2 _Vector2_97c81bcbfd26f08385a101ac3da412f3_Out_0 = float2(10, 10);
            float2 _Multiply_bccda05b3809138d8da15484dd6a7b4e_Out_2;
            Unity_Multiply_float2_float2(_Vector2_e8728bfbf194e5818f010927b4ba8d79_Out_0, _Vector2_97c81bcbfd26f08385a101ac3da412f3_Out_0, _Multiply_bccda05b3809138d8da15484dd6a7b4e_Out_2);
            float2 _Subtract_710d4672e66f7780ad90ca6e19a733bd_Out_2;
            Unity_Subtract_float2(_Multiply_561a2890964f4081a8cf5d16bccb889f_Out_2, _Multiply_bccda05b3809138d8da15484dd6a7b4e_Out_2, _Subtract_710d4672e66f7780ad90ca6e19a733bd_Out_2);
            float2 _Multiply_cd542324eceadd8daaca18ab78d3fbf6_Out_2;
            Unity_Multiply_float2_float2(_Multiply_cbcb0de832b1d08288916e46aa8d4df2_Out_2, _Subtract_710d4672e66f7780ad90ca6e19a733bd_Out_2, _Multiply_cd542324eceadd8daaca18ab78d3fbf6_Out_2);
            float2 _Vector2_df86361e9c36e7828e3097cc1fa8a75b_Out_0 = float2(0.1, 0.1);
            float2 _Multiply_8e351d40d36634829f2bb0905b9b9342_Out_2;
            Unity_Multiply_float2_float2(_Multiply_cd542324eceadd8daaca18ab78d3fbf6_Out_2, _Vector2_df86361e9c36e7828e3097cc1fa8a75b_Out_0, _Multiply_8e351d40d36634829f2bb0905b9b9342_Out_2);
            float4 _UV_1ca38e193be034888b8a707af9d47210_Out_0 = IN.uv3;
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_R_1 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[0];
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[1];
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_B_3 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[2];
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_A_4 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[3];
            float _Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2;
            Unity_Multiply_float_float(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2, -1, _Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2);
            float _Comparison_e67d9847aa86858490dd0c37e68e7505_Out_2;
            Unity_Comparison_Greater_float(_Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2, 0, _Comparison_e67d9847aa86858490dd0c37e68e7505_Out_2);
            float _Divide_2b884a92fd783d82a9012917c2b83115_Out_2;
            Unity_Divide_float(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_R_1, _Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2, _Divide_2b884a92fd783d82a9012917c2b83115_Out_2);
            float _Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1;
            Unity_Arctangent_float(_Divide_2b884a92fd783d82a9012917c2b83115_Out_2, _Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1);
            float _Add_cf6b7aee4a487f8ca16b9171b64a8f0d_Out_2;
            Unity_Add_float(_Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1, 3.141592, _Add_cf6b7aee4a487f8ca16b9171b64a8f0d_Out_2);
            float _Branch_9de100b31215ba89bb19e5b89012a25a_Out_3;
            Unity_Branch_float(_Comparison_e67d9847aa86858490dd0c37e68e7505_Out_2, _Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1, _Add_cf6b7aee4a487f8ca16b9171b64a8f0d_Out_2, _Branch_9de100b31215ba89bb19e5b89012a25a_Out_3);
            float2 _Rotate_ff1406ef744b188ca79c6d3bf0b8bbd7_Out_3;
            Unity_Rotate_Radians_float(_Multiply_8e351d40d36634829f2bb0905b9b9342_Out_2, float2 (0.5, 0.5), _Branch_9de100b31215ba89bb19e5b89012a25a_Out_3, _Rotate_ff1406ef744b188ca79c6d3bf0b8bbd7_Out_3);
            #if defined(SHADER_API_GLES) && (SHADER_TARGET < 30)
              float4 _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0 = float4(0.0f, 0.0f, 0.0f, 1.0f);
            #else
              float4 _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0 = SAMPLE_TEXTURE2D_LOD(_Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0.tex, _Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0.samplerstate, _Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0.GetTransformedUV(_Rotate_ff1406ef744b188ca79c6d3bf0b8bbd7_Out_3), 0);
            #endif
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_R_5 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.r;
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_G_6 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.g;
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_B_7 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.b;
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_A_8 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.a;
            float _Comparison_1a93f0ca7c913a8f81659b20b8d59f2b_Out_2;
            Unity_Comparison_Greater_float(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2, 0.001, _Comparison_1a93f0ca7c913a8f81659b20b8d59f2b_Out_2);
            float3 _Vector3_37ab63b41d52bb8d8eba9088ed71bc11_Out_0 = float3(0, 0, 0);
            float2 _Vector2_4aeecd1cb271dd8e96c2fb12309b9dd0_Out_0 = float2(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_R_1, _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2);
            float _Distance_47e812ff6269a181b6e0faa017faee3b_Out_2;
            Unity_Distance_float2(_Vector2_4aeecd1cb271dd8e96c2fb12309b9dd0_Out_0, float2(0, 0), _Distance_47e812ff6269a181b6e0faa017faee3b_Out_2);
            float _Multiply_e5102f48c5e759879293e06062894e0c_Out_2;
            Unity_Multiply_float_float(_Distance_47e812ff6269a181b6e0faa017faee3b_Out_2, 1.6, _Multiply_e5102f48c5e759879293e06062894e0c_Out_2);
            float _Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3;
            Unity_Clamp_float(_Multiply_e5102f48c5e759879293e06062894e0c_Out_2, 0, 1, _Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3);
            float _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2;
            Unity_Subtract_float(1, _Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3, _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2);
            float3 _Vector3_aeff4a868e81c68f85dfe4c9662da9ce_Out_0 = float3(1, _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2, 0);
            float _Absolute_5573b6b2c1616e819aa4eeb67caec457_Out_1;
            Unity_Absolute_float(_Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3, _Absolute_5573b6b2c1616e819aa4eeb67caec457_Out_1);
            float _Power_33dedbf009cacf848c403a58667a66c6_Out_2;
            Unity_Power_float(_Absolute_5573b6b2c1616e819aa4eeb67caec457_Out_1, 0.16, _Power_33dedbf009cacf848c403a58667a66c6_Out_2);
            float _Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3;
            Unity_Clamp_float(_Power_33dedbf009cacf848c403a58667a66c6_Out_2, 0, 1, _Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3);
            float3 _Lerp_e6000a54e83a8f8e8c5bc16419b360ca_Out_3;
            Unity_Lerp_float3(_Vector3_37ab63b41d52bb8d8eba9088ed71bc11_Out_0, _Vector3_aeff4a868e81c68f85dfe4c9662da9ce_Out_0, (_Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3.xxx), _Lerp_e6000a54e83a8f8e8c5bc16419b360ca_Out_3);
            float3 _Vector3_484f28086891e98d9589d1f65774f6f4_Out_0 = float3(0, _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2, 1);
            float3 _Lerp_93469a90f98ccf80a7121c0b2bf05f48_Out_3;
            Unity_Lerp_float3(_Vector3_37ab63b41d52bb8d8eba9088ed71bc11_Out_0, _Vector3_484f28086891e98d9589d1f65774f6f4_Out_0, (_Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3.xxx), _Lerp_93469a90f98ccf80a7121c0b2bf05f48_Out_3);
            float3 _Branch_365bf259ae64c386a18e2d6997727100_Out_3;
            Unity_Branch_float3(_Comparison_1a93f0ca7c913a8f81659b20b8d59f2b_Out_2, _Lerp_e6000a54e83a8f8e8c5bc16419b360ca_Out_3, _Lerp_93469a90f98ccf80a7121c0b2bf05f48_Out_3, _Branch_365bf259ae64c386a18e2d6997727100_Out_3);
            float3 _Multiply_0563147e8bc9868b8c2a4f2c442c349f_Out_2;
            Unity_Multiply_float3_float3((_SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_A_8.xxx), _Branch_365bf259ae64c386a18e2d6997727100_Out_3, _Multiply_0563147e8bc9868b8c2a4f2c442c349f_Out_2);
            surface.BaseColor = _Multiply_0563147e8bc9868b8c2a4f2c442c349f_Out_2;
            surface.Emission = float3(0, 0, 0);
            surface.Alpha = 1;
            surface.AlphaClipThreshold = 0.5;
            return surface;
        }
        
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES AttributesMesh
            #define VaryingsMeshType VaryingsMeshToPS
            #define VFX_SRP_VARYINGS VaryingsMeshType
            #define VFX_SRP_SURFACE_INPUTS FragInputs
            #endif
            SurfaceDescriptionInputs FragInputsToSurfaceDescriptionInputs(FragInputs input, float3 viewWS)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
            #if defined(SHADER_STAGE_RAY_TRACING)
            #else
            #endif
        
        #if UNITY_UV_STARTS_AT_TOP
        #else
        #endif
        
        
            output.uv0 =                                        input.texCoord0;
            output.uv1 =                                        input.texCoord1;
            output.uv2 =                                        input.texCoord2;
            output.uv3 =                                        input.texCoord3;
        
            // splice point to copy frag inputs custom interpolator pack into the SDI
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorCopyToSDI' */
        
            return output;
        }
        
            // --------------------------------------------------
            // Build Surface Data (Specific Material)
        
        void BuildSurfaceData(FragInputs fragInputs, inout SurfaceDescription surfaceDescription, float3 V, PositionInputs posInput, out SurfaceData surfaceData)
        {
            // setup defaults -- these are used if the graph doesn't output a value
            ZERO_INITIALIZE(SurfaceData, surfaceData);
        
            // copy across graph values, if defined
            surfaceData.color = surfaceDescription.BaseColor;
        
            #ifdef WRITE_NORMAL_BUFFER
            // When we need to export the normal (in the depth prepass, we write the geometry one)
            surfaceData.normalWS = fragInputs.tangentToWorld[2];
            #endif
        
            #if defined(DEBUG_DISPLAY)
            if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
            {
                // TODO
            }
            #endif
        
            #ifdef _ENABLE_SHADOW_MATTE
        
                #if (SHADERPASS == SHADERPASS_FORWARD_UNLIT) || (SHADERPASS == SHADERPASS_RAYTRACING_GBUFFER) || (SHADERPASS == SHADERPASS_RAYTRACING_INDIRECT) || (SHADERPASS == SHADERPASS_RAYTRACING_FORWARD)
        
                    HDShadowContext shadowContext = InitShadowContext();
        
                    // Evaluate the shadow, the normal is guaranteed if shadow matte is enabled on this shader.
                    float3 shadow3;
                    ShadowLoopMin(shadowContext, posInput, normalize(fragInputs.tangentToWorld[2]), asuint(_ShadowMatteFilter), GetMeshRenderingLightLayer(), shadow3);
        
                    // Compute the average value in the fourth channel
                    float4 shadow = float4(shadow3, dot(shadow3, float3(1.0/3.0, 1.0/3.0, 1.0/3.0)));
        
                    float4 shadowColor = (1.0 - shadow) * surfaceDescription.ShadowTint.rgba;
                    float  localAlpha  = saturate(shadowColor.a + surfaceDescription.Alpha);
        
                    // Keep the nested lerp
                    // With no Color (bsdfData.color.rgb, bsdfData.color.a == 0.0f), just use ShadowColor*Color to avoid a ring of "white" around the shadow
                    // And mix color to consider the Color & ShadowColor alpha (from texture or/and color picker)
                    #ifdef _SURFACE_TYPE_TRANSPARENT
                        surfaceData.color = lerp(shadowColor.rgb * surfaceData.color, lerp(lerp(shadowColor.rgb, surfaceData.color, 1.0 - surfaceDescription.ShadowTint.a), surfaceData.color, shadow.rgb), surfaceDescription.Alpha);
                    #else
                        surfaceData.color = lerp(lerp(shadowColor.rgb, surfaceData.color, 1.0 - surfaceDescription.ShadowTint.a), surfaceData.color, shadow.rgb);
                    #endif
                    localAlpha = ApplyBlendMode(surfaceData.color, localAlpha).a;
        
                    surfaceDescription.Alpha = localAlpha;
        
                #elif SHADERPASS == SHADERPASS_PATH_TRACING
        
                    surfaceData.normalWS = fragInputs.tangentToWorld[2];
                    surfaceData.shadowTint = surfaceDescription.ShadowTint.rgba;
        
                #endif
        
            #endif // _ENABLE_SHADOW_MATTE
        }
        
            // --------------------------------------------------
            // Get Surface And BuiltinData
        
            void GetSurfaceAndBuiltinData(FragInputs fragInputs, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData RAY_TRACING_OPTIONAL_PARAMETERS)
            {
                // Don't dither if displaced tessellation (we're fading out the displacement instead to match the next LOD)
                #if !defined(SHADER_STAGE_RAY_TRACING) && !defined(_TESSELLATION_DISPLACEMENT)
                #ifdef LOD_FADE_CROSSFADE // enable dithering LOD transition if user select CrossFade transition in LOD group
                LODDitheringTransition(ComputeFadeMaskSeed(V, posInput.positionSS), unity_LODFade.x);
                #endif
                #endif
        
                #ifndef SHADER_UNLIT
                #ifdef _DOUBLESIDED_ON
                    float3 doubleSidedConstants = _DoubleSidedConstants.xyz;
                #else
                    float3 doubleSidedConstants = float3(1.0, 1.0, 1.0);
                #endif
        
                ApplyDoubleSidedFlipOrMirror(fragInputs, doubleSidedConstants); // Apply double sided flip on the vertex normal
                #endif // SHADER_UNLIT
        
                SurfaceDescriptionInputs surfaceDescriptionInputs = FragInputsToSurfaceDescriptionInputs(fragInputs, V);
        
                #if defined(HAVE_VFX_MODIFICATION)
                GraphProperties properties;
                ZERO_INITIALIZE(GraphProperties, properties);
        
                GetElementPixelProperties(fragInputs, properties);
        
                SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs, properties);
                #else
                SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);
                #endif
        
                // Perform alpha test very early to save performance (a killed pixel will not sample textures)
                // TODO: split graph evaluation to grab just alpha dependencies first? tricky..
                #ifdef _ALPHATEST_ON
                    float alphaCutoff = surfaceDescription.AlphaClipThreshold;
                    #if SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_PREPASS
                    // The TransparentDepthPrepass is also used with SSR transparent.
                    // If an artists enable transaprent SSR but not the TransparentDepthPrepass itself, then we use AlphaClipThreshold
                    // otherwise if TransparentDepthPrepass is enabled we use AlphaClipThresholdDepthPrepass
                    #elif SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_POSTPASS
                    // DepthPostpass always use its own alpha threshold
                    alphaCutoff = surfaceDescription.AlphaClipThresholdDepthPostpass;
                    #elif (SHADERPASS == SHADERPASS_SHADOWS) || (SHADERPASS == SHADERPASS_RAYTRACING_VISIBILITY)
                    // If use shadow threshold isn't enable we don't allow any test
                    #endif
        
                    GENERIC_ALPHA_TEST(surfaceDescription.Alpha, alphaCutoff);
                #endif
        
                #if !defined(SHADER_STAGE_RAY_TRACING) && _DEPTHOFFSET_ON
                ApplyDepthOffsetPositionInput(V, surfaceDescription.DepthOffset, GetViewForwardDir(), GetWorldToHClipMatrix(), posInput);
                #endif
        
                #ifndef SHADER_UNLIT
                float3 bentNormalWS;
                BuildSurfaceData(fragInputs, surfaceDescription, V, posInput, surfaceData, bentNormalWS);
        
                #ifdef FRAG_INPUTS_USE_TEXCOORD1
                    float4 lightmapTexCoord1 = fragInputs.texCoord1;
                #else
                    float4 lightmapTexCoord1 = float4(0,0,0,0);
                #endif
        
                #ifdef FRAG_INPUTS_USE_TEXCOORD2
                    float4 lightmapTexCoord2 = fragInputs.texCoord2;
                #else
                    float4 lightmapTexCoord2 = float4(0,0,0,0);
                #endif
        
                // Builtin Data
                // For back lighting we use the oposite vertex normal
                InitBuiltinData(posInput, surfaceDescription.Alpha, bentNormalWS, -fragInputs.tangentToWorld[2], lightmapTexCoord1, lightmapTexCoord2, builtinData);
        
                #else
                BuildSurfaceData(fragInputs, surfaceDescription, V, posInput, surfaceData);
        
                ZERO_BUILTIN_INITIALIZE(builtinData); // No call to InitBuiltinData as we don't have any lighting
                builtinData.opacity = surfaceDescription.Alpha;
        
                #if defined(DEBUG_DISPLAY)
                    // Light Layers are currently not used for the Unlit shader (because it is not lit)
                    // But Unlit objects do cast shadows according to their rendering layer mask, which is what we want to
                    // display in the light layers visualization mode, therefore we need the renderingLayers
                    builtinData.renderingLayers = GetMeshRenderingLightLayer();
                #endif
        
                #endif // SHADER_UNLIT
        
                #ifdef _ALPHATEST_ON
                    // Used for sharpening by alpha to mask - Alpha to covertage is only used with depth only and forward pass (no shadow pass, no transparent pass)
                    builtinData.alphaClipTreshold = alphaCutoff;
                #endif
        
                // override sampleBakedGI - not used by Unlit
        
                builtinData.emissiveColor = surfaceDescription.Emission;
        
                // Note this will not fully work on transparent surfaces (can check with _SURFACE_TYPE_TRANSPARENT define)
                // We will always overwrite vt feeback with the nearest. So behind transparent surfaces vt will not be resolved
                // This is a limitation of the current MRT approach.
                #ifdef UNITY_VIRTUAL_TEXTURING
                #endif
        
                #if _DEPTHOFFSET_ON
                builtinData.depthOffset = surfaceDescription.DepthOffset;
                #endif
        
                // TODO: We should generate distortion / distortionBlur for non distortion pass
                #if (SHADERPASS == SHADERPASS_DISTORTION)
                builtinData.distortion = surfaceDescription.Distortion;
                builtinData.distortionBlur = surfaceDescription.DistortionBlur;
                #endif
        
                #ifndef SHADER_UNLIT
                // PostInitBuiltinData call ApplyDebugToBuiltinData
                PostInitBuiltinData(V, posInput, surfaceData, builtinData);
                #else
                ApplyDebugToBuiltinData(builtinData);
                #endif
        
                RAY_TRACING_OPTIONAL_ALPHA_TEST_PASS
            }
        
            // --------------------------------------------------
            // Main
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingVisibility.hlsl"
        
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
        
        	#ifdef HAVE_VFX_MODIFICATION
        	#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/VisualEffectVertex.hlsl"
        	#endif
        
            ENDHLSL
        }
        Pass
        {
            Name "ForwardDXR"
            Tags
            {
                "LightMode" = "ForwardDXR"
            }
        
            // Render State
            // RenderState: <None>
        
            // Debug
            // <None>
        
            // --------------------------------------------------
            // Pass
        
            HLSLPROGRAM
        
            // Pragmas
            #pragma target 5.0
        #pragma raytracing surface_shader
        #pragma only_renderers d3d11 xboxseries ps5
        
            // Keywords
            #pragma shader_feature_local _ _ALPHATEST_ON
        #pragma shader_feature _ _SURFACE_TYPE_TRANSPARENT
        #pragma shader_feature_local _BLENDMODE_OFF _BLENDMODE_ALPHA _BLENDMODE_ADD _BLENDMODE_PRE_MULTIPLY
        #pragma shader_feature_local _ _ADD_PRECOMPUTED_VELOCITY
        #pragma shader_feature_local _ _TRANSPARENT_WRITES_MOTION_VEC
        #pragma shader_feature_local_fragment _ _ENABLE_FOG_ON_TRANSPARENT
        #pragma multi_compile _ DEBUG_DISPLAY
            // GraphKeywords: <None>
        
            // For custom interpolators to inject a substruct definition before FragInputs definition,
            // allowing for FragInputs to capture CI's intended for ShaderGraph's SDI.
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreInclude' */
        
        
            // TODO: Merge FragInputsVFX substruct with CustomInterpolators.
        	#ifdef HAVE_VFX_MODIFICATION
        	struct FragInputsVFX
            {
                /* WARNING: $splice Could not find named fragment 'FragInputsVFX' */
            };
            #endif
        
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GeometricTools.hlsl" // Required by Tessellation.hlsl
        	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Tessellation.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl" // Required to be include before we include properties as it define DECLARE_STACK_CB
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphHeader.hlsl" // Need to be here for Gradient struct definition
        
            // --------------------------------------------------
            // Defines
        
            // Attribute
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define ATTRIBUTES_NEED_TEXCOORD2
            #define ATTRIBUTES_NEED_TEXCOORD3
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_TEXCOORD1
            #define VARYINGS_NEED_TEXCOORD2
            #define VARYINGS_NEED_TEXCOORD3
        
        
            //Strip down the FragInputs.hlsl (on graphics), so we can only optimize the interpolators we use.
            //if by accident something requests contents of FragInputs.hlsl, it will be caught as a compiler error
            //Frag inputs stripping is only enabled when FRAG_INPUTS_ENABLE_STRIPPING is set
            #if !defined(SHADER_STAGE_RAY_TRACING) && SHADERPASS != SHADERPASS_RAYTRACING_GBUFFER && SHADERPASS != SHADERPASS_FULL_SCREEN_DEBUG
            #define FRAG_INPUTS_ENABLE_STRIPPING
            #endif
            #define FRAG_INPUTS_USE_TEXCOORD0
            #define FRAG_INPUTS_USE_TEXCOORD1
            #define FRAG_INPUTS_USE_TEXCOORD2
            #define FRAG_INPUTS_USE_TEXCOORD3
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
        
        
        
            #define SHADERPASS SHADERPASS_RAYTRACING_FORWARD
        #define SHADOW_LOW
        #define RAYTRACING_SHADER_GRAPH_RAYTRACED
        
        
            // Following two define are a workaround introduce in 10.1.x for RaytracingQualityNode
            // The ShaderGraph don't support correctly migration of this node as it serialize all the node data
            // in the json file making it impossible to uprgrade. Until we get a fix, we do a workaround here
            // to still allow us to rename the field and keyword of this node without breaking existing code.
            #ifdef RAYTRACING_SHADER_GRAPH_DEFAULT
            #define RAYTRACING_SHADER_GRAPH_HIGH
            #endif
        
            #ifdef RAYTRACING_SHADER_GRAPH_RAYTRACED
            #define RAYTRACING_SHADER_GRAPH_LOW
            #endif
            // end
        
            #ifndef SHADER_UNLIT
            // We need isFrontFace when using double sided - it is not required for unlit as in case of unlit double sided only drive the cullmode
            // VARYINGS_NEED_CULLFACE can be define by VaryingsMeshToPS.FaceSign input if a IsFrontFace Node is included in the shader graph.
            #if defined(_DOUBLESIDED_ON) && !defined(VARYINGS_NEED_CULLFACE)
                #define VARYINGS_NEED_CULLFACE
            #endif
            #endif
        
            // Specific Material Define
        // Setup a define to say we are an unlit shader
        #define SHADER_UNLIT
        
        // Following Macro are only used by Unlit material
        #if defined(_ENABLE_SHADOW_MATTE) && (SHADERPASS == SHADERPASS_FORWARD_UNLIT || SHADERPASS == SHADERPASS_PATH_TRACING)
        #define LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
        #define HAS_LIGHTLOOP
        #endif
            // Caution: we can use the define SHADER_UNLIT onlit after the above Material include as it is the Unlit template who define it
        
            // To handle SSR on transparent correctly with a possibility to enable/disable it per framesettings
            // we should have a code like this:
            // if !defined(_DISABLE_SSR_TRANSPARENT)
            // pragma multi_compile _ WRITE_NORMAL_BUFFER
            // endif
            // i.e we enable the multicompile only if we can receive SSR or not, and then C# code drive
            // it based on if SSR transparent in frame settings and not (and stripper can strip it).
            // this is currently not possible with our current preprocessor as _DISABLE_SSR_TRANSPARENT is a keyword not a define
            // so instead we used this and chose to pay the extra cost of normal write even if SSR transaprent is disabled.
            // Ideally the shader graph generator should handle it but condition below can't be handle correctly for now.
            #if SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_PREPASS
            #if !defined(_DISABLE_SSR_TRANSPARENT) && !defined(SHADER_UNLIT)
                #define WRITE_NORMAL_BUFFER
            #endif
            #endif
        
            #ifndef DEBUG_DISPLAY
                // In case of opaque we don't want to perform the alpha test, it is done in depth prepass and we use depth equal for ztest (setup from UI)
                // Don't do it with debug display mode as it is possible there is no depth prepass in this case
                #if !defined(_SURFACE_TYPE_TRANSPARENT)
                    #if SHADERPASS == SHADERPASS_FORWARD
                    #define SHADERPASS_FORWARD_BYPASS_ALPHA_TEST
                    #elif SHADERPASS == SHADERPASS_GBUFFER
                    #define SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST
                    #endif
                #endif
            #endif
        
            // Define _DEFERRED_CAPABLE_MATERIAL for shader capable to run in deferred pass
            #if defined(SHADER_LIT) && !defined(_SURFACE_TYPE_TRANSPARENT)
                #define _DEFERRED_CAPABLE_MATERIAL
            #endif
        
            // Translate transparent motion vector define
            #if defined(_TRANSPARENT_WRITES_MOTION_VEC) && defined(_SURFACE_TYPE_TRANSPARENT)
                #define _WRITE_TRANSPARENT_MOTION_VECTOR
            #endif
        
            // -- Graph Properties
            CBUFFER_START(UnityPerMaterial)
        float4 _TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1_TexelSize;
        float4 _Direction_TexelSize;
        float2 _TextureSize;
        float _RotateUV;
        float Boolean_F2B1E91A;
        float4 _EmissionColor;
        float _UseShadowThreshold;
        float4 _DoubleSidedConstants;
        float _BlendMode;
        float _EnableBlendModePreserveSpecularLighting;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1);
        SAMPLER(sampler_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1);
        TEXTURE2D(_Direction);
        SAMPLER(sampler_Direction);
        
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
        
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
        
            // Includes
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracingLightLoop.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/UnlitRaytracing.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RayTracingCommon.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl"
        	// GraphIncludes: <None>
        
            // --------------------------------------------------
            // Structs and Packing
        
            struct SurfaceDescriptionInputs
        {
             float4 uv0;
             float4 uv1;
             float4 uv2;
             float4 uv3;
        };
        
            //Interpolator Packs: <None>
        
            // --------------------------------------------------
            // Graph
        
        
            // Graph Functions
            
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A / B;
        }
        
        void Unity_Fraction_float2(float2 In, out float2 Out)
        {
            Out = frac(In);
        }
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A * B;
        }
        
        void Unity_Subtract_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A - B;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_Comparison_Greater_float(float A, float B, out float Out)
        {
            Out = A > B ? 1 : 0;
        }
        
        void Unity_Divide_float(float A, float B, out float Out)
        {
            Out = A / B;
        }
        
        void Unity_Arctangent_float(float In, out float Out)
        {
            Out = atan(In);
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Branch_float(float Predicate, float True, float False, out float Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Rotate_Radians_float(float2 UV, float2 Center, float Rotation, out float2 Out)
        {
            //rotation matrix
            UV -= Center;
            float s = sin(Rotation);
            float c = cos(Rotation);
        
            //center rotation matrix
            float2x2 rMatrix = float2x2(c, -s, s, c);
            rMatrix *= 0.5;
            rMatrix += 0.5;
            rMatrix = rMatrix*2 - 1;
        
            //multiply the UVs by the rotation matrix
            UV.xy = mul(UV.xy, rMatrix);
            UV += Center;
        
            Out = UV;
        }
        
        void Unity_Distance_float2(float2 A, float2 B, out float Out)
        {
            Out = distance(A, B);
        }
        
        void Unity_Clamp_float(float In, float Min, float Max, out float Out)
        {
            Out = clamp(In, Min, Max);
        }
        
        void Unity_Subtract_float(float A, float B, out float Out)
        {
            Out = A - B;
        }
        
        void Unity_Absolute_float(float In, out float Out)
        {
            Out = abs(In);
        }
        
        void Unity_Power_float(float A, float B, out float Out)
        {
            Out = pow(A, B);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A * B;
        }
        
            // Graph Vertex
            // GraphVertex: <None>
        
            // Graph Pixel
            struct SurfaceDescription
        {
            float3 BaseColor;
            float3 Emission;
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0 = UnityBuildTexture2DStructNoScale(_Direction);
            float4 _UV_7c8a457b34527181924b2b4a71350de9_Out_0 = IN.uv0;
            float2 _TilingAndOffset_67d58b7a8092828aaece7f3acfe6e8a9_Out_3;
            Unity_TilingAndOffset_float((_UV_7c8a457b34527181924b2b4a71350de9_Out_0.xy), float2 (2, 2), float2 (0, 0), _TilingAndOffset_67d58b7a8092828aaece7f3acfe6e8a9_Out_3);
            float2 _Vector2_9a3e446bd0dfc3868e3db6f9bf101ead_Out_0 = float2(1, 1);
            float2 _Vector2_8355e4dbd796b188ba1e500dcabf0c0f_Out_0 = float2(30, 30);
            float2 _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2;
            Unity_Divide_float2(_Vector2_9a3e446bd0dfc3868e3db6f9bf101ead_Out_0, _Vector2_8355e4dbd796b188ba1e500dcabf0c0f_Out_0, _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2);
            float2 _Divide_5d0f49ab4bcab689894f976217114a79_Out_2;
            Unity_Divide_float2(_TilingAndOffset_67d58b7a8092828aaece7f3acfe6e8a9_Out_3, _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2, _Divide_5d0f49ab4bcab689894f976217114a79_Out_2);
            float2 _Fraction_e4f0f05fb76fd3809f5ec855b54a4595_Out_1;
            Unity_Fraction_float2(_Divide_5d0f49ab4bcab689894f976217114a79_Out_2, _Fraction_e4f0f05fb76fd3809f5ec855b54a4595_Out_1);
            float2 _Multiply_cbcb0de832b1d08288916e46aa8d4df2_Out_2;
            Unity_Multiply_float2_float2(_Fraction_e4f0f05fb76fd3809f5ec855b54a4595_Out_1, _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2, _Multiply_cbcb0de832b1d08288916e46aa8d4df2_Out_2);
            float2 _Vector2_e6d2595367ed0380b42c7a74855838a5_Out_0 = float2(246, 246);
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_Width_0 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.z;
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_Height_2 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.w;
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_TexelWidth_3 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.x;
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_TexelHeight_4 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.y;
            float2 _Vector2_e8728bfbf194e5818f010927b4ba8d79_Out_0 = float2(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Width_0, _TexelSize_18bb9f416e386f87bee91aa46804bc56_Height_2);
            float2 _Multiply_561a2890964f4081a8cf5d16bccb889f_Out_2;
            Unity_Multiply_float2_float2(_Vector2_e6d2595367ed0380b42c7a74855838a5_Out_0, _Vector2_e8728bfbf194e5818f010927b4ba8d79_Out_0, _Multiply_561a2890964f4081a8cf5d16bccb889f_Out_2);
            float2 _Vector2_97c81bcbfd26f08385a101ac3da412f3_Out_0 = float2(10, 10);
            float2 _Multiply_bccda05b3809138d8da15484dd6a7b4e_Out_2;
            Unity_Multiply_float2_float2(_Vector2_e8728bfbf194e5818f010927b4ba8d79_Out_0, _Vector2_97c81bcbfd26f08385a101ac3da412f3_Out_0, _Multiply_bccda05b3809138d8da15484dd6a7b4e_Out_2);
            float2 _Subtract_710d4672e66f7780ad90ca6e19a733bd_Out_2;
            Unity_Subtract_float2(_Multiply_561a2890964f4081a8cf5d16bccb889f_Out_2, _Multiply_bccda05b3809138d8da15484dd6a7b4e_Out_2, _Subtract_710d4672e66f7780ad90ca6e19a733bd_Out_2);
            float2 _Multiply_cd542324eceadd8daaca18ab78d3fbf6_Out_2;
            Unity_Multiply_float2_float2(_Multiply_cbcb0de832b1d08288916e46aa8d4df2_Out_2, _Subtract_710d4672e66f7780ad90ca6e19a733bd_Out_2, _Multiply_cd542324eceadd8daaca18ab78d3fbf6_Out_2);
            float2 _Vector2_df86361e9c36e7828e3097cc1fa8a75b_Out_0 = float2(0.1, 0.1);
            float2 _Multiply_8e351d40d36634829f2bb0905b9b9342_Out_2;
            Unity_Multiply_float2_float2(_Multiply_cd542324eceadd8daaca18ab78d3fbf6_Out_2, _Vector2_df86361e9c36e7828e3097cc1fa8a75b_Out_0, _Multiply_8e351d40d36634829f2bb0905b9b9342_Out_2);
            float4 _UV_1ca38e193be034888b8a707af9d47210_Out_0 = IN.uv3;
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_R_1 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[0];
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[1];
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_B_3 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[2];
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_A_4 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[3];
            float _Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2;
            Unity_Multiply_float_float(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2, -1, _Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2);
            float _Comparison_e67d9847aa86858490dd0c37e68e7505_Out_2;
            Unity_Comparison_Greater_float(_Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2, 0, _Comparison_e67d9847aa86858490dd0c37e68e7505_Out_2);
            float _Divide_2b884a92fd783d82a9012917c2b83115_Out_2;
            Unity_Divide_float(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_R_1, _Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2, _Divide_2b884a92fd783d82a9012917c2b83115_Out_2);
            float _Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1;
            Unity_Arctangent_float(_Divide_2b884a92fd783d82a9012917c2b83115_Out_2, _Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1);
            float _Add_cf6b7aee4a487f8ca16b9171b64a8f0d_Out_2;
            Unity_Add_float(_Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1, 3.141592, _Add_cf6b7aee4a487f8ca16b9171b64a8f0d_Out_2);
            float _Branch_9de100b31215ba89bb19e5b89012a25a_Out_3;
            Unity_Branch_float(_Comparison_e67d9847aa86858490dd0c37e68e7505_Out_2, _Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1, _Add_cf6b7aee4a487f8ca16b9171b64a8f0d_Out_2, _Branch_9de100b31215ba89bb19e5b89012a25a_Out_3);
            float2 _Rotate_ff1406ef744b188ca79c6d3bf0b8bbd7_Out_3;
            Unity_Rotate_Radians_float(_Multiply_8e351d40d36634829f2bb0905b9b9342_Out_2, float2 (0.5, 0.5), _Branch_9de100b31215ba89bb19e5b89012a25a_Out_3, _Rotate_ff1406ef744b188ca79c6d3bf0b8bbd7_Out_3);
            #if defined(SHADER_API_GLES) && (SHADER_TARGET < 30)
              float4 _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0 = float4(0.0f, 0.0f, 0.0f, 1.0f);
            #else
              float4 _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0 = SAMPLE_TEXTURE2D_LOD(_Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0.tex, _Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0.samplerstate, _Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0.GetTransformedUV(_Rotate_ff1406ef744b188ca79c6d3bf0b8bbd7_Out_3), 0);
            #endif
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_R_5 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.r;
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_G_6 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.g;
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_B_7 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.b;
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_A_8 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.a;
            float _Comparison_1a93f0ca7c913a8f81659b20b8d59f2b_Out_2;
            Unity_Comparison_Greater_float(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2, 0.001, _Comparison_1a93f0ca7c913a8f81659b20b8d59f2b_Out_2);
            float3 _Vector3_37ab63b41d52bb8d8eba9088ed71bc11_Out_0 = float3(0, 0, 0);
            float2 _Vector2_4aeecd1cb271dd8e96c2fb12309b9dd0_Out_0 = float2(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_R_1, _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2);
            float _Distance_47e812ff6269a181b6e0faa017faee3b_Out_2;
            Unity_Distance_float2(_Vector2_4aeecd1cb271dd8e96c2fb12309b9dd0_Out_0, float2(0, 0), _Distance_47e812ff6269a181b6e0faa017faee3b_Out_2);
            float _Multiply_e5102f48c5e759879293e06062894e0c_Out_2;
            Unity_Multiply_float_float(_Distance_47e812ff6269a181b6e0faa017faee3b_Out_2, 1.6, _Multiply_e5102f48c5e759879293e06062894e0c_Out_2);
            float _Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3;
            Unity_Clamp_float(_Multiply_e5102f48c5e759879293e06062894e0c_Out_2, 0, 1, _Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3);
            float _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2;
            Unity_Subtract_float(1, _Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3, _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2);
            float3 _Vector3_aeff4a868e81c68f85dfe4c9662da9ce_Out_0 = float3(1, _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2, 0);
            float _Absolute_5573b6b2c1616e819aa4eeb67caec457_Out_1;
            Unity_Absolute_float(_Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3, _Absolute_5573b6b2c1616e819aa4eeb67caec457_Out_1);
            float _Power_33dedbf009cacf848c403a58667a66c6_Out_2;
            Unity_Power_float(_Absolute_5573b6b2c1616e819aa4eeb67caec457_Out_1, 0.16, _Power_33dedbf009cacf848c403a58667a66c6_Out_2);
            float _Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3;
            Unity_Clamp_float(_Power_33dedbf009cacf848c403a58667a66c6_Out_2, 0, 1, _Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3);
            float3 _Lerp_e6000a54e83a8f8e8c5bc16419b360ca_Out_3;
            Unity_Lerp_float3(_Vector3_37ab63b41d52bb8d8eba9088ed71bc11_Out_0, _Vector3_aeff4a868e81c68f85dfe4c9662da9ce_Out_0, (_Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3.xxx), _Lerp_e6000a54e83a8f8e8c5bc16419b360ca_Out_3);
            float3 _Vector3_484f28086891e98d9589d1f65774f6f4_Out_0 = float3(0, _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2, 1);
            float3 _Lerp_93469a90f98ccf80a7121c0b2bf05f48_Out_3;
            Unity_Lerp_float3(_Vector3_37ab63b41d52bb8d8eba9088ed71bc11_Out_0, _Vector3_484f28086891e98d9589d1f65774f6f4_Out_0, (_Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3.xxx), _Lerp_93469a90f98ccf80a7121c0b2bf05f48_Out_3);
            float3 _Branch_365bf259ae64c386a18e2d6997727100_Out_3;
            Unity_Branch_float3(_Comparison_1a93f0ca7c913a8f81659b20b8d59f2b_Out_2, _Lerp_e6000a54e83a8f8e8c5bc16419b360ca_Out_3, _Lerp_93469a90f98ccf80a7121c0b2bf05f48_Out_3, _Branch_365bf259ae64c386a18e2d6997727100_Out_3);
            float3 _Multiply_0563147e8bc9868b8c2a4f2c442c349f_Out_2;
            Unity_Multiply_float3_float3((_SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_A_8.xxx), _Branch_365bf259ae64c386a18e2d6997727100_Out_3, _Multiply_0563147e8bc9868b8c2a4f2c442c349f_Out_2);
            surface.BaseColor = _Multiply_0563147e8bc9868b8c2a4f2c442c349f_Out_2;
            surface.Emission = float3(0, 0, 0);
            surface.Alpha = 1;
            surface.AlphaClipThreshold = 0.5;
            return surface;
        }
        
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES AttributesMesh
            #define VaryingsMeshType VaryingsMeshToPS
            #define VFX_SRP_VARYINGS VaryingsMeshType
            #define VFX_SRP_SURFACE_INPUTS FragInputs
            #endif
            SurfaceDescriptionInputs FragInputsToSurfaceDescriptionInputs(FragInputs input, float3 viewWS)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
            #if defined(SHADER_STAGE_RAY_TRACING)
            #else
            #endif
        
        #if UNITY_UV_STARTS_AT_TOP
        #else
        #endif
        
        
            output.uv0 =                                        input.texCoord0;
            output.uv1 =                                        input.texCoord1;
            output.uv2 =                                        input.texCoord2;
            output.uv3 =                                        input.texCoord3;
        
            // splice point to copy frag inputs custom interpolator pack into the SDI
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorCopyToSDI' */
        
            return output;
        }
        
            // --------------------------------------------------
            // Build Surface Data (Specific Material)
        
        void BuildSurfaceData(FragInputs fragInputs, inout SurfaceDescription surfaceDescription, float3 V, PositionInputs posInput, out SurfaceData surfaceData)
        {
            // setup defaults -- these are used if the graph doesn't output a value
            ZERO_INITIALIZE(SurfaceData, surfaceData);
        
            // copy across graph values, if defined
            surfaceData.color = surfaceDescription.BaseColor;
        
            #ifdef WRITE_NORMAL_BUFFER
            // When we need to export the normal (in the depth prepass, we write the geometry one)
            surfaceData.normalWS = fragInputs.tangentToWorld[2];
            #endif
        
            #if defined(DEBUG_DISPLAY)
            if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
            {
                // TODO
            }
            #endif
        
            #ifdef _ENABLE_SHADOW_MATTE
        
                #if (SHADERPASS == SHADERPASS_FORWARD_UNLIT) || (SHADERPASS == SHADERPASS_RAYTRACING_GBUFFER) || (SHADERPASS == SHADERPASS_RAYTRACING_INDIRECT) || (SHADERPASS == SHADERPASS_RAYTRACING_FORWARD)
        
                    HDShadowContext shadowContext = InitShadowContext();
        
                    // Evaluate the shadow, the normal is guaranteed if shadow matte is enabled on this shader.
                    float3 shadow3;
                    ShadowLoopMin(shadowContext, posInput, normalize(fragInputs.tangentToWorld[2]), asuint(_ShadowMatteFilter), GetMeshRenderingLightLayer(), shadow3);
        
                    // Compute the average value in the fourth channel
                    float4 shadow = float4(shadow3, dot(shadow3, float3(1.0/3.0, 1.0/3.0, 1.0/3.0)));
        
                    float4 shadowColor = (1.0 - shadow) * surfaceDescription.ShadowTint.rgba;
                    float  localAlpha  = saturate(shadowColor.a + surfaceDescription.Alpha);
        
                    // Keep the nested lerp
                    // With no Color (bsdfData.color.rgb, bsdfData.color.a == 0.0f), just use ShadowColor*Color to avoid a ring of "white" around the shadow
                    // And mix color to consider the Color & ShadowColor alpha (from texture or/and color picker)
                    #ifdef _SURFACE_TYPE_TRANSPARENT
                        surfaceData.color = lerp(shadowColor.rgb * surfaceData.color, lerp(lerp(shadowColor.rgb, surfaceData.color, 1.0 - surfaceDescription.ShadowTint.a), surfaceData.color, shadow.rgb), surfaceDescription.Alpha);
                    #else
                        surfaceData.color = lerp(lerp(shadowColor.rgb, surfaceData.color, 1.0 - surfaceDescription.ShadowTint.a), surfaceData.color, shadow.rgb);
                    #endif
                    localAlpha = ApplyBlendMode(surfaceData.color, localAlpha).a;
        
                    surfaceDescription.Alpha = localAlpha;
        
                #elif SHADERPASS == SHADERPASS_PATH_TRACING
        
                    surfaceData.normalWS = fragInputs.tangentToWorld[2];
                    surfaceData.shadowTint = surfaceDescription.ShadowTint.rgba;
        
                #endif
        
            #endif // _ENABLE_SHADOW_MATTE
        }
        
            // --------------------------------------------------
            // Get Surface And BuiltinData
        
            void GetSurfaceAndBuiltinData(FragInputs fragInputs, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData RAY_TRACING_OPTIONAL_PARAMETERS)
            {
                // Don't dither if displaced tessellation (we're fading out the displacement instead to match the next LOD)
                #if !defined(SHADER_STAGE_RAY_TRACING) && !defined(_TESSELLATION_DISPLACEMENT)
                #ifdef LOD_FADE_CROSSFADE // enable dithering LOD transition if user select CrossFade transition in LOD group
                LODDitheringTransition(ComputeFadeMaskSeed(V, posInput.positionSS), unity_LODFade.x);
                #endif
                #endif
        
                #ifndef SHADER_UNLIT
                #ifdef _DOUBLESIDED_ON
                    float3 doubleSidedConstants = _DoubleSidedConstants.xyz;
                #else
                    float3 doubleSidedConstants = float3(1.0, 1.0, 1.0);
                #endif
        
                ApplyDoubleSidedFlipOrMirror(fragInputs, doubleSidedConstants); // Apply double sided flip on the vertex normal
                #endif // SHADER_UNLIT
        
                SurfaceDescriptionInputs surfaceDescriptionInputs = FragInputsToSurfaceDescriptionInputs(fragInputs, V);
        
                #if defined(HAVE_VFX_MODIFICATION)
                GraphProperties properties;
                ZERO_INITIALIZE(GraphProperties, properties);
        
                GetElementPixelProperties(fragInputs, properties);
        
                SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs, properties);
                #else
                SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);
                #endif
        
                // Perform alpha test very early to save performance (a killed pixel will not sample textures)
                // TODO: split graph evaluation to grab just alpha dependencies first? tricky..
                #ifdef _ALPHATEST_ON
                    float alphaCutoff = surfaceDescription.AlphaClipThreshold;
                    #if SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_PREPASS
                    // The TransparentDepthPrepass is also used with SSR transparent.
                    // If an artists enable transaprent SSR but not the TransparentDepthPrepass itself, then we use AlphaClipThreshold
                    // otherwise if TransparentDepthPrepass is enabled we use AlphaClipThresholdDepthPrepass
                    #elif SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_POSTPASS
                    // DepthPostpass always use its own alpha threshold
                    alphaCutoff = surfaceDescription.AlphaClipThresholdDepthPostpass;
                    #elif (SHADERPASS == SHADERPASS_SHADOWS) || (SHADERPASS == SHADERPASS_RAYTRACING_VISIBILITY)
                    // If use shadow threshold isn't enable we don't allow any test
                    #endif
        
                    GENERIC_ALPHA_TEST(surfaceDescription.Alpha, alphaCutoff);
                #endif
        
                #if !defined(SHADER_STAGE_RAY_TRACING) && _DEPTHOFFSET_ON
                ApplyDepthOffsetPositionInput(V, surfaceDescription.DepthOffset, GetViewForwardDir(), GetWorldToHClipMatrix(), posInput);
                #endif
        
                #ifndef SHADER_UNLIT
                float3 bentNormalWS;
                BuildSurfaceData(fragInputs, surfaceDescription, V, posInput, surfaceData, bentNormalWS);
        
                #ifdef FRAG_INPUTS_USE_TEXCOORD1
                    float4 lightmapTexCoord1 = fragInputs.texCoord1;
                #else
                    float4 lightmapTexCoord1 = float4(0,0,0,0);
                #endif
        
                #ifdef FRAG_INPUTS_USE_TEXCOORD2
                    float4 lightmapTexCoord2 = fragInputs.texCoord2;
                #else
                    float4 lightmapTexCoord2 = float4(0,0,0,0);
                #endif
        
                // Builtin Data
                // For back lighting we use the oposite vertex normal
                InitBuiltinData(posInput, surfaceDescription.Alpha, bentNormalWS, -fragInputs.tangentToWorld[2], lightmapTexCoord1, lightmapTexCoord2, builtinData);
        
                #else
                BuildSurfaceData(fragInputs, surfaceDescription, V, posInput, surfaceData);
        
                ZERO_BUILTIN_INITIALIZE(builtinData); // No call to InitBuiltinData as we don't have any lighting
                builtinData.opacity = surfaceDescription.Alpha;
        
                #if defined(DEBUG_DISPLAY)
                    // Light Layers are currently not used for the Unlit shader (because it is not lit)
                    // But Unlit objects do cast shadows according to their rendering layer mask, which is what we want to
                    // display in the light layers visualization mode, therefore we need the renderingLayers
                    builtinData.renderingLayers = GetMeshRenderingLightLayer();
                #endif
        
                #endif // SHADER_UNLIT
        
                #ifdef _ALPHATEST_ON
                    // Used for sharpening by alpha to mask - Alpha to covertage is only used with depth only and forward pass (no shadow pass, no transparent pass)
                    builtinData.alphaClipTreshold = alphaCutoff;
                #endif
        
                // override sampleBakedGI - not used by Unlit
        
                builtinData.emissiveColor = surfaceDescription.Emission;
        
                // Note this will not fully work on transparent surfaces (can check with _SURFACE_TYPE_TRANSPARENT define)
                // We will always overwrite vt feeback with the nearest. So behind transparent surfaces vt will not be resolved
                // This is a limitation of the current MRT approach.
                #ifdef UNITY_VIRTUAL_TEXTURING
                #endif
        
                #if _DEPTHOFFSET_ON
                builtinData.depthOffset = surfaceDescription.DepthOffset;
                #endif
        
                // TODO: We should generate distortion / distortionBlur for non distortion pass
                #if (SHADERPASS == SHADERPASS_DISTORTION)
                builtinData.distortion = surfaceDescription.Distortion;
                builtinData.distortionBlur = surfaceDescription.DistortionBlur;
                #endif
        
                #ifndef SHADER_UNLIT
                // PostInitBuiltinData call ApplyDebugToBuiltinData
                PostInitBuiltinData(V, posInput, surfaceData, builtinData);
                #else
                ApplyDebugToBuiltinData(builtinData);
                #endif
        
                RAY_TRACING_OPTIONAL_ALPHA_TEST_PASS
            }
        
            // --------------------------------------------------
            // Main
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingForward.hlsl"
        
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
        
        	#ifdef HAVE_VFX_MODIFICATION
        	#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/VisualEffectVertex.hlsl"
        	#endif
        
            ENDHLSL
        }
        Pass
        {
            Name "GBufferDXR"
            Tags
            {
                "LightMode" = "GBufferDXR"
            }
        
            // Render State
            // RenderState: <None>
        
            // Debug
            // <None>
        
            // --------------------------------------------------
            // Pass
        
            HLSLPROGRAM
        
            // Pragmas
            #pragma target 5.0
        #pragma raytracing surface_shader
        #pragma only_renderers d3d11 xboxseries ps5
        
            // Keywords
            #pragma shader_feature_local _ _ALPHATEST_ON
        #pragma shader_feature _ _SURFACE_TYPE_TRANSPARENT
        #pragma shader_feature_local _BLENDMODE_OFF _BLENDMODE_ALPHA _BLENDMODE_ADD _BLENDMODE_PRE_MULTIPLY
        #pragma shader_feature_local _ _ADD_PRECOMPUTED_VELOCITY
        #pragma shader_feature_local _ _TRANSPARENT_WRITES_MOTION_VEC
        #pragma shader_feature_local_fragment _ _ENABLE_FOG_ON_TRANSPARENT
        #pragma multi_compile _ DEBUG_DISPLAY
            // GraphKeywords: <None>
        
            // For custom interpolators to inject a substruct definition before FragInputs definition,
            // allowing for FragInputs to capture CI's intended for ShaderGraph's SDI.
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreInclude' */
        
        
            // TODO: Merge FragInputsVFX substruct with CustomInterpolators.
        	#ifdef HAVE_VFX_MODIFICATION
        	struct FragInputsVFX
            {
                /* WARNING: $splice Could not find named fragment 'FragInputsVFX' */
            };
            #endif
        
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GeometricTools.hlsl" // Required by Tessellation.hlsl
        	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Tessellation.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl" // Required to be include before we include properties as it define DECLARE_STACK_CB
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphHeader.hlsl" // Need to be here for Gradient struct definition
        
            // --------------------------------------------------
            // Defines
        
            // Attribute
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define ATTRIBUTES_NEED_TEXCOORD2
            #define ATTRIBUTES_NEED_TEXCOORD3
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_TEXCOORD1
            #define VARYINGS_NEED_TEXCOORD2
            #define VARYINGS_NEED_TEXCOORD3
        
        
            //Strip down the FragInputs.hlsl (on graphics), so we can only optimize the interpolators we use.
            //if by accident something requests contents of FragInputs.hlsl, it will be caught as a compiler error
            //Frag inputs stripping is only enabled when FRAG_INPUTS_ENABLE_STRIPPING is set
            #if !defined(SHADER_STAGE_RAY_TRACING) && SHADERPASS != SHADERPASS_RAYTRACING_GBUFFER && SHADERPASS != SHADERPASS_FULL_SCREEN_DEBUG
            #define FRAG_INPUTS_ENABLE_STRIPPING
            #endif
            #define FRAG_INPUTS_USE_TEXCOORD0
            #define FRAG_INPUTS_USE_TEXCOORD1
            #define FRAG_INPUTS_USE_TEXCOORD2
            #define FRAG_INPUTS_USE_TEXCOORD3
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
        
        
        
            #define SHADERPASS SHADERPASS_RAYTRACING_GBUFFER
        #define SHADOW_LOW
        #define RAYTRACING_SHADER_GRAPH_RAYTRACED
        
        
            // Following two define are a workaround introduce in 10.1.x for RaytracingQualityNode
            // The ShaderGraph don't support correctly migration of this node as it serialize all the node data
            // in the json file making it impossible to uprgrade. Until we get a fix, we do a workaround here
            // to still allow us to rename the field and keyword of this node without breaking existing code.
            #ifdef RAYTRACING_SHADER_GRAPH_DEFAULT
            #define RAYTRACING_SHADER_GRAPH_HIGH
            #endif
        
            #ifdef RAYTRACING_SHADER_GRAPH_RAYTRACED
            #define RAYTRACING_SHADER_GRAPH_LOW
            #endif
            // end
        
            #ifndef SHADER_UNLIT
            // We need isFrontFace when using double sided - it is not required for unlit as in case of unlit double sided only drive the cullmode
            // VARYINGS_NEED_CULLFACE can be define by VaryingsMeshToPS.FaceSign input if a IsFrontFace Node is included in the shader graph.
            #if defined(_DOUBLESIDED_ON) && !defined(VARYINGS_NEED_CULLFACE)
                #define VARYINGS_NEED_CULLFACE
            #endif
            #endif
        
            // Specific Material Define
        // Setup a define to say we are an unlit shader
        #define SHADER_UNLIT
        
        // Following Macro are only used by Unlit material
        #if defined(_ENABLE_SHADOW_MATTE) && (SHADERPASS == SHADERPASS_FORWARD_UNLIT || SHADERPASS == SHADERPASS_PATH_TRACING)
        #define LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
        #define HAS_LIGHTLOOP
        #endif
            // Caution: we can use the define SHADER_UNLIT onlit after the above Material include as it is the Unlit template who define it
        
            // To handle SSR on transparent correctly with a possibility to enable/disable it per framesettings
            // we should have a code like this:
            // if !defined(_DISABLE_SSR_TRANSPARENT)
            // pragma multi_compile _ WRITE_NORMAL_BUFFER
            // endif
            // i.e we enable the multicompile only if we can receive SSR or not, and then C# code drive
            // it based on if SSR transparent in frame settings and not (and stripper can strip it).
            // this is currently not possible with our current preprocessor as _DISABLE_SSR_TRANSPARENT is a keyword not a define
            // so instead we used this and chose to pay the extra cost of normal write even if SSR transaprent is disabled.
            // Ideally the shader graph generator should handle it but condition below can't be handle correctly for now.
            #if SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_PREPASS
            #if !defined(_DISABLE_SSR_TRANSPARENT) && !defined(SHADER_UNLIT)
                #define WRITE_NORMAL_BUFFER
            #endif
            #endif
        
            #ifndef DEBUG_DISPLAY
                // In case of opaque we don't want to perform the alpha test, it is done in depth prepass and we use depth equal for ztest (setup from UI)
                // Don't do it with debug display mode as it is possible there is no depth prepass in this case
                #if !defined(_SURFACE_TYPE_TRANSPARENT)
                    #if SHADERPASS == SHADERPASS_FORWARD
                    #define SHADERPASS_FORWARD_BYPASS_ALPHA_TEST
                    #elif SHADERPASS == SHADERPASS_GBUFFER
                    #define SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST
                    #endif
                #endif
            #endif
        
            // Define _DEFERRED_CAPABLE_MATERIAL for shader capable to run in deferred pass
            #if defined(SHADER_LIT) && !defined(_SURFACE_TYPE_TRANSPARENT)
                #define _DEFERRED_CAPABLE_MATERIAL
            #endif
        
            // Translate transparent motion vector define
            #if defined(_TRANSPARENT_WRITES_MOTION_VEC) && defined(_SURFACE_TYPE_TRANSPARENT)
                #define _WRITE_TRANSPARENT_MOTION_VECTOR
            #endif
        
            // -- Graph Properties
            CBUFFER_START(UnityPerMaterial)
        float4 _TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1_TexelSize;
        float4 _Direction_TexelSize;
        float2 _TextureSize;
        float _RotateUV;
        float Boolean_F2B1E91A;
        float4 _EmissionColor;
        float _UseShadowThreshold;
        float4 _DoubleSidedConstants;
        float _BlendMode;
        float _EnableBlendModePreserveSpecularLighting;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1);
        SAMPLER(sampler_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1);
        TEXTURE2D(_Direction);
        SAMPLER(sampler_Direction);
        
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
        
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
        
            // Includes
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracingLightLoop.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/Deferred/RaytracingIntersectonGBuffer.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/StandardLit/StandardLit.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/UnlitRaytracing.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RayTracingCommon.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl"
        	// GraphIncludes: <None>
        
            // --------------------------------------------------
            // Structs and Packing
        
            struct SurfaceDescriptionInputs
        {
             float4 uv0;
             float4 uv1;
             float4 uv2;
             float4 uv3;
        };
        
            //Interpolator Packs: <None>
        
            // --------------------------------------------------
            // Graph
        
        
            // Graph Functions
            
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A / B;
        }
        
        void Unity_Fraction_float2(float2 In, out float2 Out)
        {
            Out = frac(In);
        }
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A * B;
        }
        
        void Unity_Subtract_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A - B;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_Comparison_Greater_float(float A, float B, out float Out)
        {
            Out = A > B ? 1 : 0;
        }
        
        void Unity_Divide_float(float A, float B, out float Out)
        {
            Out = A / B;
        }
        
        void Unity_Arctangent_float(float In, out float Out)
        {
            Out = atan(In);
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Branch_float(float Predicate, float True, float False, out float Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Rotate_Radians_float(float2 UV, float2 Center, float Rotation, out float2 Out)
        {
            //rotation matrix
            UV -= Center;
            float s = sin(Rotation);
            float c = cos(Rotation);
        
            //center rotation matrix
            float2x2 rMatrix = float2x2(c, -s, s, c);
            rMatrix *= 0.5;
            rMatrix += 0.5;
            rMatrix = rMatrix*2 - 1;
        
            //multiply the UVs by the rotation matrix
            UV.xy = mul(UV.xy, rMatrix);
            UV += Center;
        
            Out = UV;
        }
        
        void Unity_Distance_float2(float2 A, float2 B, out float Out)
        {
            Out = distance(A, B);
        }
        
        void Unity_Clamp_float(float In, float Min, float Max, out float Out)
        {
            Out = clamp(In, Min, Max);
        }
        
        void Unity_Subtract_float(float A, float B, out float Out)
        {
            Out = A - B;
        }
        
        void Unity_Absolute_float(float In, out float Out)
        {
            Out = abs(In);
        }
        
        void Unity_Power_float(float A, float B, out float Out)
        {
            Out = pow(A, B);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A * B;
        }
        
            // Graph Vertex
            // GraphVertex: <None>
        
            // Graph Pixel
            struct SurfaceDescription
        {
            float3 BaseColor;
            float3 Emission;
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0 = UnityBuildTexture2DStructNoScale(_Direction);
            float4 _UV_7c8a457b34527181924b2b4a71350de9_Out_0 = IN.uv0;
            float2 _TilingAndOffset_67d58b7a8092828aaece7f3acfe6e8a9_Out_3;
            Unity_TilingAndOffset_float((_UV_7c8a457b34527181924b2b4a71350de9_Out_0.xy), float2 (2, 2), float2 (0, 0), _TilingAndOffset_67d58b7a8092828aaece7f3acfe6e8a9_Out_3);
            float2 _Vector2_9a3e446bd0dfc3868e3db6f9bf101ead_Out_0 = float2(1, 1);
            float2 _Vector2_8355e4dbd796b188ba1e500dcabf0c0f_Out_0 = float2(30, 30);
            float2 _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2;
            Unity_Divide_float2(_Vector2_9a3e446bd0dfc3868e3db6f9bf101ead_Out_0, _Vector2_8355e4dbd796b188ba1e500dcabf0c0f_Out_0, _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2);
            float2 _Divide_5d0f49ab4bcab689894f976217114a79_Out_2;
            Unity_Divide_float2(_TilingAndOffset_67d58b7a8092828aaece7f3acfe6e8a9_Out_3, _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2, _Divide_5d0f49ab4bcab689894f976217114a79_Out_2);
            float2 _Fraction_e4f0f05fb76fd3809f5ec855b54a4595_Out_1;
            Unity_Fraction_float2(_Divide_5d0f49ab4bcab689894f976217114a79_Out_2, _Fraction_e4f0f05fb76fd3809f5ec855b54a4595_Out_1);
            float2 _Multiply_cbcb0de832b1d08288916e46aa8d4df2_Out_2;
            Unity_Multiply_float2_float2(_Fraction_e4f0f05fb76fd3809f5ec855b54a4595_Out_1, _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2, _Multiply_cbcb0de832b1d08288916e46aa8d4df2_Out_2);
            float2 _Vector2_e6d2595367ed0380b42c7a74855838a5_Out_0 = float2(246, 246);
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_Width_0 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.z;
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_Height_2 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.w;
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_TexelWidth_3 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.x;
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_TexelHeight_4 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.y;
            float2 _Vector2_e8728bfbf194e5818f010927b4ba8d79_Out_0 = float2(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Width_0, _TexelSize_18bb9f416e386f87bee91aa46804bc56_Height_2);
            float2 _Multiply_561a2890964f4081a8cf5d16bccb889f_Out_2;
            Unity_Multiply_float2_float2(_Vector2_e6d2595367ed0380b42c7a74855838a5_Out_0, _Vector2_e8728bfbf194e5818f010927b4ba8d79_Out_0, _Multiply_561a2890964f4081a8cf5d16bccb889f_Out_2);
            float2 _Vector2_97c81bcbfd26f08385a101ac3da412f3_Out_0 = float2(10, 10);
            float2 _Multiply_bccda05b3809138d8da15484dd6a7b4e_Out_2;
            Unity_Multiply_float2_float2(_Vector2_e8728bfbf194e5818f010927b4ba8d79_Out_0, _Vector2_97c81bcbfd26f08385a101ac3da412f3_Out_0, _Multiply_bccda05b3809138d8da15484dd6a7b4e_Out_2);
            float2 _Subtract_710d4672e66f7780ad90ca6e19a733bd_Out_2;
            Unity_Subtract_float2(_Multiply_561a2890964f4081a8cf5d16bccb889f_Out_2, _Multiply_bccda05b3809138d8da15484dd6a7b4e_Out_2, _Subtract_710d4672e66f7780ad90ca6e19a733bd_Out_2);
            float2 _Multiply_cd542324eceadd8daaca18ab78d3fbf6_Out_2;
            Unity_Multiply_float2_float2(_Multiply_cbcb0de832b1d08288916e46aa8d4df2_Out_2, _Subtract_710d4672e66f7780ad90ca6e19a733bd_Out_2, _Multiply_cd542324eceadd8daaca18ab78d3fbf6_Out_2);
            float2 _Vector2_df86361e9c36e7828e3097cc1fa8a75b_Out_0 = float2(0.1, 0.1);
            float2 _Multiply_8e351d40d36634829f2bb0905b9b9342_Out_2;
            Unity_Multiply_float2_float2(_Multiply_cd542324eceadd8daaca18ab78d3fbf6_Out_2, _Vector2_df86361e9c36e7828e3097cc1fa8a75b_Out_0, _Multiply_8e351d40d36634829f2bb0905b9b9342_Out_2);
            float4 _UV_1ca38e193be034888b8a707af9d47210_Out_0 = IN.uv3;
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_R_1 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[0];
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[1];
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_B_3 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[2];
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_A_4 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[3];
            float _Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2;
            Unity_Multiply_float_float(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2, -1, _Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2);
            float _Comparison_e67d9847aa86858490dd0c37e68e7505_Out_2;
            Unity_Comparison_Greater_float(_Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2, 0, _Comparison_e67d9847aa86858490dd0c37e68e7505_Out_2);
            float _Divide_2b884a92fd783d82a9012917c2b83115_Out_2;
            Unity_Divide_float(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_R_1, _Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2, _Divide_2b884a92fd783d82a9012917c2b83115_Out_2);
            float _Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1;
            Unity_Arctangent_float(_Divide_2b884a92fd783d82a9012917c2b83115_Out_2, _Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1);
            float _Add_cf6b7aee4a487f8ca16b9171b64a8f0d_Out_2;
            Unity_Add_float(_Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1, 3.141592, _Add_cf6b7aee4a487f8ca16b9171b64a8f0d_Out_2);
            float _Branch_9de100b31215ba89bb19e5b89012a25a_Out_3;
            Unity_Branch_float(_Comparison_e67d9847aa86858490dd0c37e68e7505_Out_2, _Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1, _Add_cf6b7aee4a487f8ca16b9171b64a8f0d_Out_2, _Branch_9de100b31215ba89bb19e5b89012a25a_Out_3);
            float2 _Rotate_ff1406ef744b188ca79c6d3bf0b8bbd7_Out_3;
            Unity_Rotate_Radians_float(_Multiply_8e351d40d36634829f2bb0905b9b9342_Out_2, float2 (0.5, 0.5), _Branch_9de100b31215ba89bb19e5b89012a25a_Out_3, _Rotate_ff1406ef744b188ca79c6d3bf0b8bbd7_Out_3);
            #if defined(SHADER_API_GLES) && (SHADER_TARGET < 30)
              float4 _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0 = float4(0.0f, 0.0f, 0.0f, 1.0f);
            #else
              float4 _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0 = SAMPLE_TEXTURE2D_LOD(_Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0.tex, _Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0.samplerstate, _Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0.GetTransformedUV(_Rotate_ff1406ef744b188ca79c6d3bf0b8bbd7_Out_3), 0);
            #endif
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_R_5 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.r;
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_G_6 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.g;
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_B_7 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.b;
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_A_8 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.a;
            float _Comparison_1a93f0ca7c913a8f81659b20b8d59f2b_Out_2;
            Unity_Comparison_Greater_float(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2, 0.001, _Comparison_1a93f0ca7c913a8f81659b20b8d59f2b_Out_2);
            float3 _Vector3_37ab63b41d52bb8d8eba9088ed71bc11_Out_0 = float3(0, 0, 0);
            float2 _Vector2_4aeecd1cb271dd8e96c2fb12309b9dd0_Out_0 = float2(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_R_1, _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2);
            float _Distance_47e812ff6269a181b6e0faa017faee3b_Out_2;
            Unity_Distance_float2(_Vector2_4aeecd1cb271dd8e96c2fb12309b9dd0_Out_0, float2(0, 0), _Distance_47e812ff6269a181b6e0faa017faee3b_Out_2);
            float _Multiply_e5102f48c5e759879293e06062894e0c_Out_2;
            Unity_Multiply_float_float(_Distance_47e812ff6269a181b6e0faa017faee3b_Out_2, 1.6, _Multiply_e5102f48c5e759879293e06062894e0c_Out_2);
            float _Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3;
            Unity_Clamp_float(_Multiply_e5102f48c5e759879293e06062894e0c_Out_2, 0, 1, _Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3);
            float _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2;
            Unity_Subtract_float(1, _Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3, _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2);
            float3 _Vector3_aeff4a868e81c68f85dfe4c9662da9ce_Out_0 = float3(1, _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2, 0);
            float _Absolute_5573b6b2c1616e819aa4eeb67caec457_Out_1;
            Unity_Absolute_float(_Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3, _Absolute_5573b6b2c1616e819aa4eeb67caec457_Out_1);
            float _Power_33dedbf009cacf848c403a58667a66c6_Out_2;
            Unity_Power_float(_Absolute_5573b6b2c1616e819aa4eeb67caec457_Out_1, 0.16, _Power_33dedbf009cacf848c403a58667a66c6_Out_2);
            float _Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3;
            Unity_Clamp_float(_Power_33dedbf009cacf848c403a58667a66c6_Out_2, 0, 1, _Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3);
            float3 _Lerp_e6000a54e83a8f8e8c5bc16419b360ca_Out_3;
            Unity_Lerp_float3(_Vector3_37ab63b41d52bb8d8eba9088ed71bc11_Out_0, _Vector3_aeff4a868e81c68f85dfe4c9662da9ce_Out_0, (_Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3.xxx), _Lerp_e6000a54e83a8f8e8c5bc16419b360ca_Out_3);
            float3 _Vector3_484f28086891e98d9589d1f65774f6f4_Out_0 = float3(0, _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2, 1);
            float3 _Lerp_93469a90f98ccf80a7121c0b2bf05f48_Out_3;
            Unity_Lerp_float3(_Vector3_37ab63b41d52bb8d8eba9088ed71bc11_Out_0, _Vector3_484f28086891e98d9589d1f65774f6f4_Out_0, (_Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3.xxx), _Lerp_93469a90f98ccf80a7121c0b2bf05f48_Out_3);
            float3 _Branch_365bf259ae64c386a18e2d6997727100_Out_3;
            Unity_Branch_float3(_Comparison_1a93f0ca7c913a8f81659b20b8d59f2b_Out_2, _Lerp_e6000a54e83a8f8e8c5bc16419b360ca_Out_3, _Lerp_93469a90f98ccf80a7121c0b2bf05f48_Out_3, _Branch_365bf259ae64c386a18e2d6997727100_Out_3);
            float3 _Multiply_0563147e8bc9868b8c2a4f2c442c349f_Out_2;
            Unity_Multiply_float3_float3((_SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_A_8.xxx), _Branch_365bf259ae64c386a18e2d6997727100_Out_3, _Multiply_0563147e8bc9868b8c2a4f2c442c349f_Out_2);
            surface.BaseColor = _Multiply_0563147e8bc9868b8c2a4f2c442c349f_Out_2;
            surface.Emission = float3(0, 0, 0);
            surface.Alpha = 1;
            surface.AlphaClipThreshold = 0.5;
            return surface;
        }
        
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES AttributesMesh
            #define VaryingsMeshType VaryingsMeshToPS
            #define VFX_SRP_VARYINGS VaryingsMeshType
            #define VFX_SRP_SURFACE_INPUTS FragInputs
            #endif
            SurfaceDescriptionInputs FragInputsToSurfaceDescriptionInputs(FragInputs input, float3 viewWS)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
            #if defined(SHADER_STAGE_RAY_TRACING)
            #else
            #endif
        
        #if UNITY_UV_STARTS_AT_TOP
        #else
        #endif
        
        
            output.uv0 =                                        input.texCoord0;
            output.uv1 =                                        input.texCoord1;
            output.uv2 =                                        input.texCoord2;
            output.uv3 =                                        input.texCoord3;
        
            // splice point to copy frag inputs custom interpolator pack into the SDI
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorCopyToSDI' */
        
            return output;
        }
        
            // --------------------------------------------------
            // Build Surface Data (Specific Material)
        
        void BuildSurfaceData(FragInputs fragInputs, inout SurfaceDescription surfaceDescription, float3 V, PositionInputs posInput, out SurfaceData surfaceData)
        {
            // setup defaults -- these are used if the graph doesn't output a value
            ZERO_INITIALIZE(SurfaceData, surfaceData);
        
            // copy across graph values, if defined
            surfaceData.color = surfaceDescription.BaseColor;
        
            #ifdef WRITE_NORMAL_BUFFER
            // When we need to export the normal (in the depth prepass, we write the geometry one)
            surfaceData.normalWS = fragInputs.tangentToWorld[2];
            #endif
        
            #if defined(DEBUG_DISPLAY)
            if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
            {
                // TODO
            }
            #endif
        
            #ifdef _ENABLE_SHADOW_MATTE
        
                #if (SHADERPASS == SHADERPASS_FORWARD_UNLIT) || (SHADERPASS == SHADERPASS_RAYTRACING_GBUFFER) || (SHADERPASS == SHADERPASS_RAYTRACING_INDIRECT) || (SHADERPASS == SHADERPASS_RAYTRACING_FORWARD)
        
                    HDShadowContext shadowContext = InitShadowContext();
        
                    // Evaluate the shadow, the normal is guaranteed if shadow matte is enabled on this shader.
                    float3 shadow3;
                    ShadowLoopMin(shadowContext, posInput, normalize(fragInputs.tangentToWorld[2]), asuint(_ShadowMatteFilter), GetMeshRenderingLightLayer(), shadow3);
        
                    // Compute the average value in the fourth channel
                    float4 shadow = float4(shadow3, dot(shadow3, float3(1.0/3.0, 1.0/3.0, 1.0/3.0)));
        
                    float4 shadowColor = (1.0 - shadow) * surfaceDescription.ShadowTint.rgba;
                    float  localAlpha  = saturate(shadowColor.a + surfaceDescription.Alpha);
        
                    // Keep the nested lerp
                    // With no Color (bsdfData.color.rgb, bsdfData.color.a == 0.0f), just use ShadowColor*Color to avoid a ring of "white" around the shadow
                    // And mix color to consider the Color & ShadowColor alpha (from texture or/and color picker)
                    #ifdef _SURFACE_TYPE_TRANSPARENT
                        surfaceData.color = lerp(shadowColor.rgb * surfaceData.color, lerp(lerp(shadowColor.rgb, surfaceData.color, 1.0 - surfaceDescription.ShadowTint.a), surfaceData.color, shadow.rgb), surfaceDescription.Alpha);
                    #else
                        surfaceData.color = lerp(lerp(shadowColor.rgb, surfaceData.color, 1.0 - surfaceDescription.ShadowTint.a), surfaceData.color, shadow.rgb);
                    #endif
                    localAlpha = ApplyBlendMode(surfaceData.color, localAlpha).a;
        
                    surfaceDescription.Alpha = localAlpha;
        
                #elif SHADERPASS == SHADERPASS_PATH_TRACING
        
                    surfaceData.normalWS = fragInputs.tangentToWorld[2];
                    surfaceData.shadowTint = surfaceDescription.ShadowTint.rgba;
        
                #endif
        
            #endif // _ENABLE_SHADOW_MATTE
        }
        
            // --------------------------------------------------
            // Get Surface And BuiltinData
        
            void GetSurfaceAndBuiltinData(FragInputs fragInputs, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData RAY_TRACING_OPTIONAL_PARAMETERS)
            {
                // Don't dither if displaced tessellation (we're fading out the displacement instead to match the next LOD)
                #if !defined(SHADER_STAGE_RAY_TRACING) && !defined(_TESSELLATION_DISPLACEMENT)
                #ifdef LOD_FADE_CROSSFADE // enable dithering LOD transition if user select CrossFade transition in LOD group
                LODDitheringTransition(ComputeFadeMaskSeed(V, posInput.positionSS), unity_LODFade.x);
                #endif
                #endif
        
                #ifndef SHADER_UNLIT
                #ifdef _DOUBLESIDED_ON
                    float3 doubleSidedConstants = _DoubleSidedConstants.xyz;
                #else
                    float3 doubleSidedConstants = float3(1.0, 1.0, 1.0);
                #endif
        
                ApplyDoubleSidedFlipOrMirror(fragInputs, doubleSidedConstants); // Apply double sided flip on the vertex normal
                #endif // SHADER_UNLIT
        
                SurfaceDescriptionInputs surfaceDescriptionInputs = FragInputsToSurfaceDescriptionInputs(fragInputs, V);
        
                #if defined(HAVE_VFX_MODIFICATION)
                GraphProperties properties;
                ZERO_INITIALIZE(GraphProperties, properties);
        
                GetElementPixelProperties(fragInputs, properties);
        
                SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs, properties);
                #else
                SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);
                #endif
        
                // Perform alpha test very early to save performance (a killed pixel will not sample textures)
                // TODO: split graph evaluation to grab just alpha dependencies first? tricky..
                #ifdef _ALPHATEST_ON
                    float alphaCutoff = surfaceDescription.AlphaClipThreshold;
                    #if SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_PREPASS
                    // The TransparentDepthPrepass is also used with SSR transparent.
                    // If an artists enable transaprent SSR but not the TransparentDepthPrepass itself, then we use AlphaClipThreshold
                    // otherwise if TransparentDepthPrepass is enabled we use AlphaClipThresholdDepthPrepass
                    #elif SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_POSTPASS
                    // DepthPostpass always use its own alpha threshold
                    alphaCutoff = surfaceDescription.AlphaClipThresholdDepthPostpass;
                    #elif (SHADERPASS == SHADERPASS_SHADOWS) || (SHADERPASS == SHADERPASS_RAYTRACING_VISIBILITY)
                    // If use shadow threshold isn't enable we don't allow any test
                    #endif
        
                    GENERIC_ALPHA_TEST(surfaceDescription.Alpha, alphaCutoff);
                #endif
        
                #if !defined(SHADER_STAGE_RAY_TRACING) && _DEPTHOFFSET_ON
                ApplyDepthOffsetPositionInput(V, surfaceDescription.DepthOffset, GetViewForwardDir(), GetWorldToHClipMatrix(), posInput);
                #endif
        
                #ifndef SHADER_UNLIT
                float3 bentNormalWS;
                BuildSurfaceData(fragInputs, surfaceDescription, V, posInput, surfaceData, bentNormalWS);
        
                #ifdef FRAG_INPUTS_USE_TEXCOORD1
                    float4 lightmapTexCoord1 = fragInputs.texCoord1;
                #else
                    float4 lightmapTexCoord1 = float4(0,0,0,0);
                #endif
        
                #ifdef FRAG_INPUTS_USE_TEXCOORD2
                    float4 lightmapTexCoord2 = fragInputs.texCoord2;
                #else
                    float4 lightmapTexCoord2 = float4(0,0,0,0);
                #endif
        
                // Builtin Data
                // For back lighting we use the oposite vertex normal
                InitBuiltinData(posInput, surfaceDescription.Alpha, bentNormalWS, -fragInputs.tangentToWorld[2], lightmapTexCoord1, lightmapTexCoord2, builtinData);
        
                #else
                BuildSurfaceData(fragInputs, surfaceDescription, V, posInput, surfaceData);
        
                ZERO_BUILTIN_INITIALIZE(builtinData); // No call to InitBuiltinData as we don't have any lighting
                builtinData.opacity = surfaceDescription.Alpha;
        
                #if defined(DEBUG_DISPLAY)
                    // Light Layers are currently not used for the Unlit shader (because it is not lit)
                    // But Unlit objects do cast shadows according to their rendering layer mask, which is what we want to
                    // display in the light layers visualization mode, therefore we need the renderingLayers
                    builtinData.renderingLayers = GetMeshRenderingLightLayer();
                #endif
        
                #endif // SHADER_UNLIT
        
                #ifdef _ALPHATEST_ON
                    // Used for sharpening by alpha to mask - Alpha to covertage is only used with depth only and forward pass (no shadow pass, no transparent pass)
                    builtinData.alphaClipTreshold = alphaCutoff;
                #endif
        
                // override sampleBakedGI - not used by Unlit
        
                builtinData.emissiveColor = surfaceDescription.Emission;
        
                // Note this will not fully work on transparent surfaces (can check with _SURFACE_TYPE_TRANSPARENT define)
                // We will always overwrite vt feeback with the nearest. So behind transparent surfaces vt will not be resolved
                // This is a limitation of the current MRT approach.
                #ifdef UNITY_VIRTUAL_TEXTURING
                #endif
        
                #if _DEPTHOFFSET_ON
                builtinData.depthOffset = surfaceDescription.DepthOffset;
                #endif
        
                // TODO: We should generate distortion / distortionBlur for non distortion pass
                #if (SHADERPASS == SHADERPASS_DISTORTION)
                builtinData.distortion = surfaceDescription.Distortion;
                builtinData.distortionBlur = surfaceDescription.DistortionBlur;
                #endif
        
                #ifndef SHADER_UNLIT
                // PostInitBuiltinData call ApplyDebugToBuiltinData
                PostInitBuiltinData(V, posInput, surfaceData, builtinData);
                #else
                ApplyDebugToBuiltinData(builtinData);
                #endif
        
                RAY_TRACING_OPTIONAL_ALPHA_TEST_PASS
            }
        
            // --------------------------------------------------
            // Main
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRaytracingGBuffer.hlsl"
        
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
        
        	#ifdef HAVE_VFX_MODIFICATION
        	#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/VisualEffectVertex.hlsl"
        	#endif
        
            ENDHLSL
        }
        Pass
        {
            Name "DebugDXR"
            Tags
            {
                "LightMode" = "DebugDXR"
            }
        
            // Debug
            // <None>
        
            // --------------------------------------------------
            // Pass
        
            HLSLPROGRAM
        
            // Pragmas
            #pragma target 5.0
        #pragma raytracing surface_shader
        #pragma only_renderers d3d11 xboxseries ps5
        
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
        
            // Includes
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RayTracingCommon.hlsl"
        	// GraphIncludes: <None>
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassRayTracingDebug.hlsl"
        
            ENDHLSL
        }
        Pass
        {
            Name "PathTracingDXR"
            Tags
            {
                "LightMode" = "PathTracingDXR"
            }
        
            // Render State
            // RenderState: <None>
        
            // Debug
            // <None>
        
            // --------------------------------------------------
            // Pass
        
            HLSLPROGRAM
        
            // Pragmas
            #pragma target 5.0
        #pragma raytracing surface_shader
        #pragma only_renderers d3d11 xboxseries ps5
        
            // Keywords
            #pragma shader_feature_local _ _ALPHATEST_ON
        #pragma shader_feature _ _SURFACE_TYPE_TRANSPARENT
        #pragma shader_feature_local _BLENDMODE_OFF _BLENDMODE_ALPHA _BLENDMODE_ADD _BLENDMODE_PRE_MULTIPLY
        #pragma shader_feature_local _ _ADD_PRECOMPUTED_VELOCITY
        #pragma shader_feature_local _ _TRANSPARENT_WRITES_MOTION_VEC
        #pragma shader_feature_local_fragment _ _ENABLE_FOG_ON_TRANSPARENT
            // GraphKeywords: <None>
        
            // For custom interpolators to inject a substruct definition before FragInputs definition,
            // allowing for FragInputs to capture CI's intended for ShaderGraph's SDI.
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreInclude' */
        
        
            // TODO: Merge FragInputsVFX substruct with CustomInterpolators.
        	#ifdef HAVE_VFX_MODIFICATION
        	struct FragInputsVFX
            {
                /* WARNING: $splice Could not find named fragment 'FragInputsVFX' */
            };
            #endif
        
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GeometricTools.hlsl" // Required by Tessellation.hlsl
        	#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Tessellation.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPass.cs.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl" // Required to be include before we include properties as it define DECLARE_STACK_CB
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphHeader.hlsl" // Need to be here for Gradient struct definition
        
            // --------------------------------------------------
            // Defines
        
            // Attribute
            #define ATTRIBUTES_NEED_TEXCOORD0
            #define ATTRIBUTES_NEED_TEXCOORD1
            #define ATTRIBUTES_NEED_TEXCOORD2
            #define ATTRIBUTES_NEED_TEXCOORD3
            #define VARYINGS_NEED_TEXCOORD0
            #define VARYINGS_NEED_TEXCOORD1
            #define VARYINGS_NEED_TEXCOORD2
            #define VARYINGS_NEED_TEXCOORD3
        
        
            //Strip down the FragInputs.hlsl (on graphics), so we can only optimize the interpolators we use.
            //if by accident something requests contents of FragInputs.hlsl, it will be caught as a compiler error
            //Frag inputs stripping is only enabled when FRAG_INPUTS_ENABLE_STRIPPING is set
            #if !defined(SHADER_STAGE_RAY_TRACING) && SHADERPASS != SHADERPASS_RAYTRACING_GBUFFER && SHADERPASS != SHADERPASS_FULL_SCREEN_DEBUG
            #define FRAG_INPUTS_ENABLE_STRIPPING
            #endif
            #define FRAG_INPUTS_USE_TEXCOORD0
            #define FRAG_INPUTS_USE_TEXCOORD1
            #define FRAG_INPUTS_USE_TEXCOORD2
            #define FRAG_INPUTS_USE_TEXCOORD3
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/FragInputs.hlsl"
        
        
        
            #define SHADERPASS SHADERPASS_PATH_TRACING
        
        
            // Following two define are a workaround introduce in 10.1.x for RaytracingQualityNode
            // The ShaderGraph don't support correctly migration of this node as it serialize all the node data
            // in the json file making it impossible to uprgrade. Until we get a fix, we do a workaround here
            // to still allow us to rename the field and keyword of this node without breaking existing code.
            #ifdef RAYTRACING_SHADER_GRAPH_DEFAULT
            #define RAYTRACING_SHADER_GRAPH_HIGH
            #endif
        
            #ifdef RAYTRACING_SHADER_GRAPH_RAYTRACED
            #define RAYTRACING_SHADER_GRAPH_LOW
            #endif
            // end
        
            #ifndef SHADER_UNLIT
            // We need isFrontFace when using double sided - it is not required for unlit as in case of unlit double sided only drive the cullmode
            // VARYINGS_NEED_CULLFACE can be define by VaryingsMeshToPS.FaceSign input if a IsFrontFace Node is included in the shader graph.
            #if defined(_DOUBLESIDED_ON) && !defined(VARYINGS_NEED_CULLFACE)
                #define VARYINGS_NEED_CULLFACE
            #endif
            #endif
        
            // Specific Material Define
        // Setup a define to say we are an unlit shader
        #define SHADER_UNLIT
        
        // Following Macro are only used by Unlit material
        #if defined(_ENABLE_SHADOW_MATTE) && (SHADERPASS == SHADERPASS_FORWARD_UNLIT || SHADERPASS == SHADERPASS_PATH_TRACING)
        #define LIGHTLOOP_DISABLE_TILE_AND_CLUSTER
        #define HAS_LIGHTLOOP
        #endif
            // Caution: we can use the define SHADER_UNLIT onlit after the above Material include as it is the Unlit template who define it
        
            // To handle SSR on transparent correctly with a possibility to enable/disable it per framesettings
            // we should have a code like this:
            // if !defined(_DISABLE_SSR_TRANSPARENT)
            // pragma multi_compile _ WRITE_NORMAL_BUFFER
            // endif
            // i.e we enable the multicompile only if we can receive SSR or not, and then C# code drive
            // it based on if SSR transparent in frame settings and not (and stripper can strip it).
            // this is currently not possible with our current preprocessor as _DISABLE_SSR_TRANSPARENT is a keyword not a define
            // so instead we used this and chose to pay the extra cost of normal write even if SSR transaprent is disabled.
            // Ideally the shader graph generator should handle it but condition below can't be handle correctly for now.
            #if SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_PREPASS
            #if !defined(_DISABLE_SSR_TRANSPARENT) && !defined(SHADER_UNLIT)
                #define WRITE_NORMAL_BUFFER
            #endif
            #endif
        
            #ifndef DEBUG_DISPLAY
                // In case of opaque we don't want to perform the alpha test, it is done in depth prepass and we use depth equal for ztest (setup from UI)
                // Don't do it with debug display mode as it is possible there is no depth prepass in this case
                #if !defined(_SURFACE_TYPE_TRANSPARENT)
                    #if SHADERPASS == SHADERPASS_FORWARD
                    #define SHADERPASS_FORWARD_BYPASS_ALPHA_TEST
                    #elif SHADERPASS == SHADERPASS_GBUFFER
                    #define SHADERPASS_GBUFFER_BYPASS_ALPHA_TEST
                    #endif
                #endif
            #endif
        
            // Define _DEFERRED_CAPABLE_MATERIAL for shader capable to run in deferred pass
            #if defined(SHADER_LIT) && !defined(_SURFACE_TYPE_TRANSPARENT)
                #define _DEFERRED_CAPABLE_MATERIAL
            #endif
        
            // Translate transparent motion vector define
            #if defined(_TRANSPARENT_WRITES_MOTION_VEC) && defined(_SURFACE_TYPE_TRANSPARENT)
                #define _WRITE_TRANSPARENT_MOTION_VECTOR
            #endif
        
            // -- Graph Properties
            CBUFFER_START(UnityPerMaterial)
        float4 _TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1_TexelSize;
        float4 _Direction_TexelSize;
        float2 _TextureSize;
        float _RotateUV;
        float Boolean_F2B1E91A;
        float4 _EmissionColor;
        float _UseShadowThreshold;
        float4 _DoubleSidedConstants;
        float _BlendMode;
        float _EnableBlendModePreserveSpecularLighting;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1);
        SAMPLER(sampler_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1);
        TEXTURE2D(_Direction);
        SAMPLER(sampler_Direction);
        
            // -- Property used by ScenePickingPass
            #ifdef SCENEPICKINGPASS
            float4 _SelectionID;
            #endif
        
            // -- Properties used by SceneSelectionPass
            #ifdef SCENESELECTIONPASS
            int _ObjectId;
            int _PassValue;
            #endif
        
            // Includes
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingMacros.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracing.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Material.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/ShaderVariablesRaytracingLightLoop.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RaytracingIntersection.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/Unlit/Unlit.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/BuiltinUtilities.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/MaterialUtilities.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/Raytracing/Shaders/RayTracingCommon.hlsl"
        #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderGraphFunctions.hlsl"
        	// GraphIncludes: <None>
        
            // --------------------------------------------------
            // Structs and Packing
        
            struct SurfaceDescriptionInputs
        {
             float4 uv0;
             float4 uv1;
             float4 uv2;
             float4 uv3;
        };
        
            //Interpolator Packs: <None>
        
            // --------------------------------------------------
            // Graph
        
        
            // Graph Functions
            
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        void Unity_Divide_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A / B;
        }
        
        void Unity_Fraction_float2(float2 In, out float2 Out)
        {
            Out = frac(In);
        }
        
        void Unity_Multiply_float2_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A * B;
        }
        
        void Unity_Subtract_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A - B;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_Comparison_Greater_float(float A, float B, out float Out)
        {
            Out = A > B ? 1 : 0;
        }
        
        void Unity_Divide_float(float A, float B, out float Out)
        {
            Out = A / B;
        }
        
        void Unity_Arctangent_float(float In, out float Out)
        {
            Out = atan(In);
        }
        
        void Unity_Add_float(float A, float B, out float Out)
        {
            Out = A + B;
        }
        
        void Unity_Branch_float(float Predicate, float True, float False, out float Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Rotate_Radians_float(float2 UV, float2 Center, float Rotation, out float2 Out)
        {
            //rotation matrix
            UV -= Center;
            float s = sin(Rotation);
            float c = cos(Rotation);
        
            //center rotation matrix
            float2x2 rMatrix = float2x2(c, -s, s, c);
            rMatrix *= 0.5;
            rMatrix += 0.5;
            rMatrix = rMatrix*2 - 1;
        
            //multiply the UVs by the rotation matrix
            UV.xy = mul(UV.xy, rMatrix);
            UV += Center;
        
            Out = UV;
        }
        
        void Unity_Distance_float2(float2 A, float2 B, out float Out)
        {
            Out = distance(A, B);
        }
        
        void Unity_Clamp_float(float In, float Min, float Max, out float Out)
        {
            Out = clamp(In, Min, Max);
        }
        
        void Unity_Subtract_float(float A, float B, out float Out)
        {
            Out = A - B;
        }
        
        void Unity_Absolute_float(float In, out float Out)
        {
            Out = abs(In);
        }
        
        void Unity_Power_float(float A, float B, out float Out)
        {
            Out = pow(A, B);
        }
        
        void Unity_Lerp_float3(float3 A, float3 B, float3 T, out float3 Out)
        {
            Out = lerp(A, B, T);
        }
        
        void Unity_Branch_float3(float Predicate, float3 True, float3 False, out float3 Out)
        {
            Out = Predicate ? True : False;
        }
        
        void Unity_Multiply_float3_float3(float3 A, float3 B, out float3 Out)
        {
            Out = A * B;
        }
        
            // Graph Vertex
            // GraphVertex: <None>
        
            // Graph Pixel
            struct SurfaceDescription
        {
            float3 BaseColor;
            float3 Emission;
            float Alpha;
            float AlphaClipThreshold;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0 = UnityBuildTexture2DStructNoScale(_Direction);
            float4 _UV_7c8a457b34527181924b2b4a71350de9_Out_0 = IN.uv0;
            float2 _TilingAndOffset_67d58b7a8092828aaece7f3acfe6e8a9_Out_3;
            Unity_TilingAndOffset_float((_UV_7c8a457b34527181924b2b4a71350de9_Out_0.xy), float2 (2, 2), float2 (0, 0), _TilingAndOffset_67d58b7a8092828aaece7f3acfe6e8a9_Out_3);
            float2 _Vector2_9a3e446bd0dfc3868e3db6f9bf101ead_Out_0 = float2(1, 1);
            float2 _Vector2_8355e4dbd796b188ba1e500dcabf0c0f_Out_0 = float2(30, 30);
            float2 _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2;
            Unity_Divide_float2(_Vector2_9a3e446bd0dfc3868e3db6f9bf101ead_Out_0, _Vector2_8355e4dbd796b188ba1e500dcabf0c0f_Out_0, _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2);
            float2 _Divide_5d0f49ab4bcab689894f976217114a79_Out_2;
            Unity_Divide_float2(_TilingAndOffset_67d58b7a8092828aaece7f3acfe6e8a9_Out_3, _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2, _Divide_5d0f49ab4bcab689894f976217114a79_Out_2);
            float2 _Fraction_e4f0f05fb76fd3809f5ec855b54a4595_Out_1;
            Unity_Fraction_float2(_Divide_5d0f49ab4bcab689894f976217114a79_Out_2, _Fraction_e4f0f05fb76fd3809f5ec855b54a4595_Out_1);
            float2 _Multiply_cbcb0de832b1d08288916e46aa8d4df2_Out_2;
            Unity_Multiply_float2_float2(_Fraction_e4f0f05fb76fd3809f5ec855b54a4595_Out_1, _Divide_f0d0aaa28637b781bcd99c1c912b5eae_Out_2, _Multiply_cbcb0de832b1d08288916e46aa8d4df2_Out_2);
            float2 _Vector2_e6d2595367ed0380b42c7a74855838a5_Out_0 = float2(246, 246);
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_Width_0 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.z;
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_Height_2 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.w;
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_TexelWidth_3 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.x;
            float _TexelSize_18bb9f416e386f87bee91aa46804bc56_TexelHeight_4 = UnityBuildTexture2DStructNoScale(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Texture_1).texelSize.y;
            float2 _Vector2_e8728bfbf194e5818f010927b4ba8d79_Out_0 = float2(_TexelSize_18bb9f416e386f87bee91aa46804bc56_Width_0, _TexelSize_18bb9f416e386f87bee91aa46804bc56_Height_2);
            float2 _Multiply_561a2890964f4081a8cf5d16bccb889f_Out_2;
            Unity_Multiply_float2_float2(_Vector2_e6d2595367ed0380b42c7a74855838a5_Out_0, _Vector2_e8728bfbf194e5818f010927b4ba8d79_Out_0, _Multiply_561a2890964f4081a8cf5d16bccb889f_Out_2);
            float2 _Vector2_97c81bcbfd26f08385a101ac3da412f3_Out_0 = float2(10, 10);
            float2 _Multiply_bccda05b3809138d8da15484dd6a7b4e_Out_2;
            Unity_Multiply_float2_float2(_Vector2_e8728bfbf194e5818f010927b4ba8d79_Out_0, _Vector2_97c81bcbfd26f08385a101ac3da412f3_Out_0, _Multiply_bccda05b3809138d8da15484dd6a7b4e_Out_2);
            float2 _Subtract_710d4672e66f7780ad90ca6e19a733bd_Out_2;
            Unity_Subtract_float2(_Multiply_561a2890964f4081a8cf5d16bccb889f_Out_2, _Multiply_bccda05b3809138d8da15484dd6a7b4e_Out_2, _Subtract_710d4672e66f7780ad90ca6e19a733bd_Out_2);
            float2 _Multiply_cd542324eceadd8daaca18ab78d3fbf6_Out_2;
            Unity_Multiply_float2_float2(_Multiply_cbcb0de832b1d08288916e46aa8d4df2_Out_2, _Subtract_710d4672e66f7780ad90ca6e19a733bd_Out_2, _Multiply_cd542324eceadd8daaca18ab78d3fbf6_Out_2);
            float2 _Vector2_df86361e9c36e7828e3097cc1fa8a75b_Out_0 = float2(0.1, 0.1);
            float2 _Multiply_8e351d40d36634829f2bb0905b9b9342_Out_2;
            Unity_Multiply_float2_float2(_Multiply_cd542324eceadd8daaca18ab78d3fbf6_Out_2, _Vector2_df86361e9c36e7828e3097cc1fa8a75b_Out_0, _Multiply_8e351d40d36634829f2bb0905b9b9342_Out_2);
            float4 _UV_1ca38e193be034888b8a707af9d47210_Out_0 = IN.uv3;
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_R_1 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[0];
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[1];
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_B_3 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[2];
            float _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_A_4 = _UV_1ca38e193be034888b8a707af9d47210_Out_0[3];
            float _Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2;
            Unity_Multiply_float_float(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2, -1, _Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2);
            float _Comparison_e67d9847aa86858490dd0c37e68e7505_Out_2;
            Unity_Comparison_Greater_float(_Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2, 0, _Comparison_e67d9847aa86858490dd0c37e68e7505_Out_2);
            float _Divide_2b884a92fd783d82a9012917c2b83115_Out_2;
            Unity_Divide_float(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_R_1, _Multiply_3bdf02c598430c878e3738cac7f0dae8_Out_2, _Divide_2b884a92fd783d82a9012917c2b83115_Out_2);
            float _Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1;
            Unity_Arctangent_float(_Divide_2b884a92fd783d82a9012917c2b83115_Out_2, _Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1);
            float _Add_cf6b7aee4a487f8ca16b9171b64a8f0d_Out_2;
            Unity_Add_float(_Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1, 3.141592, _Add_cf6b7aee4a487f8ca16b9171b64a8f0d_Out_2);
            float _Branch_9de100b31215ba89bb19e5b89012a25a_Out_3;
            Unity_Branch_float(_Comparison_e67d9847aa86858490dd0c37e68e7505_Out_2, _Arctangent_5140ab7fbdf95a8f9329bfd0cd09226c_Out_1, _Add_cf6b7aee4a487f8ca16b9171b64a8f0d_Out_2, _Branch_9de100b31215ba89bb19e5b89012a25a_Out_3);
            float2 _Rotate_ff1406ef744b188ca79c6d3bf0b8bbd7_Out_3;
            Unity_Rotate_Radians_float(_Multiply_8e351d40d36634829f2bb0905b9b9342_Out_2, float2 (0.5, 0.5), _Branch_9de100b31215ba89bb19e5b89012a25a_Out_3, _Rotate_ff1406ef744b188ca79c6d3bf0b8bbd7_Out_3);
            #if defined(SHADER_API_GLES) && (SHADER_TARGET < 30)
              float4 _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0 = float4(0.0f, 0.0f, 0.0f, 1.0f);
            #else
              float4 _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0 = SAMPLE_TEXTURE2D_LOD(_Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0.tex, _Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0.samplerstate, _Property_70a6ddd048a60f81b46e2042f9ae833b_Out_0.GetTransformedUV(_Rotate_ff1406ef744b188ca79c6d3bf0b8bbd7_Out_3), 0);
            #endif
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_R_5 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.r;
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_G_6 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.g;
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_B_7 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.b;
            float _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_A_8 = _SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_RGBA_0.a;
            float _Comparison_1a93f0ca7c913a8f81659b20b8d59f2b_Out_2;
            Unity_Comparison_Greater_float(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2, 0.001, _Comparison_1a93f0ca7c913a8f81659b20b8d59f2b_Out_2);
            float3 _Vector3_37ab63b41d52bb8d8eba9088ed71bc11_Out_0 = float3(0, 0, 0);
            float2 _Vector2_4aeecd1cb271dd8e96c2fb12309b9dd0_Out_0 = float2(_Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_R_1, _Split_11d6e1d03b6aec85a5cc295d4f2f2c3a_G_2);
            float _Distance_47e812ff6269a181b6e0faa017faee3b_Out_2;
            Unity_Distance_float2(_Vector2_4aeecd1cb271dd8e96c2fb12309b9dd0_Out_0, float2(0, 0), _Distance_47e812ff6269a181b6e0faa017faee3b_Out_2);
            float _Multiply_e5102f48c5e759879293e06062894e0c_Out_2;
            Unity_Multiply_float_float(_Distance_47e812ff6269a181b6e0faa017faee3b_Out_2, 1.6, _Multiply_e5102f48c5e759879293e06062894e0c_Out_2);
            float _Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3;
            Unity_Clamp_float(_Multiply_e5102f48c5e759879293e06062894e0c_Out_2, 0, 1, _Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3);
            float _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2;
            Unity_Subtract_float(1, _Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3, _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2);
            float3 _Vector3_aeff4a868e81c68f85dfe4c9662da9ce_Out_0 = float3(1, _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2, 0);
            float _Absolute_5573b6b2c1616e819aa4eeb67caec457_Out_1;
            Unity_Absolute_float(_Clamp_ec36a3c091f7148e8b7ca54cb1273530_Out_3, _Absolute_5573b6b2c1616e819aa4eeb67caec457_Out_1);
            float _Power_33dedbf009cacf848c403a58667a66c6_Out_2;
            Unity_Power_float(_Absolute_5573b6b2c1616e819aa4eeb67caec457_Out_1, 0.16, _Power_33dedbf009cacf848c403a58667a66c6_Out_2);
            float _Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3;
            Unity_Clamp_float(_Power_33dedbf009cacf848c403a58667a66c6_Out_2, 0, 1, _Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3);
            float3 _Lerp_e6000a54e83a8f8e8c5bc16419b360ca_Out_3;
            Unity_Lerp_float3(_Vector3_37ab63b41d52bb8d8eba9088ed71bc11_Out_0, _Vector3_aeff4a868e81c68f85dfe4c9662da9ce_Out_0, (_Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3.xxx), _Lerp_e6000a54e83a8f8e8c5bc16419b360ca_Out_3);
            float3 _Vector3_484f28086891e98d9589d1f65774f6f4_Out_0 = float3(0, _Subtract_2f8c058d66260b84866bd56a0469ea11_Out_2, 1);
            float3 _Lerp_93469a90f98ccf80a7121c0b2bf05f48_Out_3;
            Unity_Lerp_float3(_Vector3_37ab63b41d52bb8d8eba9088ed71bc11_Out_0, _Vector3_484f28086891e98d9589d1f65774f6f4_Out_0, (_Clamp_062fed397e52b48f8a7809d95fc105e8_Out_3.xxx), _Lerp_93469a90f98ccf80a7121c0b2bf05f48_Out_3);
            float3 _Branch_365bf259ae64c386a18e2d6997727100_Out_3;
            Unity_Branch_float3(_Comparison_1a93f0ca7c913a8f81659b20b8d59f2b_Out_2, _Lerp_e6000a54e83a8f8e8c5bc16419b360ca_Out_3, _Lerp_93469a90f98ccf80a7121c0b2bf05f48_Out_3, _Branch_365bf259ae64c386a18e2d6997727100_Out_3);
            float3 _Multiply_0563147e8bc9868b8c2a4f2c442c349f_Out_2;
            Unity_Multiply_float3_float3((_SampleTexture2DLOD_7fdbdd0c5b377086acfc5974a225b0dd_A_8.xxx), _Branch_365bf259ae64c386a18e2d6997727100_Out_3, _Multiply_0563147e8bc9868b8c2a4f2c442c349f_Out_2);
            surface.BaseColor = _Multiply_0563147e8bc9868b8c2a4f2c442c349f_Out_2;
            surface.Emission = float3(0, 0, 0);
            surface.Alpha = 1;
            surface.AlphaClipThreshold = 0.5;
            return surface;
        }
        
            // --------------------------------------------------
            // Build Graph Inputs
            #ifdef HAVE_VFX_MODIFICATION
            #define VFX_SRP_ATTRIBUTES AttributesMesh
            #define VaryingsMeshType VaryingsMeshToPS
            #define VFX_SRP_VARYINGS VaryingsMeshType
            #define VFX_SRP_SURFACE_INPUTS FragInputs
            #endif
            SurfaceDescriptionInputs FragInputsToSurfaceDescriptionInputs(FragInputs input, float3 viewWS)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
            #if defined(SHADER_STAGE_RAY_TRACING)
            #else
            #endif
        
        #if UNITY_UV_STARTS_AT_TOP
        #else
        #endif
        
        
            output.uv0 =                                        input.texCoord0;
            output.uv1 =                                        input.texCoord1;
            output.uv2 =                                        input.texCoord2;
            output.uv3 =                                        input.texCoord3;
        
            // splice point to copy frag inputs custom interpolator pack into the SDI
            /* WARNING: $splice Could not find named fragment 'CustomInterpolatorCopyToSDI' */
        
            return output;
        }
        
            // --------------------------------------------------
            // Build Surface Data (Specific Material)
        
        void BuildSurfaceData(FragInputs fragInputs, inout SurfaceDescription surfaceDescription, float3 V, PositionInputs posInput, out SurfaceData surfaceData)
        {
            // setup defaults -- these are used if the graph doesn't output a value
            ZERO_INITIALIZE(SurfaceData, surfaceData);
        
            // copy across graph values, if defined
            surfaceData.color = surfaceDescription.BaseColor;
        
            #ifdef WRITE_NORMAL_BUFFER
            // When we need to export the normal (in the depth prepass, we write the geometry one)
            surfaceData.normalWS = fragInputs.tangentToWorld[2];
            #endif
        
            #if defined(DEBUG_DISPLAY)
            if (_DebugMipMapMode != DEBUGMIPMAPMODE_NONE)
            {
                // TODO
            }
            #endif
        
            #ifdef _ENABLE_SHADOW_MATTE
        
                #if (SHADERPASS == SHADERPASS_FORWARD_UNLIT) || (SHADERPASS == SHADERPASS_RAYTRACING_GBUFFER) || (SHADERPASS == SHADERPASS_RAYTRACING_INDIRECT) || (SHADERPASS == SHADERPASS_RAYTRACING_FORWARD)
        
                    HDShadowContext shadowContext = InitShadowContext();
        
                    // Evaluate the shadow, the normal is guaranteed if shadow matte is enabled on this shader.
                    float3 shadow3;
                    ShadowLoopMin(shadowContext, posInput, normalize(fragInputs.tangentToWorld[2]), asuint(_ShadowMatteFilter), GetMeshRenderingLightLayer(), shadow3);
        
                    // Compute the average value in the fourth channel
                    float4 shadow = float4(shadow3, dot(shadow3, float3(1.0/3.0, 1.0/3.0, 1.0/3.0)));
        
                    float4 shadowColor = (1.0 - shadow) * surfaceDescription.ShadowTint.rgba;
                    float  localAlpha  = saturate(shadowColor.a + surfaceDescription.Alpha);
        
                    // Keep the nested lerp
                    // With no Color (bsdfData.color.rgb, bsdfData.color.a == 0.0f), just use ShadowColor*Color to avoid a ring of "white" around the shadow
                    // And mix color to consider the Color & ShadowColor alpha (from texture or/and color picker)
                    #ifdef _SURFACE_TYPE_TRANSPARENT
                        surfaceData.color = lerp(shadowColor.rgb * surfaceData.color, lerp(lerp(shadowColor.rgb, surfaceData.color, 1.0 - surfaceDescription.ShadowTint.a), surfaceData.color, shadow.rgb), surfaceDescription.Alpha);
                    #else
                        surfaceData.color = lerp(lerp(shadowColor.rgb, surfaceData.color, 1.0 - surfaceDescription.ShadowTint.a), surfaceData.color, shadow.rgb);
                    #endif
                    localAlpha = ApplyBlendMode(surfaceData.color, localAlpha).a;
        
                    surfaceDescription.Alpha = localAlpha;
        
                #elif SHADERPASS == SHADERPASS_PATH_TRACING
        
                    surfaceData.normalWS = fragInputs.tangentToWorld[2];
                    surfaceData.shadowTint = surfaceDescription.ShadowTint.rgba;
        
                #endif
        
            #endif // _ENABLE_SHADOW_MATTE
        }
        
            // --------------------------------------------------
            // Get Surface And BuiltinData
        
            void GetSurfaceAndBuiltinData(FragInputs fragInputs, float3 V, inout PositionInputs posInput, out SurfaceData surfaceData, out BuiltinData builtinData RAY_TRACING_OPTIONAL_PARAMETERS)
            {
                // Don't dither if displaced tessellation (we're fading out the displacement instead to match the next LOD)
                #if !defined(SHADER_STAGE_RAY_TRACING) && !defined(_TESSELLATION_DISPLACEMENT)
                #ifdef LOD_FADE_CROSSFADE // enable dithering LOD transition if user select CrossFade transition in LOD group
                LODDitheringTransition(ComputeFadeMaskSeed(V, posInput.positionSS), unity_LODFade.x);
                #endif
                #endif
        
                #ifndef SHADER_UNLIT
                #ifdef _DOUBLESIDED_ON
                    float3 doubleSidedConstants = _DoubleSidedConstants.xyz;
                #else
                    float3 doubleSidedConstants = float3(1.0, 1.0, 1.0);
                #endif
        
                ApplyDoubleSidedFlipOrMirror(fragInputs, doubleSidedConstants); // Apply double sided flip on the vertex normal
                #endif // SHADER_UNLIT
        
                SurfaceDescriptionInputs surfaceDescriptionInputs = FragInputsToSurfaceDescriptionInputs(fragInputs, V);
        
                #if defined(HAVE_VFX_MODIFICATION)
                GraphProperties properties;
                ZERO_INITIALIZE(GraphProperties, properties);
        
                GetElementPixelProperties(fragInputs, properties);
        
                SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs, properties);
                #else
                SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);
                #endif
        
                // Perform alpha test very early to save performance (a killed pixel will not sample textures)
                // TODO: split graph evaluation to grab just alpha dependencies first? tricky..
                #ifdef _ALPHATEST_ON
                    float alphaCutoff = surfaceDescription.AlphaClipThreshold;
                    #if SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_PREPASS
                    // The TransparentDepthPrepass is also used with SSR transparent.
                    // If an artists enable transaprent SSR but not the TransparentDepthPrepass itself, then we use AlphaClipThreshold
                    // otherwise if TransparentDepthPrepass is enabled we use AlphaClipThresholdDepthPrepass
                    #elif SHADERPASS == SHADERPASS_TRANSPARENT_DEPTH_POSTPASS
                    // DepthPostpass always use its own alpha threshold
                    alphaCutoff = surfaceDescription.AlphaClipThresholdDepthPostpass;
                    #elif (SHADERPASS == SHADERPASS_SHADOWS) || (SHADERPASS == SHADERPASS_RAYTRACING_VISIBILITY)
                    // If use shadow threshold isn't enable we don't allow any test
                    #endif
        
                    GENERIC_ALPHA_TEST(surfaceDescription.Alpha, alphaCutoff);
                #endif
        
                #if !defined(SHADER_STAGE_RAY_TRACING) && _DEPTHOFFSET_ON
                ApplyDepthOffsetPositionInput(V, surfaceDescription.DepthOffset, GetViewForwardDir(), GetWorldToHClipMatrix(), posInput);
                #endif
        
                #ifndef SHADER_UNLIT
                float3 bentNormalWS;
                BuildSurfaceData(fragInputs, surfaceDescription, V, posInput, surfaceData, bentNormalWS);
        
                #ifdef FRAG_INPUTS_USE_TEXCOORD1
                    float4 lightmapTexCoord1 = fragInputs.texCoord1;
                #else
                    float4 lightmapTexCoord1 = float4(0,0,0,0);
                #endif
        
                #ifdef FRAG_INPUTS_USE_TEXCOORD2
                    float4 lightmapTexCoord2 = fragInputs.texCoord2;
                #else
                    float4 lightmapTexCoord2 = float4(0,0,0,0);
                #endif
        
                // Builtin Data
                // For back lighting we use the oposite vertex normal
                InitBuiltinData(posInput, surfaceDescription.Alpha, bentNormalWS, -fragInputs.tangentToWorld[2], lightmapTexCoord1, lightmapTexCoord2, builtinData);
        
                #else
                BuildSurfaceData(fragInputs, surfaceDescription, V, posInput, surfaceData);
        
                ZERO_BUILTIN_INITIALIZE(builtinData); // No call to InitBuiltinData as we don't have any lighting
                builtinData.opacity = surfaceDescription.Alpha;
        
                #if defined(DEBUG_DISPLAY)
                    // Light Layers are currently not used for the Unlit shader (because it is not lit)
                    // But Unlit objects do cast shadows according to their rendering layer mask, which is what we want to
                    // display in the light layers visualization mode, therefore we need the renderingLayers
                    builtinData.renderingLayers = GetMeshRenderingLightLayer();
                #endif
        
                #endif // SHADER_UNLIT
        
                #ifdef _ALPHATEST_ON
                    // Used for sharpening by alpha to mask - Alpha to covertage is only used with depth only and forward pass (no shadow pass, no transparent pass)
                    builtinData.alphaClipTreshold = alphaCutoff;
                #endif
        
                // override sampleBakedGI - not used by Unlit
        
                builtinData.emissiveColor = surfaceDescription.Emission;
        
                // Note this will not fully work on transparent surfaces (can check with _SURFACE_TYPE_TRANSPARENT define)
                // We will always overwrite vt feeback with the nearest. So behind transparent surfaces vt will not be resolved
                // This is a limitation of the current MRT approach.
                #ifdef UNITY_VIRTUAL_TEXTURING
                #endif
        
                #if _DEPTHOFFSET_ON
                builtinData.depthOffset = surfaceDescription.DepthOffset;
                #endif
        
                // TODO: We should generate distortion / distortionBlur for non distortion pass
                #if (SHADERPASS == SHADERPASS_DISTORTION)
                builtinData.distortion = surfaceDescription.Distortion;
                builtinData.distortionBlur = surfaceDescription.DistortionBlur;
                #endif
        
                #ifndef SHADER_UNLIT
                // PostInitBuiltinData call ApplyDebugToBuiltinData
                PostInitBuiltinData(V, posInput, surfaceData, builtinData);
                #else
                ApplyDebugToBuiltinData(builtinData);
                #endif
        
                RAY_TRACING_OPTIONAL_ALPHA_TEST_PASS
            }
        
            // --------------------------------------------------
            // Main
        
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/ShaderPassPathTracing.hlsl"
        
            // --------------------------------------------------
            // Visual Effect Vertex Invocations
        
        	#ifdef HAVE_VFX_MODIFICATION
        	#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/VisualEffectVertex.hlsl"
        	#endif
        
            ENDHLSL
        }
    }
    CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
    CustomEditorForRenderPipeline "Rendering.HighDefinition.HDUnlitGUI" "UnityEngine.Rendering.HighDefinition.HDRenderPipelineAsset"
    FallBack "Hidden/Shader Graph/FallbackError"
}