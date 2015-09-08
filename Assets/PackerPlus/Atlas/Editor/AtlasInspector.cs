using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MiniJSON;
using UnityEditor;
using UnityEngine;

namespace Ultralpha.Editor
{
    [CustomEditor((typeof (AtlasPlus)))]
    public class AtlasInspector : UnityEditor.Editor
    {
        private UndoObject _undo;
        private Texture2D _import;
        private int _selected;
        private Vector2 _hscroll;
        private Vector2 _vscroll;
        private SerializedProperty _previewTexture;
        private SerializedProperty _previewSprite;
        private static Texture2D _borderTexture;
        private static Texture2D _backgroundTexture;

        private static Texture2D BorderTexture
        {
            get
            {
                return _borderTexture ?? (_borderTexture = CreateCheckerTex(
                    new Color(0f, 0.0f, 0f, 0.5f),
                    new Color(1f, 1f, 1f, 0.5f)));
            }
        }

        private static Texture2D BackgroundTexture
        {
            get
            {
                return _backgroundTexture ?? (_backgroundTexture = CreateCheckerTex(
                    new Color(0f, 0.0f, 0f, 0.1f),
                    new Color(1f, 1f, 1f, 0.1f)));
            }
        }

        private void OnEnable()
        {
            _undo = new UndoObject(serializedObject.targetObject);
        }

        [MenuItem(("Assets/Create/Atlas Plus"))]
        private static void CreateAtlas()
        {
            AtlasPlus atlas = CreateInstance<AtlasPlus>();
            string path = EditorUtility.SaveFilePanelInProject("Create Atlas", "atlas", "asset", "Save atlas asset");
            if (!string.IsNullOrEmpty(path))
                AssetDatabase.CreateAsset(atlas, path);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            var mode = GUILayout.Toolbar(EditorPrefs.GetInt("atlas_editor_mode"),
                Enum.GetNames(typeof (Mode)));
            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetInt("atlas_editor_mode", mode);
            }
            EditorGUILayout.Separator();

