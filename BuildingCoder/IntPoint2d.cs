using System;
using Autodesk.Revit.DB;

namespace BuildingCoder
{
    /// <summary>
    ///     An integer-based 2D point class.
    /// </summary>
    internal class IntPoint2d : IComparable<IntPoint2d>
    {
        /// <summary>
        ///     Initialise a 2D millimetre integer
        ///     point to the given values.
        /// </summary>
        public IntPoint2d(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        ///     Convert a 2D Revit UV to a 2D millimetre
        ///     integer point by scaling from feet to mm.
        /// </summary>
        public IntPoint2d(UV p)
        {
            X = Util.FootToMmInt(p.U);
            Y = Util.FootToMmInt(p.V);
        }

        /// <summary>
        ///     Convert a 3D Revit XYZ to a 2D millimetre
        ///     integer point by discarding the Z coordinate
        ///     and scaling from feet to mm.
        /// </summary>
        public IntPoint2d(XYZ p)
        {
            X = Util.FootToMmInt(p.X);
            Y = Util.FootToMmInt(p.Y);
        }

        /// <summary>
        ///     Convert Revit coordinates XYZ to a 2D
        ///     millimetre integer point by scaling
        ///     from feet to mm.
        /// </summary>
        public IntPoint2d(double x, double y)
        {
            X = Util.FootToMmInt(x);
            Y = Util.FootToMmInt(y);
        }

        public int X { get; set; }
        public int Y { get; set; }

        /// <summary>
        ///     Comparison with another point, important
        ///     for dictionary lookup support.
        /// </summary>
        public int CompareTo(IntPoint2d a)
        {
            var d = X - a.X;

            if (0 == d) d = Y - a.Y;
            return d;
        }

        /// <summary>
        ///     Display as a string.
        /// </summary>
        public override string ToString()
        {
            return $"({X},{Y})";
        }

        /// <summary>
        ///     Display as a string.
        /// </summary>
        public string ToString(
            bool onlySpaceSeparator)
        {
            var format_string = onlySpaceSeparator
                ? "{0} {1}"
                : "({0},{1})";

            return string.Format(format_string, X, Y);
        }

        /// <summary>
        ///     Add two points, i.e. treat one of
        ///     them as a translation vector.
        /// </summary>
        public static IntPoint2d operator +(
            IntPoint2d a,
            IntPoint2d b)
        {
            return new IntPoint2d(
                a.X + b.X, a.Y + b.Y);
        }
    }
}