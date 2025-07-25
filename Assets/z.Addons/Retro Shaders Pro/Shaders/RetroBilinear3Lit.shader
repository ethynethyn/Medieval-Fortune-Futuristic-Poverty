Shader "Retro Shaders Pro/Retro Bilinear3 Lit"
{
    Properties
    {
		[MainColor] [HDR] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
		[MainTexture] _BaseMap("Base Texture", 2D) = "white" {}
		_ResolutionLimit("Resolution Limit (Power of 2)", Integer) = 64
		_SnapsPerUnit("Snapping Points per Meter", Integer) = 64
		_ColorBitDepth("Bit Depth", Integer) = 64
		_ColorBitDepthOffset("Bit Depth Offset", Range(0.0, 1.0)) = 0.0
		_AmbientLight("Ambient Light Strength", Range(0.0, 1.0)) = 0.02
		_AffineTextureStrength("Affine Texture Strength", Range(0.0, 1.0)) = 1.0
		[Toggle] _USE_AMBIENT_OVERRIDE("Ambient Light Override", Float) = 1
		[Toggle] _USE_DITHERING("Use Dithering", Float) = 0

		[ToggleUI] _AlphaClip("Alpha Clip", Float) = 0.0
		[HideInInspector] _Cutoff("Alpha Clip Threshold", Range(0.0, 1.0)) = 0.5
		[HideInInspector] _SrcBlend("__src", Float) = 1.0
		[HideInInspector] _DstBlend("__dst", Float) = 0.0
		[HideInInspector] _ZWrite("__zw", Float) = 1.0
		[HideInInspector] _Cull("_Cull", Float) = 2.0
		[HideInInspector] _Surface("_Surface", Float) = 0.0
    }
    SubShader
    {
		Tags
		{
			"RenderType" = "Opaque"
			"Queue" = "Geometry"
			"RenderPipeline" = "UniversalPipeline"
		}

		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"
		#include "RetroSurfaceInput.hlsl"

		#define EPSILON 1e-06

		float3 dither(float3 col, float2 uv)
		{
			static float DITHER_THRESHOLDS[16] =
			{
				1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
				13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
				4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
				16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
			};
			uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;

			return col - DITHER_THRESHOLDS[index];
		}
		ENDHLSL

        Pass
        {
			Tags
			{
				"LightMode" = "UniversalForward"
			}

			Blend[_SrcBlend][_DstBlend]
			ZWrite[_ZWrite]
			Cull[_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fog
			#pragma multi_compile_instancing
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
			#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
			#pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
			#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
			#pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
			#pragma multi_compile_fragment _ _SHADOWS_SOFT
			#pragma multi_compile _ _FORWARD_PLUS
			#pragma multi_compile_fragment _ _LIGHT_LAYERS
			#pragma multi_compile_fragment _ _LIGHT_COOKIES

			#pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
			#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
			#pragma multi_compile _ SHADOWS_SHADOWMASK
			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
			#pragma multi_compile _ LIGHTMAP_ON
			#pragma multi_compile _ DYNAMICLIGHTMAP_ON

			#pragma shader_feature_local_fragment _ALPHATEST_ON
			#pragma shader_feature_local _USE_AMBIENT_OVERRIDE
			#pragma shader_feature_local _USE_DITHERING

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct appdata
            {
				float4 positionOS : POSITION;
				float4 color : COLOR;
				float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
				float2 staticLightmapUV : TEXCOORD1;
				float2 dynamicLightmapUV : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
				float4 positionCS : SV_POSITION;
				float4 color : COLOR;
                float2 uv : TEXCOORD0;
				float3 affineUV : TEXCOORD1;
				float fog : TEXCOORD2;
				float3 normalWS : TEXCOORD3;
				float3 positionWS : TEXCOORD4;
				float3 viewWS : TEXCOORD5;
				DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 6);
				float2 dynamicLightmapUV : TEXCOORD7;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
            };

			v2f vert(appdata v)
			{
				v2f o = (v2f)0;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

				float4 positionVS = mul(UNITY_MATRIX_MV, v.positionOS);
				positionVS = floor(positionVS * _SnapsPerUnit) / _SnapsPerUnit;
				o.positionCS = mul(UNITY_MATRIX_P, positionVS);

				o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
				o.affineUV = float3(TRANSFORM_TEX(v.uv, _BaseMap) * o.positionCS.w, o.positionCS.w);
				o.fog = ComputeFogFactor(o.positionCS.z);
				o.normalWS = TransformObjectToWorldNormal(v.normalOS);
				o.positionWS = mul(UNITY_MATRIX_M, v.positionOS);
				o.viewWS = GetWorldSpaceNormalizeViewDir(o.positionWS);
				OUTPUT_SH(o.normalWS, o.vertexSH);
				OUTPUT_LIGHTMAP_UV(v.staticLightmapUV, unity_LightmapST, o.staticLightmapUV);
				o.dynamicLightmapUV = v.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
				o.color = v.color;

				return o;
			}

			float4 frag(v2f i, float facing : VFACE) : SV_TARGET
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				// Apply resolution limit to the base texture.
				int targetResolution = (int)log2(_ResolutionLimit);
				int actualResolution = (int)log2(_BaseMap_TexelSize.zw);
				int lod = actualResolution - targetResolution;

				float2 uv = lerp(i.uv, i.affineUV.xy / i.affineUV.z, _AffineTextureStrength);

				// Calculate N64 3-point bilinear filtering.

				float modifier = pow(2.0f, lod);
				float4 targetTexelSize = float4(_BaseMap_TexelSize.xy * modifier, _BaseMap_TexelSize.zw / modifier);

				// With thanks to: https://www.emutalk.net/threads/emulating-nintendo-64-3-sample-bilinear-filtering-using-shaders.54215/
				float2 uvA = float2(targetTexelSize.x, 0.0f);
				float2 uvB = float2(0.0f, targetTexelSize.y);
				float2 uvC = float2(targetTexelSize.x, targetTexelSize.y);
				float2 uvHalf = uvC * 0.5f;
				float2 uvCenter = uv - uvHalf;

				float4 baseColorMain = SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_PointRepeat, uvCenter, lod);
				float4 baseColorA = SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_PointRepeat, uvCenter + uvA, lod);
				float4 baseColorB = SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_PointRepeat, uvCenter + uvB, lod);
				float4 baseColorC = SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_PointRepeat, uvCenter + uvC, lod);

				float interpX = modf(uvCenter.x * targetTexelSize.z, targetTexelSize.z);
				float interpY = modf(uvCenter.y * targetTexelSize.w, targetTexelSize.w);

				if(uvCenter.x < 0.0f)
				{
					interpX = 1.0f - (interpX * -1.0f);
				}

				if(uvCenter.y < 0.0f)
				{
					interpY = 1.0f - (interpY * -1.0f);
				}

				float4 baseColor = (baseColorMain + interpX * (baseColorA - baseColorMain) + interpY * (baseColorB - baseColorMain)) * (1.0f - step(1.0f, interpX + interpY));
				baseColor += (baseColorC + (1.0f - interpX) * (baseColorB - baseColorC) + (1.0f - interpY) * (baseColorA - baseColorC)) * step(1.0f, interpX + interpY);

				baseColor = _BaseColor * baseColor * i.color;

				// Clip pixels based on alpha.
