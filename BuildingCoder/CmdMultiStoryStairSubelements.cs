#region Header

//
// CmdMultistoryStairSubelements.cs - Access all subelements of all MultistoryStair instances
//
// Copyright (C) 2018-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdMultistoryStairSubelements : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;

            // Retrieve selected multistory stairs, or all 
            // such elements, if nothing is pre-selected:

            var msss = new List<Element>();

            if (!Util.GetSelectedElementsOrAll(
                msss, uidoc, typeof(MultistoryStairs)))
            {
                var sel = uidoc.Selection;
                message = 0 < sel.GetElementIds().Count
                    ? "Please select some floor elements."
                    : "No floor elements found.";
                return Result.Failed;
            }

            var n = msss.Count;

            Debug.Print("{0} multi story stair{1} selected{2}",
                n, Util.PluralSuffix(n), Util.DotOrColon(n));

            foreach (MultistoryStairs mss in msss)
            {
                // Get the stairs by `GetAllStairsIds`, then 
                // call `Element.GetSubelements` to get the 
                // subelements of each stair.

                var ids = mss.GetAllStairsIds();

                n = ids.Count;

                Debug.Print(
                    "Multi story stair '{0}' has {1} stair instance{2}{3}",
                    Util.ElementDescription(mss),
                    n, Util.PluralSuffix(n), Util.DotOrColon(n));

                foreach (var id in ids)
                {
                    var e = doc.GetElement(id);

                    Debug.Assert(e is Stairs stair,
                        "expected a stair element");

                    var ses = e.GetSubelements();

                    n = ses.Count;

                    Debug.Print(
                        "Multi story stair instance '{0}' has {1} subelement{2}{3}",
                        Util.ElementDescription(e),
                        n, Util.PluralSuffix(n), Util.DotOrColon(n));

                    foreach (var se in ses)
                    {
                        Debug.Print(
                            "Subelement {0} of type {1}",
                            se.UniqueId, se.TypeId.IntegerValue);

                        var e2 = doc.GetElement(se.UniqueId); // null
                        var e2t = doc.GetElement(se.TypeId); // StairsType
                        var ps = se.GetAllParameters(); // 24 parameters
                        var geo = se.GetGeometryObject(null);
                    }
                }
            }

            return Result.Succeeded;
        }
    }
}