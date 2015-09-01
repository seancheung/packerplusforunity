//using UnityEditor;
//using UnityEngine;

//[CustomPropertyDrawer(typeof (TextureInfo))]
//public class TextureInfoDrawer : PropertyDrawer
//{
//    private static GUIContent _w = new GUIContent("W");
//    private static GUIContent _h = new GUIContent("H");

//    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
//    {
//        EditorGUI.BeginProperty(position, label, property);

//        position.height = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("texture"));
//        EditorGUI.PropertyField(position, property.FindPropertyRelative("texture"), GUIContent.none);
//        position.y += position.height;
//        position.height = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("width"));
//        position = EditorGUI.IndentedRect(position);
//        position.width /= 2;
//        EditorGUI.PropertyField(position, property.FindPropertyRelative("width"), GUIContent.none);
//        position.x += position.width;
//        EditorGUI.PropertyField(position, property.FindPropertyRelative("height"), GUIContent.none);

//        EditorGUI.EndProperty();
//    }

//    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
//    {
//        return EditorGUI.GetPropertyHeight(property.FindPropertyRelative("texture")) +
//               EditorGUI.GetPropertyHeight(property.FindPropertyRelative("width"));
//    }
//}