#ifdef _ALPHATEST_ON
				clip(baseColor.a - _Cutoff);
#endif

				// Posterize the base color.
				float colorBitDepth = max(2, _ColorBitDepth);

				float r = max((baseColor.r - EPSILON) * colorBitDepth, 0.0f);
				float g = max((baseColor.g - EPSILON) * colorBitDepth, 0.0f);
				float b = max((baseColor.b - EPSILON) * colorBitDepth, 0.0f);

				float divisor = colorBitDepth - 1.0f;

				// Apply dithering between posterized colors.
#ifdef _USE_DITHERING
				float3 remainders = float3(frac(r), frac(g), frac(b));
				float3 ditheredColor = saturate(dither(remainders, uvCenter * _BaseMap_TexelSize.zw));
				ditheredColor = step(0.5f, ditheredColor);
#else
				float3 ditheredColor = 0.0f;
#endif

				float3 posterizedColor = float3(floor(r), floor(g), floor(b)) + ditheredColor;
				posterizedColor /= divisor;
				posterizedColor += 1.0f / colorBitDepth * _ColorBitDepthOffset;

				float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
				float4 shadowMask = SAMPLE_SHADOWMASK(i.dynamicLightmapUV);

				// Apply the main light.
				Light light = GetMainLight(shadowCoord);

				float3 normalDir = normalize(i.normalWS * facing);
				float lightAmount = saturate(dot(normalDir, light.direction) * light.distanceAttenuation * light.shadowAttenuation);
