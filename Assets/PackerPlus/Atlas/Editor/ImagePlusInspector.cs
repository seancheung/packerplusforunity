using System;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditor.UI;
using UnityEngine;

namespace Ultralpha.Editor
{
    [CustomEditor(typeof (ImagePlus))]
    [CanEditMultipleObjects]
    public class ImagePlusInspector : ImageEditor
    {
        private SerializedProperty _atlas;
        private SerializedProperty _spriteName;
        private string[] _spriteNames;
        private int _selected;
        private AnimBool showSpriteList;

        [MenuItem("GameObject/UI/Image Plus #&a")]
        private static void Create()
        {
            var image = new GameObject("Image Plus", typeof (ImagePlus));
            if (Selection.activeGameObject)
            {
                GameObjectUtility.SetParentAndAlign(image, Selection.activeGameObject);
            }
            Selection.activeGameObject = image;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _atlas = serializedObject.FindProperty("_atlas");
            _spriteName = serializedObject.FindProperty("_spriteName");
            if (_atlas.objectReferenceValue)
            {
                var sero = new SerializedObject(_atlas.objectReferenceValue);
                var sprites = sero.FindProperty("sprites");
                _spriteNames = new string[sprites.arraySize];
                for (int i = 0; i < _spriteNames.Length; i++)
                {
                    _spriteNames[i] = sprites.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue;
                }
                _selected = Mathf.Max(0, Array.IndexOf(_spriteNames, _spriteName.stringValue));
            }
            showSpriteList = new AnimBool(_atlas.objectReferenceValue && _spriteNames != null);
            showSpriteList.valueChanged.AddListener(Repaint);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_atlas);
            if (EditorGUI.EndChangeCheck() && _atlas.objectReferenceValue)
            {
                var sero = new SerializedObject(_atlas.objectReferenceValue);
                var sprites = sero.FindProperty("sprites");
                _spriteNames = new string[sprites.arraySize];
                for (int i = 0; i < _spriteNames.Length; i++)
                {
                    _spriteNames[i] = sprites.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue;
                }
                _selected = 0;
                RefreshSprite();
            }

            showSpriteList.target = _atlas.objectReferenceValue && _spriteNames != null;
            if (EditorGUILayout.BeginFadeGroup(showSpriteList.faded))
            {
                EditorGUI.BeginChangeCheck();
                _selected = EditorGUILayout.Popup("Sprite", _selected, _spriteNames);
                if (EditorGUI.EndChangeCheck())
                {
                    RefreshSprite();
                }
            }
            EditorGUILayout.EndFadeGroup();
            serializedObject.ApplyModifiedProperties();
            if (_atlas.objectReferenceValue)
                base.OnInspectorGUI();
        }

        private void RefreshSprite()
        {
            _spriteName.stringValue = _selected < _spriteNames.Length
                ? _spriteNames[_selected]
                : String.Empty;
            var atlas = _atlas.objectReferenceValue as AtlasPlus;
            if (atlas)
                serializedObject.FindProperty("m_Sprite").objectReferenceValue = atlas[_spriteName.stringValue];
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            showSpriteList.valueChanged.RemoveListener(Repaint);
        }
    }
}