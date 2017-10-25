#region Header
//
// Util.cs - The Building Coder Revit API utility methods
//
// Copyright (C) 2008-2017 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using WinForms = System.Windows.Forms;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Collections;
#endregion // Namespaces

namespace BuildingCoder
{
  class Util
  {
    #region Geometrical Comparison
    public const double _eps = 1.0e-9;

    public static double Eps
    {
      get
      {
        return _eps;
      }
    }

    public static double MinLineLength
    {
      get
      {
        return _eps;
      }
    }

    public static double TolPointOnPlane
    {
      get
      {
        return _eps;
      }
    }

    public static bool IsZero(
      double a,
      double tolerance )
    {
      return tolerance > Math.Abs( a );
    }

    public static bool IsZero( double a )
    {
      return IsZero( a, _eps );
    }

    public static bool IsEqual( double a, double b )
    {
      return IsZero( b - a );
    }

    public static int Compare( double a, double b )
    {
      return IsEqual( a, b ) ? 0 : ( a < b ? -1 : 1 );
    }

    public static int Compare( XYZ p, XYZ q )
    {
      int d = Compare( p.X, q.X );

      if( 0 == d )
      {
        d = Compare( p.Y, q.Y );

        if( 0 == d )
        {
          d = Compare( p.Z, q.Z );
        }
      }
      return d;
    }

    public static int Compare( Plane a, Plane b )
    {
      int d = Compare( a.Normal, b.Normal );

      if( 0 == d )
      {
        d = Compare( a.SignedDistanceTo( XYZ.Zero ),
          b.SignedDistanceTo( XYZ.Zero ) );

        if( 0 == d )
        {
          d = Compare( a.XVec.AngleOnPlaneTo(
            b.XVec, b.Normal ), 0 );
        }
      }
      return d;
    }

    public static bool IsEqual( XYZ p, XYZ q )
    {
      return 0 == Compare( p, q );
    }

    /// <summary>
    /// Return true if the given bounding box bb
    /// contains the given point p in its interior.
    /// </summary>
    public bool BoundingBoxXyzContains(
      BoundingBoxXYZ bb,
      XYZ p )
    {
      return 0 < Compare( bb.Min, p )
        && 0 < Compare( p, bb.Max );
    }

    /// <summary>
    /// Return true if the vectors v and w 
    /// are non-zero and perpendicular.
    /// </summary>
    bool IsPerpendicular( XYZ v, XYZ w )
    {
      double a = v.GetLength();
      double b = v.GetLength();
      double c = Math.Abs( v.DotProduct( w ) );
      return _eps < a
        && _eps < b
        && _eps > c;
      // c * c < _eps * a * b
    }

    public static bool IsParallel( XYZ p, XYZ q )
    {
      return p.CrossProduct( q ).IsZeroLength();
    }

    public static bool IsHorizontal( XYZ v )
    {
      return IsZero( v.Z );
    }

    public static bool IsVertical( XYZ v )
    {
      return IsZero( v.X ) && IsZero( v.Y );
    }

    public static bool IsVertical( XYZ v, double tolerance )
    {
      return IsZero( v.X, tolerance )
        && IsZero( v.Y, tolerance );
    }

    public static bool IsHorizontal( Edge e )
    {
      XYZ p = e.Evaluate( 0 );
      XYZ q = e.Evaluate( 1 );
      return IsHorizontal( q - p );
    }

    public static bool IsHorizontal( PlanarFace f )
    {
      return IsVertical( f.FaceNormal );
    }

    public static bool IsVertical( PlanarFace f )
    {
      return IsHorizontal( f.FaceNormal );
    }

    public static bool IsVertical( CylindricalFace f )
    {
      return IsVertical( f.Axis );
    }

    /// <summary>
    /// Minimum slope for a vector to be considered
    /// to be pointing upwards. Slope is simply the
    /// relationship between the vertical and
    /// horizontal components.
    /// </summary>
    const double _minimumSlope = 0.3;

    /// <summary>
    /// Return true if the Z coordinate of the
    /// given vector is positive and the slope
    /// is larger than the minimum limit.
    /// </summary>
    public static bool PointsUpwards( XYZ v )
    {
      double horizontalLength = v.X * v.X + v.Y * v.Y;
      double verticalLength = v.Z * v.Z;

      return 0 < v.Z
        && _minimumSlope
          < verticalLength / horizontalLength;

      //return _eps < v.Normalize().Z;
      //return _eps < v.Normalize().Z && IsVertical( v.Normalize(), tolerance );
    }

    /// <summary>
    /// Return the maximum value from an array of real numbers.
    /// </summary>
    public static double Max( double[] a )
    {
      Debug.Assert( 1 == a.Rank, "expected one-dimensional array" );
      Debug.Assert( 0 == a.GetLowerBound( 0 ), "expected zero-based array" );
      Debug.Assert( 0 < a.GetUpperBound( 0 ), "expected non-empty array" );
      double max = a[0];
      for( int i = 1; i <= a.GetUpperBound( 0 ); ++i )
      {
        if( max < a[i] )
        {
          max = a[i];
        }
      }
      return max;
    }
    #endregion // Geometrical Comparison

    #region Geometrical Calculation
    /// <summary>
    /// Return arbitrary X and Y axes for the given 
    /// normal vector according to the AutoCAD 
    /// Arbitrary Axis Algorithm
    /// https://www.autodesk.com/techpubs/autocad/acadr14/dxf/arbitrary_axis_algorithm_al_u05_c.htm
    /// </summary>
    public static void GetArbitraryAxes(
      XYZ normal,
      out XYZ ax,
      out XYZ ay )
    {
      double limit = 1.0 / 64;

      XYZ pick_cardinal_axis
        = ( IsZero( normal.X, limit )
          && IsZero( normal.Y, limit ) )
            ? XYZ.BasisY
            : XYZ.BasisZ;

      ax = pick_cardinal_axis.CrossProduct( normal ).Normalize();
      ay = normal.CrossProduct( ax ).Normalize();
    }

    /// <summary>
    /// Return the midpoint between two points.
    /// </summary>
    public static XYZ Midpoint( XYZ p, XYZ q )
    {
      return 0.5 * ( p + q );
    }

    /// <summary>
    /// Return the midpoint of a Line.
    /// </summary>
    public static XYZ Midpoint( Line line )
    {
      return Midpoint( line.GetEndPoint( 0 ),
        line.GetEndPoint( 1 ) );
    }

    /// <summary>
    /// Return the normal of a Line in the XY plane.
    /// </summary>
    public static XYZ Normal( Line line )
    {
      XYZ p = line.GetEndPoint( 0 );
      XYZ q = line.GetEndPoint( 1 );
      XYZ v = q - p;

      //Debug.Assert( IsZero( v.Z ),
      //  "expected horizontal line" );

      return v.CrossProduct( XYZ.BasisZ ).Normalize();
    }

    /// <summary>
    /// Return the bottom four XYZ corners of the given 
    /// bounding box in the XY plane at the minimum 
    /// Z elevation in the order lower left, lower 
    /// right, upper right, upper left:
    /// </summary>
    public static XYZ[] GetBottomCorners(
      BoundingBoxXYZ b )
    {
      double z = b.Min.Z;

      return new XYZ[] {
        new XYZ( b.Min.X, b.Min.Y, z ),
        new XYZ( b.Max.X, b.Min.Y, z ),
        new XYZ( b.Max.X, b.Max.Y, z ),
        new XYZ( b.Min.X, b.Max.Y, z )
      };
    }

    /// <summary>
    /// Return the 2D intersection point between two 
    /// unbounded lines defined in the XY plane by the 
    /// start and end points of the two given curves. 
    /// By Magson Leone.
    /// Return null if the two lines are coincident,
    /// in which case the intersection is an infinite 
    /// line, or non-coincident and parallel, in which 
    /// case it is empty.
    /// https://en.wikipedia.org/wiki/Line%E2%80%93line_intersection
    /// </summary>
    public static XYZ Intersection( Curve c1, Curve c2 )
    {
      XYZ p1 = c1.GetEndPoint( 0 );
      XYZ q1 = c1.GetEndPoint( 1 );
      XYZ p2 = c2.GetEndPoint( 0 );
      XYZ q2 = c2.GetEndPoint( 1 );
      XYZ v1 = q1 - p1;
      XYZ v2 = q2 - p2;
      XYZ w = p2 - p1;

      XYZ p5 = null;

      double c = ( v2.X * w.Y - v2.Y * w.X )
        / ( v2.X * v1.Y - v2.Y * v1.X );

      if( !double.IsInfinity( c ) )
      {
        double x = p1.X + c * v1.X;
        double y = p1.Y + c * v1.Y;

        p5 = new XYZ( x, y, 0 );
      }
      return p5;
    }

    /// <summary>
    /// Create and return a solid sphere 
    /// with a given radius and centre point.
    /// </summary>
    static public Solid CreateSphereAt(
      XYZ centre,
      double radius )
    {
      // Use the standard global coordinate system 
      // as a frame, translated to the sphere centre.

      Frame frame = new Frame( centre, XYZ.BasisX,
        XYZ.BasisY, XYZ.BasisZ );

      // Create a vertical half-circle loop 
      // that must be in the frame location.

      Arc arc = Arc.Create(
        centre - radius * XYZ.BasisZ,
        centre + radius * XYZ.BasisZ,
        centre + radius * XYZ.BasisX );

      Line line = Line.CreateBound(
        arc.GetEndPoint( 1 ),
        arc.GetEndPoint( 0 ) );

      CurveLoop halfCircle = new CurveLoop();
      halfCircle.Append( arc );
      halfCircle.Append( line );

      List<CurveLoop> loops = new List<CurveLoop>( 1 );
      loops.Add( halfCircle );

      return GeometryCreationUtilities
        .CreateRevolvedGeometry( frame, loops,
          0, 2 * Math.PI );
    }