#ifndef _USE_AMBIENT_OVERRIDE
				float3 lightColor = lerp(SampleSH(normalDir), 1.0f, lightAmount) * light.color;
#else
				float3 lightColor = lerp(_AmbientLight, 1.0f, lightAmount) * light.color;
#endif

#if defined(DYNAMICLIGHTMAP_ON)
				float3 bakedGI = SAMPLE_GI(i.staticLightmapUV, i.dynamicLightmapUV, i.vertexSH, normalDir);
#else
				float3 bakedGI = SAMPLE_GI(i.staticLightmapUV, i.vertexSH, normalDir);
#endif

#ifdef _DBUFFER
                float3 specular = 0;
                float metallic = 0;
                float occlusion = 0;
                float smoothness = 0;
                float3 norm = normalDir;
                ApplyDecal(i.positionCS, posterizedColor, specular, norm, metallic, occlusion, smoothness);
#endif

#ifdef _ADDITIONAL_LIGHTS

				// Apply secondary lights.
				uint pixelLightCount = GetAdditionalLightsCount();

				InputData inputData = (InputData)0;
				inputData.positionWS = i.positionWS;
				inputData.normalWS = i.normalWS;
				inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(i.positionWS);
				inputData.shadowCoord = shadowCoord;
				inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);

#if USE_FORWARD_PLUS

				// Apply secondary lights (Forward+ rendering).
				for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++) 
				{
					FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK

					Light light = GetAdditionalLight(lightIndex, i.positionWS, shadowMask);

					float3 color = saturate(dot(light.direction, normalDir)) * light.color;
					color *= light.distanceAttenuation;
					color *= light.shadowAttenuation;

					lightColor += color;
				}
#endif

				// Apply secondary lights (Forward rendering).
				LIGHT_LOOP_BEGIN(pixelLightCount)
					Light light = GetAdditionalLight(lightIndex, i.positionWS, shadowMask);

					float3 color = saturate(dot(light.direction, normalDir)) * light.color;
					color *= light.distanceAttenuation;
					color *= light.shadowAttenuation;

					lightColor += color;
				LIGHT_LOOP_END
