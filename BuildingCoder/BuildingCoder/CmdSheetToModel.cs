#region Header
//
// CmdSheetToModel.cs - Convert sheet to model coordinates and convert DWF markup to model elements
//
// Copyright (C) 2015 by Paolo Serra and Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Manual )]
  class CmdSheetToModel : IExternalCommand
  {
    double DegreesToRadians( double deg )
    {
      return deg / 180 * Math.PI;
    }

    double RadiansToDegrees( double rad )
    {
      return rad * 180 / Math.PI;
    }

    double MtoFeet( double m )
    {
      return m / 0.3048;
    }

    double MMtoFeet( double mm )
    {
      return mm / 304.8;
    }

    double FeetToM( double ft )
    {
      return ft * 0.3048;
    }

    double FeetTomm( double ft )
    {
      return ft * 304.8;
    }

    public void QTO_2_PlaceHoldersFromDWFMarkups(
      Document doc,
      string activityId )
    {
      //UIDocument uidoc = this.ActiveUIDocument;

      //Document doc = uidoc.Document;

      View activeView = doc.ActiveView;

      if( !( activeView is ViewSheet ) )
      {
        TaskDialog.Show( "QTO", "The current view must be a Sheet View with DWF markups" );
        return;
      }

      ViewSheet vs = activeView as ViewSheet;

      Viewport vp = doc.GetElement( vs.GetAllViewports().First() ) as Viewport;

      View plan = doc.GetElement( vp.ViewId ) as View;

      int scale = vp.Parameters.Cast<Parameter>().First( x => x.Id.IntegerValue.Equals( (int) BuiltInParameter.VIEW_SCALE ) ).AsInteger();

      IList<Element> dwfMarkups = new FilteredElementCollector( doc )
        .OfClass( typeof( ImportInstance ) )
        .WhereElementIsNotElementType()
        .Where( x => x.Name.StartsWith( "Markup" ) && x.OwnerViewId.IntegerValue.Equals( activeView.Id.IntegerValue ) )
        .ToList();

      using( TransactionGroup tg = new TransactionGroup( doc, "DWF markups placeholders" ) )
      {
        tg.Start();

        using( Transaction t = new Transaction( doc, "DWF transfer" ) )
        {
          t.Start();

          plan.Parameters.Cast<Parameter>().First( x => x.Id.IntegerValue.Equals( (int) BuiltInParameter.VIEWER_CROP_REGION ) ).Set( 1 );

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

          t.Start();

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

                doc.Create.NewTextNote( plan,
                                       a,
                                       XYZ.BasisX,
                                       XYZ.BasisY,
                                       MMtoFeet( 5 ),
                                       TextAlignFlags.TEF_ALIGN_CENTER,
                                       activityId );

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
}
