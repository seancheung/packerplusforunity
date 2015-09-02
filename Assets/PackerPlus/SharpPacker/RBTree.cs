using System.Collections.Generic;

namespace Ultralpha
{
    public abstract class RBTree<T> where T : RBTree<T>, new()
    {
        public T Left { get; private set; }
        public T Right { get; private set; }

        public bool HasChildren
        {
            get { return Left && Right; }
        }

        public void InitChildren()
        {
            Left = new T();
            Right = new T();
        }

        public IEnumerable<T> GetChildren()
        {
            yield return (T) this;

            if (HasChildren)
            {
                foreach (T child in Left.GetChildren())
                {
                    yield return child;
                }
                foreach (T child in Right.GetChildren())
                {
                    yield return child;
                }
            }
        }

        public void ClearChildren()
        {
            if (HasChildren)
            {
                foreach (T child in Left.GetChildren())
                {
                    if (child)
                        child.ClearChildren();
                }
                foreach (T child in Right.GetChildren())
                {
                    if (child)
                        child.ClearChildren();
                }
            }
            Left = null;
            Right = null;
        }

        public static implicit operator bool(RBTree<T> node)
        {
            return node != null;
        }
    }
}