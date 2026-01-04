using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public class KebolderPackageData
{
    public string version;
}

// Tag system overview (use in shader property attributes):
// - KHeader(Title): Big header label.
// - KSubHeader(Title): Smaller sub-header label.
// - KDivider: Horizontal separator line.
// - KHelp(Message, Info|Warning|Error|None): Help box with optional type (default Info).
// - KOptionName(Title): Draws a label immediately above the property.
// - KSection(Title): Foldout section start. Ends at KSectionEnd.
// - KToggleSection(Title): Foldout section with an on/off toggle (tag goes on the toggle property).
// - KSectionEnd: Closes the most recent KSection/KToggleSection.
// - KTitle(Title): Overrides the header title in the inspector (use on a hidden float property).
public class KebolderShaderGUI : ShaderGUI
{
    private enum KHelpType
    {
        None,
        Info,
        Warning,
        Error
    }

    private class SectionState
    {
        public bool IsOpen;
        public bool HasToggle;
    }

    private static readonly Color HeaderColorDark = new Color(0.4f, 0.4f, 0.4f, 0.5f);
    private static readonly Color HeaderColorLight = new Color(0.85f, 0.85f, 0.85f, 0.7f);
    private static readonly Color ContentColorDark = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    private static readonly Color ContentColorLight = new Color(0.95f, 0.95f, 0.95f, 0.8f);

    private static readonly Dictionary<string, bool> FoldoutStates = new Dictionary<string, bool>(StringComparer.Ordinal);
    private static readonly Dictionary<string, bool> TextureSettingsStates = new Dictionary<string, bool>(StringComparer.Ordinal);

    private static string shaderVersion;
    private static Texture2D textureSettingsBoxTexture;
    private static Color textureSettingsBoxColor;

    private static string GetShaderVersion()
    {
        if (!string.IsNullOrEmpty(shaderVersion))
            return shaderVersion;

        const string packagePath = "Packages/com.kebolder.shaders/package.json";
        if (!File.Exists(packagePath))
        {
            shaderVersion = "1.0.0";
            return shaderVersion;
        }

        try
        {
            string json = File.ReadAllText(packagePath);
            KebolderPackageData data = JsonUtility.FromJson<KebolderPackageData>(json);
            shaderVersion = string.IsNullOrEmpty(data.version) ? "1.0.0" : data.version;
        }
        catch
        {
            shaderVersion = "1.0.0";
        }

        return shaderVersion;
    }

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        Material material = materialEditor.target as Material;
        if (material == null)
            return;

        Shader shader = material.shader;
        if (shader == null)
            return;

        string headerTitle = GetHeaderTitle(shader);
        DrawHeader(headerTitle);

        var propertyLookup = new Dictionary<string, MaterialProperty>(StringComparer.Ordinal);
        foreach (MaterialProperty property in properties)
            propertyLookup[property.name] = property;

        var sectionStack = new Stack<SectionState>();

