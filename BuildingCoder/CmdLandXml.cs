#region Header

//
// CmdLandXml.cs - import LandXML data and create TopographySurface
//
// Copyright (C) 2010-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.Collections.Generic;
using System.Xml;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using W = System.Windows.Forms;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdLandXml : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var doc = app.ActiveUIDocument.Document;

            var dlg = new W.OpenFileDialog();

            // select file to open

            dlg.Filter = "LandXML files (*.xml)|*.xml";

            dlg.Title = "Import LandXML and "
                        + "Create TopographySurface";

            if (dlg.ShowDialog() != W.DialogResult.OK) return Result.Cancelled;

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(dlg.FileName);

            var pnts
                = xmlDoc.GetElementsByTagName("Pnts");

            var separator = new[] {' '};
            double x = 0, y = 0, z = 0;

            var pts = new List<XYZ>();

            for (var k = 0; k < pnts.Count; ++k)
            for (var i = 0;
                i < pnts[k].ChildNodes.Count;
                ++i)
            {
                var j = 1;

                var text = pnts[k].ChildNodes[i].InnerText;
                var coords = text.Split(separator);

                foreach (var coord in coords)
                {
                    switch (j)
                    {
                        case 1:
                            x = double.Parse(coord);
                            break;
                        case 2:
                            y = double.Parse(coord);
                            break;
                        case 3:
                            z = double.Parse(coord);
                            break;
                    }

                    j++;
                }

                pts.Add(new XYZ(x, y, z));
            }

            using var t = new Transaction(doc);
            t.Start("Create Topography Surface");

            //TopographySurface surface = doc.Create.NewTopographySurface( pntList );

            //TopographySurface surface = doc.Create.NewTopographySurface( pts ); // 2013

            var surface = TopographySurface.Create(doc, pts); // 2014

            t.Commit();

            return Result.Succeeded;
        }
    }
}

// C:\a\doc\revit\blog\zip\LandXMLfiles\GSG_features_surfaces_with_volumes.xml