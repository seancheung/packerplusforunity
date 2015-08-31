using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class PackerWindow : ScriptableWizard
{
    [SerializeField] private PackerWrapper.Format format = PackerWrapper.Format.PNG;
    [SerializeField] private PackerWrapper.ColorDepth depth = PackerWrapper.ColorDepth.TrueColor;
    [Range(128, 4096)] [SerializeField] private int width = 1024;
    [Range(128, 4096)] [SerializeField] private int height = 1024;
    [SerializeField] private bool crop = true;
    [SerializeField] private PackerWrapper.Algorithm algorithm;
    [SerializeField] private AtlasPlus atlas;
    [SerializeField] private Texture2D[] textures;

    [MenuItem("Window/PackerPlus")]
    private static void CreateWizard()
    {
        DisplayWizard<PackerWindow>("Packer", "Pack", "Create Atlas");
    }

    private void OnWizardCreate()
    {
        PackerWrapper.Pack(textures, atlas, width, height, depth, format,
            new PackerWrapper.Options {algorithm = algorithm, crop = crop});
        EditorUtility.SetDirty(atlas);
        AssetDatabase.Refresh();
    }

    private void OnWizardOtherButton()
    {
        string path = EditorUtility.SaveFilePanelInProject("Create Atlas", "atlas", "asset", "Save atlas asset");
        if (!string.IsNullOrEmpty(path))
        {
            atlas = CreateInstance<AtlasPlus>();
            AssetDatabase.CreateAsset(atlas, path);
        }
    }

    private void OnWizardUpdate()
    {
        if (!atlas)
            errorString = "Please selec atlas first!";
        else if (textures == null || textures.Length == 0 || textures.Any(t => !t))
            errorString = "Please add textures!";
        else
            errorString = String.Empty;
    }
}