        int propertyCount = shader.GetPropertyCount();
        for (int i = 0; i < propertyCount; i++)
        {
            string propName = shader.GetPropertyName(i);
            if (!propertyLookup.TryGetValue(propName, out MaterialProperty property))
                continue;

            string[] attributes = shader.GetPropertyAttributes(i);
            bool hasKAttribute = HasKAttribute(attributes);
            bool hideProperty = (shader.GetPropertyFlags(i) & ShaderPropertyFlags.HideInInspector) != 0;

            bool shouldDrawProperty = !hideProperty;
            string optionName = null;
            bool inClosedSection = IsInClosedSection(sectionStack);

            if (attributes != null && attributes.Length > 0)
            {
                foreach (string attribute in attributes)
                {
                    if (inClosedSection)
                    {
                        if (IsAttribute(attribute, "KSectionEnd"))
                        {
                            EndSection(sectionStack);
                        }
                        else if (TryGetAttributeArg(attribute, "KSection", out string hiddenSectionTitle))
                        {
                            string title = string.IsNullOrEmpty(hiddenSectionTitle) ? property.displayName : hiddenSectionTitle;
                            BeginSection(sectionStack, GetSectionKey(title, property.name), title, null, true);
                        }
                        else if (TryGetAttributeArg(attribute, "KToggleSection", out string hiddenToggleSectionTitle))
                        {
                            string title = string.IsNullOrEmpty(hiddenToggleSectionTitle) ? property.displayName : hiddenToggleSectionTitle;
                            BeginSection(sectionStack, GetSectionKey(title, property.name), title, property, true);
                        }

                        shouldDrawProperty = false;
                        continue;
                    }

                    if (TryGetAttributeArg(attribute, "KHeader", out string sectionHeaderTitle))
                    {
                        DrawSectionHeader(string.IsNullOrEmpty(sectionHeaderTitle) ? property.displayName : sectionHeaderTitle);
                    }
                    else if (TryGetAttributeArg(attribute, "KSubHeader", out string subHeaderTitle))
                    {
                        DrawSubHeader(string.IsNullOrEmpty(subHeaderTitle) ? property.displayName : subHeaderTitle);
                    }
                    else if (TryGetAttributeArg(attribute, "KOptionName", out string optionNameTitle))
                    {
                        optionName = string.IsNullOrEmpty(optionNameTitle) ? property.displayName : optionNameTitle;
                    }
                    else if (IsAttribute(attribute, "KDivider"))
                    {
                        DrawDivider();
                        shouldDrawProperty = false;
                    }
                    else if (TryGetAttributeArg(attribute, "KHelp", out string helpArgs))
                    {
                        DrawHelp(helpArgs);
                        shouldDrawProperty = false;
                    }
                    else if (TryGetAttributeArg(attribute, "KSection", out string sectionTitle))
                    {
                        string title = string.IsNullOrEmpty(sectionTitle) ? property.displayName : sectionTitle;
                        BeginSection(sectionStack, GetSectionKey(title, property.name), title, null, false);
                        shouldDrawProperty = false;
                    }
                    else if (TryGetAttributeArg(attribute, "KToggleSection", out string toggleSectionTitle))
                    {
                        string title = string.IsNullOrEmpty(toggleSectionTitle) ? property.displayName : toggleSectionTitle;
                        BeginSection(sectionStack, GetSectionKey(title, property.name), title, property, false);
                        shouldDrawProperty = false;
                    }
                    else if (IsAttribute(attribute, "KSectionEnd"))
                    {
                        EndSection(sectionStack);
                        shouldDrawProperty = false;
                    }
                }
            }

            if (hideProperty && !hasKAttribute)
                continue;

            if (inClosedSection)
                continue;

            if (shouldDrawProperty)
            {
                if (!string.IsNullOrEmpty(optionName))
                    EditorGUILayout.LabelField(optionName, EditorStyles.miniBoldLabel);

                DrawProperty(materialEditor, property);
            }
        }

        while (sectionStack.Count > 0)
            EndSection(sectionStack);

