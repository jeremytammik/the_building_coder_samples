#region Header

//
// CmdPartAtom.cs - extract part atom from family file
//
// Copyright (C) 2010-2020 by By Hеvard Dagsvik, Symetri 
// and Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.Diagnostics;
using System.IO;
using System.Text;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdPartAtom : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var app = uiapp.Application;

            var familyFilePath
                = "C:/Documents and Settings/All Users"
                  + "/Application Data/Autodesk/RAC 2011"
                  + "/Metric Library/Doors/M_Double-Flush.rfa";

            familyFilePath = "C:/Users/All Users/Autodesk"
                             + "/RVT 2017/Libraries/US Metric/Doors"
                             + "/M_Door-Double-Flush_Panel.rfa";

            var xmlPath = "C:/tmp/ExtractPartAtom.xml";

            // Using Revit API:

            app.ExtractPartAtomFromFamilyFile(
                familyFilePath, xmlPath);

            // Revit API independent:

            var xml_data = GetFamilyXmlData(xmlPath);

            return Result.Succeeded;
        }

        /// <summary>
        ///     Faster ExtractPartAtom reimplementation,
        ///     independent of Revit API, for standalone
        ///     external use. By Hеvard Dagsvik, Symetri.
        /// </summary>
        /// <param name="family_file_path">Family file path</param>
        /// <returns>XML data</returns>
        private static string GetFamilyXmlData(
            string family_file_path)
        {
            var array = File.ReadAllBytes(family_file_path);

            var string_file = Encoding.UTF8.GetString(array);

            string xml_data = null;

            var start = string_file.IndexOf("<entry");

            if (start == -1)
            {
                Debug.Print($"XML start not detected: {family_file_path}");
            }
            else
            {
                var end = string_file.IndexOf("/entry>");

                if (end == -1)
                {
                    Debug.Print($"XML end not detected: {family_file_path}");
                }
                else
                {
                    end = end + 7;

                    var length = end - start;

                    if (length <= 0)
                        Debug.Print($"XML length is 0 or less: {family_file_path}");
                    else
                        xml_data = string_file.Substring(
                            start, length);
                }
            }

            return xml_data;
        }

        private static void createPartAtomFile(
            Application app,
            string rfaFilePath,
            string partAtomFilePath)
        {
            app.ExtractPartAtomFromFamilyFile(
                rfaFilePath,
                partAtomFilePath);
        }
    }
}