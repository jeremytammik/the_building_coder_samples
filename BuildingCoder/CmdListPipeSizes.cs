#region Header

//
// CmdListPipeSizes.cs - list pipe sizes in a project
//
// Copyright (C) 2015-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdListPipeSizes : IExternalCommand
    {
        private const string _filename = "C:/pipesizes.txt";

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;
            var doc = uidoc.Document;
            GetPipeSegmentSizes(doc);
            return Result.Succeeded;
        }

        private string FootToMmString(double a)
        {
            return Util.FootToMm(a)
                .ToString("0.##")
                .PadLeft(8);
        }

        /// <summary>
        ///     List all the pipe segment sizes in the given document.
        /// </summary>
        /// <param name="doc"></param>
        private void GetPipeSegmentSizes(
            Document doc)
        {
            var segments
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(Segment));

            using var file = new StreamWriter(
                _filename, true);
            foreach (Segment segment in segments)
            {
                file.WriteLine(segment.Name);

                foreach (var size in segment.GetSizes())
                    file.WriteLine("  {0} {1} {2}", FootToMmString(size.NominalDiameter), FootToMmString(size.InnerDiameter), FootToMmString(size.OuterDiameter));
            }
        }
    }
}