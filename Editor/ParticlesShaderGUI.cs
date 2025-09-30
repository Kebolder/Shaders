using UnityEngine;
using UnityEditor;
using System.IO;

[System.Serializable]
public class PackageData
{
    public string version;
}

public class ParticlesShaderGUI : ShaderGUI
{
    public enum RenderingMode
    {
        Opaque,
        Cutout,
        Fade,
        Additive,
        Transparent,
        Subtract
    }

    public enum ColorMode
    {
        Normal,
        Multiply,
        Overlay,
        Subtract,
        Additive,
        Color,
        Difference
    }

    private static readonly Color headerColorDark = new Color(0.4f, 0.4f, 0.4f, 0.5f);
    private static readonly Color headerColorLight = new Color(0.85f, 0.85f, 0.85f, 0.7f);
    private static readonly Color contentColorDark = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    private static readonly Color contentColorLight = new Color(0.95f, 0.95f, 0.95f, 0.8f);

    private static bool mainFoldout = true;
    private static bool emissionFoldout = true;
    private static bool dissolveFoldout = true;
    private static bool edgeFadeFoldout = true;
    private static bool distortionFoldout = true;
    private static bool audioLinkFoldout = true;
    private static bool settingsFoldout = true;

    private static string shaderVersion = null;

    private static string GetShaderVersion()
    {
        if (shaderVersion != null)
            return shaderVersion;

        string packagePath = "Packages/com.kebolder.shaders/package.json";
        if (File.Exists(packagePath))
        {
            try
            {
                string json = File.ReadAllText(packagePath);
                PackageData data = JsonUtility.FromJson<PackageData>(json);
                shaderVersion = data.version ?? "1.0.0";
            }
            catch
            {
                shaderVersion = "1.0.0";
            }
        }
        else
        {
            shaderVersion = "1.0.0";
        }

        return shaderVersion;
    }

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        Material material = materialEditor.target as Material;

        MaterialProperty renderingMode = FindProperty("_RenderingMode", properties);
        MaterialProperty colorMode = FindProperty("_ColorMode", properties);

        EditorGUI.BeginChangeCheck();

        DrawHeader("Jax Particles Shader");

        mainFoldout = DrawFoldoutSection("Main", mainFoldout, () => {
            EditorGUILayout.LabelField("Rendering", EditorStyles.boldLabel);
            DrawRenderingModeDropdown(materialEditor, renderingMode);
            DrawColorModeDropdown(materialEditor, colorMode, renderingMode);

            DrawDivider();

            EditorGUILayout.LabelField("Main Maps", EditorStyles.boldLabel);
            MaterialProperty mainTex = FindProperty("_MainTex", properties);
            materialEditor.TexturePropertySingleLine(new GUIContent("Particle Texture", "Main texture defining the particle's appearance. Use textures with alpha for transparency."), mainTex);

            DrawDivider();

            EditorGUILayout.LabelField("Colors", EditorStyles.boldLabel);
            MaterialProperty tintColor = FindProperty("_TintColor", properties);
            MaterialProperty alpha = FindProperty("_Alpha", properties);
            MaterialProperty cutoff = FindProperty("_Cutoff", properties);
            materialEditor.ShaderProperty(tintColor, new GUIContent("Tint Color", "Multiplies with particle texture color. Alpha channel affects transparency."));
            materialEditor.ShaderProperty(alpha, new GUIContent("Alpha", "Overall transparency multiplier for all particles. 0=invisible, 1=opaque."));
            materialEditor.ShaderProperty(cutoff, new GUIContent("Alpha Cutoff", "Discards pixels below this alpha threshold. Higher values create harder edges."));
        });