    /// <summary>
    /// Create a cone-shaped solid at the given base
    /// location pointing along the given axis.
    /// </summary>
    static public Solid CreateCone(
      XYZ center,
      XYZ axis_vector,
      double radius,
      double height )
    {
      XYZ az = axis_vector.Normalize();

      XYZ ax, ay;
      GetArbitraryAxes( az, out ax, out ay );

      // Define a triangle in XZ plane

      XYZ px = center + radius * ax;
      XYZ pz = center + height * az;

      List<Curve> profile = new List<Curve>();

      profile.Add( Line.CreateBound( center, px ) );
      profile.Add( Line.CreateBound( px, pz ) );
      profile.Add( Line.CreateBound( pz, center ) );

      CurveLoop curveLoop = CurveLoop.Create( profile );

      Frame frame = new Frame( center, ax, ay, az );

      //SolidOptions options = new SolidOptions( 
      //  ElementId.InvalidElementId, 
      //  ElementId.InvalidElementId );

      Solid cone = GeometryCreationUtilities
        .CreateRevolvedGeometry( frame,
          new CurveLoop[] { curveLoop },
          0, 2 * Math.PI );

      return cone;

      //using( Transaction t = new Transaction( Command.Doc, "Create cone" ) )
      //{
      //  t.Start();
      //  DirectShape ds = DirectShape.CreateElement( Command.Doc, new ElementId( BuiltInCategory.OST_GenericModel ) );
      //  ds.SetShape( new GeometryObject[] { cone } );
      //  t.Commit();
      //}
    }

    /// <summary>
    /// Create and return a cube of 
    /// side length d at the origin.
    /// </summary>
    static Solid CreateCube( double d )
    {
      return CreateRectangularPrism(
        XYZ.Zero, d, d, d );
    }

    /// <summary>
    /// Create and return a rectangular prism of the
    /// given side lengths centered at the given point.
    /// </summary>
    static Solid CreateRectangularPrism(
      XYZ center,
      double d1,
      double d2,
      double d3 )
    {
      List<Curve> profile = new List<Curve>();
      XYZ profile00 = new XYZ( -d1 / 2, -d2 / 2, -d3 / 2 );
      XYZ profile01 = new XYZ( -d1 / 2, d2 / 2, -d3 / 2 );
      XYZ profile11 = new XYZ( d1 / 2, d2 / 2, -d3 / 2 );
      XYZ profile10 = new XYZ( d1 / 2, -d2 / 2, -d3 / 2 );

      profile.Add( Line.CreateBound( profile00, profile01 ) );
      profile.Add( Line.CreateBound( profile01, profile11 ) );
      profile.Add( Line.CreateBound( profile11, profile10 ) );
      profile.Add( Line.CreateBound( profile10, profile00 ) );

      CurveLoop curveLoop = CurveLoop.Create( profile );

      SolidOptions options = new SolidOptions(
        ElementId.InvalidElementId,
        ElementId.InvalidElementId );

      return GeometryCreationUtilities
        .CreateExtrusionGeometry(
          new CurveLoop[] { curveLoop },
          XYZ.BasisZ, d3, options );
    }

    /// <summary>
    /// Create and return a solid representing 
    /// the bounding box of the input solid.
    /// Assumption: aligned with Z axis.
    /// Written, described and tested by Owen Merrick for 
    /// http://forums.autodesk.com/t5/revit-api-forum/create-solid-from-boundingbox/m-p/6592486
    /// </summary>
    public static Solid CreateSolidFromBoundingBox(
      Solid inputSolid )
    {
      BoundingBoxXYZ bbox = inputSolid.GetBoundingBox();

      // Corners in BBox coords

      XYZ pt0 = new XYZ( bbox.Min.X, bbox.Min.Y, bbox.Min.Z );
      XYZ pt1 = new XYZ( bbox.Max.X, bbox.Min.Y, bbox.Min.Z );
      XYZ pt2 = new XYZ( bbox.Max.X, bbox.Max.Y, bbox.Min.Z );
      XYZ pt3 = new XYZ( bbox.Min.X, bbox.Max.Y, bbox.Min.Z );

      // Edges in BBox coords

      Line edge0 = Line.CreateBound( pt0, pt1 );
      Line edge1 = Line.CreateBound( pt1, pt2 );
      Line edge2 = Line.CreateBound( pt2, pt3 );
      Line edge3 = Line.CreateBound( pt3, pt0 );

      // Create loop, still in BBox coords

      List<Curve> edges = new List<Curve>();
      edges.Add( edge0 );
      edges.Add( edge1 );
      edges.Add( edge2 );
      edges.Add( edge3 );

      double height = bbox.Max.Z - bbox.Min.Z;

      CurveLoop baseLoop = CurveLoop.Create( edges );

      List<CurveLoop> loopList = new List<CurveLoop>();
      loopList.Add( baseLoop );

      Solid preTransformBox = GeometryCreationUtilities
        .CreateExtrusionGeometry( loopList, XYZ.BasisZ,
          height );

      Solid transformBox = SolidUtils.CreateTransformed(
        preTransformBox, bbox.Transform );

      return transformBox;
    }
    #endregion // Geometrical XYZ Calculation

    #region Convex Hull
    /// <summary>
    /// Return the convex hull of a list of points 
    /// using the Jarvis march or Gift wrapping:
    /// https://en.wikipedia.org/wiki/Gift_wrapping_algorithm
    /// Written by Maxence.
    /// </summary>
    public static List<XYZ> ConvexHull( List<XYZ> points )
    {
      if( points == null ) throw new ArgumentNullException( nameof( points ) );
      XYZ startPoint = points.MinBy( p => p.X );
      var convexHullPoints = new List<XYZ>();
      XYZ walkingPoint = startPoint;
      XYZ refVector = XYZ.BasisY.Negate();
      do
      {
        convexHullPoints.Add( walkingPoint );
        XYZ wp = walkingPoint;
        XYZ rv = refVector;
        walkingPoint = points.MinBy( p =>
        {
          double angle = ( p - wp ).AngleOnPlaneTo( rv, XYZ.BasisZ );
          if( angle < 1e-10 ) angle = 2 * Math.PI;
          return angle;
        } );
        refVector = wp - walkingPoint;
      } while( walkingPoint != startPoint );
      convexHullPoints.Reverse();
      return convexHullPoints;
    }
    #endregion // Convex Hull

    #region Unit Handling
    /// <summary>
    /// Base units currently used internally by Revit.
    /// </summary>
    enum BaseUnit
    {
      BU_Length = 0,         // length, feet (ft)
      BU_Angle,              // angle, radian (rad)
      BU_Mass,               // mass, kilogram (kg)
      BU_Time,               // time, second (s)
      BU_Electric_Current,   // electric current, ampere (A)
      BU_Temperature,        // temperature, kelvin (K)
      BU_Luminous_Intensity, // luminous intensity, candela (cd)
      BU_Solid_Angle,        // solid angle, steradian (sr)

      NumBaseUnits
    };

    const double _inchToMm = 25.4;
    const double _footToMm = 12 * _inchToMm;
    const double _footToMeter = _footToMm * 0.001;

    const double _cubicFootToCubicMeter
      = _footToMeter * _footToMeter * _footToMeter;

    /// <summary>
    /// Convert a given length in feet to millimetres.
    /// </summary>
    public static double FootToMm( double length )
    {
      return length * _footToMm;
    }

    /// <summary>
    /// Convert a given length in feet to metres.
    /// </summary>
    public static double FootToMetre( double length )
    {
      return length * _footToMeter;
    }

    /// <summary>
    /// Convert a given length in millimetres to feet.
    /// </summary>
    public static double MmToFoot( double length )
    {
      return length / _footToMm;
    }

    /// <summary>
    /// Convert a given point or vector from millimetres to feet.
    /// </summary>
    public static XYZ MmToFoot( XYZ v )
    {
      return v.Divide( _footToMm );
    }

    /// <summary>
    /// Convert a given volume in feet to cubic meters.
    /// </summary>
    public static double CubicFootToCubicMeter( double volume )
    {
      return volume * _cubicFootToCubicMeter;
    }

