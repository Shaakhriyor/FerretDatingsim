#if UNITY_EDITOR
using System;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// Updates only the existing pause-menu artwork and its three main buttons.
public static class VNHeartMenuArtUpgrade
{
    private const string ArtFolder = "Assets/HeartMenuArt";
    private static readonly Color Ink = new Color32(24, 15, 22, 255);
    private static readonly Color Paper = new Color32(255, 248, 234, 255);
    private static readonly Color Pink = new Color32(247, 55, 117, 255);

    [MenuItem("Tools/VN/Apply Illustrated Heart Style")]
    private static void Apply()
    {
        if (Application.isPlaying) { Debug.LogWarning("Stop Play mode first."); return; }
        var menu = UnityEngine.Object.FindFirstObjectByType<VNHeartMenu>(FindObjectsInactive.Include);
        if (menu == null) { Debug.LogError("Open the scene containing your existing VNHeartMenuCanvas first."); return; }
        var data = new SerializedObject(menu);
        var main = data.FindProperty("mainPage").objectReferenceValue as GameObject;
        var resume = data.FindProperty("resumeButton").objectReferenceValue as UnityEngine.UI.Button;
        var save = MainButton(data, "saveButtons", main);
        var load = MainButton(data, "loadButtons", main);
        if (main == null || resume == null || save == null || load == null)
        { Debug.LogError("The existing heart menu is missing its main-page button references. No scene changes made."); return; }
        foreach (var button in new[] { resume, save, load })
            if (button.GetComponentInChildren<TMP_Text>(true) == null)
            { Debug.LogError("A menu button is missing its text label. No scene changes made."); return; }

        Sprite backdrop;
        TMP_FontAsset font;
        try { backdrop = ImportArt(); font = CreateDisplayFont(); }
        catch (Exception error) { Debug.LogError(error.Message); return; }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply illustrated heart style");
        Undo.RegisterFullObjectHierarchyUndo(menu.gameObject, "Restyle heart menu");

        // Keep the controller, its button instances, and all save/load references intact.
        foreach (Transform child in main.transform)
        {
            if (child == resume.transform || child == save.transform || child == load.transform) continue;
            if (child.GetComponent<UnityEngine.UI.Button>() != null) continue;
            if (child.name == "IllustratedHeartBackdrop" || child.name == "ArtPauseCaption" || child.name == "ArtFooter") continue;
            child.gameObject.SetActive(false);
        }
        var background = Child("IllustratedHeartBackdrop", main.transform);
        background.gameObject.SetActive(true);
        Stretch(background);
        background.SetAsFirstSibling();
        var image = background.GetComponent<UnityEngine.UI.Image>();
        if (image == null) image = Undo.AddComponent<UnityEngine.UI.Image>(background.gameObject);
        image.sprite = backdrop;
        image.color = Color.white;
        image.type = UnityEngine.UI.Image.Type.Simple;
        image.preserveAspect = false;
        image.raycastTarget = false;

        // Large serif words become the controls themselves, with no rectangular cards.
        StyleButton(resume, font, "RESUME", -450, 285, 920, 245, 12, 209, Ink, Paper);
        StyleButton(save, font, "SAVE", -555, 5, 710, 270, 12, 269, Paper, Ink);
        StyleButton(load, font, "LOAD", -415, -277, 790, 275, 12, 270, Paper, Ink);

        var caption = Label(Child("ArtPauseCaption", main.transform), font, "P A U S E D", Ink, 29);
        Place(caption.rectTransform, -668, 450, 450, 52, 0);
        caption.alignment = TextAlignmentOptions.Left;
        var footer = Label(Child("ArtFooter", main.transform), font, "ESC  /  RETURN TO YOUR STORY", Paper, 23);
        Place(footer.rectTransform, -540, -480, 780, 42, 0);
        footer.alignment = TextAlignmentOptions.Left;

        // Keyboard navigation follows the visible diagonal order.
        Navigation(resume, load, save);
        Navigation(save, resume, load);
        Navigation(load, save, resume);
        EditorUtility.SetDirty(menu);
        EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);
        AssetDatabase.SaveAssets();
        Undo.CollapseUndoOperations(undoGroup);
        Selection.activeGameObject = menu.gameObject;
        Debug.Log("Illustrated heart style applied. Save the scene and press Play, then Escape. Your existing Save / Load pages remain connected.", menu);
    }

    private static UnityEngine.UI.Button MainButton(SerializedObject menu, string name, GameObject main)
    {
        if (main == null) return null;
        var array = menu.FindProperty(name);
        for (int i = 0; i < array.arraySize; i++)
        {
            var button = array.GetArrayElementAtIndex(i).objectReferenceValue as UnityEngine.UI.Button;
            if (button != null && button.transform.IsChildOf(main.transform)) return button;
        }
        return null;
    }

    private static Sprite ImportArt()
    {
        string path = ArtFolder + "/HeartMenuBackdrop.png";
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Copy the package's HeartMenuArt folder directly into Assets first.");
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) throw new InvalidOperationException("The heart artwork could not be imported as a Sprite.");
        return sprite;
    }

    private static TMP_FontAsset CreateDisplayFont()
    {
        string path = ArtFolder + "/HeartDisplayFont.asset";
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        if (existing != null) return existing;
        if (AssetDatabase.LoadMainAssetAtPath(path) != null) throw new InvalidOperationException("Another asset occupies " + path);
        var source = AssetDatabase.LoadAssetAtPath<Font>(ArtFolder + "/LibreBodoni-Bold.ttf");
        if (source == null) throw new InvalidOperationException("The package's LibreBodoni-Bold.ttf file is missing from Assets/HeartMenuArt.");
        var font = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDF16, 1024, 1024, AtlasPopulationMode.Dynamic);
        if (font == null) throw new InvalidOperationException("Unity could not create the display font.");
        font.name = "Heart Display Libre Bodoni";
        var characters = new StringBuilder();
        for (char c = (char)32; c <= 126; c++) characters.Append(c);
        if (!font.TryAddCharacters(characters.ToString(), out string missing))
            throw new InvalidOperationException("The display-font atlas could not include: " + missing);
        font.atlasPopulationMode = AtlasPopulationMode.Static;
        AssetDatabase.CreateAsset(font, path);
        foreach (var texture in font.atlasTextures)
        {
            texture.name = "Heart Display Atlas";
            AssetDatabase.AddObjectToAsset(texture, font);
        }
        font.material.name = "Heart Display Material";
        AssetDatabase.AddObjectToAsset(font.material, font);
        EditorUtility.SetDirty(font);
        AssetDatabase.SaveAssets();
        return font;
    }

    private static void StyleButton(UnityEngine.UI.Button button, TMP_FontAsset font, string word,
        float x, float y, float w, float h, float angle, float size, Color normal, Color shadowColor)
    {
        var rect = (RectTransform)button.transform;
        Place(rect, x, y, w, h, angle);
        // Unity allows only one Graphic per GameObject, even if it is disabled.
        // Reuse VNMenuShape (or another existing Graphic) as the invisible hit area.
        var hitArea = button.GetComponent<UnityEngine.UI.Graphic>();
        if (hitArea == null)
            hitArea = Undo.AddComponent<UnityEngine.UI.Image>(button.gameObject);
        hitArea.enabled = true;
        hitArea.color = Color.clear;
        hitArea.raycastTarget = true;
        var original = button.transform.Find("Label");
        var text = original != null ? original.GetComponent<TMP_Text>() : null;
        if (text == null) text = button.GetComponentInChildren<TMP_Text>(true);
        text.gameObject.SetActive(true);
        text.font = font;
        text.fontStyle = FontStyles.Normal;
        text.fontSize = size;
        text.enableAutoSizing = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.characterSpacing = -4;
        text.alignment = TextAlignmentOptions.Center;
        text.margin = Vector4.zero;
        text.text = word;
        text.color = Color.white;
        text.raycastTarget = true;
        Place(text.rectTransform, 0, 0, w, h, 0);
        // Condense only the letters; preserve the full, easy-to-click button region.
        text.rectTransform.localScale = new Vector3(0.86f, 1.12f, 1);
        var shadow = Label(Child("ArtWordShadow", button.transform), font, word, shadowColor, size);
        Place(shadow.rectTransform, 6, -7, w, h, 0);
        shadow.rectTransform.localScale = text.rectTransform.localScale;
        shadow.characterSpacing = text.characterSpacing;
        shadow.transform.SetAsFirstSibling();
        text.transform.SetAsLastSibling();
        button.targetGraphic = text;
        button.transition = UnityEngine.UI.Selectable.Transition.ColorTint;
        var colors = button.colors;
        colors.normalColor = normal;
        colors.highlightedColor = Pink;
        colors.selectedColor = Pink;
        colors.pressedColor = new Color32(164, 17, 65, 255);
        colors.disabledColor = Color.gray;
        colors.colorMultiplier = 1;
        colors.fadeDuration = 0.13f;
        button.colors = colors;
        text.canvasRenderer.SetColor(normal);
        EditorUtility.SetDirty(button);
        EditorUtility.SetDirty(text);
    }

    private static TMP_Text Label(RectTransform rect, TMP_FontAsset font, string value, Color color, float size)
    {
        rect.gameObject.SetActive(true);
        var text = rect.GetComponent<TextMeshProUGUI>();
        if (text == null) text = Undo.AddComponent<TextMeshProUGUI>(rect.gameObject);
        text.font = font; text.text = value; text.color = color; text.fontSize = size;
        text.fontStyle = FontStyles.Normal;
        text.enableAutoSizing = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform Child(string name, Transform parent)
    {
        var existing = parent.Find(name) as RectTransform;
        if (existing != null) return existing;
        var child = new GameObject(name, typeof(RectTransform));
        child.layer = parent.gameObject.layer;
        Undo.RegisterCreatedObjectUndo(child, "Add heart artwork element");
        Undo.SetTransformParent(child.transform, parent, "Parent heart artwork element");
        return (RectTransform)child.transform;
    }
    private static void Place(RectTransform rect, float x, float y, float w, float h, float angle)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(w, h); rect.anchoredPosition3D = new Vector3(x, y, 0);
        rect.localScale = Vector3.one; rect.localRotation = Quaternion.Euler(0, 0, angle);
    }
    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one; rect.localRotation = Quaternion.identity;
        var position = rect.localPosition; position.z = 0; rect.localPosition = position;
    }
    private static void Navigation(UnityEngine.UI.Button button, UnityEngine.UI.Button up, UnityEngine.UI.Button down)
    {
        var navigation = button.navigation;
        navigation.mode = UnityEngine.UI.Navigation.Mode.Explicit;
        navigation.selectOnUp = up; navigation.selectOnDown = down;
        navigation.selectOnLeft = up; navigation.selectOnRight = down;
        button.navigation = navigation;
    }
}
#endif
