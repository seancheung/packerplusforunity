﻿using System.Linq;
using UnityEngine;

namespace Ultralpha
{
    public class AtlasPlus : ScriptableObject
    {
        [SerializeField]
        public TextureInfo[] textures;
         [SerializeField]
        public SpriteInfo[] sprites;
        public int maxWidth;
        public int maxHeight;

        public int Count
        {
            get { return sprites == null ? 0 : sprites.Length; }
        }

        public Sprite this[int index]
        {
            get
            {
                if (sprites == null || index >= sprites.Length || index < 0)
                    return null;
                var sprite = sprites[index];
                var texture = sprite == null || textures == null || sprite.section > textures.Length ||
                              sprite.section < 0
                    ? null
                    : textures[sprite.section];
                if (texture == null || !texture.texture)
                    return null;
                return Sprite.Create(texture.texture, sprite*texture, sprite.pivot, sprite.pixelsPerUnit,
                    (uint) sprite.extrude, SpriteMeshType.Tight,
                    sprite.border);
            }
        }

        public Sprite this[string key]
        {
            get
            {
                if (sprites == null || sprites.Length == 0)
                    return null;
                var sprite = sprites.FirstOrDefault(s => s.name == key);
                var texture = sprite == null || textures == null || sprite.section > textures.Length ||
                              sprite.section < 0
                    ? null
                    : textures[sprite.section];
                if (texture == null || !texture.texture)
                    return null;
                return Sprite.Create(texture.texture, sprite*texture, sprite.pivot, sprite.pixelsPerUnit,
                    (uint) sprite.extrude, SpriteMeshType.Tight,
                    sprite.border);
            }
        }
    }
}