        EditorGUILayout.Space(6);
        DrawDivider();
        materialEditor.RenderQueueField();
    }

    private void DrawHeader(string title)
    {
        EditorGUILayout.Space();
        var headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black }
        };

        var versionStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize = 10,
            alignment = TextAnchor.MiddleRight,
            normal = { textColor = EditorGUIUtility.isProSkin ? new Color(0.7f, 0.7f, 0.7f) : new Color(0.4f, 0.4f, 0.4f) }
        };

        Rect rect = EditorGUILayout.GetControlRect(false, 25);
        GUI.Label(rect, title, headerStyle);

        string version = GetShaderVersion();
        GUI.Label(rect, "v" + version, versionStyle);

        Rect lineRect = new Rect(rect.x, rect.y + rect.height - 2, rect.width, 1);
        EditorGUI.DrawRect(lineRect, EditorGUIUtility.isProSkin ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.3f, 0.3f, 0.3f));
        EditorGUILayout.Space(2);
    }

    private void DrawSectionHeader(string title)
    {
        EditorGUILayout.Space(1);
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 13,
            fontStyle = FontStyle.Bold
        };
        EditorGUILayout.LabelField(title, headerStyle);
        EditorGUILayout.Space(1);
    }

    private void DrawSubHeader(string title)
    {
        EditorGUILayout.Space(2);
        EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel);
    }

    private void DrawDivider()
    {
        EditorGUILayout.Space(8);
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin ? new Color(0.5f, 0.5f, 0.5f, 0.5f) : new Color(0.3f, 0.3f, 0.3f, 0.5f));
        EditorGUILayout.Space(8);
    }

    private void DrawHelp(string helpArgs)
    {
        string message = helpArgs;
        KHelpType type = KHelpType.Info;

        if (TrySplitArgs(helpArgs, out string[] parts))
        {
            message = parts[0];
            if (parts.Length > 1 && Enum.TryParse(parts[1], true, out KHelpType parsedType))
                type = parsedType;
        }

        EditorGUILayout.HelpBox(message, ToMessageType(type));
    }

    private void DrawProperty(MaterialEditor materialEditor, MaterialProperty property)
    {
        if (property.type == MaterialProperty.PropType.Texture)
        {
            DrawTextureProperty(materialEditor, property);
            return;
        }

        materialEditor.ShaderProperty(property, property.displayName);
    }

    private void DrawTextureProperty(MaterialEditor materialEditor, MaterialProperty property)
    {
        Material material = materialEditor.target as Material;
        string key = material != null ? material.shader.name + "::" + property.name : property.name;

        float height = EditorGUIUtility.singleLineHeight * 3f;
        Rect rect = EditorGUILayout.GetControlRect(true, height);
        rect = EditorGUI.IndentedRect(rect);

        GUIStyle labelStyle = EditorStyles.label;
        float labelWidth = Mathf.Min(EditorGUIUtility.labelWidth, labelStyle.CalcSize(new GUIContent(property.displayName)).x + 6f);
        Rect labelRect = new Rect(rect.x, rect.y, labelWidth, rect.height);
        Rect fieldRect = new Rect(rect.x + labelWidth, rect.y, rect.width - labelWidth, rect.height);
        float buttonSize = EditorGUIUtility.singleLineHeight;
        Rect buttonRect = new Rect(labelRect.xMax + 4f, rect.y + (rect.height - buttonSize) * 0.5f, buttonSize, buttonSize);
        Rect textureRect = new Rect(buttonRect.xMax + 4f, fieldRect.y, Mathf.Max(0f, rect.xMax - (buttonRect.xMax + 4f)), fieldRect.height);

        EditorGUI.LabelField(labelRect, property.displayName);

        EditorGUI.BeginChangeCheck();
        Texture texture = (Texture)EditorGUI.ObjectField(textureRect, GUIContent.none, property.textureValue, typeof(Texture), false);
        if (EditorGUI.EndChangeCheck())
            property.textureValue = texture;

        bool isOpen = GetTextureSettingsState(key, false);
        Color boxColor = EditorGUIUtility.isProSkin ? new Color(0.35f, 0.35f, 0.35f, 1f) : new Color(0.78f, 0.78f, 0.78f, 1f);
        DrawRoundedBox(buttonRect, boxColor);

        bool newOpen = isOpen;
        if (GUI.Button(buttonRect, GUIContent.none, GUIStyle.none))
            newOpen = !isOpen;

        GUIContent foldoutIcon = EditorGUIUtility.IconContent(newOpen ? "IN Foldout On" : "IN Foldout");
        Color oldGuiColor = GUI.color;
        GUI.color = Color.white;
        if (foldoutIcon != null && foldoutIcon.image != null)
        {
            GUIStyle iconStyle = new GUIStyle(GUIStyle.none)
            {
                alignment = TextAnchor.MiddleCenter
            };
            GUI.Label(buttonRect, foldoutIcon, iconStyle);
        }
        else
        {
            GUIStyle fallbackStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter
            };
            GUI.Label(buttonRect, newOpen ? "v" : ">", fallbackStyle);
        }
        GUI.color = oldGuiColor;
        if (newOpen != isOpen)
            SetTextureSettingsState(key, newOpen);

        if (newOpen)
        {
            EditorGUILayout.Space(2);
            using (new EditorGUILayout.VerticalScope(GUI.skin.box))
            {
                Vector4 scaleOffset = property.textureScaleAndOffset;
                Vector2 tiling = new Vector2(scaleOffset.x, scaleOffset.y);
                Vector2 offset = new Vector2(scaleOffset.z, scaleOffset.w);

                EditorGUI.BeginChangeCheck();
                using (new EditorGUI.IndentLevelScope(-EditorGUI.indentLevel))
                {
                    tiling = DrawRightAlignedVector2("Tiling", tiling);
                    offset = DrawRightAlignedVector2("Offset", offset);
                }
                if (EditorGUI.EndChangeCheck())
                {
                    materialEditor.RegisterPropertyChangeUndo("Texture Settings");
                    property.textureScaleAndOffset = new Vector4(tiling.x, tiling.y, offset.x, offset.y);
                }

                if (material != null)
                {
                    string panName = property.name + "_Pan";
                    if (material.HasProperty(panName))
                    {
                        Vector4 panValue = material.GetVector(panName);
                        Vector2 pan = new Vector2(panValue.x, panValue.y);
                        EditorGUI.BeginChangeCheck();
                        pan = DrawRightAlignedVector2("Panning", pan);
                        if (EditorGUI.EndChangeCheck())
                        {
                            materialEditor.RegisterPropertyChangeUndo("Texture Panning");
                            material.SetVector(panName, new Vector4(pan.x, pan.y, 0f, 0f));
                        }
                    }
                }
            }
        }
    }

    private void BeginSection(Stack<SectionState> sectionStack, string key, string title, MaterialProperty toggleProperty, bool suppressDraw)
    {
        bool isOpen = GetFoldoutState(key, true);

        if (suppressDraw)
        {
            sectionStack.Push(new SectionState
            {
                IsOpen = false,
                HasToggle = toggleProperty != null
            });
            return;
        }

        EditorGUILayout.Space(2);

        var headerStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeColorTexture(EditorGUIUtility.isProSkin ? HeaderColorDark : HeaderColorLight) },
            padding = new RectOffset(4, 4, 4, 4),
            margin = new RectOffset(0, 0, 0, 0)
        };

        Rect headerRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight + 8);

        Rect titleRect = new Rect(headerRect.x + 6, headerRect.y + 4, headerRect.width - 12, EditorGUIUtility.singleLineHeight);
        Rect toggleRect = Rect.zero;
        if (toggleProperty != null)
        {
            titleRect.width -= 26;
            toggleRect = new Rect(headerRect.x + headerRect.width - 24, headerRect.y + 4, 20, EditorGUIUtility.singleLineHeight);
        }

        if (Event.current.type == EventType.MouseDown && headerRect.Contains(Event.current.mousePosition))
        {
            if (toggleProperty == null || !toggleRect.Contains(Event.current.mousePosition))
            {
                isOpen = !isOpen;
                SetFoldoutState(key, isOpen);
                Event.current.Use();
            }
        }

        GUI.Box(headerRect, "", headerStyle);

        var titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 12
        };

        EditorGUI.LabelField(titleRect, title, titleStyle);

        if (toggleProperty != null)
        {
            EditorGUI.BeginChangeCheck();
            bool toggleValue = EditorGUI.Toggle(toggleRect, toggleProperty.floatValue > 0.5f);
            if (EditorGUI.EndChangeCheck())
                toggleProperty.floatValue = toggleValue ? 1.0f : 0.0f;
        }

        if (isOpen)
        {
            EditorGUILayout.Space(2);

            var contentStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeColorTexture(EditorGUIUtility.isProSkin ? ContentColorDark : ContentColorLight) },
                padding = new RectOffset(8, 8, 8, 8)
            };

            EditorGUILayout.BeginVertical(contentStyle);
            EditorGUILayout.Space(1);

            if (toggleProperty != null)
                EditorGUI.BeginDisabledGroup(toggleProperty.floatValue <= 0.5f);
        }

        sectionStack.Push(new SectionState
        {
            IsOpen = isOpen,
            HasToggle = toggleProperty != null
        });
    }

    private void EndSection(Stack<SectionState> sectionStack)
    {
        if (sectionStack.Count == 0)
            return;

        SectionState state = sectionStack.Pop();
        if (!state.IsOpen)
            return;

        if (state.HasToggle)
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(1);
        EditorGUILayout.EndVertical();
    }

    private bool GetFoldoutState(string key, bool defaultValue)
    {
        if (FoldoutStates.TryGetValue(key, out bool current))
            return current;

        FoldoutStates[key] = defaultValue;
        return defaultValue;
    }

    private void SetFoldoutState(string key, bool value)
    {
        FoldoutStates[key] = value;
    }

    private bool GetTextureSettingsState(string key, bool defaultValue)
    {
        if (TextureSettingsStates.TryGetValue(key, out bool current))
            return current;

        TextureSettingsStates[key] = defaultValue;
        return defaultValue;
    }

    private void SetTextureSettingsState(string key, bool value)
    {
        TextureSettingsStates[key] = value;
    }

    private static string GetSectionKey(string title, string propName)
    {
        return title + "::" + propName;
    }

    private static bool IsInClosedSection(Stack<SectionState> sectionStack)
    {
        foreach (SectionState state in sectionStack)
        {
            if (!state.IsOpen)
                return true;
        }

        return false;
    }

    private static Texture2D MakeColorTexture(Color color)
    {
        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private static Vector2 DrawRightAlignedVector2(string label, Vector2 value)
    {
        Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
        rect = EditorGUI.IndentedRect(rect);

        GUIStyle labelStyle = EditorStyles.label;
        float labelWidth = labelStyle.CalcSize(new GUIContent(label)).x;
        Rect labelRect = new Rect(rect.x, rect.y, labelWidth, rect.height);

        float labelPadding = 3f;
        float fieldWidth = EditorGUIUtility.fieldWidth;
        float fieldSpacing = 6f;
        float fieldsWidth = (fieldWidth * 2f) + fieldSpacing;
        float maxFieldsWidth = Mathf.Max(0f, rect.width - labelWidth - labelPadding);
        float clampedFieldsWidth = Mathf.Min(fieldsWidth, maxFieldsWidth);
        Rect fieldsRect = new Rect(labelRect.xMax + labelPadding, rect.y, clampedFieldsWidth, rect.height);

        EditorGUI.LabelField(labelRect, label);
        return EditorGUI.Vector2Field(fieldsRect, GUIContent.none, value);
    }

    private static void DrawRoundedBox(Rect rect, Color color)
    {
        const int textureSize = 16;
        const int radius = 3;

        if (textureSettingsBoxTexture == null || textureSettingsBoxColor != color)
        {
            textureSettingsBoxTexture = MakeRoundedTexture(textureSize, radius, color);
            textureSettingsBoxColor = color;
        }

        GUI.DrawTexture(rect, textureSettingsBoxTexture, ScaleMode.StretchToFill, true);
    }

    private static Texture2D MakeRoundedTexture(int size, int radius, Color color)
    {
        var texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        float r = Mathf.Max(1f, radius);
        float max = size - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Min(x, max - x);
                float dy = Mathf.Min(y, max - y);
                float dist = Mathf.Min(dx, dy);
                float alpha = dist >= r ? 1f : Mathf.InverseLerp(0f, r, dist);
                Color pixel = new Color(color.r, color.g, color.b, color.a * alpha);
                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply();
        return texture;
    }

    private static string GetHeaderTitle(Shader shader)
    {
        int propertyCount = shader.GetPropertyCount();
        for (int i = 0; i < propertyCount; i++)
        {
            string[] attributes = shader.GetPropertyAttributes(i);
            if (attributes == null)
                continue;

            foreach (string attribute in attributes)
            {
                if (TryGetAttributeArg(attribute, "KTitle", out string title) && !string.IsNullOrEmpty(title))
                    return title;
            }
        }

        return shader.name;
    }

    private static bool HasKAttribute(string[] attributes)
    {
        if (attributes == null)
            return false;

        foreach (string attribute in attributes)
        {
            if (IsAttribute(attribute, "KDivider") || IsAttribute(attribute, "KSectionEnd"))
                return true;

            if (TryGetAttributeArg(attribute, "KHeader", out _) ||
                TryGetAttributeArg(attribute, "KSubHeader", out _) ||
                TryGetAttributeArg(attribute, "KOptionName", out _) ||
                TryGetAttributeArg(attribute, "KHelp", out _) ||
                TryGetAttributeArg(attribute, "KSection", out _) ||
                TryGetAttributeArg(attribute, "KToggleSection", out _) ||
                TryGetAttributeArg(attribute, "KTitle", out _))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsAttribute(string attribute, string expectedName)
    {
        return string.Equals(attribute, expectedName, StringComparison.Ordinal);
    }

    private static bool TryGetAttributeArg(string attribute, string expectedName, out string arg)
    {
        arg = null;
        if (string.Equals(attribute, expectedName, StringComparison.Ordinal))
            return true;

        string prefix = expectedName + "(";
        if (!attribute.StartsWith(prefix, StringComparison.Ordinal) || !attribute.EndsWith(")", StringComparison.Ordinal))
            return false;

        arg = attribute.Substring(prefix.Length, attribute.Length - prefix.Length - 1).Trim();
        return true;
    }

    private static bool TrySplitArgs(string args, out string[] parts)
    {
        parts = null;
        if (string.IsNullOrEmpty(args))
            return false;

        string[] rawParts = args.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (rawParts.Length == 0)
            return false;

        for (int i = 0; i < rawParts.Length; i++)
            rawParts[i] = rawParts[i].Trim();

        parts = rawParts;
        return true;
    }

    private static MessageType ToMessageType(KHelpType type)
    {
        switch (type)
        {
            case KHelpType.Warning:
                return MessageType.Warning;
            case KHelpType.Error:
                return MessageType.Error;
            case KHelpType.None:
                return MessageType.None;
            default:
                return MessageType.Info;
        }
    }
}
