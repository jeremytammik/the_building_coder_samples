#region Header
//
// CmdExportImage.cs - export a preview JPG 3D image of the family or project
//
// Copyright (C) 2013-2017 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
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
    #region SetWhiteRenderBackground
    void SetWhiteRenderBackground( View3D view )
    {
      RenderingSettings rs = view.GetRenderingSettings();
      rs.BackgroundStyle = BackgroundStyle.Color;

      ColorBackgroundSettings cbs
        = (ColorBackgroundSettings) rs
          .GetBackgroundSettings();

      cbs.Color = new Color( 255, 0, 0 );
      rs.SetBackgroundSettings( cbs );
      view.SetRenderingSettings( rs );
    }
    #endregion // SetWhiteRenderBackground

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
          // Ensure white background.

          Color white = new Color( 255, 255, 255 );

          view3D.SetBackground(
            ViewDisplayBackground.CreateGradient(
              white, white, white ) );

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

    /// <summary>
    /// Wrapper for old sample code.
    /// </summary>
    static Result ExportToImage2( Document doc )
    {
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

    /// <summary>
    /// New code as described in Revit API discussion 
    /// forum thread on how to export an image from a 
    /// specific view using Revit API C#,
    /// http://forums.autodesk.com/t5/revit-api/how-to-export-an-image-from-a-specific-view-using-revit-api-c/m-p/6424418
    /// </summary>
    static Result ExportToImage3( Document doc )
    {
      Result r = Result.Failed;

      using( Transaction tx = new Transaction( doc ) )
      {
        tx.Start( "Export Image" );

        string desktop_path = Environment.GetFolderPath( 
          Environment.SpecialFolder.Desktop );

        View view = doc.ActiveView;

        string filepath = Path.Combine( desktop_path, 
          view.Name );

        ImageExportOptions img = new ImageExportOptions();

        img.ZoomType = ZoomFitType.FitToPage;
        img.PixelSize = 32;
        img.ImageResolution = ImageResolution.DPI_600;
        img.FitDirection = FitDirectionType.Horizontal;
        img.ExportRange = ExportRange.CurrentView;
        img.HLRandWFViewsFileType = ImageFileType.PNG;
        img.FilePath = filepath;
        img.ShadowViewsFileType = ImageFileType.PNG;

        doc.ExportImage( img );

        tx.RollBack();

        filepath = Path.ChangeExtension( 
          filepath, "png" );

        Process.Start( filepath );

        r = Result.Succeeded;
      }
      return r;
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      Document doc = uiapp.ActiveUIDocument.Document;
      bool use_old_code = false;

      Result r = use_old_code
        ? ExportToImage2( doc )
        : ExportToImage3( doc );

      return r;
    }
  }
}