    /// <summary>
    /// Hard coded abbreviations for the first 26
    /// DisplayUnitType enumeration values.
    /// </summary>
    public static string[] DisplayUnitTypeAbbreviation
      = new string[] {
      "m", // DUT_METERS = 0,
      "cm", // DUT_CENTIMETERS = 1,
      "mm", // DUT_MILLIMETERS = 2,
      "ft", // DUT_DECIMAL_FEET = 3,
      "N/A", // DUT_FEET_FRACTIONAL_INCHES = 4,
      "N/A", // DUT_FRACTIONAL_INCHES = 5,
      "in", // DUT_DECIMAL_INCHES = 6,
      "ac", // DUT_ACRES = 7,
      "ha", // DUT_HECTARES = 8,
      "N/A", // DUT_METERS_CENTIMETERS = 9,
      "y^3", // DUT_CUBIC_YARDS = 10,
      "ft^2", // DUT_SQUARE_FEET = 11,
      "m^2", // DUT_SQUARE_METERS = 12,
      "ft^3", // DUT_CUBIC_FEET = 13,
      "m^3", // DUT_CUBIC_METERS = 14,
      "deg", // DUT_DECIMAL_DEGREES = 15,
      "N/A", // DUT_DEGREES_AND_MINUTES = 16,
      "N/A", // DUT_GENERAL = 17,
      "N/A", // DUT_FIXED = 18,
      "%", // DUT_PERCENTAGE = 19,
      "in^2", // DUT_SQUARE_INCHES = 20,
      "cm^2", // DUT_SQUARE_CENTIMETERS = 21,
      "mm^2", // DUT_SQUARE_MILLIMETERS = 22,
      "in^3", // DUT_CUBIC_INCHES = 23,
      "cm^3", // DUT_CUBIC_CENTIMETERS = 24,
      "mm^3", // DUT_CUBIC_MILLIMETERS = 25,
      "l" // DUT_LITERS = 26,
      };
    #endregion // Unit Handling

    #region Formatting
    /// <summary>
    /// Return an English plural suffix for the given
    /// number of items, i.e. 's' for zero or more
    /// than one, and nothing for exactly one.
    /// </summary>
    public static string PluralSuffix( int n )
    {
      return 1 == n ? "" : "s";
    }

    /// <summary>
    /// Return an English plural suffix 'ies' or
    /// 'y' for the given number of items.
    /// </summary>
    public static string PluralSuffixY( int n )
    {
      return 1 == n ? "y" : "ies";
    }

    /// <summary>
    /// Return a dot (full stop) for zero
    /// or a colon for more than zero.
    /// </summary>
    public static string DotOrColon( int n )
    {
      return 0 < n ? ":" : ".";
    }

    /// <summary>
    /// Return a string for a real number
    /// formatted to two decimal places.
    /// </summary>
    public static string RealString( double a )
    {
      return a.ToString( "0.##" );
    }

    /// <summary>
    /// Return a hash string for a real number
    /// formatted to nine decimal places.
    /// </summary>
    public static string HashString( double a )
    {
      return a.ToString( "0.#########" );
    }

    /// <summary>
    /// Return a string representation in degrees
    /// for an angle given in radians.
    /// </summary>
    public static string AngleString( double angle )
    {
      return RealString( angle * 180 / Math.PI )
        + " degrees";
    }

    /// <summary>
    /// Return a string for a length in millimetres
    /// formatted as an integer value.
    /// </summary>
    public static string MmString( double length )
    {
      //return RealString( FootToMm( length ) ) + " mm";
      return Math.Round( FootToMm( length ) )
        .ToString() + " mm";
    }

    /// <summary>
    /// Return a string for a UV point
    /// or vector with its coordinates
    /// formatted to two decimal places.
    /// </summary>
    public static string PointString( UV p )
    {
      return string.Format( "({0},{1})",
        RealString( p.U ),
        RealString( p.V ) );
    }

    /// <summary>
    /// Return a string for an XYZ point
    /// or vector with its coordinates
    /// formatted to two decimal places.
    /// </summary>
    public static string PointString( XYZ p )
    {
      return string.Format( "({0},{1},{2})",
        RealString( p.X ),
        RealString( p.Y ),
        RealString( p.Z ) );
    }

    /// <summary>
    /// Return a hash string for an XYZ point
    /// or vector with its coordinates
    /// formatted to nine decimal places.
    /// </summary>
    public static string HashString( XYZ p )
    {
      return string.Format( "({0},{1},{2})",
        HashString( p.X ),
        HashString( p.Y ),
        HashString( p.Z ) );
    }

    /// <summary>
    /// Return a string for this bounding box
    /// with its coordinates formatted to two
    /// decimal places.
    /// </summary>
    public static string BoundingBoxString( BoundingBoxUV bb )
    {
      return string.Format( "({0},{1})",
        PointString( bb.Min ),
        PointString( bb.Max ) );
    }

    /// <summary>
    /// Return a string for this bounding box
    /// with its coordinates formatted to two
    /// decimal places.
    /// </summary>
    public static string BoundingBoxString( BoundingBoxXYZ bb )
    {
      return string.Format( "({0},{1})",
        PointString( bb.Min ),
        PointString( bb.Max ) );
    }

    /// <summary>
    /// Return a string for this plane
    /// with its coordinates formatted to two
    /// decimal places.
    /// </summary>
    public static string PlaneString( Plane p )
    {
      return string.Format( "plane origin {0}, plane normal {1}",
        PointString( p.Origin ),
        PointString( p.Normal ) );
    }

    /// <summary>
    /// Return a string for this transformation
    /// with its coordinates formatted to two
    /// decimal places.
    /// </summary>
    public static string TransformString( Transform t )
    {
      return string.Format( "({0},{1},{2},{3})", PointString( t.Origin ),
        PointString( t.BasisX ), PointString( t.BasisY ), PointString( t.BasisZ ) );
    }

    /// <summary>
    /// Return a string for a list of doubles 
    /// formatted to two decimal places.
    /// </summary>
    public static string DoubleArrayString( IList<double> a )
    {
      return string.Join( ", ",
        a.Select<double, string>(
          x => RealString( x ) ) );
    }

    /// <summary>
    /// Return a string for this point array
    /// with its coordinates formatted to two
    /// decimal places.
    /// </summary>
    public static string PointArrayString( IList<XYZ> pts )
    {
      return string.Join( ", ",
        pts.Select<XYZ, string>(
          p => PointString( p ) ) );
    }

    /// <summary>
    /// Return a string representing the data of a
    /// curve. Currently includes detailed data of
    /// line and arc elements only.
    /// </summary>
    public static string CurveString( Curve c )
    {
      string s = c.GetType().Name.ToLower();

      XYZ p = c.GetEndPoint( 0 );
      XYZ q = c.GetEndPoint( 1 );

      s += string.Format( " {0} --> {1}",
        PointString( p ), PointString( q ) );

      // To list intermediate points or draw an
      // approximation using straight line segments,
      // we can access the curve tesselation, cf.
      // CurveTessellateString:

      //foreach( XYZ r in lc.Curve.Tessellate() )
      //{
      //}

      // List arc data:

      Arc arc = c as Arc;

      if( null != arc )
      {
        s += string.Format( " center {0} radius {1}",
          PointString( arc.Center ), arc.Radius );
      }

      // Todo: add support for other curve types
      // besides line and arc.

      return s;
    }

    /// <summary>
    /// Return a string for this curve with its
    /// tessellated point coordinates formatted
    /// to two decimal places.
    /// </summary>
    public static string CurveTessellateString(
      Curve curve )
    {
      return "curve tessellation "
        + PointArrayString( curve.Tessellate() );
    }

    /// <summary>
    /// Convert a UnitSymbolType enumeration value
    /// to a brief human readable abbreviation string.
    /// </summary>
    public static string UnitSymbolTypeString(
      UnitSymbolType u )
    {
      string s = u.ToString();

      Debug.Assert( s.StartsWith( "UST_" ),
        "expected UnitSymbolType enumeration value "
        + "to begin with 'UST_'" );

      s = s.Substring( 4 )
        .Replace( "_SUP_", "^" )
        .ToLower();

      return s;
    }
    #endregion // Formatting

    #region Display a message
    const string _caption = "The Building Coder";

    public static void InfoMsg( string msg )
    {
      Debug.WriteLine( msg );
      WinForms.MessageBox.Show( msg,
        _caption,
        WinForms.MessageBoxButtons.OK,
        WinForms.MessageBoxIcon.Information );
    }

    public static void InfoMsg2(
      string instruction,
      string content )
    {
      Debug.WriteLine( instruction + "\r\n" + content );
      TaskDialog d = new TaskDialog( _caption );
      d.MainInstruction = instruction;
      d.MainContent = content;
      d.Show();
    }

    public static void ErrorMsg( string msg )
    {
      Debug.WriteLine( msg );
      WinForms.MessageBox.Show( msg,
        _caption,
        WinForms.MessageBoxButtons.OK,
        WinForms.MessageBoxIcon.Error );
    }

    /// <summary>
    /// Return a string describing the given element:
    /// .NET type name,
    /// category name,
    /// family and symbol name for a family instance,
    /// element id and element name.
    /// </summary>
    public static string ElementDescription(
      Element e )
    {
      if( null == e )
      {
        return "<null>";
      }

      // For a wall, the element name equals the
      // wall type name, which is equivalent to the
      // family name ...

      FamilyInstance fi = e as FamilyInstance;

      string typeName = e.GetType().Name;

      string categoryName = ( null == e.Category )
        ? string.Empty
        : e.Category.Name + " ";

      string familyName = ( null == fi )
        ? string.Empty
        : fi.Symbol.Family.Name + " ";

      string symbolName = ( null == fi
        || e.Name.Equals( fi.Symbol.Name ) )
          ? string.Empty
          : fi.Symbol.Name + " ";

      return string.Format( "{0} {1}{2}{3}<{4} {5}>",
        typeName, categoryName, familyName,
        symbolName, e.Id.IntegerValue, e.Name );
    }