        MaterialProperty useEmission = FindProperty("_UseEmission", properties);
        emissionFoldout = DrawToggleFoldoutSection("Emission", emissionFoldout, useEmission, () => {
            MaterialProperty emissionMap = FindProperty("_EmissionMap", properties);
            MaterialProperty emissionMask = FindProperty("_EmissionMask", properties);
            MaterialProperty emissionColor = FindProperty("_EmissionColor", properties);
            MaterialProperty emissionStrength = FindProperty("_EmissionStrength", properties);
            MaterialProperty emissionBaseColorAsMap = FindProperty("_EmissionBaseColorAsMap", properties);
            MaterialProperty emissionReplaceBase = FindProperty("_EmissionReplaceBase", properties);

            materialEditor.TexturePropertySingleLine(new GUIContent("Emission Map", "RGB texture controlling emission color distribution. Black areas have no emission."), emissionMap);
            materialEditor.TexturePropertySingleLine(new GUIContent("Emission Mask", "Grayscale texture masking where emission appears. White=full emission, black=no emission."), emissionMask);
            materialEditor.ShaderProperty(emissionColor, new GUIContent("Emission Color", "HDR color tinting the emission. Values above 1 create bloom effects."));
            materialEditor.ShaderProperty(emissionStrength, new GUIContent("Emission Strength", "Multiplier for emission intensity. 0=no glow, 1=normal, 5-20=very bright HDR bloom."));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Base Color Integration", EditorStyles.miniBoldLabel);
            materialEditor.ShaderProperty(emissionBaseColorAsMap, new GUIContent("Use Base Colors", "Tint emission with particle's base texture colors for color variation."));
            materialEditor.ShaderProperty(emissionReplaceBase, new GUIContent("Override Base Color", "Replace base color with emission instead of adding emission on top."));
        });

        MaterialProperty useDissolve = FindProperty("_UseDissolve", properties);
        dissolveFoldout = DrawToggleFoldoutSection("Dissolve Effects", dissolveFoldout, useDissolve, () => {
            MaterialProperty dissolveTexture = FindProperty("_DissolveTexture", properties);
            MaterialProperty dissolveAmount = FindProperty("_DissolveAmount", properties);
            MaterialProperty dissolveEdgeWidth = FindProperty("_DissolveEdgeWidth", properties);
            MaterialProperty dissolveEdgeColor = FindProperty("_DissolveEdgeColor", properties);
            MaterialProperty dissolveScale = FindProperty("_DissolveScale", properties);

            materialEditor.TexturePropertySingleLine(new GUIContent("Dissolve Noise Texture", "Grayscale noise texture controlling dissolve pattern. Use Perlin noise for organic effects."), dissolveTexture);
            materialEditor.ShaderProperty(dissolveAmount, new GUIContent("Dissolve Amount", "How much of the particle is dissolved. Animate 0→1 for burn-away effects."));
            materialEditor.ShaderProperty(dissolveEdgeWidth, new GUIContent("Dissolve Edge Width", "Width of the glowing edge effect at dissolve boundary. Higher=thicker glow."));
            materialEditor.ShaderProperty(dissolveEdgeColor, new GUIContent("Dissolve Edge Color", "Color and intensity of the dissolve edge glow effect."));
            materialEditor.ShaderProperty(dissolveScale, new GUIContent("Dissolve Scale", "Scale multiplier for noise texture UV coordinates. Higher=smaller noise pattern."));
        });

        MaterialProperty useEdgeFade = FindProperty("_UseEdgeFade", properties);
        edgeFadeFoldout = DrawToggleFoldoutSection("Edge Fade", edgeFadeFoldout, useEdgeFade, () => {
            MaterialProperty edgeFadeDistance = FindProperty("_EdgeFadeDistance", properties);
            materialEditor.ShaderProperty(edgeFadeDistance, new GUIContent("Edge Fade Distance", "Distance from screen edges where fading begins. Try 0.05-0.15 for subtle effects."));
        });

        MaterialProperty useDistortion = FindProperty("_UseDistortion", properties);
        distortionFoldout = DrawToggleFoldoutSection("Distortion Effects", distortionFoldout, useDistortion, () => {
            MaterialProperty distortionMap = FindProperty("_DistortionMap", properties);
            MaterialProperty distortionStrength = FindProperty("_DistortionStrength", properties);
            MaterialProperty distortionScale = FindProperty("_DistortionScale", properties);
            MaterialProperty distortionScrollX = FindProperty("_DistortionScrollX", properties);
            MaterialProperty distortionScrollY = FindProperty("_DistortionScrollY", properties);

            materialEditor.TexturePropertySingleLine(new GUIContent("Distortion Normal Map", "Normal map controlling distortion direction and intensity. Use animated normals for flowing effects."), distortionMap);
            materialEditor.ShaderProperty(distortionStrength, new GUIContent("Distortion Strength", "Intensity of the distortion effect. Higher values create more dramatic warping."));
            materialEditor.ShaderProperty(distortionScale, new GUIContent("Distortion UV Scale", "UV scale for distortion texture. Higher values create smaller, more detailed distortion patterns."));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Distortion Animation", EditorStyles.miniBoldLabel);
            materialEditor.ShaderProperty(distortionScrollX, new GUIContent("Distortion Scroll X", "Horizontal scrolling speed for distortion texture. Creates flowing motion effects."));
            materialEditor.ShaderProperty(distortionScrollY, new GUIContent("Distortion Scroll Y", "Vertical scrolling speed for distortion texture. Use with X for diagonal flow."));
        });

