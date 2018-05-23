#region Header
//
// CmdExteriorWalls.cs - retrieve all parameter values for all elements of the given categories
//
// Copyright (C) 2018 Feng Wang and Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.ReadOnly )]
  class CmdExteriorWalls : IExternalCommand
  {
    static BoundingBoxXYZ GetWallBoundingBoxAroundAllWalls( 
      Document doc,
      View view = null )
    {
      // Default constructor creates cube from -100 to 100

      BoundingBoxXYZ bb = new BoundingBoxXYZ();

      FilteredElementCollector walls
        = new FilteredElementCollector( doc )
          .OfClass( typeof( Wall ) );

      foreach( Wall wall in walls )
      {
        bb.ExpandToContain( 
          wall.get_BoundingBox( 
            view ) );
      }
      return bb;
    }

    /// <summary>
    /// 过滤出需要的墙体 -- Filter out the required walls
    /// </summary>
    static List<ElementId> DetermineAdjacentElementLengthsAndWallAreas(
      Document doc,
      Room room )
    {
      List<ElementId> elementIds = new List<ElementId>();

      IList<IList<BoundarySegment>> boundaries
        = room.GetBoundarySegments( 
          new SpatialElementBoundaryOptions() );

      int n = boundaries.Count;

      int iBoundary = 0, iSegment;

      foreach( IList<BoundarySegment> b in boundaries )
      {
        ++iBoundary;
        iSegment = 0;
        foreach( BoundarySegment s in b )
        {
          ++iSegment;
          Element neighbour = doc.GetElement(
            s.ElementId );

          Curve curve = s.GetCurve();
          double length = curve.Length;

          if( neighbour is Wall )
          {
            Wall wall = neighbour as Wall;

            Parameter p = wall.get_Parameter(
              BuiltInParameter.HOST_AREA_COMPUTED );

            double area = p.AsDouble();

            LocationCurve lc
              = wall.Location as LocationCurve;

            double wallLength = lc.Curve.Length;

            elementIds.Add( wall.Id );
          }
        }
      }
      return elementIds;
    }

    /// <summary>
    /// 获取当前模型指定视图内的所有最外层的墙体
    /// Get all the outermost walls in the 
    /// specified view of the current model
    /// </summary>
    /// <param name="doc"></param>
    /// <param name="view">视图,默认是当前激活的视图 
    /// View, default is currently active view</param>
    public static List<ElementId> GetOutermostWalls( 
      Document doc, 
      View view = null )
    {
      double offset = 1000 / 304.8;

      //获取顶点 最小x，最大y ； 最大x，最小y

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

      minX -= offset;
      maxX += offset;
      minY -= offset;
      maxY += offset;

      CurveArray curves = new CurveArray();
      Line line1 = Line.CreateBound( new XYZ( minX, maxY, 0 ), new XYZ( maxX, maxY, 0 ) );
      Line line2 = Line.CreateBound( new XYZ( maxX, maxY, 0 ), new XYZ( maxX, minY, 0 ) );
      Line line3 = Line.CreateBound( new XYZ( maxX, minY, 0 ), new XYZ( minX, minY, 0 ) );
      Line line4 = Line.CreateBound( new XYZ( minX, minY, 0 ), new XYZ( minX, maxY, 0 ) );
      curves.Append( line1 );
      curves.Append( line2 );
      curves.Append( line3 );
      curves.Append( line4 );

      using( TransactionGroup group 
        = new TransactionGroup( doc ) )
      {
        Room newRoom = null;
        RoomTag tag1 = null;

        group.Start( "Find Outermost Walls" );

        using( Transaction transaction 
          = new Transaction( doc ) )
        {
          transaction.Start( 
            "Create New Room Boundary Lines" );

          if( view == null )
          {
            view = doc.ActiveView;
          }

          SketchPlane sketchPlane = SketchPlane.Create( 
            doc, view.GenLevel.Id );

          ModelCurveArray modelCaRoomBoundaryLines 
            = doc.Create.NewRoomBoundaryLines( 
              sketchPlane, curves, view );

          //创建房间的坐标点

          XYZ point = new XYZ( minX + 600 / 304.8, 
            maxY - 600 / 304.8, 0 );

          //根据选中点，创建房间   当前视图的楼层doc.ActiveView.GenLevel

          newRoom = doc.Create.NewRoom( view.GenLevel, 
            new UV( point.X, point.Y ) );

          if( newRoom == null )
          {
            string msg = "创建房间失败。";
            TaskDialog.Show( "xx", msg );
            transaction.RollBack();
            return null;
          }
          tag1 = doc.Create.NewRoomTag( 
            new LinkElementId( newRoom.Id ), 
            new UV( point.X, point.Y ), 
            view.Id );

          transaction.Commit();
        }

        //获取房间的墙体 -- Get the room wall

        List<ElementId> elementIds 
          = DetermineAdjacentElementLengthsAndWallAreas( 
            doc, newRoom );

        group.RollBack(); // 撤销

        return elementIds;
      }
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Document doc = uidoc.Document;

      List<ElementId> elementIds = GetOutermostWalls( doc );

      uidoc.Selection.SetElementIds( elementIds );

      return Result.Succeeded;
    }
  }
}