    /// <summary>
    /// Return a location for the given element using
    /// its LocationPoint Point property,
    /// LocationCurve start point, whichever 
    /// is available.
    /// </summary>
    /// <param name="p">Return element location point</param>
    /// <param name="e">Revit Element</param>
    /// <returns>True if a location point is available 
    /// for the given element, otherwise false.</returns>
    static public bool GetElementLocation(
      out XYZ p,
      Element e )
    {
      p = XYZ.Zero;
      bool rc = false;
      Location loc = e.Location;
      if( null != loc )
      {
        LocationPoint lp = loc as LocationPoint;
        if( null != lp )
        {
          p = lp.Point;
          rc = true;
        }
        else
        {
          LocationCurve lc = loc as LocationCurve;

          Debug.Assert( null != lc,
            "expected location to be either point or curve" );

          p = lc.Curve.GetEndPoint( 0 );
          rc = true;
        }
      }
      return rc;
    }

    /// <summary>
    /// Return the location point of a family instance or null.
    /// This null coalesces the location so you won't get an 
    /// error if the FamilyInstance is an invalid object.  
    /// </summary>
    public static XYZ GetFamilyInstanceLocation(
      FamilyInstance fi )
    {
      return ( (LocationPoint) fi?.Location )?.Point;
    }
    #endregion // Display a message

    #region Element Selection
    public static Element SelectSingleElement(
      UIDocument uidoc,
      string description )
    {
      if( ViewType.Internal == uidoc.ActiveView.ViewType )
      {
        TaskDialog.Show( "Error",
          "Cannot pick element in this view: "
          + uidoc.ActiveView.Name );

        return null;
      }

#if _2010
    sel.Elements.Clear();
    Element e = null;
    sel.StatusbarTip = "Please select " + description;
    if( sel.PickOne() )
    {
      ElementSetIterator elemSetItr
        = sel.Elements.ForwardIterator();
      elemSetItr.MoveNext();
      e = elemSetItr.Current as Element;
    }
    return e;
#endif // _2010

      try
      {
        Reference r = uidoc.Selection.PickObject(
          ObjectType.Element,
          "Please select " + description );

        // 'Autodesk.Revit.DB.Reference.Element' is
        // obsolete: Property will be removed. Use
        // Document.GetElement(Reference) instead.
        //return null == r ? null : r.Element; // 2011

        return uidoc.Document.GetElement( r ); // 2012
      }
      catch( Autodesk.Revit.Exceptions.OperationCanceledException )
      {
        return null;
      }
    }

    public static Element GetSingleSelectedElement(
      UIDocument uidoc )
    {
      ICollection<ElementId> ids
        = uidoc.Selection.GetElementIds();

      Element e = null;

      if( 1 == ids.Count )
      {
        foreach( ElementId id in ids )
        {
          e = uidoc.Document.GetElement( id );
        }
      }
      return e;
    }

    static bool HasRequestedType(
      Element e,
      Type t,
      bool acceptDerivedClass )
    {
      bool rc = null != e;

      if( rc )
      {
        Type t2 = e.GetType();

        rc = t2.Equals( t );

        if( !rc && acceptDerivedClass )
        {
          rc = t2.IsSubclassOf( t );
        }
      }
      return rc;
    }

    public static Element SelectSingleElementOfType(
      UIDocument uidoc,
      Type t,
      string description,
      bool acceptDerivedClass )
    {
      Element e = GetSingleSelectedElement( uidoc );

      if( !HasRequestedType( e, t, acceptDerivedClass ) )
      {
        e = Util.SelectSingleElement(
          uidoc, description );
      }
      return HasRequestedType( e, t, acceptDerivedClass )
        ? e
        : null;
    }

    /// <summary>
    /// Retrieve all pre-selected elements of the specified type,
    /// if any elements at all have been pre-selected. If not,
    /// retrieve all elements of specified type in the database.
    /// </summary>
    /// <param name="a">Return value container</param>
    /// <param name="uidoc">Active document</param>
    /// <param name="t">Specific type</param>
    /// <returns>True if some elements were retrieved</returns>
    public static bool GetSelectedElementsOrAll(
      List<Element> a,
      UIDocument uidoc,
      Type t )
    {
      Document doc = uidoc.Document;

      ICollection<ElementId> ids
        = uidoc.Selection.GetElementIds();

      if( 0 < ids.Count )
      {
        a.AddRange( ids
          .Select<ElementId, Element>(
            id => doc.GetElement( id ) )
          .Where<Element>(
            e => t.IsInstanceOfType( e ) ) );
      }
      else
      {
        a.AddRange( new FilteredElementCollector( doc )
          .OfClass( t ) );
      }
      return 0 < a.Count;
    }
    #endregion // Element Selection

    #region Element filtering
    /// <summary>
    /// Return all elements of the requested class i.e. System.Type
    /// matching the given built-in category in the given document.
    /// </summary>
    public static FilteredElementCollector GetElementsOfType(
      Document doc,
      Type type,
      BuiltInCategory bic )
    {
      FilteredElementCollector collector
        = new FilteredElementCollector( doc );

      collector.OfCategory( bic );
      collector.OfClass( type );

      return collector;
    }

    /// <summary>
    /// Return the first element of the given type and name.
    /// </summary>
    public static Element GetFirstElementOfTypeNamed(
      Document doc,
      Type type,
      string name )
    {
      FilteredElementCollector collector
        = new FilteredElementCollector( doc )
          .OfClass( type );

#if EXPLICIT_CODE

      // explicit iteration and manual checking of a property:

      Element ret = null;
      foreach( Element e in collector )
      {
        if( e.Name.Equals( name ) )
        {
          ret = e;
          break;
        }
      }
      return ret;
#endif // EXPLICIT_CODE

#if USE_LINQ

      // using LINQ:

      IEnumerable<Element> elementsByName =
        from e in collector
        where e.Name.Equals( name )
        select e;

      return elementsByName.First<Element>();
#endif // USE_LINQ

      // using an anonymous method:

      // if no matching elements exist, First<> throws an exception.

      //return collector.Any<Element>( e => e.Name.Equals( name ) )
      //  ? collector.First<Element>( e => e.Name.Equals( name ) )
      //  : null;

      // using an anonymous method to define a named method:

      Func<Element, bool> nameEquals = e => e.Name.Equals( name );

      return collector.Any<Element>( nameEquals )
        ? collector.First<Element>( nameEquals )
        : null;
    }

    /// <summary>
    /// Return the first 3D view which is not a template,
    /// useful for input to FindReferencesByDirection().
    /// In this case, one cannot use FirstElement() directly,
    /// since the first one found may be a template and
    /// unsuitable for use in this method.
    /// This demonstrates some interesting usage of
    /// a .NET anonymous method.
    /// </summary>
    public static Element GetFirstNonTemplate3dView( Document doc )
    {
      FilteredElementCollector collector
        = new FilteredElementCollector( doc );

      collector.OfClass( typeof( View3D ) );

      return collector
        .Cast<View3D>()
        .First<View3D>( v3 => !v3.IsTemplate );
    }

    /// <summary>
    /// Given a specific family and symbol name,
    /// return the appropriate family symbol.
    /// </summary>
    public static FamilySymbol FindFamilySymbol(
      Document doc,
      string familyName,
      string symbolName )
    {
      FilteredElementCollector collector
        = new FilteredElementCollector( doc )
          .OfClass( typeof( Family ) );

      foreach( Family f in collector )
      {
        if( f.Name.Equals( familyName ) )
        {
          //foreach( FamilySymbol symbol in f.Symbols ) // 2014

          ISet<ElementId> ids = f.GetFamilySymbolIds(); // 2015

          foreach( ElementId id in ids )
          {
            FamilySymbol symbol = doc.GetElement( id )
              as FamilySymbol;

            if( symbol.Name == symbolName )
            {
              return symbol;
            }
          }
        }
      }
      return null;
    }
    #endregion // Element filtering

    #region MEP utilities
    /// <summary>
    /// Return the given element's connector manager, 
    /// using either the family instance MEPModel or 
    /// directly from the MEPCurve connector manager
    /// for ducts and pipes.
    /// </summary>
    static ConnectorManager GetConnectorManager(
      Element e )
    {
      MEPCurve mc = e as MEPCurve;
      FamilyInstance fi = e as FamilyInstance;

      if( null == mc && null == fi )
      {
        throw new ArgumentException(
          "Element is neither an MEP curve nor a fitting." );
      }

      return null == mc
        ? fi.MEPModel.ConnectorManager
        : mc.ConnectorManager;
    }

    /// <summary>
    /// Return the element's connector at the given
    /// location, and its other connector as well, 
    /// in case there are exactly two of them.
    /// </summary>
    /// <param name="e">An element, e.g. duct, pipe or family instance</param>
    /// <param name="location">The location of one of its connectors</param>
    /// <param name="otherConnector">The other connector, in case there are just two of them</param>
    /// <returns>The connector at the given location</returns>
    static Connector GetConnectorAt(
      Element e,
      XYZ location,
      out Connector otherConnector )
    {
      otherConnector = null;

      Connector targetConnector = null;

      ConnectorManager cm = GetConnectorManager( e );

      bool hasTwoConnectors = 2 == cm.Connectors.Size;

      foreach( Connector c in cm.Connectors )
      {
        if( c.Origin.IsAlmostEqualTo( location ) )
        {
          targetConnector = c;

          if( !hasTwoConnectors )
          {
            break;
          }
        }
        else if( hasTwoConnectors )
        {
          otherConnector = c;
        }
      }
      return targetConnector;
    }

    /// <summary>
    /// Return the connector set element
    /// closest to the given point.
    /// </summary>
    static Connector GetConnectorClosestTo(
      ConnectorSet connectors,
      XYZ p )
    {
      Connector targetConnector = null;
      double minDist = double.MaxValue;

      foreach( Connector c in connectors )
      {
        double d = c.Origin.DistanceTo( p );

        if( d < minDist )
        {
          targetConnector = c;
          minDist = d;
        }
      }
      return targetConnector;
    }

