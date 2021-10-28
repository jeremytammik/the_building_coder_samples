#region Header

//
// CmdSheetData.cs - export sheet data to XML file
//
// Copyright (C) 2010-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System;
using System.Collections.Generic;
using System.Xml;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    /// <summary>
    ///     Arbitrary sheet data container.
    /// </summary>
    internal class SheetData
    {
        public SheetData(ViewSheet v)
        {
            IsPlaceholder = v.IsPlaceholder;
            Name = v.Name;
            SheetNumber = v.SheetNumber;
        }

        public bool IsPlaceholder { get; set; }
        public string Name { get; set; }
        public string SheetNumber { get; set; }
    }

    /// <summary>
    ///     Gather sheet data from document
    ///     and export to XML file.
    /// </summary>
    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdSheetData : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var doc = app.ActiveUIDocument.Document;

            // retrieve all sheets

            var a
                = new FilteredElementCollector(doc);

            a.OfCategory(BuiltInCategory.OST_Sheets);
            a.OfClass(typeof(ViewSheet));

            // create a collection of all relevant data

            var data = new List<SheetData>();

            foreach (ViewSheet v in a)
            {
                // create some data for each sheet and add
                // to some serializable collection called Data
                var item = new SheetData(v);
                data.Add(item);
            }

            // write out data collection to xml

            var w = new XmlTextWriter(
                "C:/SheetData.xml", null);

            w.Formatting = Formatting.Indented;
            w.WriteStartDocument();
            w.WriteComment($" SheetData from {doc.PathName} on {DateTime.Now} by Jeremy ");

            w.WriteStartElement("ViewSheets");

            foreach (var item in data)
            {
                w.WriteStartElement("ViewSheet");

                w.WriteElementString("IsPlaceholder",
                    item.IsPlaceholder.ToString());

                w.WriteElementString("Name", item.Name);

                w.WriteElementString("SheetNumber",
                    item.SheetNumber);

                w.WriteEndElement();
            }

            w.WriteEndElement();
            w.WriteEndDocument();
            w.Close();

            return Result.Succeeded;
        }
    }
}