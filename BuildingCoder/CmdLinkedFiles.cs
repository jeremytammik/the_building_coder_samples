#region Header

//
// CmdLinkedFiles.cs - retrieve linked files
// in current project
//
// Copyright (C) 2008-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdLinkedFiles : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var app = uiapp.Application;
            var doc = uiapp.ActiveUIDocument.Document;

            var dict
                = GetFilePaths(app, true);

            var links
                = GetLinkedFiles(doc).ToElements();

            var n = links.Count;
            Debug.Print(
                "There {0} {1} linked Revit model{2}.",
                1 == n ? "is" : "are", n,
                Util.PluralSuffix(n));

            string name;
            var sep = new[] {':'};
            string[] a;

            foreach (var link in links)
            {
                name = link.Name;
                a = name.Split(sep);
                name = a[0].Trim();

                Debug.Print(
                    "Link '{0}' full path is '{1}'.",
                    name, dict[name]);

                #region Explore Location

                var loc = link.Location; // unknown content in here
                if (loc is LocationPoint lp)
                {
                    var p = lp.Point;
                }

                var e = link.get_Geometry(new Options());
                if (null != e) // no geometry defined
                    //GeometryObjectArray objects = e.Objects; // 2012
                    //n = objects.Size; // 2012
                    n = e.Count(); // 2013

                #endregion // Explore Location

                #region Explore Pinning

                if (link is ImportInstance instance) // nope, this never happens ...
                {
                    var s = instance.Pinned ? "" : "not ";
                    Debug.Print("{1}pinned", s);
                    instance.Pinned = !instance.Pinned;
                }

                #endregion // Explore Pinning
            }

            return Result.Succeeded;
        }

        /// <summary>
        ///     Return DWF links
        /// </summary>
        //FilteredElementCollector GetDwfLinks(
        //  Document doc )
        //{
        //  // http://forums.autodesk.com/t5/revit-api/get-all-linked-dwfx-files-from-in-revit-document/td-p/5769622

        //  return Util.GetElementsOfType( doc,
        //    typeof( ImportInstance ) );
        //}
        private FilteredElementCollector GetLinkedFiles(
            Document doc)
        {
            return Util.GetElementsOfType(doc,
                typeof(Instance),
                BuiltInCategory.OST_RvtLinks);
        }

        private Dictionary<string, string> GetFilePaths(
            Application app,
            bool onlyImportedFiles)
        {
            var docs = app.Documents;
            var n = docs.Size;

            var dict
                = new Dictionary<string, string>(n);

            foreach (Document doc in docs)
                if (!onlyImportedFiles
                    || null == doc.ActiveView)
                {
                    var path = doc.PathName;
                    var i = path.LastIndexOf("\\") + 1;
                    var name = path.Substring(i);
                    dict.Add(name, path);
                }

            return dict;
        }
    }
}