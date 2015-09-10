using Ultralpha;
using UnityEngine;
using UnityEngine.UI;

public class RuntimePacking : MonoBehaviour
{
    public RawImage image;
    public ImagePlus[] images;
    public int width = 1024;
    public int height = 1024;

    private void Start()
    {
        Texture2D[] textures = Resources.LoadAll<Texture2D>("");
        AtlasPlus atlas = SharpPacker.Create(textures, width, height, false);
        image.texture = atlas.textures[0].texture;
        image.SetNativeSize();
        for (int i = 0; i < images.Length && i < atlas.Count; i++)
        {
            images[i].Atlas = atlas;
            images[i].SpriteName = atlas.sprites[i].name;
            images[i].SetNativeSize();
        }

        image.rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, image.texture.height);
    }
}