#endif

				lightColor += bakedGI;

				// Combine everything.
				float3 finalColor = posterizedColor * lightColor;
				finalColor = MixFog(finalColor, i.fog);

				return float4(finalColor, baseColor.a);
			}
            ENDHLSL
        }

		Pass
		{
			Name "ShadowCaster"

			Tags
			{
				"LightMode" = "ShadowCaster"
			}

			ZWrite On
			ZTest LEqual
			ColorMask 0
			Cull[_Cull]

			HLSLPROGRAM
			#pragma vertex shadowPassVert
			#pragma fragment shadowPassFrag

			#pragma multi_compile_instancing
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

			#pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
			#pragma shader_feature_local_fragment _ALPHATEST_ON
			#pragma shader_feature_local _USE_POINT_FILTER_ON

			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
			#include "RetroShadowCasterPass.hlsl"
			ENDHLSL
		}

		Pass
		{
			Name "GBuffer"

			Tags
			{
				"LightMode" = "UniversalGBuffer"
			}

			ZWrite[_ZWrite]
			ZTest LEqual
			Cull[_Cull]

			HLSLPROGRAM
			#pragma target 4.5
			#pragma exclude_renderers gles3 glcore

			#pragma vertex gBufferVert
			#pragma fragment gBufferFragBilinear

			#pragma multi_compile_instancing
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			#pragma multi_compile _ _ADDITIONAL_LIGHTS
			#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
			#pragma multi_compile_fragment _ _SHADOWS_SOFT
			#pragma multi_compile _ _FORWARD_PLUS
			#pragma multi_compile_fragment _ _LIGHT_COOKIES

			#pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
			#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
			#pragma multi_compile _ SHADOWS_SHADOWMASK
			#pragma multi_compile _ DIRLIGHTMAP_COMBINED
			#pragma multi_compile _ LIGHTMAP_ON
			#pragma multi_compile _ DYNAMICLIGHTMAP_ON

			#pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

			#pragma shader_feature_local_fragment _ALPHATEST_ON
			#pragma shader_feature_local _USE_DITHERING

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "RetroGBufferPass.hlsl"

			FragmentOutput gBufferFragBilinear(v2f i)
			{
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				// Apply resolution limit to the base texture.
				int targetResolution = (int)log2(_ResolutionLimit);
				int actualResolution = (int)log2(_BaseMap_TexelSize.zw);
				int lod = actualResolution - targetResolution;

				// Calculate N64 3-point bilinear filtering.

				float modifier = pow(2.0f, lod);
				float4 targetTexelSize = float4(_BaseMap_TexelSize.xy * modifier, _BaseMap_TexelSize.zw / modifier);

				// With thanks to: https://www.emutalk.net/threads/emulating-nintendo-64-3-sample-bilinear-filtering-using-shaders.54215/
				float2 uvA = float2(targetTexelSize.x, 0.0f);
				float2 uvB = float2(0.0f, targetTexelSize.y);
				float2 uvC = float2(targetTexelSize.x, targetTexelSize.y);
				float2 uvHalf = uvC * 0.5f;
				float2 uvCenter = i.uv - uvHalf;

				float4 baseColorMain = SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_PointRepeat, uvCenter, lod);
				float4 baseColorA = SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_PointRepeat, uvCenter + uvA, lod);
				float4 baseColorB = SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_PointRepeat, uvCenter + uvB, lod);
				float4 baseColorC = SAMPLE_TEXTURE2D_LOD(_BaseMap, sampler_PointRepeat, uvCenter + uvC, lod);

				float interpX = modf(uvCenter.x * targetTexelSize.z, targetTexelSize.z);
				float interpY = modf(uvCenter.y * targetTexelSize.w, targetTexelSize.w);

				if(uvCenter.x < 0.0f)
				{
					interpX = 1.0f - (interpX * -1.0f);
				}

				if(uvCenter.y < 0.0f)
				{
					interpY = 1.0f - (interpY * -1.0f);
				}

				float4 baseColor = (baseColorMain + interpX * (baseColorA - baseColorMain) + interpY * (baseColorB - baseColorMain)) * (1.0f - step(1.0f, interpX + interpY));
				baseColor += (baseColorC + (1.0f - interpX) * (baseColorB - baseColorC) + (1.0f - interpY) * (baseColorA - baseColorC)) * step(1.0f, interpX + interpY);

				baseColor = _BaseColor * baseColor * i.color;

				Alpha(baseColor.a, _BaseColor, _Cutoff);

				// Posterize the base color.
				float colorBitDepth = max(2, _ColorBitDepth);

				float r = max((baseColor.r - EPSILON) * colorBitDepth, 0.0f);
				float g = max((baseColor.g - EPSILON) * colorBitDepth, 0.0f);
				float b = max((baseColor.b - EPSILON) * colorBitDepth, 0.0f);

				float divisor = colorBitDepth - 1.0f;

				// Apply dithering between posterized colors.