        MaterialProperty useAudioLink = FindProperty("_UseAudioLink", properties);

        // Handle AudioLink keyword
        EditorGUI.BeginChangeCheck();
        audioLinkFoldout = DrawToggleFoldoutSection("AudioLink (VRChat)", audioLinkFoldout, useAudioLink, () => {
            MaterialProperty audioLinkSmoothing = FindProperty("_AudioLinkSmoothing", properties);
            MaterialProperty audioBandStrength = FindProperty("_AudioBandStrength", properties);
            MaterialProperty audioBandColor = FindProperty("_AudioBandColor", properties);
            MaterialProperty audioBandSize = FindProperty("_AudioBandSize", properties);
            MaterialProperty audioLinkStrength = FindProperty("_AudioLinkStrength", properties);
            MaterialProperty audioMultMin = FindProperty("_AudioMultMin", properties);
            MaterialProperty audioMultMax = FindProperty("_AudioMultMax", properties);
            MaterialProperty audioLinkColorShift = FindProperty("_AudioLinkColorShift", properties);
            MaterialProperty audioLinkColorLow = FindProperty("_AudioLinkColorLow", properties);
            MaterialProperty audioLinkColorMid = FindProperty("_AudioLinkColorMid", properties);
            MaterialProperty audioLinkColorHigh = FindProperty("_AudioLinkColorHigh", properties);
            MaterialProperty audioLinkSize = FindProperty("_AudioLinkSize", properties);
            MaterialProperty audioSizeMin = FindProperty("_AudioSizeMin", properties);
            MaterialProperty audioSizeMax = FindProperty("_AudioSizeMax", properties);

            EditorGUILayout.LabelField("Global Settings", EditorStyles.boldLabel);
            materialEditor.ShaderProperty(audioLinkSmoothing, new GUIContent("Smoothing", "Uses AudioLink's pre-smoothed data and applies exponential damping. 0=instant response (raw), >0=uses smoothed values with curve damping."));
            EditorGUILayout.HelpBox("Smoothing applies to all AudioLink features. Note: This uses AudioLink's built-in smoothed data, so the effect may be subtle. For dramatic smoothing, adjust AudioLink's global settings.", MessageType.Info);

            DrawDivider();

            EditorGUILayout.LabelField("Emission Intensity Modulation", EditorStyles.boldLabel);
            materialEditor.ShaderProperty(audioLinkStrength, new GUIContent("Enable Strength Modulation", "Toggle audio-reactive emission strength on/off."));

            EditorGUI.BeginDisabledGroup(audioLinkStrength.floatValue < 0.5f);
            DrawAudioBandDropdown(materialEditor, audioBandStrength, "Audio Band (Strength)");
            EditorGUILayout.HelpBox("AudioLink overrides your Emission Strength with these values based on the selected audio band's intensity.", MessageType.Info);
            materialEditor.ShaderProperty(audioMultMin, new GUIContent("Min Strength", "Emission strength at low/silent audio. Example: Min=1 means emission strength of 1 when silent."));
            materialEditor.ShaderProperty(audioMultMax, new GUIContent("Max Strength", "Emission strength at peak audio. Example: Max=10 means emission strength of 10 at full volume."));
            EditorGUI.EndDisabledGroup();

            DrawDivider();

            EditorGUILayout.LabelField("Color Shifting", EditorStyles.boldLabel);
            materialEditor.ShaderProperty(audioLinkColorShift, new GUIContent("Enable Color Shifting", "Toggle audio-reactive color gradient shifting on/off."));

            EditorGUI.BeginDisabledGroup(audioLinkColorShift.floatValue < 0.5f);
            DrawAudioBandDropdown(materialEditor, audioBandColor, "Audio Band (Color)");
            EditorGUILayout.HelpBox("Audio band intensity blends between three colors to create a smooth gradient effect.", MessageType.Info);
            materialEditor.ShaderProperty(audioLinkColorLow, new GUIContent("Silent Color", "Color displayed when audio is silent or at minimum intensity."));
            materialEditor.ShaderProperty(audioLinkColorMid, new GUIContent("Mid Color", "Color displayed at medium audio intensity (50%)."));
            materialEditor.ShaderProperty(audioLinkColorHigh, new GUIContent("Peak Color", "Color displayed at peak audio intensity (100%)."));
            EditorGUI.EndDisabledGroup();

            DrawDivider();

            EditorGUILayout.LabelField("Size Modulation", EditorStyles.boldLabel);
            materialEditor.ShaderProperty(audioLinkSize, new GUIContent("Enable Size Modulation", "Toggle audio-reactive particle size scaling on/off."));

            EditorGUI.BeginDisabledGroup(audioLinkSize.floatValue < 0.5f);
            DrawAudioBandDropdown(materialEditor, audioBandSize, "Audio Band (Size)");
            EditorGUILayout.HelpBox("AudioLink scales particle size based on the selected audio band's intensity.", MessageType.Info);
            materialEditor.ShaderProperty(audioSizeMin, new GUIContent("Min Size", "Size multiplier at low/silent audio. Example: Min=0.5 means particles are half size when silent."));
            materialEditor.ShaderProperty(audioSizeMax, new GUIContent("Max Size", "Size multiplier at peak audio. Example: Max=2.0 means particles are double size at full volume."));
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("💡 AudioLink features work independently! Emission features require Emission to be enabled. Size modulation works on all particles.", MessageType.None);
        });

