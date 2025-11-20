using System.Windows.Media.Media3D;

namespace SEToolbox.Models
{
    public class BindableSize3DModel : BaseModel
    {
        #region Fields

        private Size3D _size;

        #endregion

        public BindableSize3DModel()
        {
            _size = new Size3D(0, 0, 0);
        }

        public BindableSize3DModel(int width, int height, int depth)
        {
            _size = new Size3D(width, height, depth);
        }

        public BindableSize3DModel(Rect3D size)
        {
            _size = size.IsEmpty ? new Size3D() : new Size3D(size.SizeX, size.SizeY, size.SizeZ);
        }

        public BindableSize3DModel(Size3D size)
        {
            _size = new Size3D(size.X, size.Y, size.Z);
        }
        

        #region Properties

        public double Width
        {
            get => _size.X;
            set => SetProperty(_size.X , nameof(Width));
        }

        public double Height
        {
            get => _size.Y;
            set => SetProperty(_size.Y = value, nameof(Height));
        
        }

        public double Depth
        {
            get => _size.Z;
            set => SetProperty(_size.Z = value, nameof(Depth));
        }

        public Size3D ToSize3D
        {
            get  => _size;
        }

        #endregion
    }
}
