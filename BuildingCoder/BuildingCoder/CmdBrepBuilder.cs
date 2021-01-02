#region Header
//
// CmdBrepBuilder.cs - create DirectShape using BrepBuilder and Boolean difference
//
// Copyright (C) 2018-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
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
  [Transaction( TransactionMode.Manual )]
  class CmdBrepBuilder : IExternalCommand
  {
    /// <summary>
    /// Create a cube 100 x 100 x 100, from 
    /// (0,0,0) to (100, 100, 100).
    /// </summary>
    public BRepBuilder CreateBrepSolid()
    {
      BRepBuilder b = new BRepBuilder( BRepType.Solid );

      // 1. Planes.
      // naming convention for faces and planes:
      // We are looking at this cube in an isometric view. 
      // X is down and to the left of us, Y is horizontal 
      // and points to the right, Z is up.
      // front and back faces are along the X axis, left 
      // and right are along the Y axis, top and bottom 
      // are along the Z axis.
      Plane bottom = Plane.CreateByOriginAndBasis( new XYZ( 50, 50, 0 ), new XYZ( 1, 0, 0 ), new XYZ( 0, 1, 0 ) ); // bottom. XY plane, Z = 0, normal pointing inside the cube.
      Plane top = Plane.CreateByOriginAndBasis( new XYZ( 50, 50, 100 ), new XYZ( 1, 0, 0 ), new XYZ( 0, 1, 0 ) ); // top. XY plane, Z = 100, normal pointing outside the cube.
      Plane front = Plane.CreateByOriginAndBasis( new XYZ( 100, 50, 50 ), new XYZ( 0, 0, 1 ), new XYZ( 0, 1, 0 ) ); // front side. ZY plane, X = 0, normal pointing inside the cube.
      Plane back = Plane.CreateByOriginAndBasis( new XYZ( 0, 50, 50 ), new XYZ( 0, 0, 1 ), new XYZ( 0, 1, 0 ) ); // back side. ZY plane, X = 0, normal pointing outside the cube.
      Plane left = Plane.CreateByOriginAndBasis( new XYZ( 50, 0, 50 ), new XYZ( 0, 0, 1 ), new XYZ( 1, 0, 0 ) ); // left side. ZX plane, Y = 0, normal pointing inside the cube
      Plane right = Plane.CreateByOriginAndBasis( new XYZ( 50, 100, 50 ), new XYZ( 0, 0, 1 ), new XYZ( 1, 0, 0 ) ); // right side. ZX plane, Y = 100, normal pointing outside the cube

      // 2. Faces.
      BRepBuilderGeometryId faceId_Bottom = b.AddFace( BRepBuilderSurfaceGeometry.Create( bottom, null ), true );
      BRepBuilderGeometryId faceId_Top = b.AddFace( BRepBuilderSurfaceGeometry.Create( top, null ), false );
      BRepBuilderGeometryId faceId_Front = b.AddFace( BRepBuilderSurfaceGeometry.Create( front, null ), true );
      BRepBuilderGeometryId faceId_Back = b.AddFace( BRepBuilderSurfaceGeometry.Create( back, null ), false );
      BRepBuilderGeometryId faceId_Left = b.AddFace( BRepBuilderSurfaceGeometry.Create( left, null ), true );
      BRepBuilderGeometryId faceId_Right = b.AddFace( BRepBuilderSurfaceGeometry.Create( right, null ), false );

      // 3. Edges.

      // 3.a (define edge geometry)
      // walk around bottom face
      BRepBuilderEdgeGeometry edgeBottomFront = BRepBuilderEdgeGeometry.Create( new XYZ( 100, 0, 0 ), new XYZ( 100, 100, 0 ) );
      BRepBuilderEdgeGeometry edgeBottomRight = BRepBuilderEdgeGeometry.Create( new XYZ( 100, 100, 0 ), new XYZ( 0, 100, 0 ) );
      BRepBuilderEdgeGeometry edgeBottomBack = BRepBuilderEdgeGeometry.Create( new XYZ( 0, 100, 0 ), new XYZ( 0, 0, 0 ) );
      BRepBuilderEdgeGeometry edgeBottomLeft = BRepBuilderEdgeGeometry.Create( new XYZ( 0, 0, 0 ), new XYZ( 100, 0, 0 ) );

      // now walk around top face
      BRepBuilderEdgeGeometry edgeTopFront = BRepBuilderEdgeGeometry.Create( new XYZ( 100, 0, 100 ), new XYZ( 100, 100, 100 ) );
      BRepBuilderEdgeGeometry edgeTopRight = BRepBuilderEdgeGeometry.Create( new XYZ( 100, 100, 100 ), new XYZ( 0, 100, 100 ) );
      BRepBuilderEdgeGeometry edgeTopBack = BRepBuilderEdgeGeometry.Create( new XYZ( 0, 100, 100 ), new XYZ( 0, 0, 100 ) );
      BRepBuilderEdgeGeometry edgeTopLeft = BRepBuilderEdgeGeometry.Create( new XYZ( 0, 0, 100 ), new XYZ( 100, 0, 100 ) );

      // sides
      BRepBuilderEdgeGeometry edgeFrontRight = BRepBuilderEdgeGeometry.Create( new XYZ( 100, 100, 0 ), new XYZ( 100, 100, 100 ) );
      BRepBuilderEdgeGeometry edgeRightBack = BRepBuilderEdgeGeometry.Create( new XYZ( 0, 100, 0 ), new XYZ( 0, 100, 100 ) );
      BRepBuilderEdgeGeometry edgeBackLeft = BRepBuilderEdgeGeometry.Create( new XYZ( 0, 0, 0 ), new XYZ( 0, 0, 100 ) );
      BRepBuilderEdgeGeometry edgeLeftFront = BRepBuilderEdgeGeometry.Create( new XYZ( 100, 0, 0 ), new XYZ( 100, 0, 100 ) );

      // 3.b (define the edges themselves)
      BRepBuilderGeometryId edgeId_BottomFront = b.AddEdge( edgeBottomFront );
      BRepBuilderGeometryId edgeId_BottomRight = b.AddEdge( edgeBottomRight );
      BRepBuilderGeometryId edgeId_BottomBack = b.AddEdge( edgeBottomBack );
      BRepBuilderGeometryId edgeId_BottomLeft = b.AddEdge( edgeBottomLeft );
      BRepBuilderGeometryId edgeId_TopFront = b.AddEdge( edgeTopFront );
      BRepBuilderGeometryId edgeId_TopRight = b.AddEdge( edgeTopRight );
      BRepBuilderGeometryId edgeId_TopBack = b.AddEdge( edgeTopBack );
      BRepBuilderGeometryId edgeId_TopLeft = b.AddEdge( edgeTopLeft );
      BRepBuilderGeometryId edgeId_FrontRight = b.AddEdge( edgeFrontRight );
      BRepBuilderGeometryId edgeId_RightBack = b.AddEdge( edgeRightBack );
      BRepBuilderGeometryId edgeId_BackLeft = b.AddEdge( edgeBackLeft );
      BRepBuilderGeometryId edgeId_LeftFront = b.AddEdge( edgeLeftFront );

      // 4. Loops.
      BRepBuilderGeometryId loopId_Bottom = b.AddLoop( faceId_Bottom );
      BRepBuilderGeometryId loopId_Top = b.AddLoop( faceId_Top );
      BRepBuilderGeometryId loopId_Front = b.AddLoop( faceId_Front );
      BRepBuilderGeometryId loopId_Back = b.AddLoop( faceId_Back );
      BRepBuilderGeometryId loopId_Right = b.AddLoop( faceId_Right );
      BRepBuilderGeometryId loopId_Left = b.AddLoop( faceId_Left );

      // 5. Co-edges. 
      // Bottom face. All edges reversed
      b.AddCoEdge( loopId_Bottom, edgeId_BottomFront, true ); // other direction in front loop
      b.AddCoEdge( loopId_Bottom, edgeId_BottomLeft, true );  // other direction in left loop
      b.AddCoEdge( loopId_Bottom, edgeId_BottomBack, true );  // other direction in back loop
      b.AddCoEdge( loopId_Bottom, edgeId_BottomRight, true ); // other direction in right loop
      b.FinishLoop( loopId_Bottom );
      b.FinishFace( faceId_Bottom );

      // Top face. All edges NOT reversed.
      b.AddCoEdge( loopId_Top, edgeId_TopFront, false );  // other direction in front loop.
      b.AddCoEdge( loopId_Top, edgeId_TopRight, false );  // other direction in right loop
      b.AddCoEdge( loopId_Top, edgeId_TopBack, false );   // other direction in back loop
      b.AddCoEdge( loopId_Top, edgeId_TopLeft, false );   // other direction in left loop
      b.FinishLoop( loopId_Top );
      b.FinishFace( faceId_Top );

      // Front face.
      b.AddCoEdge( loopId_Front, edgeId_BottomFront, false ); // other direction in bottom loop
      b.AddCoEdge( loopId_Front, edgeId_FrontRight, false );  // other direction in right loop
      b.AddCoEdge( loopId_Front, edgeId_TopFront, true ); // other direction in top loop.
      b.AddCoEdge( loopId_Front, edgeId_LeftFront, true ); // other direction in left loop.
      b.FinishLoop( loopId_Front );
      b.FinishFace( faceId_Front );

      // Back face
      b.AddCoEdge( loopId_Back, edgeId_BottomBack, false ); // other direction in bottom loop
      b.AddCoEdge( loopId_Back, edgeId_BackLeft, false );   // other direction in left loop.
      b.AddCoEdge( loopId_Back, edgeId_TopBack, true ); // other direction in top loop
      b.AddCoEdge( loopId_Back, edgeId_RightBack, true ); // other direction in right loop.
      b.FinishLoop( loopId_Back );
      b.FinishFace( faceId_Back );

      // Right face
      b.AddCoEdge( loopId_Right, edgeId_BottomRight, false ); // other direction in bottom loop
      b.AddCoEdge( loopId_Right, edgeId_RightBack, false );  // other direction in back loop
      b.AddCoEdge( loopId_Right, edgeId_TopRight, true );   // other direction in top loop
      b.AddCoEdge( loopId_Right, edgeId_FrontRight, true ); // other direction in front loop
      b.FinishLoop( loopId_Right );
      b.FinishFace( faceId_Right );

      // Left face
      b.AddCoEdge( loopId_Left, edgeId_BottomLeft, false ); // other direction in bottom loop
      b.AddCoEdge( loopId_Left, edgeId_LeftFront, false ); // other direction in front loop
      b.AddCoEdge( loopId_Left, edgeId_TopLeft, true );   // other direction in top loop
      b.AddCoEdge( loopId_Left, edgeId_BackLeft, true );  // other direction in back loop
      b.FinishLoop( loopId_Left );
      b.FinishFace( faceId_Left );

      b.Finish();

      return b;
    }

    /// <summary>
    /// Create a cylinder to subtract from the cube.
    /// </summary>
    public BRepBuilder CreateBrepVoid()
    {
      // Naming convention for faces and edges: we 
      // assume that x is to the left and pointing down, 
      // y is horizontal and pointing to the right, 
      // z is up.

      BRepBuilder b = new BRepBuilder( BRepType.Solid );

      // The surfaces of the four faces.
      Frame basis = new Frame( new XYZ( 50, 0, 0 ), new XYZ( 0, 1, 0 ), new XYZ( -1, 0, 0 ), new XYZ( 0, 0, 1 ) );
      CylindricalSurface cylSurf = CylindricalSurface.Create( basis, 40 );
      Plane top1 = Plane.CreateByNormalAndOrigin( new XYZ( 0, 0, 1 ), new XYZ( 0, 0, 100 ) );  // normal points outside the cylinder
      Plane bottom1 = Plane.CreateByNormalAndOrigin( new XYZ( 0, 0, 1 ), new XYZ( 0, 0, 0 ) ); // normal points inside the cylinder

      // Add the four faces
      BRepBuilderGeometryId frontCylFaceId = b.AddFace( BRepBuilderSurfaceGeometry.Create( cylSurf, null ), false );
      BRepBuilderGeometryId backCylFaceId = b.AddFace( BRepBuilderSurfaceGeometry.Create( cylSurf, null ), false );
      BRepBuilderGeometryId topFaceId = b.AddFace( BRepBuilderSurfaceGeometry.Create( top1, null ), false );
      BRepBuilderGeometryId bottomFaceId = b.AddFace( BRepBuilderSurfaceGeometry.Create( bottom1, null ), true );

      // Geometry for the four semi-circular edges and two vertical linear edges
      BRepBuilderEdgeGeometry frontEdgeBottom = BRepBuilderEdgeGeometry.Create( Arc.Create( new XYZ( 10, 0, 0 ), new XYZ( 90, 0, 0 ), new XYZ( 50, 40, 0 ) ) );
      BRepBuilderEdgeGeometry backEdgeBottom = BRepBuilderEdgeGeometry.Create( Arc.Create( new XYZ( 90, 0, 0 ), new XYZ( 10, 0, 0 ), new XYZ( 50, -40, 0 ) ) );

      BRepBuilderEdgeGeometry frontEdgeTop = BRepBuilderEdgeGeometry.Create( Arc.Create( new XYZ( 10, 0, 100 ), new XYZ( 90, 0, 100 ), new XYZ( 50, 40, 100 ) ) );
      BRepBuilderEdgeGeometry backEdgeTop = BRepBuilderEdgeGeometry.Create( Arc.Create( new XYZ( 10, 0, 100 ), new XYZ( 90, 0, 100 ), new XYZ( 50, -40, 100 ) ) );

      BRepBuilderEdgeGeometry linearEdgeFront = BRepBuilderEdgeGeometry.Create( new XYZ( 90, 0, 0 ), new XYZ( 90, 0, 100 ) );
      BRepBuilderEdgeGeometry linearEdgeBack = BRepBuilderEdgeGeometry.Create( new XYZ( 10, 0, 0 ), new XYZ( 10, 0, 100 ) );

      // Add the six edges
      BRepBuilderGeometryId frontEdgeBottomId = b.AddEdge( frontEdgeBottom );
      BRepBuilderGeometryId frontEdgeTopId = b.AddEdge( frontEdgeTop );
      BRepBuilderGeometryId linearEdgeFrontId = b.AddEdge( linearEdgeFront );
      BRepBuilderGeometryId linearEdgeBackId = b.AddEdge( linearEdgeBack );
      BRepBuilderGeometryId backEdgeBottomId = b.AddEdge( backEdgeBottom );
      BRepBuilderGeometryId backEdgeTopId = b.AddEdge( backEdgeTop );

      // Loops of the four faces
      BRepBuilderGeometryId loopId_Top = b.AddLoop( topFaceId );
      BRepBuilderGeometryId loopId_Bottom = b.AddLoop( bottomFaceId );
      BRepBuilderGeometryId loopId_Front = b.AddLoop( frontCylFaceId );
      BRepBuilderGeometryId loopId_Back = b.AddLoop( backCylFaceId );

      // Add coedges for the loop of the front face
      b.AddCoEdge( loopId_Front, linearEdgeBackId, false );
      b.AddCoEdge( loopId_Front, frontEdgeTopId, false );
      b.AddCoEdge( loopId_Front, linearEdgeFrontId, true );
      b.AddCoEdge( loopId_Front, frontEdgeBottomId, true );
      b.FinishLoop( loopId_Front );
      b.FinishFace( frontCylFaceId );

      // Add coedges for the loop of the back face
      b.AddCoEdge( loopId_Back, linearEdgeBackId, true );
      b.AddCoEdge( loopId_Back, backEdgeBottomId, true );
      b.AddCoEdge( loopId_Back, linearEdgeFrontId, false );
      b.AddCoEdge( loopId_Back, backEdgeTopId, true );
      b.FinishLoop( loopId_Back );
      b.FinishFace( backCylFaceId );

      // Add coedges for the loop of the top face
      b.AddCoEdge( loopId_Top, backEdgeTopId, false );
      b.AddCoEdge( loopId_Top, frontEdgeTopId, true );
      b.FinishLoop( loopId_Top );
      b.FinishFace( topFaceId );

      // Add coedges for the loop of the bottom face
      b.AddCoEdge( loopId_Bottom, frontEdgeBottomId, false );
      b.AddCoEdge( loopId_Bottom, backEdgeBottomId, false );
      b.FinishLoop( loopId_Bottom );
      b.FinishFace( bottomFaceId );

      b.Finish();

      return b;
    }

    public Result Execute( 
      ExternalCommandData commandData,
      ref string message, 
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      UIDocument uidoc = app.ActiveUIDocument;
      Document doc = uidoc.Document;

      // Execute the BrepBuilder methods.

      BRepBuilder brepBuilderSolid = CreateBrepSolid();
      BRepBuilder brepBuilderVoid = CreateBrepVoid();

      Solid cube = brepBuilderSolid.GetResult();
      Solid cylinder = brepBuilderVoid.GetResult();

      // Determine their Boolean difference.

      Solid difference 
        = BooleanOperationsUtils.ExecuteBooleanOperation( 
          cube, cylinder, BooleanOperationsType.Difference );

      IList<GeometryObject> list = new List<GeometryObject>();
      list.Add( difference );

      using( Transaction tr = new Transaction( doc ) )
      {
        tr.Start( "Create a DirectShape" );

        // Create a direct shape.

        DirectShape ds = DirectShape.CreateElement( doc, 
          new ElementId( BuiltInCategory.OST_GenericModel ) );

        ds.SetShape( list );

        tr.Commit();
      }
      return Result.Succeeded;
    }
  }
}
