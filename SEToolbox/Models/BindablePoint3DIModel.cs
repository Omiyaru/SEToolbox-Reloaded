namespace SEToolbox.Models
{
    public class BindablePoint3DIModel : BaseModel
    {
        #region Fields

        private int _x;
        private int _y;
        private int _z;

        #endregion

        #region Ctor

        public BindablePoint3DIModel()
        {
            X = 0;
            Y = 0;
            Z = 0;
        }

        public BindablePoint3DIModel(int x, int y, int z)
            : this()
        {
            X = x;
            Y = y;
            Z = z;
        }

        public BindablePoint3DIModel(VRageMath.Vector3I vector)
            : this()
        {
            X = vector.X;
            Y = vector.Y;
            Z = vector.Z;
        }

        #endregion

        #region Properties

        public int X
        {
            get  => _x ;

            set
            {
                SetProperty(ref _x, value, nameof(X));

            }
        }

        public int Y
        {
            get  => _y;
            set => SetProperty(ref _y, value, nameof(Y));
        }

        public int Z
        {
            get => _z;
            set => SetProperty(ref _z, value, nameof(Z));
        }

        #endregion

        #region Methods

        public VRageMath.Vector3I ToVector3I()
        {
            return new VRageMath.Vector3I(X, Y, Z);
        }

        public override string ToString()
        {
            return string.Format($"{X},{Y},{Z}");
        }
        

        #endregion
    }
}
