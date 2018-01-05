#region Header
//
// CmdSheetToModel.cs - Convert sheet to model coordinates and convert DWF markup to model elements
//
// Copyright (C) 2015-2018 by Paolo Serra and Jeremy Tammik,
// Autodesk Inc. All rights reserved.
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
using Autodesk.Revit.UI;
// Miro Ambiguities
using ApplicationRvt = Autodesk.Revit.ApplicationServices.Application;
using ViewRvt = Autodesk.Revit.DB.View;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Manual )]
  class CmdSheetToModel : IExternalCommand
  {
    public void QTO_2_PlaceHoldersFromDWFMarkups(
      Document doc,
      string activityId )
    {
      View activeView = doc.ActiveView;

      if( !( activeView is ViewSheet ) )
      {
        TaskDialog.Show( "QTO",
          "The current view must be a Sheet View with DWF markups" );
        return;
      }

      ViewSheet vs = activeView as ViewSheet;

      Viewport vp = doc.GetElement(
        vs.GetAllViewports().First() ) as Viewport;

      View plan = doc.GetElement( vp.ViewId ) as View;

      int scale = vp.Parameters.Cast<Parameter>()
        .First( x => x.Id.IntegerValue.Equals(
          (int) BuiltInParameter.VIEW_SCALE ) )
        .AsInteger();

      IEnumerable<Element> dwfMarkups
        = new FilteredElementCollector( doc )
          .OfClass( typeof( ImportInstance ) )
          .WhereElementIsNotElementType()
          .Where( x => x.Name.StartsWith( "Markup" )
            && x.OwnerViewId.IntegerValue.Equals(
              activeView.Id.IntegerValue ) );

      using( TransactionGroup tg = new TransactionGroup( doc ) )
      {
        tg.Start( "DWF markups placeholders" );

        using( Transaction t = new Transaction( doc ) )
        {
          t.Start( "DWF Transfer" );

          plan.Parameters.Cast<Parameter>()
            .First( x => x.Id.IntegerValue.Equals(
              (int) BuiltInParameter.VIEWER_CROP_REGION ) )
            .Set( 1 );

          XYZ VC = ( plan.CropBox.Min + plan.CropBox.Max ) / 2;

          XYZ BC = vp.GetBoxCenter();

          t.RollBack();

          foreach( Element e in dwfMarkups )
          {
            GeometryElement GeoElem = e.get_Geometry( new Options() );

            GeometryInstance gi = GeoElem.Cast<GeometryInstance>().First();

            GeometryElement gei = gi.GetSymbolGeometry();

            IList<GeometryObject> gos = new List<GeometryObject>();

            if( gei.Cast<GeometryObject>().Count( x => x is Arc ) > 0 )
            {
              continue;
            }

            foreach( GeometryObject go in gei )
            {
              XYZ med = new XYZ();

              if( go is PolyLine )
              {
                PolyLine pl = go as PolyLine;

                XYZ min = new XYZ( pl.GetCoordinates().Min( p => p.X ),
                                pl.GetCoordinates().Min( p => p.Y ),
                                pl.GetCoordinates().Min( p => p.Z ) );

                XYZ max = new XYZ( pl.GetCoordinates().Max( p => p.X ),
                                pl.GetCoordinates().Max( p => p.Y ),
                                pl.GetCoordinates().Max( p => p.Z ) );

                med = ( min + max ) / 2;
              }

              med = med - BC;

              // Convert DWF sheet coordinates into model coordinates

              XYZ a = VC + new XYZ( med.X * scale, med.Y * scale, 0 );
            }
          }

          t.Start( "DWF Transfer" );

          foreach( Element e in dwfMarkups )
          {
            GeometryElement GeoElem = e.get_Geometry( new Options() );

            GeometryInstance gi = GeoElem.Cast<GeometryInstance>().First();

            GeometryElement gei = gi.GetSymbolGeometry();

            IList<GeometryObject> gos = new List<GeometryObject>();

            if( gei.Cast<GeometryObject>().Count( x => x is Arc ) == 0 )
            {
              continue;
            }

            foreach( GeometryObject go in gei )
            {
              if( go is Arc )
              {
                Curve c = go as Curve;

                XYZ med = c.Evaluate( 0.5, true );

                med = med - BC;

                XYZ a = VC + new XYZ( med.X * scale, med.Y * scale, 0 );

                // Warning CS0618: 
                // Autodesk.Revit.Creation.ItemFactoryBase.NewTextNote(
                //   View, XYZ, XYZ, XYZ, double, TextAlignFlags, string) 
                // is obsolete: 
                // This method is deprecated in Revit 2016. 
                // Please use one of the TextNote.Create methods instead.

                //doc.Create.NewTextNote( plan,
                //                       a,
                //                       XYZ.BasisX,
                //                       XYZ.BasisY,
                //                       MMtoFeet( 5 ),
                //                       TextAlignFlags.TEF_ALIGN_CENTER,
                //                       activityId );

                ElementId textTypeId = new FilteredElementCollector( doc )
                  .OfClass( typeof( TextNoteType ) )
                  .FirstElementId();

                TextNote.Create( doc, plan.Id, a, activityId, textTypeId );
              }
            }

            t.Commit();
          }
        }

        tg.Assimilate();
      }
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      Document doc = uiapp.ActiveUIDocument.Document;

      QTO_2_PlaceHoldersFromDWFMarkups(
        doc, "DWF Markup" );

      return Result.Succeeded;
    }
  }

  #region CmdMiroTest2
  [Transaction( TransactionMode.Manual )]
  public class CmdMiroTest2 : IExternalCommand
  {
    // KIS - public fields
    public UIApplication _appUI = null;
    public ApplicationRvt _app = null;
    public UIDocument _docUI = null;
    public Document _doc = null;

    Result IExternalCommand.Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      // cache admin data
      _appUI = commandData.Application;
      _app = _appUI.Application;
      _docUI = commandData.Application.ActiveUIDocument;
      _doc = _docUI.Document;

      try // generic
      {
        // Current View must be Sheet
        ViewSheet sheet = _doc.ActiveView as ViewSheet;
        if( null == sheet )
        {
          Util.ErrorMsg( "Current View is NOT a Sheet!" );
          return Result.Cancelled;
        }

        // There must be a Floor Plan named "Level 0" 
        // which is the "master" to align to
        Viewport vpMaster = null;
        // There must be at least one more Floor Plan 
        // View to align (move)
        List<Viewport> vpsSlave = new List<Viewport>();
        // Find them:
        foreach( ElementId idVp in sheet.GetAllViewports() )
        {
          Viewport vp = _doc.GetElement( idVp ) as Viewport;
          ViewRvt v = _doc.GetElement( vp.ViewId ) as ViewRvt;
          if( v.ViewType == ViewType.FloorPlan )
          {
            if( v.Name.Equals( "Level 0", StringComparison
              .CurrentCultureIgnoreCase ) )
            {
              vpMaster = vp;
            }
            else
            {
              vpsSlave.Add( vp );
            }

          } //if FloorPlan

        } //foreeach idVp

        // Check if got them all
        if( null == vpMaster )
        {
          Util.ErrorMsg( "NO 'Level 0' Floor Plan on the Sheet!" );
          return Result.Cancelled;
        }
        else if( vpsSlave.Count == 0 )
        {
          Util.ErrorMsg( "NO other Floor Plans to adjust on the Sheet!" );
          return Result.Cancelled;
        }

          // Process Master
          // --------------

          XYZ ptMasterVpCenter = vpMaster.GetBoxCenter();
        ViewRvt viewMaster = _doc.GetElement(
          vpMaster.ViewId ) as ViewRvt;
        double scaleMaster = viewMaster.Scale;

        // Process Slaves
        // --------------

        using ( Transaction t = new Transaction( _doc ) )
        {
          t.Start( "Set Box Centres" );

          foreach ( Viewport vpSlave in vpsSlave )
          {
            XYZ ptSlaveVpCenter = vpSlave.GetBoxCenter();
            ViewRvt viewSlave = _doc.GetElement(
              vpSlave.ViewId ) as ViewRvt;
            double scaleSlave = viewSlave.Scale;
            // MUST be the same scale, otherwise can't really overlap
            if ( scaleSlave != scaleMaster ) continue;

            // Work out how to move the center of Slave 
            // Viewport to coincide model-wise with Master
            // (must use center as only Viewport.SetBoxCenter 
            // is provided in API)
            // We can ignore View.Outline as Viewport.GetBoxOutline 
            // is ALWAYS the same dimensions enlarged by 
            // 0.01 ft in each direction.
            // This guarantees that the center of View is 
            // also center of Viewport, BUT there is a 
            // problem when any Elevation Symbols outside 
            // the crop box are visible (can't work out why
            // - BUG?, or how to calculate it all if BY-DESIGN)

            BoundingBoxXYZ bbm = viewMaster.CropBox;
            BoundingBoxXYZ bbs = viewSlave.CropBox;

            // 0) Center points in WCS
            XYZ wcsCenterMaster = 0.5 * bbm.Min.Add( bbm.Max );
            XYZ wcsCenterSlave = 0.5 * bbs.Min.Add( bbs.Max );

            // 1) Delta (in model's feet) of the slave center w.r.t master center
            double deltaX = wcsCenterSlave.X - wcsCenterMaster.X;
            double deltaY = wcsCenterSlave.Y - wcsCenterMaster.Y;

            // 1a) Scale to Delta in Sheet's paper-space feet
            deltaX *= 1.0 / (double) scaleMaster;
            deltaY *= 1.0 / (double) scaleMaster;

            // 2) New center point for the slave viewport, so *models* "overlap":
            XYZ newCenter = new XYZ(
              ptMasterVpCenter.X + deltaX,
              ptMasterVpCenter.Y + deltaY,
              ptSlaveVpCenter.Z );
            vpSlave.SetBoxCenter( newCenter );
          }
          t.Commit();
        }
      }
      catch( Exception ex )
      {
        Util.ErrorMsg( "Generic exception: " + ex.Message );
        return Result.Failed;
      }
      return Result.Succeeded;
    }
  } // CmdMiroTest2
  #endregion // CmdMiroTest2
}
