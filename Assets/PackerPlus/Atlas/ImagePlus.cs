using UnityEngine;
using UnityEngine.UI;

namespace Ultralpha
{
    [AddComponentMenu("UI/Image Plus", 0)]
    public class ImagePlus : Image
    {
        [SerializeField] private AtlasPlus _atlas;
        [SerializeField] private string _spriteName;

        public AtlasPlus Atlas
        {
            get { return _atlas; }
            set
            {
                _atlas = value;
                RefreshSprite();
            }
        }

        public string SpriteName
        {
            get { return _spriteName; }
            set
            {
                if (_spriteName == value)
                    return;
                _spriteName = value;
                RefreshSprite();
            }
        }


        private void RefreshSprite()
        {
            sprite = _atlas ? _atlas[_spriteName] : null;
        }
    }
}