    /// <summary>
    /// Return the connector on the element 
    /// closest to the given point.
    /// </summary>
    public static Connector GetConnectorClosestTo(
      Element e,
      XYZ p )
    {
      ConnectorManager cm = GetConnectorManager( e );

      return null == cm
        ? null
        : GetConnectorClosestTo( cm.Connectors, p );
    }

    /// <summary>
    /// Connect two MEP elements at a given point p.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown if
    /// one of the given elements lacks connectors.
    /// </exception>
    public static void Connect(
      XYZ p,
      Element a,
      Element b )
    {
      ConnectorManager cm = GetConnectorManager( a );

      if( null == cm )
      {
        throw new ArgumentException(
          "Element a has no connectors." );
      }

      Connector ca = GetConnectorClosestTo(
        cm.Connectors, p );

      cm = GetConnectorManager( b );

      if( null == cm )
      {
        throw new ArgumentException(
          "Element b has no connectors." );
      }

      Connector cb = GetConnectorClosestTo(
        cm.Connectors, p );

      ca.ConnectTo( cb );
      //cb.ConnectTo( ca );
    }

    /// <summary>
    /// Compare Connector objects based on their location point.
    /// </summary>
    public class ConnectorXyzComparer : IEqualityComparer<Connector>
    {
      public bool Equals( Connector x, Connector y )
      {
        return null != x
          && null != y
          && IsEqual( x.Origin, y.Origin );
      }

      public int GetHashCode( Connector x )
      {
        return HashString( x.Origin ).GetHashCode();
      }
    }

    /// <summary>
    /// Get distinct connectors from a set of MEP elements.
    /// </summary>
    public static HashSet<Connector> GetDistinctConnectors(
      List<Connector> cons )
    {
      return cons.Distinct( new ConnectorXyzComparer() )
        .ToHashSet();
    }
    #endregion // MEP utilities

    #region Compatibility fix for spelling error change
    /// <summary>
    /// Wrapper to fix a spelling error prior to Revit 2016.
    /// </summary>
    public class SpellingErrorCorrector
    {
      static bool _in_revit_2015_or_earlier;
      static Type _external_definition_creation_options_type;

      public SpellingErrorCorrector( Application app )
      {
        _in_revit_2015_or_earlier = 0
          <= app.VersionNumber.CompareTo( "2015" );

        string s
          = _in_revit_2015_or_earlier
            ? "ExternalDefinitonCreationOptions"
            : "ExternalDefinitionCreationOptions";

        _external_definition_creation_options_type
          = System.Reflection.Assembly
            .GetExecutingAssembly().GetType( s );
      }

      object NewExternalDefinitionCreationOptions(
        string name,
        ParameterType parameterType )
      {
        object[] args = new object[] {
          name, parameterType };

        return _external_definition_creation_options_type
          .GetConstructor( new Type[] {
            _external_definition_creation_options_type } )
          .Invoke( args );
      }

      public Definition NewDefinition(
        Definitions definitions,
        string name,
        ParameterType parameterType )
      {
        //return definitions.Create( 
        //  NewExternalDefinitionCreationOptions() );

        object opt
          = NewExternalDefinitionCreationOptions(
            name,
            parameterType );

        return typeof( Definitions ).InvokeMember(
          "Create", BindingFlags.InvokeMethod, null,
          definitions, new object[] { opt } )
          as Definition;
      }
    }
    #endregion // Compatibility fix for spelling error change
  }

  #region Extension Method Classes

  public static class IEnumerableExtensions
  {
    // (C) Jonathan Skeet
    // from https://github.com/morelinq/MoreLINQ/blob/master/MoreLinq/MinBy.cs
    public static tsource MinBy<tsource, tkey>(
      this IEnumerable<tsource> source,
      Func<tsource, tkey> selector )
    {
      return source.MinBy( selector, Comparer<tkey>.Default );
    }

    public static tsource MinBy<tsource, tkey>(
      this IEnumerable<tsource> source,
      Func<tsource, tkey> selector,
      IComparer<tkey> comparer )
    {
      if( source == null ) throw new ArgumentNullException( nameof( source ) );
      if( selector == null ) throw new ArgumentNullException( nameof( selector ) );
      if( comparer == null ) throw new ArgumentNullException( nameof( comparer ) );
      using( IEnumerator<tsource> sourceIterator = source.GetEnumerator() )
      {
        if( !sourceIterator.MoveNext() )
          throw new InvalidOperationException( "Sequence was empty" );
        tsource min = sourceIterator.Current;
        tkey minKey = selector( min );
        while( sourceIterator.MoveNext() )
        {
          tsource candidate = sourceIterator.Current;
          tkey candidateProjected = selector( candidate );
          if( comparer.Compare( candidateProjected, minKey ) < 0 )
          {
            min = candidate;
            minKey = candidateProjected;
          }
        }
        return min;
      }
    }

    /// <summary>
    /// Create HashSet from IEnumerable given selector and comparer.
    /// http://geekswithblogs.net/BlackRabbitCoder/archive/2011/03/31/c.net-toolbox-adding-a-tohashset-extension-method.aspx
    /// </summary>
    public static HashSet<TElement> ToHashSet<TSource, TElement>(
      this IEnumerable<TSource> source,
      Func<TSource, TElement> elementSelector,
      IEqualityComparer<TElement> comparer )
    {
      if( source == null ) throw new ArgumentNullException( "source" );
      if( elementSelector == null ) throw new ArgumentNullException( "elementSelector" );

      // you can unroll this into a foreach if you want efficiency gain, but for brevity...
      return new HashSet<TElement>(
        source.Select( elementSelector ), comparer );
    }

    /// <summary>
    /// Create a HashSet of TSource from an IEnumerable 
    /// of TSource using the identity selector and 
    /// default equality comparer.
    /// </summary>
    public static HashSet<TSource> ToHashSet<TSource>(
      this IEnumerable<TSource> source )
    {
      // key selector is identity fxn and null is default comparer
      return source.ToHashSet<TSource, TSource>(
        item => item, null );
    }

    /// <summary>
    /// Create a HashSet of TSource from an IEnumerable 
    /// of TSource using the identity selector and 
    /// specified equality comparer.
    /// </summary>
    public static HashSet<TSource> ToHashSet<TSource>(
      this IEnumerable<TSource> source,
      IEqualityComparer<TSource> comparer )
    {
      return source.ToHashSet<TSource, TSource>(
        item => item, comparer );
    }

    /// <summary>
    /// Create a HashSet of TElement from an IEnumerable 
    /// of TSource using the specified element selector 
    /// and default equality comparer.
    /// </summary>
    public static HashSet<TElement> ToHashSet<TSource, TElement>(
      this IEnumerable<TSource> source,
      Func<TSource, TElement> elementSelector )
    {
      return source.ToHashSet<TSource, TElement>(
        elementSelector, null );
    }
  }

  public static class JtElementExtensionMethods
  {
    /// <summary>
    /// Predicate to determine whether given element 
    /// is a physical element, i.e. valid category,
    /// not view specific, etc.
    /// </summary>
    public static bool IsPhysicalElement(
      this Element e )
    {
      if( e.Category == null ) return false;
      // does this produce same result as 
      // WhereElementIsViewIndependent ?
      if( e.ViewSpecific ) return false;
      // exclude specific unwanted categories
      if( ( (BuiltInCategory) e.Category.Id.IntegerValue )
        == BuiltInCategory.OST_HVAC_Zones )
      {
        return false;
      }
      return e.Category.CategoryType == CategoryType.Model
        && e.Category.CanAddSubcategory;
    }

    /// <summary>
    /// Return the curve from a Revit database Element 
    /// location curve, if it has one.
    /// </summary>
    public static Curve GetCurve( this Element e )
    {
      Debug.Assert( null != e.Location,
        "expected an element with a valid Location" );

      LocationCurve lc = e.Location as LocationCurve;

      Debug.Assert( null != lc,
        "expected an element with a valid LocationCurve" );

      return lc.Curve;
    }
  }

  public static class JtElementIdExtensionMethods
  {
    /// <summary>
    /// Predicate returning true for invalid element ids.
    /// </summary>
    public static bool IsInvalid( this ElementId id )
    {
      return ElementId.InvalidElementId == id;
    }
    /// <summary>
    /// Predicate returning true for valid element ids.
    /// </summary>
    public static bool IsValid( this ElementId id )
    {
      return !IsInvalid( id );
    }
  }

  public static class JtLineExtensionMethods
  {
    /// <summary>
    /// Return true if the given point is very close 
    /// to this line, within a very narrow ellipse
    /// whose focal points are the line start and end.
    /// The tolerance is defined as (1 - e) using the 
    /// eccentricity e. e = 0 means we have a circle; 
    /// The closer e is to 1, the more elongated the 
    /// shape of the ellipse.
    /// https://en.wikipedia.org/wiki/Ellipse#Eccentricity
    /// </summary>
    public static bool Contains(
      this Line line,
      XYZ p,
      double tolerance = Util._eps )
    {
      XYZ a = line.GetEndPoint( 0 ); // line start point
      XYZ b = line.GetEndPoint( 1 ); // line end point
      double f = a.DistanceTo( b ); // distance between focal points
      double da = a.DistanceTo( p );
      double db = p.DistanceTo( b );
      // da + db is always greater or equal f
      return ( ( da + db ) - f ) * f < tolerance;
    }
  }

