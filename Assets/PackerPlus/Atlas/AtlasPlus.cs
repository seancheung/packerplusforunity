using UnityEngine;

public class AtlasPlus : ScriptableObject
{
    public TextureInfo[] textures;
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
            if (index >= sprites.Length || index < 0)
                return null;
            var sprite = sprites[index];
            var texture = sprite == null || textures == null || sprite.section > textures.Length || sprite.section < 0
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