        if (EditorGUI.EndChangeCheck())
        {
            Material mat = materialEditor.target as Material;
            if (useAudioLink.floatValue > 0.5f)
                mat.EnableKeyword("AUDIOLINK");
            else
                mat.DisableKeyword("AUDIOLINK");
        }

        settingsFoldout = DrawFoldoutSection("Settings", settingsFoldout, () => {
            MaterialProperty invFade = FindProperty("_InvFade", properties);
            MaterialProperty dualSided = FindProperty("_DualSided", properties);

            EditorGUILayout.LabelField("Soft Particles", EditorStyles.boldLabel);
            materialEditor.ShaderProperty(invFade, new GUIContent("Soft Particles Factor", "Softly fades particles when intersecting geometry. Low values=sharp intersections, high values=very soft blending."));

            DrawDivider();

            EditorGUILayout.LabelField("Dual Sided Rendering", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            materialEditor.ShaderProperty(dualSided, new GUIContent("Enable Dual Sided", "Renders particles on both front and back faces for double-sided visibility."));
            if (EditorGUI.EndChangeCheck())
            {
                Material mat = materialEditor.target as Material;
                if (dualSided.floatValue > 0.5f)
                    mat.EnableKeyword("_DUALSIDED_ON");
                else
                    mat.DisableKeyword("_DUALSIDED_ON");
            }
        });

        if (EditorGUI.EndChangeCheck())
        {
            SetupMaterialBlendMode(material, (RenderingMode)renderingMode.floatValue, (ColorMode)colorMode.floatValue);
        }

        SetupMaterialBlendMode(material, (RenderingMode)renderingMode.floatValue, (ColorMode)colorMode.floatValue);

        // Ensure AudioLink keyword is properly set on material load
        if (useAudioLink.floatValue > 0.5f)
            material.EnableKeyword("AUDIOLINK");
        else
            material.DisableKeyword("AUDIOLINK");
    }

    void SetupMaterialBlendMode(Material material, RenderingMode renderingMode, ColorMode colorMode)
    {
        switch (renderingMode)
        {
            case RenderingMode.Opaque:
                material.SetOverrideTag("RenderType", "Opaque");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = -1;
                break;

            case RenderingMode.Cutout:
                material.SetOverrideTag("RenderType", "TransparentCutout");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.EnableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                break;

            case RenderingMode.Fade:
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                break;

            case RenderingMode.Transparent:
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                break;

            case RenderingMode.Additive:
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                break;

            case RenderingMode.Subtract:
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcColor);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                break;
        }

        if (renderingMode != RenderingMode.Opaque && renderingMode != RenderingMode.Cutout)
        {
            switch (colorMode)
            {
                case ColorMode.Normal:
                    break;

                case ColorMode.Multiply:
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    break;

                case ColorMode.Overlay:
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.SrcColor);
                    break;

                case ColorMode.Subtract:
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcColor);
                    break;

                case ColorMode.Additive:
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    break;

