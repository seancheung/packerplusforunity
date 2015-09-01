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
        private UndoObject undo;
        private Texture2D import;
        private int selected;

        private void OnEnable()
        {
            undo = new UndoObject(serializedObject.targetObject);
        }

        [MenuItem(("Assets/Create/Atlas"))]
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
            import = EditorGUILayout.ObjectField(import, typeof (Texture2D), false) as Texture2D;

            if (import == null)
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
                texture.FindPropertyRelative("texture").objectReferenceValue = import;
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
                undo.Clear();
            }
        }

        private void DrawTexture()
        {
            var textures = serializedObject.FindProperty("textures");
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
            selected = GUILayout.SelectionGrid(selected, names, 2);

            var sprite = sprites.GetArrayElementAtIndex(selected);
            var textures = serializedObject.FindProperty("textures");
            int section = sprite.FindPropertyRelative("section").intValue;
            if (textures.arraySize <= section ||
                !textures.GetArrayElementAtIndex(section).FindPropertyRelative("texture").objectReferenceValue)
            {
                EditorGUILayout.HelpBox("Texture Missing!", MessageType.Error);
                return;
            }
            var texture = textures.GetArrayElementAtIndex(section);
            var width = sprite.FindPropertyRelative("sourceRect").rectValue.width;
            var height = sprite.FindPropertyRelative("sourceRect").rectValue.height;
            Rect rect = EditorGUILayout.GetControlRect(false, height);
            if (rect.width > width)
            {
                rect.x = rect.width/2 - width/2f;
                rect.width = width;
            }
            else
            {
                rect.height = rect.width/width*height;
            }

            GUI.DrawTextureWithTexCoords(rect, texture.FindPropertyRelative("texture").objectReferenceValue as Texture2D,
                sprite.FindPropertyRelative("uvRect").rectValue);
            EditorGUILayout.Separator();

            //draw border preview
            var border = sprite.FindPropertyRelative("border").vector4Value;
            Handles.color = new Color32(124, 244, 255, 255);
            //top
            if (border.w > 0)
                Handles.DrawLine(new Vector3(rect.xMin, rect.yMin + border.w),
                    new Vector3(rect.xMax, rect.yMin + border.w));
            //left
            if (border.x > 0)
                Handles.DrawLine(new Vector3(rect.xMin + border.x, rect.yMin),
                    new Vector3(rect.xMin + border.x, rect.yMax));
            //bottom
            if (border.y > 0)
                Handles.DrawLine(new Vector3(rect.xMin, rect.yMax - border.y),
                    new Vector3(rect.xMax, rect.yMax - border.y));
            //right
            if (border.z > 0)
                Handles.DrawLine(new Vector3(rect.xMax - border.z, rect.yMin),
                    new Vector3(rect.xMax - border.z, rect.yMax));
            GUILayout.Space(2);

            GUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.BeginHorizontal();
                {
                    using (undo.CheckChange())
                    {
                        var newName = EditorGUILayout.TextField("Sprite Name", sprite.FindPropertyRelative("name").stringValue,
                            GUILayout.ExpandWidth(true));
                        if (undo.TryRecord("Set sprite name"))
                        {
                            sprite.FindPropertyRelative("name").stringValue = UniqueName(newName, names);
                        }
                    }
                    EditorGUI.BeginChangeCheck();
                    if (GUILayout.Button("X", EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                    {
                        //RemoveSprite(sprite);
                    }
                }
                ;
                GUILayout.EndHorizontal();
                using (undo.CheckChange())
                {
                    var sourceRect = sprite.FindPropertyRelative("sourceRect").rectValue;
                    var x = EditorGUILayout.IntSlider("Left", (int) border.x, 0,
                        (int) sourceRect.width);
                    var z = EditorGUILayout.IntSlider("Right", (int) border.z, 0,
                        (int) sourceRect.width);
                    var w = EditorGUILayout.IntSlider("Top", (int) border.w, 0,
                        (int) sourceRect.height);
                    var y = EditorGUILayout.IntSlider("Bottom", (int) border.y, 0,
                        (int) sourceRect.height);
                    if (undo.TryRecord("Set sprite border"))
                    {
                        sprite.FindPropertyRelative("border").vector4Value = new Vector4(x, y, z, w);
                    }
                }

                GUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Copy", EditorStyles.miniButtonLeft))
                    {
                        EditorGUIUtility.systemCopyBuffer =
                            sprite.FindPropertyRelative("border").vector4Value.ToString();
                    }
                    if (GUILayout.Button("Paste", EditorStyles.miniButtonRight))
                    {
                        if (!string.IsNullOrEmpty(EditorGUIUtility.systemCopyBuffer) &&
                            Regex.IsMatch(EditorGUIUtility.systemCopyBuffer,
                                @"^\(\d+\.0,\s*\d+\.0,\s*\d+\.0,\s*\d+\.0\)$"))
                        {
                            string[] data = EditorGUIUtility.systemCopyBuffer.Trim('(', ')').Split(',');
                            using (undo.Record("Paste sprite border"))
                            {
                                sprite.FindPropertyRelative("border").vector4Value =
                                    new Vector4(float.Parse(data[0].Trim()), float.Parse(data[1].Trim()),
                                        float.Parse(data[2].Trim()), float.Parse(data[3].Trim()));
                            }
                        }
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(2);
                EditorGUILayout.PropertyField(sprite.FindPropertyRelative("padding"));
            }
            GUILayout.EndVertical();
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
                EditorWindow.GetWindow<PackerWindow>().atlas = serializedObject.targetObject as AtlasPlus;
            }
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