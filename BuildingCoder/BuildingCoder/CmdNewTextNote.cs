#region Header
//
// CmdNewTextNote.cs - Create a new text note and determine its exact width
//
// Copyright (C) 2014 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Drawing;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Diagnostics;
using System.Runtime.InteropServices;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Manual )]
  class CmdNewTextNote : IExternalCommand
  {
    [DllImport( "user32.dll" )]
    private static extern IntPtr GetDC( IntPtr hwnd );

    [DllImport( "user32.dll" )]
    private static extern Int32 ReleaseDC( IntPtr hwnd );

    /// <summary>
    /// Determine the current display 
    /// horizontal dots per inch.
    /// </summary>
    static float DpiX
    {
      get
      {
        Single xDpi, yDpi;

        IntPtr dc = GetDC( IntPtr.Zero );

        using( Graphics g = Graphics.FromHdc( dc ) )
        {
          xDpi = g.DpiX;
          yDpi = g.DpiY;
        }

        if( ReleaseDC( IntPtr.Zero ) != 0 )
        {
          // GetLastError and handle...
        }
        return xDpi;
      }
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Document doc = uidoc.Document;
      View view = doc.ActiveView;

      XYZ p;

      try
      {
        p = uidoc.Selection.PickPoint(
          "Please pick text insertion point" );
      }
      catch( Autodesk.Revit.Exceptions.OperationCanceledException )
      {
        return Result.Cancelled;
      }

      //TextNoteType boldTextType = doc.GetElement(
      //  new ElementId( 1212838 ) ) as TextNoteType; // Arial 3/32" Bold

      // 1 inch = 72 points
      // 3/32" = 72 * 3/32 points = ...

      TextNoteType textType
        = new FilteredElementCollector( doc )
          .OfClass( typeof( TextNoteType ) )
          .FirstElement() as TextNoteType;

      Debug.Print( "TextNoteType.Name = " + textType.Name );

      // 6 mm Arial happens to be the first text type found
      // 6 mm = 6 / 25.4 inch = 72 * 6 / 25.4 points = 17 pt.
      // Nowadays, Windows does not assume that a point is
      // 1/72", but moved to 1/96" instead.

      float text_type_height_mm = 6;

      float mm_per_inch = 25.4f;

      float points_per_inch = 96; // not 72

      float em_size = points_per_inch
        * ( text_type_height_mm / mm_per_inch );

      em_size += 2.5f;

      Font font = new Font( "Arial", em_size,
        FontStyle.Regular );

      using( Transaction t = new Transaction( doc ) )
      {
        t.Start( "Create TextNote" );

        //string s = "TEST BOLD";

        string s = "The quick brown fox jumps over the lazy dog";

        Size txtBox = System.Windows.Forms.TextRenderer
          .MeasureText( s, font );

        double w_inch = txtBox.Width / DpiX;
        double v_scale = view.Scale; // ratio of true model size to paper size

        Debug.Print(
          "Text box width in pixels {0} = {1} inch, "
          + "view scale = {2}",
          txtBox.Width, w_inch, v_scale );

        double newWidth = w_inch / 12;

        TextNote txNote = doc.Create.NewTextNote(
          doc.ActiveView, p, XYZ.BasisX, XYZ.BasisY,
          newWidth, TextAlignFlags.TEF_ALIGN_LEFT
          | TextAlignFlags.TEF_ALIGN_BOTTOM, s );

        txNote.TextNoteType = textType;

        Debug.Print(
          "NewTextNote lineWidth {0} times view scale "
          + "{1} = {2} generated TextNote.Width {3}",
          Util.RealString( newWidth ),
          Util.RealString( v_scale ),
          Util.RealString( newWidth * v_scale ),
          Util.RealString( txNote.Width ) );

        // This fails.

        //Debug.Assert(
        //  Util.IsEqual( newWidth * v_scale, txNote.Width ),
        //  "expected the NewTextNote lineWidth "
        //  + "argument to determine the resulting "
        //  + "text note width" );

        txNote.Width = newWidth * v_scale;

        //6mm Arial
        //Text box width in pixels 668 = 6.95833349227905 inch, scale 100
        //NewTextNote lineWidth 0.58 times view scale 100 = 57.99 generated TextNote.Width 59.32

        t.Commit();
      }
      return Result.Succeeded;
    }
  }
}
