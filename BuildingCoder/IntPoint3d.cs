using System;
using Autodesk.Revit.DB;

namespace BuildingCoder
{
    /// <summary>
    ///     An integer-based 3D point class.
    /// </summary>
    internal class IntPoint3d : IComparable<IntPoint3d>
    {
        /// <summary>
        ///     Initialise a 2D millimetre integer
        ///     point to the given values.
        /// </summary>
        public IntPoint3d(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>
        ///     Convert a 2D Revit UV to a 3D millimetre
        ///     integer point by scaling from feet to mm.
        /// </summary>
        public IntPoint3d(UV p)
        {
            X = Util.FootToMmInt(p.U);
            Y = Util.FootToMmInt(p.V);
            Z = 0;
        }

        /// <summary>
        ///     Convert a 3D Revit XYZ to a 3D millimetre
        ///     integer point, scaling from feet to mm.
        /// </summary>
        public IntPoint3d(XYZ p)
        {
            X = Util.FootToMmInt(p.X);
            Y = Util.FootToMmInt(p.Y);
            Z = Util.FootToMmInt(p.Z);
        }

        /// <summary>
        ///     Convert Revit coordinates XYZ to a 3D
        ///     millimetre integer point by scaling
        ///     from feet to mm.
        /// </summary>
        public IntPoint3d(double x, double y, double z)
        {
            X = Util.FootToMmInt(x);
            Y = Util.FootToMmInt(y);
            Z = Util.FootToMmInt(z);
        }

        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }

        /// <summary>
        ///     Comparison with another point, important
        ///     for dictionary lookup support.
        /// </summary>
        public int CompareTo(IntPoint3d a)
        {
            var d = X - a.X;

            if (0 == d)
            {
                d = Y - a.Y;

                if (0 == d) d = Z - a.Z;
            }

            return d;
        }

        /// <summary>
        ///     Display as a string.
        /// </summary>
        public override string ToString()
        {
            return $"({X},{Y},{Z})";
        }

        /// <summary>
        ///     Display as a string.
        /// </summary>
        public string ToString(
            bool onlySpaceSeparator)
        {
            var format_string = onlySpaceSeparator
                ? "{0} {1} {2}"
                : "({0},{1},{2})";

            return string.Format(format_string, X, Y, Z);
        }

        /// <summary>
        ///     Add two points, i.e. treat one of
        ///     them as a translation vector.
        /// </summary>
        public static IntPoint3d operator +(
            IntPoint3d a,
            IntPoint3d b)
        {
            return new IntPoint3d(
                a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }
    }
}