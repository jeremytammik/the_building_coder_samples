#region Header
//
// CmdSlopedFloor.cs - create a sloped floor
//
// Copyright (C) 2008-2021 by Jeremy Tammik,
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
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
#endregion // Namespaces

namespace BuildingCoder
{
  /// <summary>
  /// Create sloped slab using the NewSlab method.
  /// Also demonstrate checking whether a specific 
  /// level exists and creating it is not.
  /// </summary>
  [Transaction( TransactionMode.Manual )]
  public class CmdCreateSlopedSlab : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData revit,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = revit.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Document doc = uidoc.Document;

      using( Transaction tx = new Transaction( doc ) )
      {
        tx.Start( "Create Sloped Slab" );

        double width = 19.685039400;
        double length = 59.055118200;
        double height = 9.84251968503937;

        XYZ[] pts = new XYZ[] {
          new XYZ( 0.0, 0.0, height ),
          new XYZ( width, 0.0, height ),
          new XYZ( width, length, height ),
          new XYZ( 0, length, height )
        };

        #region Before Floor.Create method
#if BEFORE_FLOOR_CREATE_METHOD
        CurveArray profile_arr
          = uiapp.Application.Create.NewCurveArray();

        Line line = null;

        int n = pts.GetLength( 0 );

        XYZ q = pts[n - 1];

        foreach( XYZ p in pts )
        {
          line = Line.CreateBound( q, p );
          profile_arr.Append( line );
          q = p;
        }

        Level level
          = new FilteredElementCollector( doc )
            .OfClass( typeof( Level ) )
            .Where<Element>(
              e => e.Name.Equals( "CreateSlopedSlab" ) )
              .FirstOrDefault<Element>() as Level;

        if( null == level )
        {
          //level = doc.Create.NewLevel( height ); // 2015
          level = Level.Create( doc, height ); // 2016
          level.Name = "Sloped Slab";
        }

        Floor floor1 = doc.Create.NewSlab(
          profile_arr, level, line, 0.5, true ); // 2021
#endif // BEFORE_FLOOR_CREATE_METHOD
         #endregion // Before Floor.Create method

        bool isFoundation = true;

        ElementId floorTypeId = Floor.GetDefaultFloorType( 
          doc, isFoundation );

        double offset;

        ElementId levelId = Level.GetNearestLevelId(
          doc, height, out offset );

        // Build a floor profile for the floor creation

        CurveLoop profile = new CurveLoop();
        profile.Append( Line.CreateBound( pts[ 0 ], pts[ 1 ] ) );
        profile.Append( Line.CreateBound( pts[ 1 ], pts[ 2 ] ) );
        profile.Append( Line.CreateBound( pts[ 2 ], pts[ 3 ] ) );
        profile.Append( Line.CreateBound( pts[ 3 ], pts[ 0 ] ) );

        // The elevation of the curve loops is not taken 
        // into account (unlike the obsolete NewFloor and 
        // NewSlab methods).
        // If the default elevation is not what you want, 
        // you need to set it explicitly.

        Floor floor = Floor.Create( doc, 
          new List<CurveLoop> { profile }, 
          floorTypeId, levelId );

        Parameter param = floor.get_Parameter(
          BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM );

        param.Set( offset );

        tx.Commit();
      }
      return Result.Succeeded;
    }
  }

#region Unsuccessful attempt to modify existing floor slope
  /// <summary>
  /// Unsuccessful attempt to change the 
  /// slope of an existing floor element.
  /// </summary>
  [Transaction( TransactionMode.Manual )]
  public class CmdChangeFloorSlope : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData revit,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = revit.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Document doc = uidoc.Document;
      Selection sel = uidoc.Selection;

      Reference ref1 = sel.PickObject(
        ObjectType.Element, "Please pick a floor." );

      Floor f = doc.GetElement( ref1 ) as Floor;

      if( f == null )
        return Result.Failed;

      // Retrieve floor edge model line elements.

      ICollection<ElementId> deleted_ids;

      using( Transaction tx = new Transaction( doc ) )
      {
        tx.Start( "Temporarily Delete Floor" );

        deleted_ids = doc.Delete( f.Id );

        tx.RollBack();
      }

      // Grab the first floor edge model line.

      ModelLine ml = null;

      foreach( ElementId id in deleted_ids )
      {
        ml = doc.GetElement( id ) as ModelLine;

        if( null != ml )
        {
          break;
        }
      }

      if( null != ml )
      {
        using( Transaction tx = new Transaction( doc ) )
        {
          tx.Start( "Change Slope Angle" );

          // This parameter is read only. Therefore,
          // the change does not work and we cannot 
          // change the floor slope angle after the 
          // floor is created.

          ml.get_Parameter(
            BuiltInParameter.CURVE_IS_SLOPE_DEFINING )
              .Set( 1 );

          ml.get_Parameter(
            BuiltInParameter.ROOF_SLOPE )
              .Set( 1.2 );

          tx.Commit();
        }
      }
      return Result.Succeeded;
    }
  }
#endregion // Unsuccessful attempt to modify existing floor slope
}