  public static class JtBoundingBoxXyzExtensionMethods
  {
    /// <summary>
    /// Expand the given bounding box to include 
    /// and contain the given point.
    /// </summary>
    public static void ExpandToContain(
      this BoundingBoxXYZ bb,
      XYZ p )
    {
      bb.Min = new XYZ( Math.Min( bb.Min.X, p.X ),
        Math.Min( bb.Min.Y, p.Y ),
        Math.Min( bb.Min.Z, p.Z ) );

      bb.Max = new XYZ( Math.Max( bb.Max.X, p.X ),
        Math.Max( bb.Max.Y, p.Y ),
        Math.Max( bb.Max.Z, p.Z ) );
    }

    /// <summary>
    /// Expand the given bounding box to include 
    /// and contain the given other one.
    /// </summary>
    public static void ExpandToContain(
      this BoundingBoxXYZ bb,
      BoundingBoxXYZ other )
    {
      bb.ExpandToContain( other.Min );
      bb.ExpandToContain( other.Max );
    }
  }

  public static class JtPlaneExtensionMethods
  {
    /// <summary>
    /// Return the signed distance from 
    /// a plane to a given point.
    /// </summary>
    public static double SignedDistanceTo(
      this Plane plane,
      XYZ p )
    {
      Debug.Assert(
        Util.IsEqual( plane.Normal.GetLength(), 1 ),
        "expected normalised plane normal" );

      XYZ v = p - plane.Origin;

      return plane.Normal.DotProduct( v );
    }

    /// <summary>
    /// Project given 3D XYZ point onto plane.
    /// </summary>
    public static XYZ ProjectOnto(
      this Plane plane,
      XYZ p )
    {
      double d = plane.SignedDistanceTo( p );

      XYZ q = p + d * plane.Normal;

      Debug.Assert(
        Util.IsZero( plane.SignedDistanceTo( q ) ),
        "expected point on plane to have zero distance to plane" );

      return q;
    }

    /// <summary>
    /// Project given 3D XYZ point into plane, 
    /// returning the UV coordinates of the result 
    /// in the local 2D plane coordinate system.
    /// </summary>
    public static UV ProjectInto(
      this Plane plane,
      XYZ p )
    {
      XYZ q = plane.ProjectOnto( p );
      XYZ o = plane.Origin;
      XYZ d = q - o;
      double u = d.DotProduct( plane.XVec );
      double v = d.DotProduct( plane.YVec );
      return new UV( u, v );
    }
  }

  public static class JtEdgeArrayExtensionMethods
  {
    /// <summary>
    /// Return a polygon as a list of XYZ points from
    /// an EdgeArray. If any of the edges are curved,
    /// we retrieve the tessellated points, i.e. an
    /// approximation determined by Revit.
    /// </summary>
    public static List<XYZ> GetPolygon(
      this EdgeArray ea )
    {
      int n = ea.Size;

      List<XYZ> polygon = new List<XYZ>( n );

      foreach( Edge e in ea )
      {
        IList<XYZ> pts = e.Tessellate();

        n = polygon.Count;

        if( 0 < n )
        {
          Debug.Assert( pts[0]
            .IsAlmostEqualTo( polygon[n - 1] ),
            "expected last edge end point to "
            + "equal next edge start point" );

          polygon.RemoveAt( n - 1 );
        }
        polygon.AddRange( pts );
      }
      n = polygon.Count;

      Debug.Assert( polygon[0]
        .IsAlmostEqualTo( polygon[n - 1] ),
        "expected first edge start point to "
        + "equal last edge end point" );

      polygon.RemoveAt( n - 1 );

      return polygon;
    }
  }

  public static class JtFamilyParameterExtensionMethods
  {
    public static bool IsShared(
      this FamilyParameter familyParameter )
    {
      MethodInfo mi = familyParameter
        .GetType()
        .GetMethod( "getParameter",
          BindingFlags.Instance
          | BindingFlags.NonPublic );

      if( null == mi )
      {
        throw new InvalidOperationException(
          "Could not find getParameter method" );
      }

      var parameter = mi.Invoke( familyParameter,
        new object[] { } ) as Parameter;

      return parameter.IsShared;
    }
  }

  public static class JtFilteredElementCollectorExtensions
  {
    public static FilteredElementCollector OfClass<T>(
      this FilteredElementCollector collector )
        where T : Element
    {
      return collector.OfClass( typeof( T ) );
    }

    public static IEnumerable<T> OfType<T>(
      this FilteredElementCollector collector )
        where T : Element
    {
      return Enumerable.OfType<T>(
        collector.OfClass<T>() );
    }
  }
  #endregion // Extension Method Classes

  #region Compatibility Methods by Magson Leone
  /// <summary>
  /// These compatibility helper methods make use of 
  /// Reflection to determine which Revit method is
  /// available and call that. You can use these 
  /// methods to create an add-in that is compatible 
  /// across all versions of Revit from 2012 to 2016.
  /// </summary>
  public static class CompatibilityMethods
  {
    #region Autodesk.Revit.DB.Curve
    public static XYZ GetPoint2(
      this Curve curva,
      int i )
    {
      XYZ value = null;

      MethodInfo met = curva.GetType().GetMethod(
        "GetEndPoint",
        new Type[] { typeof( int ) } );

      if( met == null )
      {
        met = curva.GetType().GetMethod(
          "get_EndPoint",
          new Type[] { typeof( int ) } );
      }

      value = met.Invoke( curva, new object[] { i } )
        as XYZ;

      return value;
    }
    #endregion // Autodesk.Revit.DB.Curve

    #region Autodesk.Revit.DB.Definitions
    public static Definition Create2(
      this Definitions definitions,
      Document doc,
      string nome,
      ParameterType tipo,
      bool visibilidade )
    {
      // Does this need updating to check for 
      // ExternalDefinitionCreationOptions with
      // the additional 'i' in Revit 2016?

      Definition value = null;
      List<Type> ls = doc.GetType().Assembly
      .GetTypes().Where( a => a.IsClass && a
      .Name == "ExternalDefinitonCreationOptions" ).ToList();
      if( ls.Count > 0 )
      {
        Type t = ls[0];
        ConstructorInfo c = t
        .GetConstructor( new Type[] { typeof(string),
                typeof(ParameterType) } );
        object ed = c
        .Invoke( new object[] { nome, tipo } );
        ed.GetType().GetProperty( "Visible" )
        .SetValue( ed, visibilidade, null );
        value = definitions.GetType()
        .GetMethod( "Create", new Type[] { t } ).Invoke( definitions,
          new object[] { ed } ) as Definition;
      }
      else
      {
        value = definitions.GetType()
        .GetMethod( "Create", new Type[] { typeof(string),
                typeof(ParameterType), typeof(bool) } ).Invoke( definitions,
          new object[] { nome, tipo,
                visibilidade } ) as Definition;
      }
      return value;
    }
    #endregion // Autodesk.Revit.DB.Definitions

