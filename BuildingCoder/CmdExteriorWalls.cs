#region Header

//
// CmdExteriorWalls.cs - retrieve all parameter values for all elements of the given categories
//
// Copyright (C) 2018-2020 by Feng Wang and Jeremy Tammik, Autodesk Inc. All rights reserved.
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
    internal class CmdExteriorWalls : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;

            var ids = GetOutermostWalls(doc);

            uidoc.Selection.SetElementIds(ids);

            return Result.Succeeded;
        }

        /// <summary>
        ///     Return a bounding box around all the
        ///     walls in the entire model; for just a
        ///     building, or several buildings, this is
        ///     obviously equal to the model extents.
        /// </summary>
        private static BoundingBoxXYZ GetBoundingBoxAroundAllWalls(
            Document doc,
            View view = null)
        {
            // Default constructor creates cube from -100 to 100;
            // maybe too big, but who cares?

            var bb = new BoundingBoxXYZ();

            var walls
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(Wall));

            foreach (Wall wall in walls)
                bb.ExpandToContain(
                    wall.get_BoundingBox(
                        view));
            return bb;
        }

        /// <summary>
        ///     过滤出需要的墙体 --
        ///     Return all walls that are generating boundary
        ///     segments for the given room. Includes debug
        ///     code to compare wall lengths and wall areas.
        /// </summary>
        private static List<ElementId>
            RetrieveWallsGeneratingRoomBoundaries(
                Document doc,
                Room room)
        {
            var ids = new List<ElementId>();

            var boundaries
                = room.GetBoundarySegments(
                    new SpatialElementBoundaryOptions());

            var n = boundaries.Count;

            int iBoundary = 0, iSegment;

            foreach (var b in boundaries)
            {
                ++iBoundary;
                iSegment = 0;
                foreach (var s in b)
                {
                    ++iSegment;

                    // Retrieve the id of the element that 
                    // produces this boundary segment

                    var neighbour = doc.GetElement(
                        s.ElementId);

                    var curve = s.GetCurve();
                    var length = curve.Length;

                    if (neighbour is Wall wall)
                    {
                        var p = wall.get_Parameter(
                            BuiltInParameter.HOST_AREA_COMPUTED);

                        var area = p.AsDouble();

                        var lc
                            = wall.Location as LocationCurve;

                        var wallLength = lc.Curve.Length;

                        ids.Add(wall.Id);
                    }
                }
            }

            return ids;
        }

        /// <summary>
        ///     获取当前模型指定视图内的所有最外层的墙体
        ///     Get all the outermost walls in the
        ///     specified view of the current model
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="view">
        ///     视图,默认是当前激活的视图
        ///     View, default is currently active view
        /// </param>
        public static List<ElementId> GetOutermostWalls(
            Document doc,
            View view = null)
        {
            var offset = Util.MmToFoot(1000);

            view ??= doc.ActiveView;

            #region Obsolete code using wall location line instad of bounding box

            //获取顶点 最小x，最大y ； 最大x，最小y

#if BEFORE_USING_BOUNDING_BOX
      List<Wall> wallList = new FilteredElementCollector( doc )
        .OfClass( typeof( Wall ) )
        .Cast<Wall>()
        .ToList();

      double maxX = -1D;
      double minX = -1D;
      double maxY = -1D;
      double minY = -1D;

      wallList.ForEach( ( wall ) =>
      {
        Curve curve = ( wall.Location as LocationCurve ).Curve;
        XYZ xyz1 = curve.GetEndPoint( 0 );
        XYZ xyz2 = curve.GetEndPoint( 1 );

        double _minX = Math.Min( xyz1.X, xyz2.X );
        double _maxX = Math.Max( xyz1.X, xyz2.X );
        double _minY = Math.Min( xyz1.Y, xyz2.Y );
        double _maxY = Math.Max( xyz1.Y, xyz2.Y );

        if( curve.IsCyclic )
        {
          Arc arc = curve as Arc;
          double _radius = arc.Radius;
          //粗略对x和y 加/减
          _maxX += _radius;
          _minX -= _radius;
          _maxY += _radius;
          _minY += _radius;
        }

        if( minX == -1 ) minX = _minX;
        if( maxX == -1 ) maxX = _maxX;
        if( maxY == -1 ) maxY = _maxY;
        if( minY == -1 ) minY = _minY;

        if( _minX < minX ) minX = _minX;
        if( _maxX > maxX ) maxX = _maxX;
        if( _maxY > maxY ) maxY = _maxY;
        if( _minY < minY ) minY = _minY;
      } );
      double minX = bb.Min.X - offset;
      double maxX = bb.Max.X + offset;
      double minY = bb.Min.Y - offset;
      double maxY = bb.Max.Y + offset;
      CurveArray curves = new CurveArray();
      Line line1 = Line.CreateBound( new XYZ( minX, maxY, 0 ), new XYZ( maxX, maxY, 0 ) );
      Line line2 = Line.CreateBound( new XYZ( maxX, maxY, 0 ), new XYZ( maxX, minY, 0 ) );
      Line line3 = Line.CreateBound( new XYZ( maxX, minY, 0 ), new XYZ( minX, minY, 0 ) );
      Line line4 = Line.CreateBound( new XYZ( minX, minY, 0 ), new XYZ( minX, maxY, 0 ) );
      curves.Append( line1 );
      curves.Append( line2 );
      curves.Append( line3 );
      curves.Append( line4 );
#endif // BEFORE_USING_BOUNDING_BOX

            #endregion // Obsolete code using wall location line instad of bounding box

            var bb = GetBoundingBoxAroundAllWalls(
                doc, view);

            var voffset = offset * (XYZ.BasisX + XYZ.BasisY);
            bb.Min -= voffset;
            bb.Max += voffset;

            var bottom_corners = Util.GetBottomCorners(
                bb, 0);

            var curves = new CurveArray();
            for (var i = 0; i < 4; ++i)
            {
                var j = i < 3 ? i + 1 : 0;
                curves.Append(Line.CreateBound(
                    bottom_corners[i], bottom_corners[j]));
            }

            using var group
                = new TransactionGroup(doc);
            Room newRoom;

            group.Start("Find Outermost Walls");

            using (var transaction
                = new Transaction(doc))
            {
                transaction.Start(
                    "Create New Room Boundary Lines");

                var sketchPlane = SketchPlane.Create(
                    doc, view.GenLevel.Id);

                var modelCaRoomBoundaryLines
                    = doc.Create.NewRoomBoundaryLines(
                        sketchPlane, curves, view);

                // 创建房间的坐标点 -- Create room coordinates

                var d = Util.MmToFoot(600);
                var point = new UV(bb.Min.X + d, bb.Min.Y + d);

                // 根据选中点，创建房间 当前视图的楼层 doc.ActiveView.GenLevel
                // Create room at selected point on the current view level

                newRoom = doc.Create.NewRoom(view.GenLevel, point);

                if (newRoom == null)
                {
                    var msg = "创建房间失败。";
                    TaskDialog.Show("xx", msg);
                    transaction.RollBack();
                    return null;
                }

                var tag = doc.Create.NewRoomTag(
                    new LinkElementId(newRoom.Id),
                    point, view.Id);

                transaction.Commit();
            }

            //获取房间的墙体 -- Get the room walls

            var ids
                = RetrieveWallsGeneratingRoomBoundaries(
                    doc, newRoom);

            group.RollBack(); // 撤销

            return ids;
        }

        /// <summary>
        ///     Convert a newline-separated string of integers
        ///     to a list of ElementId instances suitable for
        ///     passing into SetElementIds.
        /// </summary>
        private List<ElementId> GetElementIdsFromString(string x)
        {
            return new List<ElementId>(x.Split('\n')
                .Select(s
                    => new ElementId(int.Parse(s))));
        }
    }
}