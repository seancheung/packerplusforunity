using System.Collections.Generic;

namespace Ultralpha
{
    public abstract class RBTree<T> where T : RBTree<T>, new()
    {
        public T Left { get; private set; }
        public T Right { get; private set; }
        public T Parent { get; private set; }

        public bool HasChildren
        {
            get { return Left && Right; }
        }

        public void InitChildren()
        {
            Left = new T();
            Right = new T();
            Left.Parent = (T) this;
            Right.Parent = (T) this;
        }

        public IEnumerable<T> GetChildren()
        {
            if (HasChildren)
            {
                foreach (T child in Left.GetChildren())
                {
                    yield return child;
                }
                yield return Left;
                foreach (T child in Right.GetChildren())
                {
                    yield return child;
                }
                yield return Right;
            }
            if (!Parent)
                yield return (T)this;
        }

        public T GetRoot()
        {
            if (!Parent)
                return (T)this;
            return Parent.GetRoot();
        }

        public static implicit operator bool(RBTree<T> node)
        {
            return node != null;
        }
    }
}