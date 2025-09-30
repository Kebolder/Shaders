// Jax Particles Shader
Shader "_Jax/Particles"
{
    Properties
    {
        [Enum(Opaque,0,Cutout,1,Fade,2,Additive,3,Transparent,4,Subtract,5)] _RenderingMode ("Rendering Mode", Int) = 2
        [Enum(Normal,0,Multiply,1,Overlay,2,Subtract,3,Additive,4,Color,5,Difference,6)] _ColorMode ("Color Mode", Int) = 0

        [HideInInspector] _SrcBlend ("Source Blend", Int) = 5
        [HideInInspector] _DstBlend ("Destination Blend", Int) = 10
        [HideInInspector] _ZWrite ("Z Write", Int) = 0

        _MainTex ("Particle Texture", 2D) = "white" {}
        _TintColor ("Tint Color", Color) = (1,1,1,1)
        _Alpha ("Alpha", Range(0,1)) = 1.0
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5

        [Toggle] _UseEmission ("Use Emission", Float) = 0
        _EmissionMap ("Emission Map", 2D) = "black" {}
        _EmissionMask ("Emission Mask", 2D) = "white" {}
        [HDR] _EmissionColor ("Emission Color", Color) = (1,1,1,1)
        _EmissionStrength ("Emission Strength", Range(0,20)) = 1.0
        [Toggle] _EmissionBaseColorAsMap ("Use Base Colors", Float) = 0
        [Toggle] _EmissionReplaceBase ("Override Base Color", Float) = 0

        [Toggle] _UseAudioLink ("Use AudioLink", Float) = 0
        _AudioLinkSmoothing ("Smoothing", Range(0,5)) = 0.0
        [Toggle] _AudioLinkStrength ("Enable Strength Modulation", Float) = 1
        [Enum(Bass,0,Low Mid,1,High Mid,2,Treble,3,Volume,4)] _AudioBandStrength ("Audio Band (Strength)", Int) = 0
        _AudioMultMin ("Emission Min Strength", Range(0,50)) = 1.0
        _AudioMultMax ("Emission Max Strength", Range(0,50)) = 5.0
        [Toggle] _AudioLinkColorShift ("Enable Color Shifting", Float) = 0
        [Enum(Bass,0,Low Mid,1,High Mid,2,Treble,3,Volume,4)] _AudioBandColor ("Audio Band (Color)", Int) = 0
        _AudioLinkColorLow ("Color (Silent)", Color) = (1,0,0,1)
        _AudioLinkColorMid ("Color (Mid)", Color) = (0,1,0,1)
        _AudioLinkColorHigh ("Color (Peak)", Color) = (0,0,1,1)
        [Toggle] _AudioLinkSize ("Enable Size Modulation", Float) = 0
        [Enum(Bass,0,Low Mid,1,High Mid,2,Treble,3,Volume,4)] _AudioBandSize ("Audio Band (Size)", Int) = 0
        _AudioSizeMin ("Min Size", Range(0,5)) = 1.0
        _AudioSizeMax ("Max Size", Range(0,5)) = 1.0

        [Toggle] _UseDissolve ("Use Dissolve Effect", Float) = 0
        _DissolveTexture ("Dissolve Noise Texture", 2D) = "white" {}
        _DissolveAmount ("Dissolve Amount", Range(0,1)) = 0.0
        _DissolveEdgeWidth ("Dissolve Edge Width", Range(0,0.2)) = 0.05
        _DissolveEdgeColor ("Dissolve Edge Color", Color) = (1,0.5,0,1)
        _DissolveScale ("Dissolve Scale", Float) = 1.0

        [Toggle] _UseEdgeFade ("Use Edge Fade", Float) = 0
        _EdgeFadeDistance ("Edge Fade Distance", Range(0,0.5)) = 0.1

        [Toggle] _UseDistortion ("Use Distortion", Float) = 0
        _DistortionMap ("Distortion Normal Map", 2D) = "bump" {}
        _DistortionStrength ("Distortion Strength", Range(0,1)) = 0.1
        _DistortionScrollX ("Distortion Scroll X", Float) = 0.0
        _DistortionScrollY ("Distortion Scroll Y", Float) = 0.0
        _DistortionScale ("Distortion UV Scale", Float) = 1.0

        _InvFade ("Soft Particles Factor", Range(0.01,3.0)) = 1.0
        [Toggle] _DualSided ("Dual Sided Rendering", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        LOD 200

        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_particles
            #pragma multi_compile_instancing
            #pragma shader_feature AUDIOLINK
            #include "UnityCG.cginc"

            #if defined(AUDIOLINK)
                #include "Packages/com.llealloo.audiolink/Runtime/Shaders/AudioLink.cginc"
            #endif

            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
            float4 _CameraDepthTexture_ST;

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color : COLOR;
                float4 texcoord1 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color : COLOR;
                float4 screenPos : TEXCOORD1;
                UNITY_FOG_COORDS(2)
                #ifdef SOFTPARTICLES_ON
                float4 projPos : TEXCOORD3;
                #endif
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _TintColor;

            float _UseEmission;
            sampler2D _EmissionMap;
            float4 _EmissionMap_ST;
            sampler2D _EmissionMask;
            float4 _EmissionMask_ST;
            half4 _EmissionColor;
            half _EmissionStrength;
            float _EmissionBaseColorAsMap;
            float _EmissionReplaceBase;
            float _InvFade;
            float _Cutoff;
            float _Alpha;

            float _UseDissolve;
            UNITY_DECLARE_TEX2D(_DissolveTexture);
            float4 _DissolveTexture_ST;
            float _DissolveAmount;
            float _DissolveEdgeWidth;
            fixed4 _DissolveEdgeColor;
            float _DissolveScale;

            float _UseEdgeFade;
            float _EdgeFadeDistance;

            float _UseDistortion;
            sampler2D _DistortionMap;
            float4 _DistortionMap_ST;
            float _DistortionStrength;
            float _DistortionScrollX;
            float _DistortionScrollY;
            float _DistortionScale;

            float _UseAudioLink;
            float _AudioLinkSmoothing;
            int _AudioBandStrength;
            int _AudioBandColor;
            int _AudioBandSize;
            float _AudioLinkStrength;
            float _AudioMultMin;
            float _AudioMultMax;
            float _AudioLinkColorShift;
            half4 _AudioLinkColorLow;
            half4 _AudioLinkColorMid;
            half4 _AudioLinkColorHigh;
            float _AudioLinkSize;
            float _AudioSizeMin;
            float _AudioSizeMax;

            int _RenderingMode;
            int _ColorMode;

            #if defined(AUDIOLINK)
            float GetAudioReactiveValue(int band)
            {
                if (_UseAudioLink < 0.5 || !AudioLinkIsAvailable())
                    return 0.0;

                float audioValue = 0.0;

                // Get audio value - use progressively smoother channels as smoothing increases
                if (_AudioLinkSmoothing < 0.01)
                {
                    // No smoothing - use raw value (.r)
                    if (band == 0)
                        audioValue = AudioLinkData(ALPASS_AUDIOBASS).r;
                    else if (band == 1)
                        audioValue = AudioLinkData(ALPASS_AUDIOLOWMIDS).r;
                    else if (band == 2)
                        audioValue = AudioLinkData(ALPASS_AUDIOHIGHMIDS).r;
                    else if (band == 3)
                        audioValue = AudioLinkData(ALPASS_AUDIOTREBLE).r;
                    else if (band == 4)
                        audioValue = (AudioLinkData(ALPASS_AUDIOBASS).r +
                                     AudioLinkData(ALPASS_AUDIOLOWMIDS).r +
                                     AudioLinkData(ALPASS_AUDIOHIGHMIDS).r +
                                     AudioLinkData(ALPASS_AUDIOTREBLE).r) * 0.25;
                }
                else
                {
                    // Use smoothed value (.g) and apply additional smoothing via pow function
                    if (band == 0)
                        audioValue = AudioLinkData(ALPASS_AUDIOBASS).g;
                    else if (band == 1)
                        audioValue = AudioLinkData(ALPASS_AUDIOLOWMIDS).g;
                    else if (band == 2)
                        audioValue = AudioLinkData(ALPASS_AUDIOHIGHMIDS).g;
                    else if (band == 3)
                        audioValue = AudioLinkData(ALPASS_AUDIOTREBLE).g;
                    else if (band == 4)
                        audioValue = (AudioLinkData(ALPASS_AUDIOBASS).g +
                                     AudioLinkData(ALPASS_AUDIOLOWMIDS).g +
                                     AudioLinkData(ALPASS_AUDIOHIGHMIDS).g +
                                     AudioLinkData(ALPASS_AUDIOTREBLE).g) * 0.25;

                    // Apply exponential smoothing curve - higher values create more damping
                    // This makes the value approach changes more slowly
                    float smoothPower = 1.0 + (_AudioLinkSmoothing * 0.4); // Map 0-5 to 1.0-3.0
                    audioValue = pow(audioValue, smoothPower);
                }

                return audioValue;
            }
            #endif

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float4 vertex = v.vertex;

                // Apply AudioLink size modulation
                #if defined(AUDIOLINK)
                if (_UseAudioLink > 0.5 && _AudioLinkSize > 0.5 && AudioLinkIsAvailable())
                {
                    float audioValue = GetAudioReactiveValue(_AudioBandSize);
                    float sizeMultiplier = lerp(_AudioSizeMin, _AudioSizeMax, audioValue);
                    vertex.xyz *= sizeMultiplier;
                }
                #endif

                o.vertex = UnityObjectToClipPos(vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _TintColor;
                o.screenPos = ComputeScreenPos(o.vertex);

                UNITY_TRANSFER_FOG(o,o.vertex);

                #ifdef SOFTPARTICLES_ON
                o.projPos = o.screenPos;
                COMPUTE_EYEDEPTH(o.projPos.z);
                #endif

                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                // Distortion
                float2 mainUV = i.texcoord;
                if (_UseDistortion > 0.5)
                {
                    float2 distortionUV = i.texcoord * _DistortionScale;
                    distortionUV += float2(_DistortionScrollX, _DistortionScrollY) * _Time.y;
                    float3 distortionNormal = UnpackNormal(tex2D(_DistortionMap, distortionUV));
                    float2 distortionOffset = distortionNormal.xy * _DistortionStrength * 0.1;
                    mainUV += distortionOffset;
                }

                half4 col = tex2D(_MainTex, mainUV) * i.color;

                // Emission
                half3 emission = half3(0,0,0);
                if (_UseEmission > 0.5)
                {
                    half4 emissionMap = tex2D(_EmissionMap, mainUV);
                    half4 emissionMask = tex2D(_EmissionMask, mainUV);
                    half3 emissionMapContrib = lerp(half3(1,1,1), emissionMap.rgb, step(0.01, dot(emissionMap.rgb, half3(1,1,1))));
                    half3 baseColorTint = lerp(half3(1,1,1), col.rgb, _EmissionBaseColorAsMap);

                    // Determine emission strength
                    float emissionStrength = _EmissionStrength;
                    #if defined(AUDIOLINK)
                    if (_UseAudioLink > 0.5 && _AudioLinkStrength > 0.5 && AudioLinkIsAvailable())
                    {
                        // Override emission strength with AudioLink value
                        float audioValue = GetAudioReactiveValue(_AudioBandStrength);
                        emissionStrength = lerp(_AudioMultMin, _AudioMultMax, audioValue);
                    }
                    #endif

                    emission = emissionMapContrib * emissionMask.rgb * baseColorTint * _EmissionColor.rgb * emissionStrength;

                    #if defined(AUDIOLINK)
                    // Apply color shifting if enabled
                    if (_UseAudioLink > 0.5 && _AudioLinkColorShift > 0.5 && AudioLinkIsAvailable())
                    {
                        float audioValue = GetAudioReactiveValue(_AudioBandColor);
                        half3 gradientColor;
                        if (audioValue < 0.5)
                        {
                            gradientColor = lerp(_AudioLinkColorLow.rgb, _AudioLinkColorMid.rgb, audioValue * 2.0);
                        }
                        else
                        {
                            gradientColor = lerp(_AudioLinkColorMid.rgb, _AudioLinkColorHigh.rgb, (audioValue - 0.5) * 2.0);
                        }
                        emission = gradientColor * emissionStrength;
                    }
                    #endif
                    emission = max(emission, 0.0);
                }
                if (_EmissionReplaceBase > 0.5)
                {
                    col.rgb = lerp(col.rgb, emission, saturate(length(emission)));
                }
                else
                {
                    col.rgb += emission;
                }
                col.a *= _Alpha;

                // Edge Fade
                if (_UseEdgeFade > 0.5)
                {
                    float2 screenUV = i.screenPos.xy / i.screenPos.w;
                    float2 edgeDist = min(screenUV, 1.0 - screenUV);
                    float edgeFactor = min(edgeDist.x, edgeDist.y);
                    float edgeFade = saturate(edgeFactor / _EdgeFadeDistance);
                    col.a *= edgeFade;
                }

                // Dissolve
                if (_UseDissolve > 0.5)
                {
                    float2 dissolveUV = i.texcoord * _DissolveScale;
                    float noise = UNITY_SAMPLE_TEX2D(_DissolveTexture, dissolveUV).r;
                    float dissolveEdge = _DissolveAmount + _DissolveEdgeWidth;

                    if (noise < _DissolveAmount)
                    {
                        discard;
                    }

                    if (noise < dissolveEdge)
                    {
                        float edgeBlend = (dissolveEdge - noise) / _DissolveEdgeWidth;
                        col.rgb = lerp(col.rgb, _DissolveEdgeColor.rgb, edgeBlend * _DissolveEdgeColor.a);
                    }
                }

                clip(col.a - _Cutoff);

                #ifdef SOFTPARTICLES_ON
                float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
                float partZ = i.projPos.z;
                float fade = saturate(_InvFade * (sceneZ - partZ));
                col.a *= fade;
                #endif

                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }

        // Back face pass
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull Front

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_particles
            #pragma multi_compile_instancing
            #pragma shader_feature _DUALSIDED_ON
            #pragma shader_feature AUDIOLINK
            #include "UnityCG.cginc"

            // AudioLink support
            #if defined(AUDIOLINK)
                #include "Packages/com.llealloo.audiolink/Runtime/Shaders/AudioLink.cginc"
            #endif

            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
            float4 _CameraDepthTexture_ST;

            struct appdata_t {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color : COLOR;
                float4 texcoord1 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color : COLOR;
                float4 screenPos : TEXCOORD1;
                UNITY_FOG_COORDS(2)
                #ifdef SOFTPARTICLES_ON
                float4 projPos : TEXCOORD3;
                #endif
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _TintColor;

            float _UseEmission;
            sampler2D _EmissionMap;
            float4 _EmissionMap_ST;
            sampler2D _EmissionMask;
            float4 _EmissionMask_ST;
            half4 _EmissionColor;
            half _EmissionStrength;
            float _EmissionBaseColorAsMap;
            float _EmissionReplaceBase;
            float _InvFade;
            float _Cutoff;
            float _Alpha;

            float _UseDissolve;
            UNITY_DECLARE_TEX2D(_DissolveTexture);
            float4 _DissolveTexture_ST;
            float _DissolveAmount;
            float _DissolveEdgeWidth;
            fixed4 _DissolveEdgeColor;
            float _DissolveScale;

            float _UseEdgeFade;
            float _EdgeFadeDistance;

            float _UseDistortion;
            sampler2D _DistortionMap;
            float4 _DistortionMap_ST;
            float _DistortionStrength;
            float _DistortionScrollX;
            float _DistortionScrollY;
            float _DistortionScale;

            float _UseAudioLink;
            float _AudioLinkSmoothing;
            int _AudioBandStrength;
            int _AudioBandColor;
            int _AudioBandSize;
            float _AudioLinkStrength;
            float _AudioMultMin;
            float _AudioMultMax;
            float _AudioLinkColorShift;
            half4 _AudioLinkColorLow;
            half4 _AudioLinkColorMid;
            half4 _AudioLinkColorHigh;
            float _AudioLinkSize;
            float _AudioSizeMin;
            float _AudioSizeMax;

            int _RenderingMode;
            int _ColorMode;

            #if defined(AUDIOLINK)
            float GetAudioReactiveValue(int band)
            {
                if (_UseAudioLink < 0.5 || !AudioLinkIsAvailable())
                    return 0.0;

                float audioValue = 0.0;

                // Get audio value - use progressively smoother channels as smoothing increases
                if (_AudioLinkSmoothing < 0.01)
                {
                    // No smoothing - use raw value (.r)
                    if (band == 0)
                        audioValue = AudioLinkData(ALPASS_AUDIOBASS).r;
                    else if (band == 1)
                        audioValue = AudioLinkData(ALPASS_AUDIOLOWMIDS).r;
                    else if (band == 2)
                        audioValue = AudioLinkData(ALPASS_AUDIOHIGHMIDS).r;
                    else if (band == 3)
                        audioValue = AudioLinkData(ALPASS_AUDIOTREBLE).r;
                    else if (band == 4)
                        audioValue = (AudioLinkData(ALPASS_AUDIOBASS).r +
                                     AudioLinkData(ALPASS_AUDIOLOWMIDS).r +
                                     AudioLinkData(ALPASS_AUDIOHIGHMIDS).r +
                                     AudioLinkData(ALPASS_AUDIOTREBLE).r) * 0.25;
                }
                else
                {
                    // Use smoothed value (.g) and apply additional smoothing via pow function
                    if (band == 0)
                        audioValue = AudioLinkData(ALPASS_AUDIOBASS).g;
                    else if (band == 1)
                        audioValue = AudioLinkData(ALPASS_AUDIOLOWMIDS).g;
                    else if (band == 2)
                        audioValue = AudioLinkData(ALPASS_AUDIOHIGHMIDS).g;
                    else if (band == 3)
                        audioValue = AudioLinkData(ALPASS_AUDIOTREBLE).g;
                    else if (band == 4)
                        audioValue = (AudioLinkData(ALPASS_AUDIOBASS).g +
                                     AudioLinkData(ALPASS_AUDIOLOWMIDS).g +
                                     AudioLinkData(ALPASS_AUDIOHIGHMIDS).g +
                                     AudioLinkData(ALPASS_AUDIOTREBLE).g) * 0.25;

                    // Apply exponential smoothing curve - higher values create more damping
                    // This makes the value approach changes more slowly
                    float smoothPower = 1.0 + (_AudioLinkSmoothing * 0.4); // Map 0-5 to 1.0-3.0
                    audioValue = pow(audioValue, smoothPower);
                }

                return audioValue;
            }
            #endif

            float GetAudioReactiveMultiplier()
            {
                #if defined(AUDIOLINK)
                    if (_AudioLinkStrength < 0.5 || _UseAudioLink < 0.5 || !AudioLinkIsAvailable())
                        return 1.0;

                    float audioValue = GetAudioReactiveValue(_AudioBandStrength);
                    return lerp(_AudioMultMin, _AudioMultMax, audioValue);
                #else
                    return 1.0;
                #endif
            }

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float4 vertex = v.vertex;

                // Apply AudioLink size modulation
                #if defined(AUDIOLINK)
                if (_UseAudioLink > 0.5 && _AudioLinkSize > 0.5 && AudioLinkIsAvailable())
                {
                    float audioValue = GetAudioReactiveValue(_AudioBandSize);
                    float sizeMultiplier = lerp(_AudioSizeMin, _AudioSizeMax, audioValue);
                    vertex.xyz *= sizeMultiplier;
                }
                #endif

                o.vertex = UnityObjectToClipPos(vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _TintColor;
                o.screenPos = ComputeScreenPos(o.vertex);

                UNITY_TRANSFER_FOG(o,o.vertex);

                #ifdef SOFTPARTICLES_ON
                o.projPos = o.screenPos;
                COMPUTE_EYEDEPTH(o.projPos.z);
                #endif

                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float audioMultiplier = GetAudioReactiveMultiplier();

                float2 mainUV = i.texcoord;
                if (_UseDistortion > 0.5)
                {
                    float2 distortionUV = i.texcoord * _DistortionScale;
                    distortionUV += float2(_DistortionScrollX, _DistortionScrollY) * _Time.y;

                    float3 distortionNormal = UnpackNormal(tex2D(_DistortionMap, distortionUV));
                    float2 distortionOffset = distortionNormal.xy * _DistortionStrength * 0.1;

                    mainUV += distortionOffset;
                }

                half4 col = tex2D(_MainTex, mainUV) * i.color;

                half3 emission = half3(0,0,0);
                if (_UseEmission > 0.5)
                {
                    half4 emissionMap = tex2D(_EmissionMap, mainUV);
                    half4 emissionMask = tex2D(_EmissionMask, mainUV);

                    half3 emissionMapContrib = lerp(half3(1,1,1), emissionMap.rgb, step(0.01, dot(emissionMap.rgb, half3(1,1,1))));
                    half3 baseColorTint = lerp(half3(1,1,1), col.rgb, _EmissionBaseColorAsMap);

                    // Determine emission strength
                    float emissionStrength = _EmissionStrength;
                    #if defined(AUDIOLINK)
                    if (_UseAudioLink > 0.5 && _AudioLinkStrength > 0.5 && AudioLinkIsAvailable())
                    {
                        // Override emission strength with AudioLink value
                        float audioValue = GetAudioReactiveValue(_AudioBandStrength);
                        emissionStrength = lerp(_AudioMultMin, _AudioMultMax, audioValue);
                    }
                    #endif

                    emission = emissionMapContrib * emissionMask.rgb * baseColorTint * _EmissionColor.rgb * emissionStrength;

                    #if defined(AUDIOLINK)
                    // Apply color shifting if enabled
                    if (_UseAudioLink > 0.5 && _AudioLinkColorShift > 0.5 && AudioLinkIsAvailable())
                    {
                        float audioValue = GetAudioReactiveValue(_AudioBandColor);
                        half3 gradientColor;
                        if (audioValue < 0.5)
                        {
                            gradientColor = lerp(_AudioLinkColorLow.rgb, _AudioLinkColorMid.rgb, audioValue * 2.0);
                        }
                        else
                        {
                            gradientColor = lerp(_AudioLinkColorMid.rgb, _AudioLinkColorHigh.rgb, (audioValue - 0.5) * 2.0);
                        }
                        emission = gradientColor * emissionStrength;
                    }
                    #endif

                    emission = max(emission, 0.0);
                }

                if (_EmissionReplaceBase > 0.5)
                {
                    col.rgb = lerp(col.rgb, emission, saturate(length(emission)));
                }
                else
                {
                    col.rgb += emission;
                }
                col.a *= _Alpha;

                if (_UseEdgeFade > 0.5)
                {
                    float2 screenUV = i.screenPos.xy / i.screenPos.w;
                    float2 edgeDist = min(screenUV, 1.0 - screenUV);
                    float edgeFactor = min(edgeDist.x, edgeDist.y);
                    float edgeFade = saturate(edgeFactor / _EdgeFadeDistance);
                    col.a *= edgeFade;
                }

                if (_UseDissolve > 0.5)
                {
                    float2 dissolveUV = i.texcoord * _DissolveScale;
                    float noise = UNITY_SAMPLE_TEX2D(_DissolveTexture, dissolveUV).r;

                    float dissolveEdge = _DissolveAmount + _DissolveEdgeWidth;

                    if (noise < _DissolveAmount)
                    {
                        discard;
                    }

                    if (noise < dissolveEdge)
                    {
                        float edgeBlend = (dissolveEdge - noise) / _DissolveEdgeWidth;
                        col.rgb = lerp(col.rgb, _DissolveEdgeColor.rgb, edgeBlend * _DissolveEdgeColor.a);
                    }
                }

                clip(col.a - _Cutoff);

                #ifdef SOFTPARTICLES_ON
                float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
                float partZ = i.projPos.z;
                float fade = saturate(_InvFade * (sceneZ - partZ));
                col.a *= fade;
                #endif

                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }

    Fallback "Particles/Alpha Blended"
    CustomEditor "ParticlesShaderGUI"
}