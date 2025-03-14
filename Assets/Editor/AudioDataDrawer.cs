using System.Linq;
using UnityEditor;
using UnityEngine;

public class AudioDataDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty clipProp = property.FindPropertyRelative("clip");
        SerializedProperty selectedIndexProp = property.FindPropertyRelative("selectedClipIndex");
        SerializedProperty nameProp = property.FindPropertyRelative("name");

        string[] clipNames = GetAllAudioClipNames();
        int selectedIndex = selectedIndexProp.intValue;

        if (clipNames.Length > 0)
        {
            selectedIndex = EditorGUI.Popup(
                new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
                "Audio Clip",
                selectedIndex,
                clipNames
            );

            selectedIndexProp.intValue = selectedIndex;
            nameProp.stringValue = clipNames[selectedIndex];

            // Assign the actual AudioClip asset based on the selection
            clipProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Audio/" + clipNames[selectedIndex] + ".wav" // Adjust based on your file format
            );
        }
        else
        {
            EditorGUI.LabelField(position, "No Audio Clips Found");
        }

        EditorGUI.EndProperty();
    }

    private string[] GetAllAudioClipNames()
    {
        string[] guids = AssetDatabase.FindAssets("t:AudioClip");
        return guids.Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                    .Select(path => System.IO.Path.GetFileNameWithoutExtension(path))
                    .ToArray();
    }
}
