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
    #region Tiago Cerqueira
    class FindIntersection
    {
      public Conduit ConduitRun { get; set; }
      public FamilyInstance Jbox { get; set; }
      public List<Conduit> GetListOfConduits = new List<Conduit>();
      public FindIntersection( FamilyInstance jbox, UIDocument uiDoc )
      {
        XYZ jboxPoint = ( jbox.Location as LocationPoint ).Point;
        FilteredElementCollector filteredCloserConduits = new FilteredElementCollector( uiDoc.Document );
        List<Element> listOfCloserConduit = filteredCloserConduits.OfClass( typeof( Conduit ) ).ToList().Where( x =>
             ( ( x as Conduit ).Location as LocationCurve ).Curve.GetEndPoint( 0 ).DistanceTo( jboxPoint ) < 30 ||
             ( ( x as Conduit ).Location as LocationCurve ).Curve.GetEndPoint( 1 ).DistanceTo( jboxPoint ) < 30 ).ToList();
        //getting the location of the box and all conduit around.
        Options opt = new Options();
        opt.View = uiDoc.ActiveView;
        GeometryElement geoEle = jbox.get_Geometry( opt );
        //getting the geometry of the element to acess the geometry of the instance.
        foreach( GeometryObject geomObje1 in geoEle )
        {
          GeometryElement geoInstance = ( geomObje1 as GeometryInstance ).GetInstanceGeometry();
          //the geometry of the family instance can be acess by this method that returns a GeometryElement type.
          //so we must get the GeometryObject again to acess the Face of the famil y instance.
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

      Element e = Util.SelectSingleElement(
        uidoc, "a junction box" );

      BoundingBoxXYZ bb = e.get_BoundingBox( null );

      Outline outLne = new Outline( bb.Min, bb.Max );

      ElementQuickFilter fbb
        = new BoundingBoxIntersectsFilter( outLne );

      FilteredElementCollector conduits
        = new FilteredElementCollector( doc )
          .OfClass( typeof( Conduit ) )
          .WherePasses( fbb );

      int nbb = conduits.GetElementCount();

      ElementSlowFilter intersect_junction
        = new ElementIntersectsElementFilter( e );

      conduits = new FilteredElementCollector( doc )
          .OfClass( typeof( Conduit ) )
          .WherePasses( intersect_junction );

      int nintersect = conduits.GetElementCount();

      Debug.Assert( nbb <= nintersect, 
        "expected element intersection to be stricter"
        + "than bounding box containment" );

      return Result.Succeeded;
    }

    }
  }
