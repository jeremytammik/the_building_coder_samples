#region Header
//
// CmdWallOpeningProfiles.cs - determine and display all wall opening face edges including elevation profile lines
//
// Copyright (C) 2015-2018 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Manual )]
  class CmdWallOpeningProfiles : IExternalCommand
  {
    #region Isolate element in new view
    public void testTwo( UIDocument uidoc )
    {
      Document doc = uidoc.Document;

      View newView;

      using( Transaction t = new Transaction( doc ) )
      {
        t.Start( "Trans" );

        // Get Floorplan for Level1 and copy its 
        // properties for ouw newly to create ViewPlan.

        View existingView = doc.GetElement(
          new ElementId( 312 ) ) as View;

        // Create new Floorplan.

        newView = doc.GetElement( existingView.Duplicate(
          ViewDuplicateOption.Duplicate ) ) as View;

        t.Commit();

        // Important to set new view as active view.

        uidoc.ActiveView = newView;

        t.Start( "Trans 2" );

        // Try to isolate a Wall. Fails.

        newView.IsolateElementTemporary( new ElementId( 317443 ) );

        t.Commit();
      }

      // Change the View to the new View.

      uidoc.ActiveView = newView;
    }
    #endregion // Isolate element in new view

    /// <summary>
    /// Retrieve all planar faces belonging to the 
    /// specified opening in the given wall.
    /// </summary>
    static List<PlanarFace> GetWallOpeningPlanarFaces(
      Wall wall,
      ElementId openingId )
    {
      List<PlanarFace> faceList = new List<PlanarFace>();

      List<Solid> solidList = new List<Solid>();

      Options geomOptions = wall.Document.Application.Create.NewGeometryOptions();

      if( geomOptions != null )
      {
        //geomOptions.ComputeReferences = true; // expensive, avoid if not needed
        //geomOptions.DetailLevel = ViewDetailLevel.Fine;
        //geomOptions.IncludeNonVisibleObjects = false;

        GeometryElement geoElem = wall.get_Geometry( geomOptions );

        if( geoElem != null )
        {
          foreach( GeometryObject geomObj in geoElem )
          {
            if( geomObj is Solid )
            {
              solidList.Add( geomObj as Solid );
            }
          }
        }
      }

      foreach( Solid solid in solidList )
      {
        foreach( Face face in solid.Faces )
        {
          if( face is PlanarFace )
          {
            if( wall.GetGeneratingElementIds( face )
              .Any( x => x == openingId ) )
            {
              faceList.Add( face as PlanarFace );
            }
          }
        }
      }
      return faceList;
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Document doc = uidoc.Document;
      Result commandResult = Result.Succeeded;
      Categories cats = doc.Settings.Categories;

      ElementId catDoorsId = cats.get_Item(
        BuiltInCategory.OST_Doors ).Id;

      ElementId catWindowsId = cats.get_Item(
        BuiltInCategory.OST_Windows ).Id;

      try
      {
        List<ElementId> selectedIds = uidoc.Selection
          .GetElementIds().ToList();

        using( Transaction trans = new Transaction( doc ) )
        {
          trans.Start( "Cmd: GetOpeningProfiles" );

          List<ElementId> newIds = new List<ElementId>();

          foreach( ElementId selectedId in selectedIds )
          {
            Wall wall = doc.GetElement( selectedId ) as Wall;

            if( wall != null )
            {
              List<PlanarFace> faceList = new List<PlanarFace>();

              List<ElementId> insertIds = wall.FindInserts(
                true, false, false, false ).ToList();

              foreach( ElementId insertId in insertIds )
              {
                Element elem = doc.GetElement( insertId );

                if( elem is FamilyInstance )
                {
                  FamilyInstance inst = elem as FamilyInstance;

                  CategoryType catType = inst.Category
                    .CategoryType;

                  Category cat = inst.Category;

                  if( catType == CategoryType.Model
                    && ( cat.Id == catDoorsId
                      || cat.Id == catWindowsId ) )
                  {
                    faceList.AddRange(
                      GetWallOpeningPlanarFaces(
                        wall, insertId ) );
                  }
                }
                else if( elem is Opening )
                {
                  faceList.AddRange(
                    GetWallOpeningPlanarFaces(
                      wall, insertId ) );
                }
              }

              foreach( PlanarFace face in faceList )
              {
                //Plane facePlane = new Plane(
                //  face.ComputeNormal( UV.Zero ), 
                //  face.Origin ); // 2016

                Plane facePlane = Plane.CreateByNormalAndOrigin(
                  face.ComputeNormal( UV.Zero ),
                  face.Origin ); // 2017

                SketchPlane sketchPlane
                  = SketchPlane.Create( doc, facePlane );

                foreach( CurveLoop curveLoop in
                  face.GetEdgesAsCurveLoops() )
                {
                  foreach( Curve curve in curveLoop )
                  {
                    ModelCurve modelCurve = doc.Create
                      .NewModelCurve( curve, sketchPlane );

                    newIds.Add( modelCurve.Id );
                  }
                }
              }
            }
          }

          if( newIds.Count > 0 )
          {
            View activeView = uidoc.ActiveGraphicalView;
            activeView.IsolateElementsTemporary( newIds );
          }
          trans.Commit();
        }
      }

      #region Exception Handling

      catch( Autodesk.Revit.Exceptions
        .ExternalApplicationException e )
      {
        message = e.Message;
        Debug.WriteLine(
          "Exception Encountered (Application)\n"
          + e.Message + "\nStack Trace: "
          + e.StackTrace );

        commandResult = Result.Failed;
      }
      catch( Autodesk.Revit.Exceptions
        .OperationCanceledException e )
      {
        Debug.WriteLine( "Operation cancelled. "
          + e.Message );

        message = "Operation cancelled.";

        commandResult = Result.Succeeded;
      }
      catch( Exception e )
      {
        message = e.Message;
        Debug.WriteLine(
          "Exception Encountered (General)\n"
          + e.Message + "\nStack Trace: "
          + e.StackTrace );

        commandResult = Result.Failed;
      }

      #endregion

      return commandResult;
    }
  }
}
