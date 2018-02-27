#region Header
//
// CmdNewTextNote.cs - Create a new text note and determine its exact width
//
// Copyright (C) 2014-2018 by Scott Wilson and Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Manual )]
  class CmdNewTextNote : IExternalCommand
  {
    void SetTextAlignment( TextNote textNote )
    {
      Document doc = textNote.Document;

      using( Transaction t = new Transaction( doc ) )
      {
        t.Start( "AlignTextNote" );

        Parameter p = textNote.get_Parameter(
          BuiltInParameter.TEXT_ALIGN_VERT );

        p.Set( (Int32)
          TextAlignFlags.TEF_ALIGN_MIDDLE );

        t.Commit();
      }
    }

    #region Solution 1 using TextRenderer.MeasureText
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
        float xDpi, yDpi;

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

      TextNote txNote = null;

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

        //TextNote txNote = doc.Create.NewTextNote(
        //  doc.ActiveView, p, XYZ.BasisX, XYZ.BasisY,
        //  newWidth, TextAlignFlags.TEF_ALIGN_LEFT
        //  | TextAlignFlags.TEF_ALIGN_BOTTOM, s ); // 2015
        //txNote.TextNoteType = textType; // 2015

        txNote = TextNote.Create( doc,
          doc.ActiveView.Id, p, s, textType.Id ); // 2016

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

        double wmin = txNote.GetMinimumAllowedWidth();
        double wmax = txNote.GetMaximumAllowedWidth();
        //double wnew = newWidth * v_scale; // this is 100 times too big
        double wnew = newWidth;

        txNote.Width = wnew;

        //6mm Arial
        //Text box width in pixels 668 = 6.95833349227905 inch, scale 100
        //NewTextNote lineWidth 0.58 times view scale 100 = 57.99 generated TextNote.Width 59.32

        t.Commit();
      }
      using( Transaction t = new Transaction( doc ) )
      {
        t.Start( "Change Text Colour" );

        int color = Util.ToColorParameterValue( 
          255, 0, 0 );

        Element textNoteType = doc.GetElement( 
          txNote.GetTypeId() );

        Parameter param = textNoteType.get_Parameter(
          BuiltInParameter.LINE_COLOR );

        // Note that this modifies the existing text 
        // note type for all instances using it. If
        // not desired, use Duplicate() first.

        param.Set( color );

        t.Commit();
      }
      return Result.Succeeded;
    }
    #endregion // Solution 1 using TextRenderer.MeasureText

    #region Solution 2 using Graphics.MeasureString
    static float GetDpiX()
    {
      float xDpi, yDpi;

      using( Graphics g = Graphics.FromHwnd( IntPtr.Zero ) )
      {
        xDpi = g.DpiX;
        yDpi = g.DpiY;
      }
      return xDpi;
    }

    static double GetStringWidth( string text, Font font )
    {
      double textWidth = 0.0;

      using( Graphics g = Graphics.FromHwnd( IntPtr.Zero ) )
      {
        textWidth = g.MeasureString( text, font ).Width;
      }
      return textWidth;
    }

    public Result Execute_2(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      Result commandResult = Result.Succeeded;

      try
      {
        UIApplication uiApp = commandData.Application;
        UIDocument uiDoc = uiApp.ActiveUIDocument;
        Document dbDoc = uiDoc.Document;
        View view = uiDoc.ActiveGraphicalView;

        XYZ pLoc = XYZ.Zero;

        try
        {
          pLoc = uiDoc.Selection.PickPoint(
            "Please pick text insertion point" );
        }
        catch( Autodesk.Revit.Exceptions.OperationCanceledException )
        {
          Debug.WriteLine( "Operation cancelled." );
          message = "Operation cancelled.";

          return Result.Succeeded;
        }

        List<TextNoteType> noteTypeList
          = new FilteredElementCollector( dbDoc )
            .OfClass( typeof( TextNoteType ) )
            .Cast<TextNoteType>()
            .ToList();

        // Sort note types into ascending text size

        BuiltInParameter bipTextSize
          = BuiltInParameter.TEXT_SIZE;

        noteTypeList.Sort( ( a, b )
          => a.get_Parameter( bipTextSize ).AsDouble()
            .CompareTo(
              b.get_Parameter( bipTextSize ).AsDouble() ) );

        foreach( TextNoteType textType in noteTypeList )
        {
          Debug.WriteLine( textType.Name );

          Parameter paramTextFont
            = textType.get_Parameter(
              BuiltInParameter.TEXT_FONT );

          Parameter paramTextSize
            = textType.get_Parameter(
              BuiltInParameter.TEXT_SIZE );

          Parameter paramBorderSize
            = textType.get_Parameter(
              BuiltInParameter.LEADER_OFFSET_SHEET );

          Parameter paramTextBold
            = textType.get_Parameter(
              BuiltInParameter.TEXT_STYLE_BOLD );

          Parameter paramTextItalic
            = textType.get_Parameter(
              BuiltInParameter.TEXT_STYLE_ITALIC );

          Parameter paramTextUnderline
            = textType.get_Parameter(
              BuiltInParameter.TEXT_STYLE_UNDERLINE );

          Parameter paramTextWidthScale
            = textType.get_Parameter(
              BuiltInParameter.TEXT_WIDTH_SCALE );

          string fontName = paramTextFont.AsString();

          double textHeight = paramTextSize.AsDouble();

          bool textBold = paramTextBold.AsInteger() == 1
            ? true : false;

          bool textItalic = paramTextItalic.AsInteger() == 1
            ? true : false;

          bool textUnderline = paramTextUnderline.AsInteger() == 1
            ? true : false;

          double textBorder = paramBorderSize.AsDouble();

          double textWidthScale = paramTextWidthScale.AsDouble();

          FontStyle textStyle = FontStyle.Regular;

          if( textBold )
          {
            textStyle |= FontStyle.Bold;
          }

          if( textItalic )
          {
            textStyle |= FontStyle.Italic;
          }

          if( textUnderline )
          {
            textStyle |= FontStyle.Underline;
          }

          float fontHeightInch = (float) textHeight * 12.0f;
          float displayDpiX = GetDpiX();

          float fontDpi = 96.0f;
          float pointSize = (float) ( textHeight * 12.0 * fontDpi );

          Font font = new Font( fontName, pointSize, textStyle );

          int viewScale = view.Scale;

          using( Transaction t = new Transaction( dbDoc ) )
          {
            t.Start( "Test TextNote lineWidth calculation" );

            string textString = textType.Name
              + " (" + fontName + " "
              + ( textHeight * 304.8 ).ToString( "0.##" ) + "mm, "
              + textStyle.ToString() + ", "
              + ( textWidthScale * 100.0 ).ToString( "0.##" )
              + "%): The quick brown fox jumps over the lazy dog.";

            double stringWidthPx = GetStringWidth( textString, font );

            double stringWidthIn = stringWidthPx / displayDpiX;

            Debug.WriteLine( "String Width in pixels: "
              + stringWidthPx.ToString( "F3" ) );
            Debug.WriteLine( ( stringWidthIn * 25.4 * viewScale ).ToString( "F3" )
              + " mm at 1:" + viewScale.ToString() );

            double stringWidthFt = stringWidthIn / 12.0;

            double lineWidth = ( ( stringWidthFt * textWidthScale )
              + ( textBorder * 2.0 ) ) * viewScale;

            //TextNote textNote = dbDoc.Create.NewTextNote(
            //  view, pLoc, XYZ.BasisX, XYZ.BasisY, 0.001,
            //  TextAlignFlags.TEF_ALIGN_LEFT
            //  | TextAlignFlags.TEF_ALIGN_TOP, textString ); // 2015
            //textNote.TextNoteType = textType; // 2015

            TextNote textNote = TextNote.Create( dbDoc,
              view.Id, pLoc, textString, textType.Id); // 2016

            textNote.Width = lineWidth;

            t.Commit();
          }

          // Place next text note below this one with 5 mm gap

          pLoc += view.UpDirection.Multiply(
            ( textHeight + ( 5.0 / 304.8 ) )
              * viewScale ).Negate();
        }
      }
      catch( Autodesk.Revit.Exceptions.ExternalApplicationException e )
      {
        message = e.Message;
        Debug.WriteLine( "Exception Encountered (Application)\n"
          + e.Message + "\nStack Trace: " + e.StackTrace );

        commandResult = Result.Failed;
      }
      catch( Exception e )
      {
        message = e.Message;
        Debug.WriteLine( "Exception Encountered (General)\n"
          + e.Message + "\nStack Trace: " + e.StackTrace );

        commandResult = Result.Failed;
      }
      return commandResult;
    }
    #endregion // Solution 2 using Graphics.MeasureString
  }
}
