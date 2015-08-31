using System;
using UnityEditor;
using UnityEngine;

public class ImageGenerator : ScriptableWizard
{
    [SerializeField] private Color32 color = Color.white;
    [SerializeField] private PackerWrapper.Format format = PackerWrapper.Format.PNG;
    [SerializeField] private PackerWrapper.ColorDepth depth = PackerWrapper.ColorDepth.TrueColor;
    [Range(4, 4096)] [SerializeField] private int width = 32;
    [Range(4, 4096)] [SerializeField] private int height = 32;
    [SerializeField] private string filename = "name";

    [MenuItem("Assets/ImageGenerator")]
    private static void CreateWizard()
    {
        DisplayWizard<ImageGenerator>("Create Image", "Create");
    }

    private void OnWizardCreate()
    {
        PackerWrapper.Create(width, height, "Assets/" + filename + "." + format, depth, format, color);
        AssetDatabase.Refresh();
    }

    private void OnWizardUpdate()
    {
        errorString = string.IsNullOrEmpty(filename) ? "Please set the file name!" : String.Empty;
    }
}