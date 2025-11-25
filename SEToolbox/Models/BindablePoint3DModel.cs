using System;
using System.Windows.Media.Media3D;

namespace SEToolbox.Models
{
    public class BindablePoint3DModel : BaseModel
    {
        #region Fields

        private Point3D _point;

        #endregion

        #region Ctor

        public BindablePoint3DModel()
        {
            _point = new Point3D();
        }

        public BindablePoint3DModel(double x, double y, double z)
            : this()
        {
            X = x;
            Y = y;
            Z = z;
        }

        public BindablePoint3DModel(Point3D point)
            : this()
        {
            X = point.X;
            Y = point.Y;
            Z = point.Z;
        }

        public BindablePoint3DModel(float x, float y, float z)
            : this()
        {
            X = x;
            Y = y;
            Z = z;
        }

        public BindablePoint3DModel(VRageMath.Vector3 vector)
            : this()
        {
            X = vector.X;
            Y = vector.Y;
            Z = vector.Z;
        }

        public BindablePoint3DModel(VRageMath.Vector3D vector)
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
            get => _point.X;
            set => SetProperty(_point.X, value, nameof(X));
        }

        public double Y
        {
            get => _point.Y;
            set => SetProperty(_point.Y, value, nameof(Y));
        }

        public double Z
        {
            get => _point.Z;
            set => SetProperty(_point.Z, value, nameof(Z));
        }

        public Point3D Point3D
        {
            get => _point;
            set => SetProperty(_point, value, nameof(Point3D));
        }

        #endregion

        #region Methods

        public VRageMath.Vector3 ToVector3()
        {
            return new(ToFloat(X), 
                       ToFloat(Y),
                       ToFloat(Z));
        }

        public VRageMath.Vector3D ToVector3D()
        {
            return new(X, Y, Z);
        }

        private static float ToFloat(double value)
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

        public BindablePoint3DModel RoundOff(double roundTo)
        {
            Point3D v = new(Math.Round(_point.X / roundTo, 0, MidpointRounding.ToEven) * roundTo,
                            Math.Round(_point.Y / roundTo, 0, MidpointRounding.ToEven) * roundTo,
                            Math.Round(_point.Z / roundTo, 0, MidpointRounding.ToEven) * roundTo);
            return new BindablePoint3DModel(v);
        }

        public override string ToString()
        {
            return string.Format($"{X},{Y},{Z}");
        }

        #endregion
    }
}
