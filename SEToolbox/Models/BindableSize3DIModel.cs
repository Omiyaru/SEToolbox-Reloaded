using System.Drawing;
using System.Windows.Media.Media3D;

using VRageMath;

namespace SEToolbox.Models
{
    public class BindableSize3DIModel : BaseModel
    {
        #region Fields

        private int _width;
        private int _height;
        private int _depth;

        #endregion

        public BindableSize3DIModel()
        {
            Width = 0;
            Height = 0;
            Depth = 0;
        }

        public BindableSize3DIModel(int width, int height, int depth)
            : this()
        {
            Width = width;
            Height = height;
            Depth = depth;
        }

        public BindableSize3DIModel(Vector3I size)
        {
            Width = size.X;
            Height = size.Y;
            Depth = size.Z;
        }

        public BindableSize3DIModel(Size size)
            : this()
        {
            Width = size.Width;
            Height = size.Height;
        }

        #region Properties

        public int Width
        {
            get => _width;
            set => SetProperty(ref _width, nameof(Width));
        }

        public int Height
        {
            get => _height;
            set => SetProperty(ref _height, nameof(Height));
        }

        public int Depth
        {
            get => _depth;
            set => SetProperty(ref _depth, nameof(Depth));
        }

        public Size3D ToSize3D
        {
            get => new(Width, Height, Depth);
        }

        #endregion

        public Vector3I ToVector3I()
        {
            return new Vector3I(Width, Height, Depth);
        }

        public override string ToString()
        {
            return string.Format($"{Width},{Height},{Depth}");
        }
    }
}

