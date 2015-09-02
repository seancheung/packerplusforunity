using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ultralpha
{
    public class TextureTree : RBTree<TextureTree>
    {
        public static int padding = 2;
        public static bool bleed;
        public Rect rect;
        public Texture2D texture;
        private bool _filled;
        public string name;
        public int index;

        public TextureTree(Rect rect)
        {
            this.rect = rect;
        }

        public TextureTree()
        {
        }

        public TextureTree AddTexture(Texture2D texture, int index)
        {
            if (!texture)
                return null;
            if (HasChildren)
                return Left.AddTexture(texture, index) ?? Right.AddTexture(texture, index);
            if (_filled)
                return null;
            if (rect.width < texture.width || rect.height < texture.height)
                return null;
            if (rect.width == texture.width && rect.height == texture.height)
            {
                this.texture = texture;
                _filled = true;
                this.index = index;
                name = texture.name;
                return this;
            }

            InitChildren();

            float deltaW = rect.width - texture.width;
            float deltaH = rect.height - texture.height;
            if (deltaW > deltaH)
            {
                Left.rect = new Rect(rect.xMin, rect.yMin, texture.width, rect.height);
                Right.rect = new Rect(rect.xMin + texture.width + padding, rect.yMin,
                    rect.width - texture.width - padding,
                    rect.height);
            }
            else
            {
                Left.rect = new Rect(rect.xMin, rect.yMin, rect.width, texture.height);
                Right.rect = new Rect(rect.xMin, rect.yMin + texture.height + padding, rect.width,
                    rect.height - texture.height - padding);
            }

            return Left.AddTexture(texture, index);
        }

        public IEnumerable<TextureTree> GetBounds()
        {
            return GetChildren().Where(n => n._filled);
        }

        public IEnumerable<string> GetNames()
        {
            return GetChildren().Where(n => n._filled).Select(n => n.name);
        }

        public void Build(Texture2D target)
        {
            if (HasChildren)
            {
                Left.Build(target);
                Right.Build(target);
            }
            if (texture)
            {
                if (target.format == TextureFormat.ARGB32 && texture.format == TextureFormat.ARGB32) //faster only on argb32
                {
                    var data = texture.GetPixels32(0);
                    target.SetPixels32((int)rect.x, (int)rect.y, texture.width, texture.height, data);
                }
                else
                {
                    var data = texture.GetPixels(0);
                    for (int x = 0; x < texture.width; x++)
                    {
                        for (int y = 0; y < texture.height; y++)
                        {
                            target.SetPixel(x + (int)rect.x, y + (int)rect.y, data[x + y * texture.width]);
                        }
                    }
                    if (bleed && padding > 0)
                    {
                        for (int y = 0; y < texture.height; y++)
                        {
                            int x = texture.width - 1;
                            target.SetPixel(x + (int)rect.x + padding, y + (int)rect.y, data[x + y * texture.width]);
                        }
                        for (int x = 0; x < texture.width; x++)
                        {
                            int y = texture.height - 1;
                            target.SetPixel(x + (int)rect.x, y + (int)rect.y + padding, data[x + y * texture.width]);
                        }
                    }
                }
            }
        }
    }
}