using System;
using UnityEngine;

[Serializable]
public class SpriteInfo
{
    public string name;
    public Rect uvRect;
    public Rect sourceRect;
    public Vector4 border;
    public Vector4 padding;
    public int section;
}

[Serializable]
public class TextureInfo
{
    public Texture2D texture;
    public int width;
    public int height;
}