    #region Autodesk.Revit.DB.Document
    public static Element GetElement2(
      this Document doc,
      ElementId id )
    {
      Element value = null;
      MethodInfo met = doc.GetType()
      .GetMethod( "get_Element", new Type[] { typeof( ElementId ) } );
      if( met == null )
        met = doc.GetType()
        .GetMethod( "GetElement", new Type[] { typeof( ElementId ) } );
      value = met.Invoke( doc,
        new object[] { id } ) as Element;
      return value;
    }
    public static Element GetElement2( this Document
    doc, Reference refe )
    {
      Element value = null;
      value = doc.GetElement( refe );
      return value;
    }
    public static Line CreateLine2(
      this Document doc,
      XYZ p1, XYZ p2,
      bool bound = true )
    {
      Line value = null;
      object[] parametros = new object[] { p1,
            p2 };
      Type[] tipos = parametros.Select( a => a
      .GetType() ).ToArray();
      string metodo = "CreateBound";
      if( bound == false ) metodo =
        "CreateUnbound";
      MethodInfo met = typeof( Line )
      .GetMethod( metodo, tipos );
      if( met != null )
      {
        value = met.Invoke( null,
          parametros ) as Line;
      }
      else
      {
        parametros = new object[] { p1, p2,
                bound };
        tipos = parametros.Select( a => a
        .GetType() ).ToArray();
        value = doc.Application.Create
        .GetType().GetMethod( "NewLine", tipos ).Invoke( doc
        .Application.Create, parametros ) as Line;
      }
      return value;
    }
    public static Wall CreateWall2(
      this Document doc,
      Curve curve, ElementId wallTypeId,
      ElementId levelId, double height,
      double offset, bool flip,
      bool structural )
    {
      Wall value = null;
      object[] parametros = new object[] { doc,
            curve, wallTypeId, levelId, height, offset, flip,
            structural };
      Type[] tipos = parametros.Select( a => a
      .GetType() ).ToArray();
      MethodInfo met = typeof( Wall )
      .GetMethod( "Create", tipos );
      if( met != null )
      {
        value = met.Invoke( null,
          parametros ) as Wall;
      }
      else
      {
        parametros = new object[] { curve,
                (WallType)doc.GetElement2(wallTypeId), (Level)doc
              .GetElement2(levelId), height, offset, flip,
                structural };
        tipos = parametros.Select( a => a
        .GetType() ).ToArray();
        value = doc.Create.GetType()
        .GetMethod( "NewWall", tipos ).Invoke( doc.Create,
          parametros ) as Wall;
      }
      return value;
    }
    public static Arc CreateArc2(
      this Document doc,
      XYZ p1, XYZ p2, XYZ p3 )
    {
      Arc value = null;
      object[] parametros = new object[] { p1,
            p2, p3 };
      Type[] tipos = parametros.Select( a => a
      .GetType() ).ToArray();
      string metodo = "Create";
      MethodInfo met = typeof( Arc )
      .GetMethod( metodo, tipos );
      if( met != null )
      {
        value = met.Invoke( null,
          parametros ) as Arc;
      }
      else
      {
        value = doc.Application.Create
        .GetType().GetMethod( "NewArc", tipos ).Invoke( doc
        .Application.Create, parametros ) as Arc;
      }
      return value;
    }
    public static char GetDecimalSymbol2(
      this Document doc )
    {
      char valor = ',';
      MethodInfo met = doc.GetType()
      .GetMethod( "GetUnits" );
      if( met != null )
      {
        object temp = met.Invoke( doc, null );
        PropertyInfo prop = temp.GetType()
        .GetProperty( "DecimalSymbol" );
        object o = prop.GetValue( temp, null );
        if( o.ToString() == "Comma" )
          valor = ',';
        else
          valor = '.';
      }
      else
      {
        object temp = doc.GetType()
        .GetProperty( "ProjectUnit" ).GetValue( doc, null );
        PropertyInfo prop = temp.GetType()
        .GetProperty( "DecimalSymbolType" );
        object o = prop.GetValue( temp, null );
        if( o.ToString() == "DST_COMMA" )
          valor = ',';
        else
          valor = '.';
      }
      return valor;
    }
    public static void UnjoinGeometry2(
      this Document doc,
      Element firstElement,
      Element secondElement )
    {
      List<Type> ls = doc.GetType().Assembly
      .GetTypes().Where( a => a.IsClass && a
      .Name == "JoinGeometryUtils" ).ToList();
      object[] parametros = new object[] { doc,
            firstElement, secondElement };
      Type[] tipos = parametros.Select( a => a
      .GetType() ).ToArray();
      if( ls.Count > 0 )
      {
        Type t = ls[0];
        MethodInfo met = t
        .GetMethod( "UnjoinGeometry", tipos );
        met.Invoke( null, parametros );
      }
    }
    public static void JoinGeometry2(
      this Document doc,
      Element firstElement,
      Element secondElement )
    {
      List<Type> ls = doc.GetType().Assembly
      .GetTypes().Where( a => a.IsClass && a
      .Name == "JoinGeometryUtils" ).ToList();
      object[] parametros = new object[] { doc,
            firstElement, secondElement };
      Type[] tipos = parametros.Select( a => a
      .GetType() ).ToArray();
      if( ls.Count > 0 )
      {
        Type t = ls[0];
        MethodInfo met = t
        .GetMethod( "JoinGeometry", tipos );
        met.Invoke( null, parametros );
      }
    }
    public static bool IsJoined2(
      this Document doc,
      Element firstElement,
      Element secondElement )
    {
      bool value = false;
      List<Type> ls = doc.GetType().Assembly
      .GetTypes().Where( a => a.IsClass && a
      .Name == "JoinGeometryUtils" ).ToList();
      object[] parametros = new object[] { doc,
            firstElement, secondElement };
      Type[] tipos = parametros.Select( a => a
      .GetType() ).ToArray();
      if( ls.Count > 0 )
      {
        Type t = ls[0];
        MethodInfo met = t
        .GetMethod( "AreElementsJoined", tipos );
        value = (bool) met.Invoke( null,
          parametros );
      }
      return value;
    }
    public static bool CalculateVolumeArea2(
      this Document doc, bool value )
    {
      List<Type> ls = doc.GetType().Assembly
      .GetTypes().Where( a => a.IsClass && a
      .Name == "AreaVolumeSettings" ).ToList();
      if( ls.Count > 0 )
      {
        Type t = ls[0];
        object[] parametros = new object[] {
              doc };
        Type[] tipos = parametros
        .Select( a => a.GetType() ).ToArray();
        MethodInfo met = t
        .GetMethod( "GetAreaVolumeSettings", tipos );
        object temp = met.Invoke( null,
          parametros );
        temp.GetType()
        .GetProperty( "ComputeVolumes" ).SetValue( temp, value, null );
      }
      else
      {
        PropertyInfo prop = doc.Settings
        .GetType().GetProperty( "VolumeCalculationSetting" );
        object temp = prop.GetValue( doc
        .Settings, null );
        prop = temp.GetType()
        .GetProperty( "VolumeCalculationOptions" );
        temp = prop.GetValue( temp, null );
        prop = temp.GetType()
        .GetProperty( "VolumeComputationEnable" );
        prop.SetValue( temp, value, null );
      }
      return value;
    }
    public static Group CreateGroup2(
      this Document doc,
      List<Element> elementos )
    {
      Group value = null;
      ElementSet eleset = new ElementSet();
      foreach( Element ele in elementos )
      {
        eleset.Insert( ele );
      }
      ICollection<ElementId> col = elementos
      .Select( a => a.Id ).ToList();
      object obj = doc.Create;
      MethodInfo met = obj.GetType()
      .GetMethod( "NewGroup", new Type[] { col.GetType() } );
      if( met != null )
      {
        met.Invoke( obj, new object[] { col } );
      }
      else
      {
        met = obj.GetType()
        .GetMethod( "NewGroup", new Type[] { eleset.GetType() } );
        met.Invoke( obj,
          new object[] { eleset } );
      }
      return value;
    }
    public static void Delete2(
      this Document doc,
      Element ele )
    {
      object obj = doc;
      MethodInfo met = obj.GetType()
      .GetMethod( "Delete", new Type[] { typeof( Element ) } );
      if( met != null )
      {
        met.Invoke( obj, new object[] { ele } );
      }
      else
      {
        met = obj.GetType()
        .GetMethod( "Delete", new Type[] { typeof( ElementId ) } );
        met.Invoke( obj, new object[] { ele
              .Id } );
      }
    }
    #endregion // Autodesk.Revit.DB.Document

    #region Autodesk.Revit.DB.Element
    public static Element Level2( this Element ele )
    {
      Element value = null;
      Document doc = ele.Document;
      Type t = ele.GetType();
      if( t.GetProperty( "Level" ) != null )
        value = t.GetProperty( "Level" )
        .GetValue( ele, null ) as Element;
      else
        value = doc.GetElement2( (ElementId) t
        .GetProperty( "LevelId" ).GetValue( ele, null ) );
      return value;
    }
    public static List<Material> Materiais2(
      this Element ele )
    {
      List<Material> value = new List<Material>();
      Document doc = ele.Document;
      Type t = ele.GetType();
      if( t.GetProperty( "Materials" ) != null )
        value = ( (IEnumerable) t
        .GetProperty( "Materials" ).GetValue( ele, null ) ).Cast<Material>()
        .ToList();
      else
      {
        MethodInfo met = t
        .GetMethod( "GetMaterialIds", new Type[] { typeof( bool ) } );
        value = ( (ICollection<ElementId>) met
        .Invoke( ele, new object[] { false } ) )
        .Select( a => doc.GetElement2( a ) ).Cast<Material>().ToList();
      }
      return value;
    }
    public static Parameter GetParameter2(
      this Element ele, string nome_paramentro )
    {
      Parameter value = null;
      Type t = ele.GetType();
      MethodInfo met = t
      .GetMethod( "LookupParameter", new Type[] { typeof( string ) } );
      if( met == null )
        met = t.GetMethod( "get_Parameter",
          new Type[] { typeof( string ) } );
      value = met.Invoke( ele,
        new object[] { nome_paramentro } ) as Parameter;
      if( value == null )
      {
        var pas = ele.Parameters
        .Cast<Parameter>().ToList();
        if( pas.Exists( a => a.Definition
        .Name.ToLower() == nome_paramentro.Trim().ToLower() ) )
          value = pas.First( a => a
          .Definition.Name.ToLower() == nome_paramentro.Trim()
          .ToLower() );
      }
      return value;
    }
    public static Parameter GetParameter2(
      this Element ele,
      BuiltInParameter builtInParameter )
    {
      Parameter value = null;
      Type t = ele.GetType();
      MethodInfo met = t
      .GetMethod( "LookupParameter", new Type[] { typeof( BuiltInParameter ) } );
      if( met == null )
        met = t.GetMethod( "get_Parameter",
          new Type[] { typeof( BuiltInParameter ) } );
      value = met.Invoke( ele,
        new object[] { builtInParameter } ) as Parameter;
      return value;
    }
    public static double GetMaterialArea2(
      this Element ele, Material m )
    {
      double value = 0;
      Type t = ele.GetType();
      MethodInfo met = t
      .GetMethod( "GetMaterialArea", new Type[] { typeof(ElementId),
            typeof(bool) } );
      if( met != null )
      {
        value = (double) met.Invoke( ele,
          new object[] { m.Id, false } );
      }
      else
      {
        met = t.GetMethod( "GetMaterialArea",
          new Type[] { typeof( Element ) } );
        value = (double) met.Invoke( ele,
          new object[] { m } );
      }
      return value;
    }
    public static double GetMaterialVolume2(
      this Element ele, Material m )
    {
      double value = 0;
      Type t = ele.GetType();
      MethodInfo met = t
      .GetMethod( "GetMaterialVolume", new Type[] { typeof(ElementId),
            typeof(bool) } );
      if( met != null )
      {
        value = (double) met.Invoke( ele,
          new object[] { m.Id, false } );
      }
      else
      {
        met = t
        .GetMethod( "GetMaterialVolume", new Type[] { typeof( ElementId ) } );
        value = (double) met.Invoke( ele,
          new object[] { m.Id } );
      }
      return value;
    }
    public static List<GeometryObject>
      GetGeometricObjects2( this Element ele )
    {
      List<GeometryObject> value =
      new List<GeometryObject>();
      Options op = new Options();
      object obj = ele.get_Geometry( op );
      PropertyInfo prop = obj.GetType()
      .GetProperty( "Objects" );
      if( prop != null )
      {
        obj = prop.GetValue( obj, null );
        IEnumerable arr = obj as IEnumerable;
        foreach( GeometryObject geo in arr )
        {
          value.Add( geo );
        }
      }
      else
      {
        IEnumerable<GeometryObject> geos =
        obj as IEnumerable<GeometryObject>;
        foreach( var geo in geos )
        {
          value.Add( geo );
        }
      }
      return value;
    }
    #endregion // Autodesk.Revit.DB.Element