            switch ((Mode) mode)
            {
                case Mode.Sprites:
                    DrawTexture();
                    DrawSprites();
                    break;
                case Mode.Packer:
                    DrawTexture();
                    DrawPack();
                    break;
                case Mode.Importer:
                    DrawTexture();
                    DrawImporter();
                    break;
                case Mode.Debug:
                    GUI.enabled = false;
                    DrawDefaultInspector();
                    GUI.enabled = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawImporter()
        {
            _import = EditorGUILayout.ObjectField(_import, typeof (Texture2D), false) as Texture2D;

            if (_import == null)
            {
                EditorGUILayout.HelpBox("Texture should be added first", MessageType.Info);
                return;
            }

            EditorGUI.BeginChangeCheck();
            var asset = EditorGUILayout.ObjectField(null, typeof (TextAsset), false) as TextAsset;
            if (EditorGUI.EndChangeCheck() && asset)
            {
                ImportTP(asset.text);
            }
        }

        private void ImportTP(string json)
        {
            var jd = Json.Deserialize(json) as IDictionary;
            if (jd != null)
            {
                var meta = (IDictionary) jd["meta"];
                var size = (IDictionary) meta["size"];
                var width = (long) size["w"];
                var height = (long) size["h"];
                var frames = (Dictionary<string, object>) jd["frames"];
                List<SpriteInfo> sprites = new List<SpriteInfo>();
                foreach (KeyValuePair<string, object> frame in frames)
                {
                    var sprite = new SpriteInfo();
                    sprite.name = frame.Key;
                    var info = (IDictionary) frame.Value;
                    var pos = (IDictionary) info["frame"];
                    var x = (long) pos["x"];
                    var y = (long) pos["y"];
                    var w = (int) (long) pos["w"];
                    var h = (int) (long) pos["h"];
                    sprite.sourceRect = new Rect(x, height - y, w, h);
                    sprite.uvRect = Rect.MinMaxRect(1f*x/width, 1 - 1f*(y + h)/height,
                        sprite.sourceRect.xMax/width,
                        1 - 1f*y/height);
                    var sourceSize = (IDictionary) info["sourceSize"];
                    var spriteSourceSize = (IDictionary) info["spriteSourceSize"];
                    sprite.padding = new Vector4((long) spriteSourceSize["x"],
                        (long) sourceSize["h"] - (long) spriteSourceSize["y"] - (long) spriteSourceSize["h"],
                        (long) sourceSize["w"] - (long) spriteSourceSize["x"] - (long) spriteSourceSize["w"],
                        (long) spriteSourceSize["y"]);
                    sprites.Add(sprite);
                }

                var textures = serializedObject.FindProperty("textures");
                textures.ClearArray();
                textures.InsertArrayElementAtIndex(0);
                var texture = textures.GetArrayElementAtIndex(0);
                texture.FindPropertyRelative("texture").objectReferenceValue = _import;
                texture.FindPropertyRelative("width").intValue = (int) width;
                texture.FindPropertyRelative("height").intValue = (int) height;
                var sps = serializedObject.FindProperty("sprites");
                sps.ClearArray();
                for (int i = 0; i < sprites.Count; i++)
                {
                    sps.InsertArrayElementAtIndex(i);
                    var sp = sps.GetArrayElementAtIndex(i);
                    sp.FindPropertyRelative("name").stringValue = sprites[i].name;
                    sp.FindPropertyRelative("uvRect").rectValue = sprites[i].uvRect;
                    sp.FindPropertyRelative("sourceRect").rectValue = sprites[i].sourceRect;
                    sp.FindPropertyRelative("padding").vector4Value = sprites[i].padding;
                }
                EditorUtility.SetDirty(serializedObject.targetObject);
                Debug.Log("Imported " + sprites.Count + " sprite(s)");
                _undo.Clear();
            }
        }

        private void DrawTexture()
        {
            var textures = serializedObject.FindProperty("textures");
            if (textures.arraySize > 0)
            {
                _hscroll = GUILayout.BeginScrollView(_hscroll, GUILayout.Height(85));
                {
                    GUILayout.BeginHorizontal();
                    {
                        for (int i = 0; i < textures.arraySize; i++)
                        {
                            var texture = textures.GetArrayElementAtIndex(i);
                            var rect = EditorGUILayout.GetControlRect(false, 80, GUILayout.Width(80));
                            EditorGUI.DrawTextureTransparent(rect,
                                texture.FindPropertyRelative("texture").objectReferenceValue as Texture,
                                ScaleMode.ScaleToFit);
                        }
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndScrollView();
            }
            EditorGUILayout.Separator();
        }

        private void DrawSprites()
        {
            var sprites = serializedObject.FindProperty("sprites");
            if (sprites.arraySize == 0)
                return;
            string[] names = new string[sprites.arraySize];
            for (int i = 0; i < sprites.arraySize; i++)
            {
                names[i] = sprites.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue;
            }

            GUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.Space(2);
                _vscroll = GUILayout.BeginScrollView(_vscroll);
                {
                    _selected = GUILayout.SelectionGrid(_selected, names, 2);
                }
                GUILayout.EndScrollView();
                GUILayout.Space(2);
            }
            GUILayout.EndVertical();
            GUILayout.Space(4);

            _previewSprite = sprites.GetArrayElementAtIndex(_selected);
            var textures = serializedObject.FindProperty("textures");
            int section = _previewSprite.FindPropertyRelative("section").intValue;
            if (textures.arraySize <= section ||
                !textures.GetArrayElementAtIndex(section).FindPropertyRelative("texture").objectReferenceValue)
            {
                EditorGUILayout.HelpBox("Texture Missing!", MessageType.Error);
                _previewTexture = null;
                return;
            }
            _previewTexture = textures.GetArrayElementAtIndex(section);

            //draw border preview
            var border = _previewSprite.FindPropertyRelative("border").vector4Value;

            GUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.BeginHorizontal();
                {
                    using (_undo.CheckChange())
                    {
                        var newName = EditorGUILayout.TextField("Sprite Name",
                            _previewSprite.FindPropertyRelative("name").stringValue,
                            GUILayout.ExpandWidth(true));
                        if (_undo.TryRecord("Set sprite name"))
                        {
                            _previewSprite.FindPropertyRelative("name").stringValue = UniqueName(newName, names);
                        }
                    }
                    EditorGUI.BeginChangeCheck();
                    if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                    {
                        //RemoveSprite(sprite);
                    }
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(4);

                using (_undo.CheckChange())
                {
                    var sourceRect = _previewSprite.FindPropertyRelative("sourceRect").rectValue;
                    var x = EditorGUILayout.IntSlider("Left", (int) border.x, 0,
                        (int) sourceRect.width);
                    var z = EditorGUILayout.IntSlider("Right", (int) border.z, 0,
                        (int) sourceRect.width);
                    var w = EditorGUILayout.IntSlider("Top", (int) border.w, 0,
                        (int) sourceRect.height);
                    var y = EditorGUILayout.IntSlider("Bottom", (int) border.y, 0,
                        (int) sourceRect.height);
                    if (_undo.TryRecord("Set sprite border"))
                    {
                        _previewSprite.FindPropertyRelative("border").vector4Value = new Vector4(x, y, z, w);
                    }
                }

                GUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Copy", EditorStyles.miniButtonLeft))
                    {
                        EditorGUIUtility.systemCopyBuffer =
                            _previewSprite.FindPropertyRelative("border").vector4Value.ToString();
                    }
                    if (GUILayout.Button("Paste", EditorStyles.miniButtonRight))
                    {
                        if (!string.IsNullOrEmpty(EditorGUIUtility.systemCopyBuffer) &&
                            Regex.IsMatch(EditorGUIUtility.systemCopyBuffer,
                                @"^\(\d+\.0,\s*\d+\.0,\s*\d+\.0,\s*\d+\.0\)$"))
                        {
                            string[] data = EditorGUIUtility.systemCopyBuffer.Trim('(', ')').Split(',');
                            using (_undo.Record("Paste sprite border"))
                            {
                                _previewSprite.FindPropertyRelative("border").vector4Value =
                                    new Vector4(float.Parse(data[0].Trim()), float.Parse(data[1].Trim()),
                                        float.Parse(data[2].Trim()), float.Parse(data[3].Trim()));
                            }
                        }
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(2);
                EditorGUILayout.Vector2Field("Pivot", _previewSprite.FindPropertyRelative("pivot").vector2Value);

                GUILayout.Space(2);
                EditorGUILayout.Vector4Field("Padding", _previewSprite.FindPropertyRelative("padding").vector4Value);
            }
            GUILayout.EndVertical();
        }

        public override void DrawPreview(Rect rect)
        {
            var center = rect.center;
            var width = _previewSprite.FindPropertyRelative("sourceRect").rectValue.width;
            var height = _previewSprite.FindPropertyRelative("sourceRect").rectValue.height;
            var w = width/rect.width;
            var h = height/rect.height;
            if (w < h)
            {
                rect.width = width/height*rect.height;
            }
            else if (w > h)
            {
                rect.height = height/width*rect.width;
            }
            rect.width = Mathf.Min(rect.width, width);
            rect.height = Mathf.Min(rect.height, height);
            rect.center = center;

            rect.x = Mathf.RoundToInt(rect.x);
            rect.y = Mathf.RoundToInt(rect.y);
            rect.width = Mathf.RoundToInt(rect.width);
            rect.height = Mathf.RoundToInt(rect.height);

            //bg
            DrawTiledTexture(rect, BackgroundTexture);

            //texture
            GUI.DrawTextureWithTexCoords(rect,
                _previewTexture.FindPropertyRelative("texture").objectReferenceValue as Texture2D,
                _previewSprite.FindPropertyRelative("uvRect").rectValue);

            //draw border preview
            var border = _previewSprite.FindPropertyRelative("border").vector4Value;
            //top
            if (border.w > 0)
                DrawTiledTexture(new Rect(rect.xMin, rect.yMin + border.w, rect.width, 1), BorderTexture);
            //left
            if (border.x > 0)
                DrawTiledTexture(new Rect(rect.xMin + border.x, rect.yMin, 1, rect.height), BorderTexture);
            //bottom
            if (border.y > 0)
                DrawTiledTexture(new Rect(rect.xMin, rect.yMax - border.y, rect.width, 1), BorderTexture);
            //right
            if (border.z > 0)
                DrawTiledTexture(new Rect(rect.xMax - border.z, rect.yMin, 1, rect.height), BorderTexture);

            //outline
            Handles.color = Color.black;
            Handles.DrawPolyLine(new Vector3(rect.xMin, rect.yMin), new Vector3(rect.xMax, rect.yMin),
                new Vector3(rect.xMax, rect.yMax), new Vector3(rect.xMin, rect.yMax), new Vector3(rect.xMin, rect.yMin));

            //info
            var source = _previewSprite.FindPropertyRelative("sourceRect").rectValue;
            EditorGUI.DropShadowLabel(EditorGUILayout.GetControlRect(), "Source: " + source);
        }

        public override bool HasPreviewGUI()
        {
            return _previewSprite != null && _previewTexture != null;
        }

        public override GUIContent GetPreviewTitle()
        {
            return new GUIContent(_previewSprite.FindPropertyRelative("name").stringValue);
        }

        private static string UniqueName(string original, string[] names)
        {
            int suffix = 1;
            string uniqueName = original;
            while (names.Contains(uniqueName))
            {
                uniqueName = original + "_" + suffix++;
            }
            return uniqueName;
        }

        private void DrawPack()
        {
            if (GUILayout.Button("Pack"))
            {
                ScriptableWizard.DisplayWizard<PackerWindow>("Packer", "Repack").atlas =
                    serializedObject.targetObject as AtlasPlus;
            }
        }

        private static Texture2D CreateCheckerTex(Color c0, Color c1)
        {
            Texture2D tex = new Texture2D(16, 16);
            tex.name = "[Generated] Checker Texture";
            tex.hideFlags = HideFlags.DontSave;

            for (int y = 0; y < 8; ++y) for (int x = 0; x < 8; ++x) tex.SetPixel(x, y, c1);
            for (int y = 8; y < 16; ++y) for (int x = 0; x < 8; ++x) tex.SetPixel(x, y, c0);
            for (int y = 0; y < 8; ++y) for (int x = 8; x < 16; ++x) tex.SetPixel(x, y, c0);
            for (int y = 8; y < 16; ++y) for (int x = 8; x < 16; ++x) tex.SetPixel(x, y, c1);

            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return tex;
        }

        private static void DrawTiledTexture(Rect rect, Texture tex)
        {
            GUI.BeginGroup(rect);
            {
                int width = Mathf.RoundToInt(rect.width);
                int height = Mathf.RoundToInt(rect.height);

                for (int y = 0; y < height; y += tex.height)
                {
                    for (int x = 0; x < width; x += tex.width)
                    {
                        GUI.DrawTexture(new Rect(x, y, tex.width, tex.height), tex);
                    }
                }
            }
            GUI.EndGroup();
        }

        private enum Mode
        {
            Sprites,
            Packer,
            Importer,
            Debug
        }
    }
}