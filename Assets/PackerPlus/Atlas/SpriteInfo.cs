using System;
using UnityEngine;

namespace Ultralpha
{
    [Serializable]
    public class SpriteInfo
    {
        public string name;
        public Rect uvRect;
        public Rect sourceRect;
        public Vector4 border;
        public Vector4 padding;
        public Vector2 pivot = Vector2.one*0.5f;
        public float pixelsPerUnit = 100f;
        public int extrude;
        public int section;

        public static Rect operator *(SpriteInfo sprite, TextureInfo texture)
        {
            return Rect.MinMaxRect(sprite.uvRect.xMin*texture.width, sprite.uvRect.yMin*texture.height,
                sprite.uvRect.xMax*texture.width, sprite.uvRect.yMax*texture.height);
        }
    }
}