                case ColorMode.Color:
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcColor);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcColor);
                    break;

                case ColorMode.Difference:
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusDstColor);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcColor);
                    break;
            }
        }
    }

    private void DrawHeader(string title)
    {
        EditorGUILayout.Space();
        var headerStyle = new GUIStyle(EditorStyles.boldLabel);
        headerStyle.fontSize = 14;
        headerStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;

        var versionStyle = new GUIStyle(EditorStyles.label);
        versionStyle.fontSize = 10;
        versionStyle.alignment = TextAnchor.MiddleRight;
        versionStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.7f, 0.7f, 0.7f) : new Color(0.4f, 0.4f, 0.4f);

        var rect = EditorGUILayout.GetControlRect(false, 25);

        GUI.Label(rect, title, headerStyle);

        string version = GetShaderVersion();
        GUI.Label(rect, "v" + version, versionStyle);

        var lineRect = new Rect(rect.x, rect.y + rect.height - 2, rect.width, 1);
        EditorGUI.DrawRect(lineRect, EditorGUIUtility.isProSkin ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.3f, 0.3f, 0.3f));
    }

    private bool DrawFoldoutSection(string title, bool foldout, System.Action drawContent)
    {
        EditorGUILayout.Space(2);

        var headerStyle = new GUIStyle(GUI.skin.box);
        headerStyle.normal.background = MakeColorTexture(EditorGUIUtility.isProSkin ? headerColorDark : headerColorLight);
        headerStyle.padding = new RectOffset(4, 4, 4, 4);

        var headerRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + 8);

        if (Event.current.type == EventType.MouseDown && headerRect.Contains(Event.current.mousePosition))
        {
            foldout = !foldout;
            Event.current.Use();
        }

        GUI.Box(headerRect, "", headerStyle);

        var titleStyle = new GUIStyle(EditorStyles.boldLabel);
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.fontSize = 12;
        var titleRect = new Rect(headerRect.x + 6, headerRect.y + 4, headerRect.width - 12, EditorGUIUtility.singleLineHeight);
        GUI.Label(titleRect, title, titleStyle);

        if (foldout)
        {
            EditorGUILayout.Space(2);

            var contentStyle = new GUIStyle(GUI.skin.box);
            contentStyle.normal.background = MakeColorTexture(EditorGUIUtility.isProSkin ? contentColorDark : contentColorLight);
            contentStyle.padding = new RectOffset(8, 8, 8, 8);

            EditorGUILayout.BeginVertical(contentStyle);
            EditorGUILayout.Space(4);

            drawContent?.Invoke();

            EditorGUILayout.Space(4);
            EditorGUILayout.EndVertical();
        }

        return foldout;
    }

    private void DrawDivider()
    {
        EditorGUILayout.Space(8);
        var rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin ? new Color(0.5f, 0.5f, 0.5f, 0.5f) : new Color(0.3f, 0.3f, 0.3f, 0.5f));
        EditorGUILayout.Space(8);
    }

    private bool DrawToggleFoldoutSection(string title, bool foldout, MaterialProperty toggleProperty, System.Action drawContent)
    {
        EditorGUILayout.Space(2);

        var headerStyle = new GUIStyle(GUI.skin.box);
        headerStyle.normal.background = MakeColorTexture(EditorGUIUtility.isProSkin ? headerColorDark : headerColorLight);
        headerStyle.padding = new RectOffset(4, 4, 4, 4);
        headerStyle.margin = new RectOffset(0, 0, 0, 0);

        var headerRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + 8);

        var titleRect = new Rect(headerRect.x + 6, headerRect.y + 4, headerRect.width - 35, EditorGUIUtility.singleLineHeight);
        var toggleRect = new Rect(headerRect.x + headerRect.width - 24, headerRect.y + 4, 20, EditorGUIUtility.singleLineHeight);

        if (Event.current.type == EventType.MouseDown && headerRect.Contains(Event.current.mousePosition) && !toggleRect.Contains(Event.current.mousePosition))
        {
            foldout = !foldout;
            Event.current.Use();
        }

        GUI.Box(headerRect, "", headerStyle);

        var titleStyle = new GUIStyle(EditorStyles.boldLabel);
        titleStyle.fontSize = 12;

        EditorGUI.LabelField(titleRect, title, titleStyle);

        EditorGUI.BeginChangeCheck();
        var toggleValue = EditorGUI.Toggle(toggleRect, toggleProperty.floatValue > 0.5f);
        if (EditorGUI.EndChangeCheck())
        {
            toggleProperty.floatValue = toggleValue ? 1.0f : 0.0f;
        }

        if (foldout)
        {
            EditorGUILayout.Space(2);

            var contentStyle = new GUIStyle(GUI.skin.box);
            contentStyle.normal.background = MakeColorTexture(EditorGUIUtility.isProSkin ? contentColorDark : contentColorLight);
            contentStyle.padding = new RectOffset(8, 8, 8, 8);

            EditorGUILayout.BeginVertical(contentStyle);
            EditorGUILayout.Space(4);

            EditorGUI.BeginDisabledGroup(toggleProperty.floatValue <= 0.5f);
            drawContent?.Invoke();
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(4);
            EditorGUILayout.EndVertical();
        }

        return foldout;
    }

    private Texture2D MakeColorTexture(Color color)
    {
        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private void DrawRenderingModeDropdown(MaterialEditor materialEditor, MaterialProperty renderingMode)
    {
        var renderingModeNames = new string[] { "Opaque", "Cutout", "Fade", "Additive", "Transparent", "Subtract" };
        var renderingModeTooltips = new string[] {
            "Solid particles with no transparency - best performance",
            "Binary transparency using alpha cutoff - good for foliage",
            "Standard alpha blending transparency - most common for particles",
            "Brightening additive blend - great for glowing effects",
            "Premultiplied alpha transparency - smooth blending",
            "Subtractive blend - darkens the background"
        };

        EditorGUI.BeginChangeCheck();

        var currentMode = (int)renderingMode.floatValue;
        var newMode = EditorGUILayout.Popup(new GUIContent("Rendering Mode", "Controls the base transparency and rendering method"),
                                           currentMode, renderingModeNames);

        if (EditorGUI.EndChangeCheck())
        {
            renderingMode.floatValue = newMode;
        }

        if (currentMode >= 0 && currentMode < renderingModeTooltips.Length)
        {
            EditorGUILayout.HelpBox(renderingModeTooltips[currentMode], MessageType.Info);
        }
    }

    private void DrawColorModeDropdown(MaterialEditor materialEditor, MaterialProperty colorMode, MaterialProperty renderingMode)
    {
        var colorModeNames = new string[] { "Normal", "Multiply", "Overlay", "Subtract", "Additive", "Color", "Difference" };
        var colorModeTooltips = new string[] {
            "Standard color blending - no color modifications",
            "Multiplies particle color with background - darkening effect",
            "Complex blend combining multiply and screen - enhances contrast",
            "Subtracts particle color from background - darkening effect",
            "Adds particle color to background - brightening effect",
            "Uses particle color while preserving background luminance",
            "Creates high contrast by finding color differences"
        };

        EditorGUI.BeginChangeCheck();

        var currentMode = (int)colorMode.floatValue;
        var newMode = EditorGUILayout.Popup(new GUIContent("Color Mode", "Controls how particle colors blend with the background"),
                                           currentMode, colorModeNames);

        if (EditorGUI.EndChangeCheck())
        {
            colorMode.floatValue = newMode;
        }

        if (currentMode >= 0 && currentMode < colorModeTooltips.Length)
        {
            EditorGUILayout.HelpBox(colorModeTooltips[currentMode], MessageType.Info);
        }

        if (renderingMode != null)
        {
            var renderingModeValue = (int)renderingMode.floatValue;
            if (renderingModeValue == 0 || renderingModeValue == 1)
            {
                EditorGUILayout.HelpBox("Color Mode only affects transparent rendering modes.", MessageType.Info);
            }
        }
    }

    private void DrawAudioBandDropdown(MaterialEditor materialEditor, MaterialProperty audioBand, string label = "Audio Band")
    {
        var bandNames = new string[] { "Bass", "Low Mid", "High Mid", "Treble", "Volume" };
        var bandTooltips = new string[] {
            "Bass frequencies - Deep, thumping sounds",
            "Low-Mid frequencies - Vocals and mid-range instruments",
            "High-Mid frequencies - Bright instruments and harmonics",
            "Treble frequencies - High-pitched sounds and cymbals",
            "Volume (All Bands) - Average of all frequency bands"
        };

        EditorGUI.BeginChangeCheck();

        var currentBand = (int)audioBand.floatValue;
        var newBand = EditorGUILayout.Popup(new GUIContent(label, "Which frequency range to react to"),
                                           currentBand, bandNames);

        if (EditorGUI.EndChangeCheck())
        {
            audioBand.floatValue = newBand;
        }

        if (currentBand >= 0 && currentBand < bandTooltips.Length)
        {
            EditorGUILayout.HelpBox(bandTooltips[currentBand], MessageType.Info);
        }
    }

}