#region Header

//
// CmdCropToRoom.cs - set 3D view crop box to room extents
//
// Copyright (C) 2009-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdCropToRoom : IExternalCommand
    {
        private static int _i = -1;

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var doc = app.ActiveUIDocument.Document;

            if (doc.ActiveView is not View3D view3d)
            {
                message = "Please activate a 3D view"
                          + " before running this command.";

                return Result.Failed;
            }

            using var t = new Transaction(doc);
            t.Start("Crop to Room");

            // get the 3d view crop box:

            var bb = view3d.CropBox;

            // get the transform from the current view
            // to the 3D model:

            var transform = bb.Transform;

            // get the transform from the 3D model
            // to the current view:

            var transformInverse = transform.Inverse;

            // get all rooms in the model:

            var collector
                = new FilteredElementCollector(doc);

            collector.OfClass(typeof(Room));
            var rooms = collector.ToElements();
            var n = rooms.Count;

            var room = 0 < n
                ? rooms[BumpRoomIndex(n)] as Room
                : null;

            if (null == room)
            {
                message = "No room element found in project.";
                return Result.Failed;
            }

            // Collect all vertices of room closed shell
            // to determine its extents:

            var e = room.ClosedShell;
            var vertices = new List<XYZ>();

            //foreach( GeometryObject o in e.Objects ) // 2012

            foreach (var o in e) // 2013
                if (o is Solid solid)
                    // Iterate over all the edges of all solids:

                    foreach (Edge edge in solid.Edges)
                    foreach (var p in edge.Tessellate())
                        // Collect all vertices,
                        // including duplicates:

                        vertices.Add(p);

            var verticesIn3dView = new List<XYZ>();

            foreach (var p in vertices)
                verticesIn3dView.Add(
                    transformInverse.OfPoint(p));

            // Ignore the Z coorindates and find the
            // min and max X and Y in the 3d view:

            double xMin = 0, yMin = 0, xMax = 0, yMax = 0;

            var first = true;
            foreach (var p in verticesIn3dView)
                if (first)
                {
                    xMin = p.X;
                    yMin = p.Y;
                    xMax = p.X;
                    yMax = p.Y;
                    first = false;
                }
                else
                {
                    if (xMin > p.X)
                        xMin = p.X;
                    if (yMin > p.Y)
                        yMin = p.Y;
                    if (xMax < p.X)
                        xMax = p.X;
                    if (yMax < p.Y)
                        yMax = p.Y;
                }

            // Grow the crop box by one twentieth of its
            // size to include the walls of the room:

            var d = 0.05 * (xMax - xMin);
            xMin = xMin - d;
            xMax = xMax + d;

            d = 0.05 * (yMax - yMin);
            yMin = yMin - d;
            yMax = yMax + d;

            bb.Max = new XYZ(xMax, yMax, bb.Max.Z);
            bb.Min = new XYZ(xMin, yMin, bb.Min.Z);

            view3d.CropBox = bb;

            // Change the crop view setting manually or
            // programmatically to see the result:

            view3d.CropBoxActive = true;
            view3d.CropBoxVisible = true;
            t.Commit();

            return Result.Succeeded;
        }

        #region SetSectionBox

        /// <summary>
        ///     Set 3D view section box to selected element extents.
        /// </summary>
        private void SectionBox(UIDocument uidoc)
        {
            var doc = uidoc.Document;
            var view = doc.ActiveView;

            var Min_X = double.MaxValue;
            var Min_Y = double.MaxValue;
            var Min_Z = double.MaxValue;

            var Max_X = Min_X;
            var Max_Y = Min_Y;
            var Max_Z = Min_Z;

            var ids
                = uidoc.Selection.GetElementIds();

            foreach (var id in ids)
            {
                var elm = doc.GetElement(id);
                var box = elm.get_BoundingBox(view);
                if (box.Max.X > Max_X) Max_X = box.Max.X;
                if (box.Max.Y > Max_Y) Max_Y = box.Max.Y;
                if (box.Max.Z > Max_Z) Max_Z = box.Max.Z;

                if (box.Min.X < Min_X) Min_X = box.Min.X;
                if (box.Min.Y < Min_Y) Min_Y = box.Min.Y;
                if (box.Min.Z < Min_Z) Min_Z = box.Min.Z;
            }

            var Max = new XYZ(Max_X, Max_Y, Max_Z);
            var Min = new XYZ(Min_X, Min_Y, Min_Z);

            var myBox = new BoundingBoxXYZ();

            myBox.Min = Min;
            myBox.Max = Max;

            (view as View3D).SetSectionBox(myBox);
        }

        #endregion // SetSectionBox


        #region Element in View Crop Box Predicate

        /// <summary>
        ///     Return true if element is outside of view crop box
        /// </summary>
        private bool IsElementOutsideCropBox(Element e, View v)
        {
            var rc = v.CropBoxActive;

            if (rc)
            {
                var vBox = v.CropBox;
                var eBox = e.get_BoundingBox(v);

                var tInv = v.CropBox.Transform.Inverse;
                eBox.Max = tInv.OfPoint(eBox.Max);
                eBox.Min = tInv.OfPoint(eBox.Min);

                rc = eBox.Min.X > vBox.Max.X
                     || eBox.Max.X < vBox.Min.X
                     || eBox.Min.Y > vBox.Max.Y
                     || eBox.Max.Y < vBox.Min.Y;
            }

            return rc;
        }

        #endregion // Element in View Crop Box Predicate

        /// <summary>
        ///     Increment and return the current room index.
        ///     Every call to this method increments the current room index by one.
        ///     If it exceeds the number of rooms in the model, loop back to zero.
        /// </summary>
        /// <param name="room_count">Number of rooms in the model.</param>
        /// <returns>Incremented current room index, looping around to zero when max room count is reached.</returns>
        private static int BumpRoomIndex(int room_count)
        {
            ++_i;

            if (_i >= room_count) _i = 0;
            return _i;
        }

        #region Set View Cropbox to Section Box

        // https://forums.autodesk.com/t5/revit-api-forum/set-view-cropbox-to-a-section-box/m-p/9600049

        public static void AdjustViewCropToSectionBox(
            /*this*/ View3D view)
        {
            if (!view.IsSectionBoxActive) return;
            if (!view.CropBoxActive) view.CropBoxActive = true;
            var CropBox = view.CropBox;
            var SectionBox = view.GetSectionBox();
            var T = CropBox.Transform;
            var Corners = BBCorners(SectionBox, T);
            var MinX = Corners.Min(j => j.X);
            var MinY = Corners.Min(j => j.Y);
            var MinZ = Corners.Min(j => j.Z);
            var MaxX = Corners.Max(j => j.X);
            var MaxY = Corners.Max(j => j.Y);
            var MaxZ = Corners.Max(j => j.Z);
            CropBox.Min = new XYZ(MinX, MinY, MinZ);
            CropBox.Max = new XYZ(MaxX, MaxY, MaxZ);
            view.CropBox = CropBox;
        }

        private static XYZ[] BBCorners(BoundingBoxXYZ SectionBox, Transform T)
        {
            var sbmn = SectionBox.Min;
            var sbmx = SectionBox.Max;
            var Btm_LL = sbmn; // Lower Left
            var Btm_LR = new XYZ(sbmx.X, sbmn.Y, sbmn.Z); // Lower Right
            var Btm_UL = new XYZ(sbmn.X, sbmx.Y, sbmn.Z); // Upper Left
            var Btm_UR = new XYZ(sbmx.X, sbmx.Y, sbmn.Z); // Upper Right
            var Top_UR = sbmx; // Upper Right
            var Top_UL = new XYZ(sbmn.X, sbmx.Y, sbmx.Z); // Upper Left
            var Top_LR = new XYZ(sbmx.X, sbmn.Y, sbmx.Z); // Lower Right
            var Top_LL = new XYZ(sbmn.X, sbmn.Y, sbmx.Z); // Lower Left
            var Out = new XYZ[8]
            {
                Btm_LL, Btm_LR, Btm_UL, Btm_UR,
                Top_UR, Top_UL, Top_LR, Top_LL
            };
            for (int i = 0, loopTo = Out.Length - 1; i <= loopTo; i++)
            {
                // Transform bounding box coords to model coords
                Out[i] = SectionBox.Transform.OfPoint(Out[i]);
                // Transform bounding box coords to view coords
                Out[i] = T.Inverse.OfPoint(Out[i]);
            }

            return Out;
        }

        #endregion // Set View Cropbox to Section Box
    }
}