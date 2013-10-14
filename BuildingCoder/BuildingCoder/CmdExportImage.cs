#region Header
//
// CmdExportImage.cs - export a preview JPG 3D image of the family or project
//
// Copyright (C) 2013 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
#endregion // Header

#define VERSION2014

#region Namespaces
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.IO;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Manual )]
  class CmdExportImage : IExternalCommand
  {
    static string ExportToImage( Document doc )
    {
      var tempFileName = Path.ChangeExtension(
        Path.GetRandomFileName(), "png" );

      string tempImageFile;

      try
      {
        tempImageFile = Path.Combine(
          Path.GetTempPath(), tempFileName );
      }
      catch( IOException )
      {
        return null;
      }

      IList<ElementId> views = new List<ElementId>();

      try
      {

#if !VERSION2014
    var direction = new XYZ(-1, 1, -1);
    var view3D = doc.IsFamilyDocument
      ? doc.FamilyCreate.NewView3D(direction)
      : doc.Create.NewView3D(direction);
#else
        var collector = new FilteredElementCollector(
          doc );

        var viewFamilyType = collector
          .OfClass( typeof( ViewFamilyType ) )
          .OfType<ViewFamilyType>()
          .FirstOrDefault( x =>
            x.ViewFamily == ViewFamily.ThreeDimensional );

        var view3D = ( viewFamilyType != null )
          ? View3D.CreateIsometric( doc, viewFamilyType.Id )
          : null;

#endif // VERSION2014

        if( view3D != null )
        {
          views.Add( view3D.Id );

          var graphicDisplayOptions
            = view3D.get_Parameter(
              BuiltInParameter.MODEL_GRAPHICS_STYLE );

          // Settings for best quality

          graphicDisplayOptions.Set( 6 );
        }
      }
      catch( Autodesk.Revit.Exceptions
        .InvalidOperationException )
      {
      }

      var ieo = new ImageExportOptions
      {
        FilePath = tempImageFile,
        FitDirection = FitDirectionType.Horizontal,
        HLRandWFViewsFileType = ImageFileType.PNG,
        ImageResolution = ImageResolution.DPI_150,
        ShouldCreateWebSite = false
      };

      if( views.Count > 0 )
      {
        ieo.SetViewsAndSheets( views );
        ieo.ExportRange = ExportRange.SetOfViews;
      }
      else
      {
        ieo.ExportRange = ExportRange
          .VisibleRegionOfCurrentView;
      }

      ieo.ZoomType = ZoomFitType.FitToPage;
      ieo.ViewName = "tmp";

      if( ImageExportOptions.IsValidFileName(
        tempImageFile ) )
      {
        // If ExportRange = ExportRange.SetOfViews 
        // and document is not active, then image 
        // exports successfully, but throws
        // Autodesk.Revit.Exceptions.InternalException

        try
        {
          doc.ExportImage( ieo );
        }
        catch
        {
          return string.Empty;
        }
      }
      else
      {
        return string.Empty;
      }

      // File name has format like 
      // "tempFileName - view type - view name", e.g.
      // "luccwjkz - 3D View - {3D}.png".
      // Get the first image (we only listed one view
      // in views).

      var files = Directory.GetFiles(
        Path.GetTempPath(),
        string.Format( "{0}*.*", Path
          .GetFileNameWithoutExtension(
            tempFileName ) ) );

      return files.Length > 0
        ? files[0]
        : string.Empty;
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      Document doc = uiapp.ActiveUIDocument.Document;
      Result r = Result.Failed;

      using( Transaction tx = new Transaction( doc ) )
      {
        tx.Start( "Export Image" );
        string filepath = ExportToImage( doc );
        tx.RollBack();

        if( 0 < filepath.Length )
        {
          Process.Start( filepath );
          r = Result.Succeeded;
        }
      }
      return r;
    }
  }
}