    #region Autodesk.Revit.DB.FamilySymbol
    public static void EnableFamilySymbol2(
      this FamilySymbol fsymbol )
    {
      MethodInfo met = fsymbol.GetType()
      .GetMethod( "Activate" );
      if( met != null )
      {
        met.Invoke( fsymbol, null );
      }
    }
    #endregion // Autodesk.Revit.DB.FamilySymbol

    #region Autodesk.Revit.DB.InternalDefinition
    public static void VaryGroup2(
      this InternalDefinition def, Document doc )
    {
      object[] parametros = new object[] { doc,
            true };
      Type[] tipos = parametros.Select( a => a
      .GetType() ).ToArray();
      MethodInfo met = def.GetType()
      .GetMethod( "SetAllowVaryBetweenGroups", tipos );
      if( met != null )
      {
        met.Invoke( def, parametros );
      }
    }
    #endregion // Autodesk.Revit.DB.InternalDefinition

    #region Autodesk.Revit.DB.Part
    public static ElementId GetSource2( this Part part )
    {
      ElementId value = null;
      PropertyInfo prop = part.GetType()
      .GetProperty( "OriginalDividedElementId" );
      if( prop != null )
        value = prop.GetValue( part,
          null ) as ElementId;
      else
      {
        MethodInfo met = part.GetType()
        .GetMethod( "GetSourceElementIds" );
        object temp = met.Invoke( part, null );
        met = temp.GetType()
        .GetMethod( "First" );
        temp = met.Invoke( temp, null );
        prop = temp.GetType()
        .GetProperty( "HostElementId" );
        value = prop.GetValue( temp,
          null ) as ElementId;
      }
      return value;
    }
    #endregion // Autodesk.Revit.DB.Part

    #region Autodesk.Revit.UI.Selection.Selection
    public static List<Element> GetSelection2(
      this Selection sel, Document doc )
    {
      List<Element> value = new List<Element>();
      sel.GetElementIds();
      Type t = sel.GetType();
      if( t.GetMethod( "GetElementIds" ) != null )
      {
        MethodInfo met = t
        .GetMethod( "GetElementIds" );
        value = ( (ICollection<ElementId>) met
        .Invoke( sel, null ) ).Select( a => doc.GetElement2( a ) )
        .ToList();
      }
      else
      {
        value = ( (IEnumerable) t
        .GetProperty( "Elements" ).GetValue( sel, null ) ).Cast<Element>()
        .ToList();
      }
      return value;
    }
    public static void SetSelection2(
      this Selection sel,
      Document doc,
      ICollection<ElementId> elementos )
    {
      sel.ClearSelection2();
      object[] parametros = new object[] {
          elementos };
      Type[] tipos = parametros.Select( a => a
      .GetType() ).ToArray();
      MethodInfo met = sel.GetType()
      .GetMethod( "SetElementIds", tipos );
      if( met != null )
      {
        met.Invoke( sel, parametros );
      }
      else
      {
        PropertyInfo prop = sel.GetType()
        .GetProperty( "Elements" );
        object temp = prop.GetValue( sel, null );
        if( elementos.Count == 0 )
        {
          met = temp.GetType()
          .GetMethod( "Clear" );
          met.Invoke( temp, null );
        }
        else
        {
          foreach( ElementId id in elementos )
          {
            Element elemento = doc
            .GetElement2( id );
            parametros = new object[] {
                      elemento };
            tipos = parametros
            .Select( a => a.GetType() ).ToArray();
            met = temp.GetType()
            .GetMethod( "Add", tipos );
            met.Invoke( temp, parametros );
          }
        }
      }
    }
    public static void ClearSelection2(
      this Selection sel )
    {
      PropertyInfo prop = sel.GetType()
        .GetProperty( "Elements" );
      if( prop != null )
      {
        object obj = prop.GetValue( sel, null );
        MethodInfo met = obj.GetType()
          .GetMethod( "Clear" );
        met.Invoke( obj, null );
      }
      else
      {
        ICollection<ElementId> ids
          = new List<ElementId>();
        MethodInfo met = sel.GetType().GetMethod(
          "SetElementIds", new Type[] { ids.GetType() } );
        met.Invoke( sel, new object[] { ids } );
      }
    }
    #endregion // Autodesk.Revit.UI.Selection.Selection

    #region Autodesk.Revit.UI.UIApplication
    public static System.Drawing.Rectangle
      GetDrawingArea2( this UIApplication ui )
    {
      System.Drawing.Rectangle value = System
      .Windows.Forms.Screen.PrimaryScreen.Bounds;
      return value;
    }
    #endregion // Autodesk.Revit.UI.UIApplication

    #region Autodesk.Revit.DB.View
    public static ElementId Duplicate2( this View view )
    {
      ElementId value = null;
      Document doc = view.Document;
      List<Type> ls = doc.GetType().Assembly.GetTypes()
        .Where( a => a.IsEnum
          && a.Name == "ViewDuplicateOption" )
        .ToList();
      if( ls.Count > 0 )
      {
        Type t = ls[0];
        object obj = view;
        MethodInfo met = view.GetType().GetMethod(
          "Duplicate", new Type[] { t } );
        if( met != null )
        {
          value = met.Invoke( obj,
            new object[] { 2 } ) as ElementId;
        }
      }
      return value;
    }
    public static void SetOverlayView2(
      this View view,
      List<ElementId> ids,
      Color cor = null,
      int espessura = -1 )
    {
      Document doc = view.Document;
      List<Type> ls = doc.GetType().Assembly
        .GetTypes().Where(
          a => a.IsClass
            && a.Name == "OverrideGraphicSettings" )
        .ToList();
      if( ls.Count > 0 )
      {
        Type t = ls[0];
        ConstructorInfo construtor = t
          .GetConstructor( new Type[] { } );
        construtor.Invoke( new object[] { } );
        object obj = construtor.Invoke( new object[] { } );
        MethodInfo met = obj.GetType()
          .GetMethod( "SetProjectionLineColor",
            new Type[] { cor.GetType() } );
        met.Invoke( obj, new object[] { cor } );
        met = obj.GetType()
          .GetMethod( "SetProjectionLineWeight",
            new Type[] { espessura.GetType() } );
        met.Invoke( obj, new object[] { espessura } );
        met = view.GetType()
          .GetMethod( "SetElementOverrides",
            new Type[] { typeof(ElementId),
            obj.GetType() } );
        foreach( ElementId id in ids )
        {
          met.Invoke( view, new object[] { id, obj } );
        }
      }
      else
      {
        MethodInfo met = view.GetType()
          .GetMethod( "set_ProjColorOverrideByElement",
            new Type[] { typeof( ICollection<ElementId> ),
            typeof( Color ) } );
        met.Invoke( view, new object[] { ids, cor } );
        met = view.GetType()
          .GetMethod( "set_ProjLineWeightOverrideByElement",
            new Type[] { typeof( ICollection<ElementId> ),
            typeof( int ) } );
        met.Invoke( view, new object[] { ids, espessura } );
      }
    }
    #endregion // Autodesk.Revit.DB.View

    #region Autodesk.Revit.DB.Viewplan
    public static ElementId GetViewTemplateId2(
      this ViewPlan view )
    {
      ElementId value = null;
      PropertyInfo prop = view.GetType()
        .GetProperty( "ViewTemplateId" );
      if( prop != null )
      {
        value = prop.GetValue( view,
          null ) as ElementId;
      }
      return value;
    }
    public static void SetViewTemplateId2(
      this ViewPlan view,
      ElementId id )
    {
      PropertyInfo prop = view.GetType()
        .GetProperty( "ViewTemplateId" );
      if( prop != null )
      {
        prop.SetValue( view, id, null );
      }
    }
    #endregion // Autodesk.Revit.DB.Viewplan

    #region Autodesk.Revit.DB.Wall
    public static void FlipWall2( this Wall wall )
    {
      string metodo = "Flip";
      MethodInfo met = typeof( Wall )
        .GetMethod( metodo );
      if( met != null )
      {
        met.Invoke( wall, null );
      }
      else
      {
        metodo = "flip";
        met = typeof( Wall ).GetMethod( metodo );
        met.Invoke( wall, null );
      }
    }
    #endregion // Autodesk.Revit.DB.Wall
  }
  #endregion // Compatibility Methods by Magson Leone
}