#ifdef _USE_DITHERING
				float3 remainders = float3(frac(r), frac(g), frac(b));
				float3 ditheredColor = saturate(dither(remainders, uvCenter * _BaseMap_TexelSize.zw));
				ditheredColor = step(0.5f, ditheredColor);
#else
				float3 ditheredColor = 0.0f;
#endif

				float3 posterizedColor = float3(floor(r), floor(g), floor(b)) + ditheredColor;
				posterizedColor /= divisor;
				posterizedColor += 1.0f / colorBitDepth * _ColorBitDepthOffset;

#ifdef REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
				float4 shadowCoord = i.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
				float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
#else
				float4 shadowCoord = 0;
#endif

				InputData inputData = (InputData)0;
				inputData.positionCS = i.positionCS;
				inputData.positionWS = i.positionWS;
				inputData.normalWS = i.normalWS;
				inputData.viewDirectionWS = normalize(GetWorldSpaceViewDir(i.positionWS));
				inputData.shadowCoord = shadowCoord;
				inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(i.positionCS);
				inputData.shadowMask = SAMPLE_SHADOWMASK(i.staticLightmapUV);

#ifdef DYNAMICLIGHTMAP_ON
				inputData.bakedGI = SAMPLE_GI(i.staticLightmapUV, i.dynamicLightmapUV, i.vertexSH, normalWS);
#else
				inputData.bakedGI = SAMPLE_GI(i.staticLightmapUV, i.vertexSH, i.normalWS);
#endif

				SurfaceData surfaceData = (SurfaceData)0;
				surfaceData.albedo = posterizedColor.rgb;
				surfaceData.alpha = baseColor.a;
				surfaceData.emission = 0.0f;
				surfaceData.metallic = 0.0f;
				surfaceData.occlusion = 1.0f;
				surfaceData.smoothness = 0.0f;
				surfaceData.specular = 0.0f;
				//surfaceData.normalTS = normalTS;
	
				FragmentOutput output = GBuffer(inputData, surfaceData);
				return output;
			}

			ENDHLSL
		}

		Pass
		{
			Name "DepthOnly"

			Tags
			{
				"LightMode" = "DepthOnly"
			}

			ZWrite On
			ColorMask R
			Cull[_Cull]

			HLSLPROGRAM
			#pragma target 2.0
			#pragma vertex depthOnlyVert
			#pragma fragment depthOnlyFrag

			#pragma multi_compile_instancing
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

			#pragma shader_feature_local_fragment _ALPHATEST_ON

			#include "RetroDepthOnlyPass.hlsl"

			ENDHLSL
		}

		Pass
		{
			Name "DepthNormals"

			Tags
			{
				"LightMode" = "DepthNormals"
			}

			ZWrite On
			Cull[_Cull]

			HLSLPROGRAM
			#pragma target 2.0
			#pragma vertex depthNormalsVert
			#pragma fragment depthNormalsFrag

			#pragma multi_compile_instancing
			#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

			#pragma shader_feature_local_fragment _ALPHATEST_ON

			#include "RetroDepthNormalsPass.hlsl"
			ENDHLSL
		}

		Pass
		{
			Name "Meta"

			Tags
			{
				"LightMode" = "Meta"
			}

			Cull Off

			HLSLPROGRAM
			#pragma target 2.0
			#pragma vertex metaVert
			#pragma fragment metaFrag

			#pragma shader_feature_local_fragment _ALPHATEST_ON
			#pragma shader_feature_local _USE_DITHERING
			#pragma shader_feature EDITOR_VISUALIZATION

			#include "RetroMetaPass.hlsl"
			ENDHLSL
		}
    }

	CustomEditor "RetroShadersPro.URP.RetroLitShaderGUI"
}
