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
      // 3/32" = 72*3/32 points = 

      TextNoteType textType
        = new FilteredElementCollector( doc )
          .OfClass( typeof( TextNoteType ) )
          .FirstElement() as TextNoteType;

      Debug.Print( textType.Name );

      // 6 mm Arial happens to be the first text type found
      // 6 mm = 6 / 25.4 inch = 72 * 6 / 25.4 points = 17 pt
      // The 6 mm Revit font height presumably refers to 
      // an upper case character such as 'M'. The Font
      // constructor takes an em size argument, which 
      // might refer to the height of a lower case 'm'
      // character. Let's say there is a factor 1.4 
      // difference between the two:

      double text_type_height_mm = 6;

      double mm_per_inch = 25.4;

      double points_per_inch = 72;

      double scale_upper_to_lower = 0.6;

      double em_size 
        = scale_upper_to_lower * points_per_inch
        * ( text_type_height_mm / mm_per_inch );

      Font font = new Font( "Arial", 17, FontStyle.Bold );

      using( Transaction t = new Transaction( doc ) )
      {
        t.Start( "Create TextNote" );

        string s = "TEST BOLD";
        //string s = "The quick brown fox jumps over the lazy dog";

        Size txtBox = System.Windows.Forms.TextRenderer
          .MeasureText( s, font );

        double w_inch = txtBox.Width / DpiX;
        double v_scale = view.Scale; // ratio of true model size to paper size

        Debug.Print(
          "Text box width in pixels {0} = {1} inch, scale {2}",
          txtBox.Width, w_inch, v_scale );

        //double newWidth
        //  = ( (double) txtBox.Width / 86 ) / 12;

        double newWidth = w_inch / 12;

        newWidth = newWidth * v_scale;

        newWidth *= 1.4;

        TextNote txNote = doc.Create.NewTextNote(
          doc.ActiveView, p, XYZ.BasisX, XYZ.BasisY,
          0.1, TextAlignFlags.TEF_ALIGN_LEFT
          | TextAlignFlags.TEF_ALIGN_BOTTOM, s );

        txNote.TextNoteType = textType;
        txNote.Width = newWidth;

        t.Commit();
      }
      return Result.Succeeded;
    }
  }
}
