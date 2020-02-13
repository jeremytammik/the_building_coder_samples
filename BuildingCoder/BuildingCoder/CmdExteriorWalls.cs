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
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System.Diagnostics;
using System.Linq;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Manual )]
  class CmdExteriorWalls : IExternalCommand
  {
    /// <summary>
    /// Return a bounding box around all the 
    /// walls in the entire model; for just a
    /// building, or several buildings, this is 
    /// obviously equal to the model extents.
    /// </summary>
    static BoundingBoxXYZ GetBoundingBoxAroundAllWalls( 
      Document doc,
      View view = null )
    {
      // Default constructor creates cube from -100 to 100;
      // maybe too big, but who cares?

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
    /// 过滤出需要的墙体 --
    /// Return all walls that are generating boundary
    /// segments for the given room. Includes debug
    /// code to compare wall lengths and wall areas.
    /// </summary>
    static List<ElementId> 
      RetrieveWallsGeneratingRoomBoundaries(
        Document doc,
        Room room )
    {
      List<ElementId> ids = new List<ElementId>();

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

          // Retrieve the id of the element that 
          // produces this boundary segment

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

            ids.Add( wall.Id );
          }
        }
      }
      return ids;
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
      double offset = Util.MmToFoot( 1000 );

      if( view == null )
      {
        view = doc.ActiveView;
      }

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

      BoundingBoxXYZ bb = GetBoundingBoxAroundAllWalls( 
        doc, view );

      XYZ voffset = offset * ( XYZ.BasisX + XYZ.BasisY );
      bb.Min -= voffset;
      bb.Max += voffset;

      XYZ[] bottom_corners = Util.GetBottomCorners( 
        bb, 0 );

      CurveArray curves = new CurveArray();
      for( int i = 0; i < 4; ++i )
      {
        int j = i < 3 ? i + 1 : 0;
        curves.Append( Line.CreateBound( 
          bottom_corners[i], bottom_corners[j] ) );
      }

      using( TransactionGroup group 
        = new TransactionGroup( doc ) )
      {
        Room newRoom = null;

        group.Start( "Find Outermost Walls" );

        using( Transaction transaction 
          = new Transaction( doc ) )
        {
          transaction.Start( 
            "Create New Room Boundary Lines" );

          SketchPlane sketchPlane = SketchPlane.Create( 
            doc, view.GenLevel.Id );

          ModelCurveArray modelCaRoomBoundaryLines 
            = doc.Create.NewRoomBoundaryLines( 
              sketchPlane, curves, view );

          // 创建房间的坐标点 -- Create room coordinates

          double d = Util.MmToFoot( 600 );
          UV point = new UV( bb.Min.X + d, bb.Min.Y + d );

          // 根据选中点，创建房间 当前视图的楼层 doc.ActiveView.GenLevel
          // Create room at selected point on the current view level

          newRoom = doc.Create.NewRoom( view.GenLevel, point );

          if( newRoom == null )
          {
            string msg = "创建房间失败。";
            TaskDialog.Show( "xx", msg );
            transaction.RollBack();
            return null;
          }

          RoomTag tag = doc.Create.NewRoomTag( 
            new LinkElementId( newRoom.Id ), 
            point, view.Id );

          transaction.Commit();
        }

        //获取房间的墙体 -- Get the room walls

        List<ElementId> ids 
          = RetrieveWallsGeneratingRoomBoundaries( 
            doc, newRoom );

        group.RollBack(); // 撤销

        return ids;
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

      List<ElementId> ids = GetOutermostWalls( doc );

      uidoc.Selection.SetElementIds( ids );

      return Result.Succeeded;
    }

    /// <summary>
    /// Convert a newline-separated string of integers
    /// to a list of ElementId instances suitable for
    /// passing into SetElementIds.
    /// </summary>
    List<ElementId> GetElementIdsFromString( string x )
    {
      return new List<ElementId>( x.Split( '\n' )
        .Select<string, ElementId>( s 
          => new ElementId( int.Parse( s ) ) ) );
    }
  }
}
