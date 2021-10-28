#region Header

//
// CmdWallBottomFace.cs - determine the bottom face of a wall
//
// Copyright (C) 2009-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdWallBottomFace : IExternalCommand
    {
        private const double _tolerance = 0.001;

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;
            var doc = uidoc.Document;

            var s = "a wall, to retrieve its bottom face";

            if (Util.SelectSingleElementOfType(
                uidoc, typeof(Wall), s, false) is not Wall wall)
            {
                message = "Please select a wall.";
            }
            else
            {
                var opt = app.Application.Create.NewGeometryOptions();
                var e = wall.get_Geometry(opt);

                //foreach( GeometryObject obj in e.Objects ) // 2012

                foreach (var obj in e) // 2013
                {
                    var solid = obj as Solid;
                    if (null != solid)
                        foreach (Face face in solid.Faces)
                        {
                            var pf = face as PlanarFace;
                            if (null != pf)
                                if (Util.IsVertical(pf.FaceNormal, _tolerance)
                                    && pf.FaceNormal.Z < 0)
                                {
                                    Util.InfoMsg(string.Format(
                                        "The bottom face area is {0},"
                                        + " and its origin is at {1}.",
                                        Util.RealString(pf.Area),
                                        Util.PointString(pf.Origin)));
                                    break;
                                }
                        }
                }
            }

            return Result.Failed;
        }
    }
}