#region Header

//
// CmdNewTextNote.cs - Create a new text note and determine its exact width
//
// Copyright (C) 2014-2020 by Scott Wilson and Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.Exceptions;
using Autodesk.Revit.UI;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdNewTextNote : IExternalCommand
    {
        private void SetTextAlignment(TextNote textNote)
        {
            var doc = textNote.Document;

            using var t = new Transaction(doc);
            t.Start("AlignTextNote");

            var p = textNote.get_Parameter(
                BuiltInParameter.TEXT_ALIGN_VERT);

            p.Set((int)
                TextAlignFlags.TEF_ALIGN_MIDDLE);

            t.Commit();
        }

        #region Get specific TextNoteType by name

        // implemented for https://forums.autodesk.com/t5/revit-api-forum/creating-a-textnote-with-a-specific-type-i-e-1-10-quot-arial-1/m-p/8765648
        /// <summary>
        ///     Return the first text note type matching the given name.
        ///     Note that TextNoteType is a subclass of ElementType,
        ///     so this method is more restrictive above all faster
        ///     than Util.GetElementTypeByName.
        ///     This filter could be speeded up by using a (quick)
        ///     parameter filter instead of the (slower than slow)
        ///     LINQ post-processing.
        /// </summary>
        private TextNoteType GetTextNoteTypeByName(
            Document doc,
            string name)
        {
            return new FilteredElementCollector(doc)
                    .OfClass(typeof(TextNoteType))
                    .First(q => q.Name.Equals(name))
                as TextNoteType;
        }

        #endregion // Get specific TextNoteType by name

        #region Solution 1 using TextRenderer.MeasureText

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hwnd);

        /// <summary>
        ///     Determine the current display
        ///     horizontal dots per inch.
        /// </summary>
        private static float DpiX
        {
            get
            {
                float xDpi, yDpi;

                var dc = GetDC(IntPtr.Zero);

                using (var g = Graphics.FromHdc(dc))
                {
                    xDpi = g.DpiX;
                    yDpi = g.DpiY;
                }

                if (ReleaseDC(IntPtr.Zero) != 0)
                {
                    // GetLastError and handle...
                }

                return xDpi;
            }
        }

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;
            var view = doc.ActiveView;

            XYZ p;

            try
            {
                p = uidoc.Selection.PickPoint(
                    "Please pick text insertion point");
            }
            catch (OperationCanceledException)
            {
                return Result.Cancelled;
            }

            //TextNoteType boldTextType = doc.GetElement(
            //  new ElementId( 1212838 ) ) as TextNoteType; // Arial 3/32" Bold

            // 1 inch = 72 points
            // 3/32" = 72 * 3/32 points = ...

            var textType
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(TextNoteType))
                    .FirstElement() as TextNoteType;

            Debug.Print($"TextNoteType.Name = {textType.Name}");

            // 6 mm Arial happens to be the first text type found
            // 6 mm = 6 / 25.4 inch = 72 * 6 / 25.4 points = 17 pt.
            // Nowadays, Windows does not assume that a point is
            // 1/72", but moved to 1/96" instead.

            float text_type_height_mm = 6;

            var mm_per_inch = 25.4f;

            float points_per_inch = 96; // not 72

            var em_size = points_per_inch
                          * (text_type_height_mm / mm_per_inch);

            em_size += 2.5f;

            var font = new Font("Arial", em_size,
                FontStyle.Regular);

            TextNote txNote = null;

            using (var t = new Transaction(doc))
            {
                t.Start("Create TextNote");

                //string s = "TEST BOLD";

                var s = "The quick brown fox jumps over the lazy dog";

                var txtBox = TextRenderer
                    .MeasureText(s, font);

                double w_inch = txtBox.Width / DpiX;
                double v_scale = view.Scale; // ratio of true model size to paper size

                Debug.Print(
                    "Text box width in pixels {0} = {1} inch, "
                    + "view scale = {2}",
                    txtBox.Width, w_inch, v_scale);

                var newWidth = w_inch / 12;

                //TextNote txNote = doc.Create.NewTextNote(
                //  doc.ActiveView, p, XYZ.BasisX, XYZ.BasisY,
                //  newWidth, TextAlignFlags.TEF_ALIGN_LEFT
                //  | TextAlignFlags.TEF_ALIGN_BOTTOM, s ); // 2015
                //txNote.TextNoteType = textType; // 2015

                txNote = TextNote.Create(doc,
                    doc.ActiveView.Id, p, s, textType.Id); // 2016

                Debug.Print(
                    "NewTextNote lineWidth {0} times view scale "
                    + "{1} = {2} generated TextNote.Width {3}",
                    Util.RealString(newWidth),
                    Util.RealString(v_scale),
                    Util.RealString(newWidth * v_scale),
                    Util.RealString(txNote.Width));

                // This fails.

                //Debug.Assert(
                //  Util.IsEqual( newWidth * v_scale, txNote.Width ),
                //  "expected the NewTextNote lineWidth "
                //  + "argument to determine the resulting "
                //  + "text note width" );

                var wmin = txNote.GetMinimumAllowedWidth();
                var wmax = txNote.GetMaximumAllowedWidth();
                //double wnew = newWidth * v_scale; // this is 100 times too big
                var wnew = newWidth;

                txNote.Width = wnew;

                //6mm Arial
                //Text box width in pixels 668 = 6.95833349227905 inch, scale 100
                //NewTextNote lineWidth 0.58 times view scale 100 = 57.99 generated TextNote.Width 59.32

                t.Commit();
            }

            using (var t = new Transaction(doc))
            {
                t.Start("Change Text Colour");

                var color = Util.ToColorParameterValue(
                    255, 0, 0);

                var textNoteType = doc.GetElement(
                    txNote.GetTypeId());

                var param = textNoteType.get_Parameter(
                    BuiltInParameter.LINE_COLOR);

                // Note that this modifies the existing text 
                // note type for all instances using it. If
                // not desired, use Duplicate() first.

                param.Set(color);

                t.Commit();
            }

            return Result.Succeeded;
        }

        #endregion // Solution 1 using TextRenderer.MeasureText

        #region Solution 2 using Graphics.MeasureString

        private static float GetDpiX()
        {
            float xDpi, yDpi;

            using var g = Graphics.FromHwnd(IntPtr.Zero);
            xDpi = g.DpiX;
            yDpi = g.DpiY;

            return xDpi;
        }

        private static double GetStringWidth(string text, Font font)
        {
            var textWidth = 0.0;

            using var g = Graphics.FromHwnd(IntPtr.Zero);
            textWidth = g.MeasureString(text, font).Width;

            return textWidth;
        }

        public Result Execute_2(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var commandResult = Result.Succeeded;

            try
            {
                var uiApp = commandData.Application;
                var uiDoc = uiApp.ActiveUIDocument;
                var dbDoc = uiDoc.Document;
                var view = uiDoc.ActiveGraphicalView;

                var pLoc = XYZ.Zero;

                try
                {
                    pLoc = uiDoc.Selection.PickPoint(
                        "Please pick text insertion point");
                }
                catch (OperationCanceledException)
                {
                    Debug.WriteLine("Operation cancelled.");
                    message = "Operation cancelled.";

                    return Result.Succeeded;
                }

                var noteTypeList
                    = new FilteredElementCollector(dbDoc)
                        .OfClass(typeof(TextNoteType))
                        .Cast<TextNoteType>()
                        .ToList();

                // Sort note types into ascending text size

                var bipTextSize
                    = BuiltInParameter.TEXT_SIZE;

                noteTypeList.Sort((a, b)
                    => a.get_Parameter(bipTextSize).AsDouble()
                        .CompareTo(
                            b.get_Parameter(bipTextSize).AsDouble()));

                foreach (var textType in noteTypeList)
                {
                    Debug.WriteLine(textType.Name);

                    var paramTextFont
                        = textType.get_Parameter(
                            BuiltInParameter.TEXT_FONT);

                    var paramTextSize
                        = textType.get_Parameter(
                            BuiltInParameter.TEXT_SIZE);

                    var paramBorderSize
                        = textType.get_Parameter(
                            BuiltInParameter.LEADER_OFFSET_SHEET);

                    var paramTextBold
                        = textType.get_Parameter(
                            BuiltInParameter.TEXT_STYLE_BOLD);

                    var paramTextItalic
                        = textType.get_Parameter(
                            BuiltInParameter.TEXT_STYLE_ITALIC);

                    var paramTextUnderline
                        = textType.get_Parameter(
                            BuiltInParameter.TEXT_STYLE_UNDERLINE);

                    var paramTextWidthScale
                        = textType.get_Parameter(
                            BuiltInParameter.TEXT_WIDTH_SCALE);

                    var fontName = paramTextFont.AsString();

                    var textHeight = paramTextSize.AsDouble();

                    var textBold = paramTextBold.AsInteger() == 1
                        ? true
                        : false;

                    var textItalic = paramTextItalic.AsInteger() == 1
                        ? true
                        : false;

                    var textUnderline = paramTextUnderline.AsInteger() == 1
                        ? true
                        : false;

                    var textBorder = paramBorderSize.AsDouble();

                    var textWidthScale = paramTextWidthScale.AsDouble();

                    var textStyle = FontStyle.Regular;

                    if (textBold) textStyle |= FontStyle.Bold;

                    if (textItalic) textStyle |= FontStyle.Italic;

                    if (textUnderline) textStyle |= FontStyle.Underline;

                    var fontHeightInch = (float) textHeight * 12.0f;
                    var displayDpiX = GetDpiX();

                    var fontDpi = 96.0f;
                    var pointSize = (float) (textHeight * 12.0 * fontDpi);

                    var font = new Font(fontName, pointSize, textStyle);

                    var viewScale = view.Scale;

                    using (var t = new Transaction(dbDoc))
                    {
                        t.Start("Test TextNote lineWidth calculation");

                        var textString =
                            $"{textType.Name} ({fontName} {textHeight * 304.8:0.##}mm, {textStyle}, {textWidthScale * 100.0:0.##}%): The quick brown fox jumps over the lazy dog.";

                        var stringWidthPx = GetStringWidth(textString, font);

                        var stringWidthIn = stringWidthPx / displayDpiX;

                        Debug.WriteLine($"String Width in pixels: {stringWidthPx:F3}");
                        Debug.WriteLine($"{stringWidthIn * 25.4 * viewScale:F3} mm at 1:{viewScale}");

                        var stringWidthFt = stringWidthIn / 12.0;

                        var lineWidth = (stringWidthFt * textWidthScale
                                         + textBorder * 2.0) * viewScale;

                        //TextNote textNote = dbDoc.Create.NewTextNote(
                        //  view, pLoc, XYZ.BasisX, XYZ.BasisY, 0.001,
                        //  TextAlignFlags.TEF_ALIGN_LEFT
                        //  | TextAlignFlags.TEF_ALIGN_TOP, textString ); // 2015
                        //textNote.TextNoteType = textType; // 2015

                        var textNote = TextNote.Create(dbDoc,
                            view.Id, pLoc, textString, textType.Id); // 2016

                        textNote.Width = lineWidth;

                        t.Commit();
                    }

                    // Place next text note below this one with 5 mm gap

                    pLoc += view.UpDirection.Multiply(
                        (textHeight + 5.0 / 304.8)
                        * viewScale).Negate();
                }
            }
            catch (ExternalApplicationException e)
            {
                message = e.Message;
                Debug.WriteLine($"Exception Encountered (Application)\n{e.Message}\nStack Trace: {e.StackTrace}");

                commandResult = Result.Failed;
            }
            catch (Exception e)
            {
                message = e.Message;
                Debug.WriteLine($"Exception Encountered (General)\n{e.Message}\nStack Trace: {e.StackTrace}");

                commandResult = Result.Failed;
            }

            return commandResult;
        }

        #endregion // Solution 2 using Graphics.MeasureString
    }
}