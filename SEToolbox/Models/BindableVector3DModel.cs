using System;
using System.Windows.Media.Media3D;

namespace SEToolbox.Models
{
    public class BindableVector3DModel : BaseModel
    {
        #region Fields

        private Vector3D _vector;

        #endregion

        #region Ctor

        public BindableVector3DModel()
        {
            _vector = new Vector3D();
        }

        public BindableVector3DModel(double x, double y, double z)
            : this()
        {
            X = x;
            Y = y;
            Z = z;
        }

        public BindableVector3DModel(Vector3D vector)
            : this()
        {
            X = vector.X;
            Y = vector.Y;
            Z = vector.Z;
        }

        public BindableVector3DModel(float x, float y, float z)
            : this()
        {
            X = x;
            Y = y;
            Z = z;
        }

        public BindableVector3DModel(VRageMath.Vector3 vector)
            : this()
        {
            X = vector.X;
            Y = vector.Y;
            Z = vector.Z;
        }

        #endregion

        #region Properties

        public double X
        {
            get => _vector.X;
            set => SetProperty(_vector.X, value, nameof(X));
        }

        public double Y
        {
            get => _vector.Y;
            set => SetProperty(_vector.Y, value, nameof(Y));
        }

        

        public double Z
        {
            get => _vector.Z;
            set => SetProperty(_vector.Z, value, nameof(Z));
        }

        public Vector3D Vector3D
        {
            get => _vector;
            set => SetProperty(ref _vector, value, nameof(Vector3D));
        }

        #endregion

        #region Methods

        public VRageMath.Vector3 ToVector3()
        {
            return new(ToFloat(X), ToFloat(Y), ToFloat(Z));
        }

        public VRageMath.Vector3D ToVector3D()
        {
            return new(X, Y, Z);
        }

        private float ToFloat(double value)
        {
            float result = (float)value;
            if (float.IsPositiveInfinity(result))
            {
                result = float.MaxValue;
            }
            else if (float.IsNegativeInfinity(result))
            {
                result = float.MinValue;
            }
            return result;
        }

        public override string ToString()
        {
            return $"{_vector}";
        }

        public BindableVector3DModel Negate()
        {
            Vector3D v = _vector;
            v.Negate();
            return new BindableVector3DModel(v);
        }

        public BindableVector3DModel RoundToAxis()
        {
            _ = new Vector3D();

            int axis = Math.Abs(_vector.X) > Math.Abs(_vector.Y) ?
                      (Math.Abs(_vector.X) > Math.Abs(_vector.Z) ? 0 : 2) :
                      (Math.Abs(_vector.Y) > Math.Abs(_vector.Z) ? 1 : 2);

            Vector3D v = new(axis == 0 ? Math.Sign(_vector.X) : 0,
                        axis == 1 ? Math.Sign(_vector.Y) : 0,
                        axis == 2 ? Math.Sign(_vector.Z) : 0);

            return new BindableVector3DModel(v);
        }

        #endregion
    }
}
