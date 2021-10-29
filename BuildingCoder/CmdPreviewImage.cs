#region Header

//
// CmdPreviewImage.cs - display the element type preview image of all family instances
//
// Copyright (C) 2010-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Size = System.Drawing.Size;

// WindowsBase
// PresentationCore

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdPreviewImage : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;

            var collector
                = new FilteredElementCollector(doc);

            collector.OfClass(typeof(FamilyInstance));

            foreach (FamilyInstance fi in collector)
            {
                Debug.Assert(null != fi.Category,
                    "expected family instance to have a valid category");

                var typeId = fi.GetTypeId();

                var type = doc.GetElement(typeId)
                    as ElementType;

                var imgSize = new Size(200, 200);

                var image = type.GetPreviewImage(imgSize);

                // encode image to jpeg for test display purposes:

                var encoder
                    = new JpegBitmapEncoder();

                encoder.Frames.Add(BitmapFrame.Create(
                    ConvertBitmapToBitmapSource(image)));

                encoder.QualityLevel = 25;

                var filename = "a.jpg";

                var file = new FileStream(
                    filename, FileMode.Create, FileAccess.Write);

                encoder.Save(file);
                file.Close();

                Process.Start(filename); // test display
            }

            return Result.Succeeded;
        }

        private static BitmapSource ConvertBitmapToBitmapSource(
            Bitmap bmp)
        {
            return Imaging
                .CreateBitmapSourceFromHBitmap(
                    bmp.GetHbitmap(),
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
        }
    }
}