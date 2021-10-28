#region Header

//
// CmdWallNeighbours.cs - determine wall
// neighbours, i.e. walls joined at end points
//
// Copyright (C) 2008-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdWallNeighbours : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;
            var doc = app.ActiveUIDocument.Document;

            var walls = new List<Element>();
            if (!Util.GetSelectedElementsOrAll(
                walls, uidoc, typeof(Wall)))
            {
                var sel = uidoc.Selection;
                message = 0 < sel.GetElementIds().Count
                    ? "Please select some wall elements."
                    : "No wall elements found.";
                return Result.Failed;
            }

            int i, n;
            string desc, s = null;
            //List<Element> neighbours;
            ElementArray neighbours;

            foreach (Wall wall in walls)
            {
                desc = Util.ElementDescription(wall);

                if (wall.Location is not LocationCurve c)
                {
                    s = $"{desc}: No wall curve found.";
                }
                else
                {
                    s = string.Empty;

                    for (i = 0; i < 2; ++i)
                    {
                        neighbours = c.get_ElementsAtJoin(i);
                        n = neighbours.Size;

                        s += $"\n\n{desc} {(0 == i ? "start" : "end")} point has {n} neighbour{Util.PluralSuffix(n)}{Util.DotOrColon(n)}";

                        foreach (Wall nb in neighbours)
                            s += $"\n  {Util.ElementDescription(nb)}";
                    }
                }

                Util.InfoMsg(s);
            }

            return Result.Failed;
        }
    }
}