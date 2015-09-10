using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ultralpha
{
    public class TextureTree : RBTree<TextureTree>
    {
        public static int padding = 2;
        public int index;
        public string name;
        public Rect rect;
        public Texture2D texture;

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
            if (this.texture)
                return null;
            if (rect.width < texture.width || rect.height < texture.height)
                return null;
            if (rect.width == texture.width && rect.height == texture.height)
            {
                this.texture = texture;
                this.index = index;
                name = texture.name;
                return this;
            }

            InitChildren();

            var deltaW = rect.width - texture.width;
            var deltaH = rect.height - texture.height;
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
            return GetChildren().Where(n => n.texture);
        }

        public IEnumerable<string> GetNames()
        {
            return GetBounds().Select(n => n.name);
        }

        public Vector2 GetRootSize()
        {
            var root = GetRoot();
            var bounds = root.GetBounds().ToArray();
            var width= bounds.Max(b => b.rect.xMax) - bounds.Min(b => b.rect.xMin);
            var height= bounds.Max(b => b.rect.yMax) - bounds.Min(b => b.rect.yMin);
            return new Vector2(width, height);
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
                if (target.format == TextureFormat.ARGB32 && texture.format == TextureFormat.ARGB32)
                    //faster only on argb32
                {
                    var data = texture.GetPixels32(0);
                    target.SetPixels32((int) rect.x, (int) rect.y, texture.width, texture.height, data);
                }
                else
                {
                    var data = texture.GetPixels(0);
                    target.SetPixels((int) rect.x, (int) rect.y, texture.width, texture.height, data);
                }
            }
        }
    }
}