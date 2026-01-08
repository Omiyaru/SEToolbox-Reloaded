using System.Drawing;

namespace SEToolbox.Models
{
    public class BindableSizeModel : BaseModel
    {
        private Size _size;

        public BindableSizeModel()
        {
            _size = new Size();
        }

        public BindableSizeModel(int width, int height)
            : this()
        {
            Width = width;
            Height = height;
        }

        public BindableSizeModel(Size size)
            : this()
        {
            Width = size.Width;
            Height = size.Height;
        }

        #region Properties

        public int Width
        {
            get => _size.Width;
            set => SetProperty(_size.Width, value, nameof(Width));
        }

        public int Height
        {
            get => _size.Height;
            set => SetProperty(_size.Height, value, nameof(Height));
        }

        public Size Size
        {
            get => _size;
            set => SetProperty(ref _size, value, nameof(Size), nameof(Width), nameof(Height));
        }

        #endregion
    }
}
