#region Header
//
// CmdPreviewImage.cs - display the element type preview image of all family instances
//
// Copyright (C) 2010-2013 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media; // WindowsBase
using System.Windows.Media.Imaging; // PresentationCore
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Size = System.Drawing.Size;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.ReadOnly )]
  class CmdPreviewImage : IExternalCommand
  {
    static BitmapSource ConvertBitmapToBitmapSource(
      Bitmap bmp )
    {
      return System.Windows.Interop.Imaging
        .CreateBitmapSourceFromHBitmap(
          bmp.GetHbitmap(),
          IntPtr.Zero,
          Int32Rect.Empty,
          BitmapSizeOptions.FromEmptyOptions() );
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Document doc = uidoc.Document;

      FilteredElementCollector collector
        = new FilteredElementCollector( doc );

      collector.OfClass( typeof( FamilyInstance ) );

      foreach( FamilyInstance fi in collector )
      {
        Debug.Assert( null != fi.Category,
          "expected family instance to have a valid category" );

        ElementId typeId = fi.GetTypeId();

        ElementType type = doc.GetElement( typeId )
          as ElementType;

        Size imgSize = new Size( 200, 200 );

        Bitmap image = type.GetPreviewImage( imgSize );

        // encode image to jpeg for test display purposes:

        JpegBitmapEncoder encoder
          = new JpegBitmapEncoder();

        encoder.Frames.Add( BitmapFrame.Create(
          ConvertBitmapToBitmapSource( image ) ) );

        encoder.QualityLevel = 25;

        string filename = "a.jpg";

        FileStream file = new FileStream(
          filename, FileMode.Create, FileAccess.Write );

        encoder.Save( file );
        file.Close();

        Process.Start( filename ); // test display
      }
      return Result.Succeeded;
    }
  }
}
