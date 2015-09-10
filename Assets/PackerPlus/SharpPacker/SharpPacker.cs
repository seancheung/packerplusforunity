using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ultralpha
{
    public static class SharpPacker
    {
        public static AtlasPlus Create(Texture2D[] textures, int width, int height, bool readable)
        {
            if (textures == null || textures.Length == 0)
            {
                Debug.LogError("Textures are null or empty");
                return null;
            }
            if (textures.Any(t => !t))
            {
                Debug.LogError("some textures are null");
                return null;
            }
            if (textures.Any(t => t.width > width || t.height > height))
            {
                Debug.LogError("One or more Textures' sizes are larger than packing output size");
                return null;
            }

            var index = textures.Length - 1;
            //textures = textures.Reverse().ToArray();
            textures = textures.OrderBy(t => t.width*t.height).ToArray();
            var atlas = ScriptableObject.CreateInstance<AtlasPlus>();
            atlas.maxWidth = width;
            atlas.maxHeight = height;
            var nodes = new List<TextureTree>();

            while (index >= 0)
            {
                nodes.Add(new TextureTree(new Rect(0, 0, width, height)));
                var addIndex = 0;
                while (index >= 0)
                {
                    textures[index].name = UniqueName(textures[index].name,
                        nodes.SelectMany(n => n.GetNames()).ToArray());
                    if (!nodes.Any(node => node.AddTexture(textures[index], addIndex++)))
                    {
                        Debug.Log("Multiple Textures generated");
                        break;
                    }
                    index--;
                }
            }

            var sprites = new List<SpriteInfo>();
            atlas.sprites = sprites.ToArray();
            atlas.textures = new TextureInfo[nodes.Count];

            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                var size = node.GetRootSize();
                node.rect.width = size.x;
                node.rect.height = size.y;
                atlas.textures[i] = new TextureInfo();
                atlas.textures[i].texture = new Texture2D((int) node.rect.width, (int) node.rect.height,
                    TextureFormat.ARGB32, false);
                atlas.textures[i].texture.SetPixels32(
                    Enumerable.Repeat(new Color32(0, 0, 0, 0),
                        atlas.textures[i].texture.width*atlas.textures[i].texture.height).ToArray());
                node.Build(atlas.textures[i].texture);
                var bounds = node.GetBounds().OrderBy(n => n.index).ToArray();

                foreach (var bound in bounds)
                {
                    var uv = new Rect(bound.rect.xMin/node.rect.width, bound.rect.yMin/node.rect.height,
                        bound.rect.width/node.rect.width, bound.rect.height/node.rect.height);
                    //maybe add uv boundary padding
                    sprites.Add(new SpriteInfo
                    {
                        sourceRect = bound.rect,
                        name = bound.name,
                        uvRect = uv,
                        section = i
                    });
                }
                atlas.textures[i].texture.Apply(false, !readable);
                atlas.textures[i].width = (int) node.rect.width;
                atlas.textures[i].height = (int) node.rect.height;
            }

            atlas.sprites = sprites.ToArray();
            return atlas;
        }

        private static string UniqueName(string original, string[] names)
        {
            var suffix = 1;
            var uniqueName = original;
            while (names.Contains(uniqueName))
            {
                uniqueName = original + "_" + suffix++;
            }
            return uniqueName;
        }
    }
}