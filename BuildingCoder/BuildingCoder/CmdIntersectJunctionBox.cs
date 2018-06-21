#region Header
//
// CmdIntersectJunctionBox.cs - determine conduits intersecting junction box
//
// Copyright (C) 2018 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI;
using System.Diagnostics;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.ReadOnly )]
  class CmdIntersectJunctionBox : IExternalCommand
  {
    #region Intersect strange result
    /// <summary>
    /// Return all faces from first solid  
    /// of the given element.
    /// </summary>
    IEnumerable<Face> GetFaces( Element e )
    {
      Options opt = new Options();
      IEnumerable<Face> faces = e
        .get_Geometry( opt )
        .OfType<Solid>()
        .First()
        .Faces
        .OfType<Face>();
      int n = faces.Count();
      Debug.Print( "{0} has {1} face{2}.", 
        e.GetType().Name, n, Util.PluralSuffix( n ) );
      return faces;
    }

    // This has been mentioned in this post from 2016 but maybe it's worth bringing up again as 2018 hasn't resolved the issue yet.
    // As far as I can tell, the Face.Intersect( face) method always returns FaceIntersectionFaceResult.Intersecting - or I am not implementing it correctly.When I run the code below in a view with a single wall and single floor, each face to face test returns an intersection. Can someone please verify (maybe 2019)?
    // https://forums.autodesk.com/t5/revit-api-forum/get-conection-type-and-geometry-between-two-elements-from-the/m-p/6465671
    // https://forums.autodesk.com/t5/revit-api-forum/surprising-results-from-face-intersect-face-method/m-p/8079881
    // /a/doc/revit/tbc/git/a/img/intersect_strange_result.png
    // /a/doc/revit/tbc/git/a/img/floor_wall_disjunct.png
    void TestIntersect( Document doc )
    {
      View view = doc.ActiveView;

      var list = new FilteredElementCollector( doc, view.Id )
        .WhereElementIsNotElementType()
        .Where( e => e is Wall || e is Floor );

      int n = list.Count();

      Element floor = null;
      Element wall = null;

      if( 2 == n )
      {
        floor = list.First() as Floor;
        if( null == floor )
        {
          floor = list.Last() as Floor;
          wall = list.First() as Wall;
        }
        else
        {
          wall = list.Last() as Wall;
        }
      }

      if( null == floor || null == wall )
      {
        Util.ErrorMsg( "Please run this command in a "
          + "document with just one floor and one wall "
          + "with no mutual intersection" );
      }
      else
      {
        Options opt = new Options();
        IEnumerable<Face> floorFaces = GetFaces( floor );
        IEnumerable<Face> wallFaces = GetFaces( wall );
        n = 0;
        foreach( var f1 in floorFaces )
        {
          foreach( var f2 in wallFaces )
          {
            if( f1.Intersect( f2 ) 
              == FaceIntersectionFaceResult.Intersecting )
            {
              ++n;

              if( System.Windows.Forms.MessageBox.Show(
                "Intersects", "Continue",
                System.Windows.Forms.MessageBoxButtons.OKCancel,
                System.Windows.Forms.MessageBoxIcon.Exclamation )
                  == System.Windows.Forms.DialogResult.Cancel )
              {
                return;
              }
            }
          }
        }
        Debug.Print( "{0} face-face intersection{1}.", 
          n, Util.PluralSuffix( n ) );
      }
    }
    #endregion

    #region Tiago Cerqueira
    class FindIntersection
    {
      public Conduit ConduitRun { get; set; }

      public FamilyInstance Jbox { get; set; }

      public List<Conduit> GetListOfConduits = new List<Conduit>();

      public FindIntersection(
        FamilyInstance jbox,
        UIDocument uiDoc )
      {
        XYZ jboxPoint = ( jbox.Location
          as LocationPoint ).Point;

        FilteredElementCollector filteredCloserConduits
          = new FilteredElementCollector( uiDoc.Document );

        List<Element> listOfCloserConduit
          = filteredCloserConduits
            .OfClass( typeof( Conduit ) )
            .ToList()
            .Where( x
              => ( ( x as Conduit ).Location as LocationCurve ).Curve
                .GetEndPoint( 0 ).DistanceTo( jboxPoint ) < 30
              || ( ( x as Conduit ).Location as LocationCurve ).Curve
                .GetEndPoint( 1 ).DistanceTo( jboxPoint ) < 30 )
            .ToList();

        // getting the location of the box and all conduit around.

        Options opt = new Options();
        opt.View = uiDoc.ActiveView;
        GeometryElement geoEle = jbox.get_Geometry( opt );

        // getting the geometry of the element to 
        // access the geometry of the instance.

        foreach( GeometryObject geomObje1 in geoEle )
        {
          GeometryElement geoInstance = ( geomObje1
            as GeometryInstance ).GetInstanceGeometry();

          // the geometry of the family instance can be 
          // accessed by this method that returns a 
          // GeometryElement type. so we must get the 
          // GeometryObject again to access the Face of 
          // the family instance.

          if( geoInstance != null )
          {
            foreach( GeometryObject geomObje2 in geoInstance )
            {
              Solid geoSolid = geomObje2 as Solid;
              if( geoSolid != null )
              {
                foreach( Face face in geoSolid.Faces )
                {
                  foreach( Element cond in listOfCloserConduit )
                  {
                    Conduit con = cond as Conduit;
                    Curve conCurve = ( con.Location as LocationCurve ).Curve;
                    SetComparisonResult set = face.Intersect( conCurve );
                    if( set.ToString() == "Overlap" )
                    {
                      //getting the conduit the intersect the box.

                      GetListOfConduits.Add( con );
                    }
                  }
                }
              }
            }
          }
        }
      }
    }
    #endregion // Tiago Cerqueira

    public Result Execute(
      ExternalCommandData commandData,
      ref String message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      UIDocument uidoc = app.ActiveUIDocument;
      Document doc = uidoc.Document;

      bool test_strange_intersect_result = true;
      if( test_strange_intersect_result )
      {
        TestIntersect( doc );
        return Result.Succeeded;
      }

      Element e = Util.SelectSingleElement(
        uidoc, "a junction box" );

      BoundingBoxXYZ bb = e.get_BoundingBox( null );

      Outline outLne = new Outline( bb.Min, bb.Max );

      // Use a quick bounding box filter - axis aligned

      ElementQuickFilter fbb
        = new BoundingBoxIntersectsFilter( outLne );

      FilteredElementCollector conduits
        = new FilteredElementCollector( doc )
          .OfClass( typeof( Conduit ) )
          .WherePasses( fbb );

      // How many elements did we find?

      int nbb = conduits.GetElementCount();

      // Use a slow intersection filter - exact results

      ElementSlowFilter intersect_junction
        = new ElementIntersectsElementFilter( e );

      conduits = new FilteredElementCollector( doc )
          .OfClass( typeof( Conduit ) )
          .WherePasses( intersect_junction );

      // How many elements did we find?

      int nintersect = conduits.GetElementCount();

      Debug.Assert( nintersect <= nbb,
        "expected element intersection to be stricter"
        + "than bounding box containment" );

      return Result.Succeeded;
    }
  }
}
