#region Header
//
// Util.cs - The Building Coder Revit API utility methods
//
// Copyright (C) 2008-2013 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
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
#endregion // Namespaces

namespace BuildingCoder
{
  class Util
  {
    #region Geometrical Comparison
    const double _eps = 1.0e-9;

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
      return IsVertical( f.Normal );
    }

    public static bool IsVertical( PlanarFace f )
    {
      return IsHorizontal( f.Normal );
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

    /// <summary>
    /// Return the midpoint between two points.
    /// </summary>
    public static XYZ Midpoint( XYZ p, XYZ q )
    {
      return p + 0.5 * ( q - p );
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
    #endregion // Geometrical Comparison

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

    const double _convertFootToMm = 12 * 25.4;

    const double _convertFootToMeter
      = _convertFootToMm * 0.001;

    const double _convertCubicFootToCubicMeter
      = _convertFootToMeter
      * _convertFootToMeter
      * _convertFootToMeter;

    /// <summary>
    /// Convert a given length in feet to millimetres.
    /// </summary>
    public static double FootToMm( double length )
    {
      return length * _convertFootToMm;
    }

    /// <summary>
    /// Convert a given length in millimetres to feet.
    /// </summary>
    public static double MmToFoot( double length )
    {
      return length / _convertFootToMm;
    }

    /// <summary>
    /// Convert a given point or vector from millimetres to feet.
    /// </summary>
    public static XYZ MmToFoot( XYZ v )
    {
      return v.Divide( _convertFootToMm );
    }

    /// <summary>
    /// Convert a given volume in feet to cubic meters.
    /// </summary>
    public static double CubicFootToCubicMeter( double volume )
    {
      return volume * _convertCubicFootToCubicMeter;
    }
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
    /// Return a string representation in degrees
    /// for an angle given in radians.
    /// </summary>
    public static string AngleString( double angle )
    {
      return RealString( angle * 180 / Math.PI ) + " degrees";
    }

    /// <summary>
    /// Return a string for a length in millimetres
    /// formatted to two decimal places.
    /// </summary>
    public static string MmString( double length )
    {
      return RealString( FootToMm( length ) ) + " mm";
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
    /// Return a string for this point array
    /// with its coordinates formatted to two
    /// decimal places.
    /// </summary>
    public static string PointArrayString( IList<XYZ> pts )
    {
      string s = string.Empty;
      foreach( XYZ p in pts )
      {
        if( 0 < s.Length )
        {
          s += ", ";
        }
        s += PointString( p );
      }
      return s;
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
    #endregion // Display a message

    #region Element Selection
    public static Element SelectSingleElement(
      UIDocument doc,
      string description )
    {
      Selection sel = doc.Selection;

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

      if( ViewType.Internal == doc.ActiveView.ViewType )
      {
        TaskDialog.Show( "Error",
          "Cannot pick element in this view: "
          + doc.ActiveView.Name );

        return null;
      }

      Reference r = null;

      try
      {
        r = sel.PickObject( ObjectType.Element,
          "Please select " + description );
      }
      catch( Autodesk.Revit.Exceptions.OperationCanceledException )
      {
        return null;
      }

      // 'Autodesk.Revit.DB.Reference.Element' is
      // obsolete: Property will be removed. Use
      // Document.GetElement(Reference) instead.
      //return null == r ? null : r.Element; // 2011

      return null == r
        ? null
        : doc.Document.GetElement( r ); // 2012
    }

    public static Element GetSingleSelectedElement(
      UIDocument doc )
    {
      Element e = null;
      SelElementSet set = doc.Selection.Elements;

      if ( 1 == set.Size )
      {
        foreach ( Element e2 in set )
        {
          e = e2;
        }
      }
      return e;
    }

    static bool HasRequestedType(
      Element e,
      Type t,
      bool acceptDerivedClass )
    {
      return ( null != e )
        && ( acceptDerivedClass
          ? e.GetType().IsSubclassOf( t )
          : e.GetType().Equals( t ) );
    }

    public static Element SelectSingleElementOfType(
      UIDocument doc,
      Type t,
      string description,
      bool acceptDerivedClass )
    {
      Element e = GetSingleSelectedElement( doc );

      if( !HasRequestedType( e, t, acceptDerivedClass ) )
      {
        e = Util.SelectSingleElement( doc, description );
      }
      return ( null == e ) || HasRequestedType( e, t, acceptDerivedClass )
        ? e
        : null;
    }

    /// <summary>
    /// Retrieve all pre-selected elements of the specified type,
    /// if any elements at all have been pre-selected. If not,
    /// retrieve all elements of specified type in the database.
    /// </summary>
    /// <param name="a">Return value container</param>
    /// <param name="doc">Active document</param>
    /// <param name="t">Specific type</param>
    /// <returns>True if some elements were retrieved</returns>
    public static bool GetSelectedElementsOrAll(
      List<Element> a,
      UIDocument doc,
      Type t )
    {
      Selection sel = doc.Selection;
      if( 0 < sel.Elements.Size )
      {
        foreach( Element e in sel.Elements )
        {
          if( t.IsInstanceOfType( e ) )
          {
            a.Add( e );
          }
        }
      }
      else
      {
        FilteredElementCollector collector
          = new FilteredElementCollector( doc.Document );

        collector.OfClass( t );

        a.AddRange( collector );
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
          foreach( FamilySymbol symbol in f.Symbols )
          {
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
  }

  public static class JtPlaneExtensionMethods
  {
    /// <summary>
    /// Return signed distance from plane to a given point.
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
            .IsAlmostEqualTo( polygon[n-1] ),
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
}
