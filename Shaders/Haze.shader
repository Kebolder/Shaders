Shader "_Kebolder/Haze"
{
    Properties
    {
        [HideInInspector][KTitle(Haze Shader)] _KTitle ("", Float) = 0
        _MaskTex      ("Mask", 2D) = "white" {}
        _DistortTex   ("Distortion (RG) Noise/Flow", 2D) = "gray" {}

        [HideInInspector][KSection(Settings)] _KSectionSettings ("", Float) = 0
        _Strength     ("Distortion Strength", Range(0, 0.05)) = 0.01
        _Speed        ("Distortion Scroll Speed (XY)", Vector) = (0.1, 0.0, 0, 0)

        _MaskPower    ("Mask Power (Edge Contrast)", Range(0.25, 4.0)) = 1.0
        _Overall      ("Overall Effect Multiplier", Range(0, 1)) = 1.0
        [HideInInspector][KSectionEnd] _KSectionSettingsEnd ("", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        GrabPass { "_GrabTexture" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Packages/com.kebolder.shaders/Shaders/Includes/KebolderUV.cginc"

            sampler2D _GrabTexture;

            sampler2D _MaskTex;     float4 _MaskTex_ST;
            sampler2D _DistortTex;  float4 _DistortTex_ST;

            float  _Strength;
            float4 _Speed;
            float  _MaskPower;
            float  _Overall;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos     : SV_POSITION;
                float4 grabPos : TEXCOORD0;
                float2 uvMask  : TEXCOORD1;
                float2 uvDis   : TEXCOORD2;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.grabPos = ComputeGrabScreenPos(o.pos);

                o.uvMask = KebolderUV(v.uv, _MaskTex_ST);

                float2 duv = KebolderUV(v.uv, _DistortTex_ST);
                duv += _Time.y * _Speed.xy;
                o.uvDis = duv;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float mask = tex2D(_MaskTex, i.uvMask).r;

                mask = saturate(pow(mask, _MaskPower));

                mask *= _Overall;

                float2 flow = tex2D(_DistortTex, i.uvDis).rg * 2.0 - 1.0;

                float2 offset = flow * (_Strength * mask);

                float4 gp = i.grabPos;
                gp.xy += offset * gp.w;

                fixed4 refracted = tex2Dproj(_GrabTexture, gp);

                refracted.a = mask;

                return refracted;
            }
            ENDCG
        }
    }

    CustomEditor "KebolderShaderGUI"
}