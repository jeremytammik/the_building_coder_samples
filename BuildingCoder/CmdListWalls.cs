#region Header

//
// CmdListWalls.cs - list walls
//
// Copyright (C) 2009-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdListWalls : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var doc = app.ActiveUIDocument.Document;

            var walls
                = new FilteredElementCollector(doc);

            walls.OfClass(typeof(Wall));

            foreach (Wall wall in walls)
            {
                var param = wall.get_Parameter(
                    BuiltInParameter.HOST_AREA_COMPUTED);

                var a = param is {StorageType: StorageType.Double}
                    ? param.AsDouble()
                    : 0.0;

                var s = null != param
                    ? param.AsValueString()
                    : "null";

                var lc = wall.Location as LocationCurve;

                var p = lc.Curve.GetEndPoint(0);
                var q = lc.Curve.GetEndPoint(1);

                var l = q.DistanceTo(p);

                var format
                    = "Wall <{0} {1}> length {2} area {3} ({4})";

                Debug.Print(format,
                    wall.Id.IntegerValue.ToString(), wall.Name,
                    Util.RealString(l), Util.RealString(a),
                    s);
            }

            return Result.Succeeded;
        }
    }
}