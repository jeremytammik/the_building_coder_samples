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
    [Transaction(TransactionMode.Manual)]
    internal class CmdBrepBuilder : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;
            var doc = uidoc.Document;

            // Execute the BrepBuilder methods.

            var brepBuilderSolid = CreateBrepSolid();
            var brepBuilderVoid = CreateBrepVoid();

            var cube = brepBuilderSolid.GetResult();
            var cylinder = brepBuilderVoid.GetResult();

            // Determine their Boolean difference.

            var difference
                = BooleanOperationsUtils.ExecuteBooleanOperation(
                    cube, cylinder, BooleanOperationsType.Difference);

            IList<GeometryObject> list = new List<GeometryObject>();
            list.Add(difference);

            using var tr = new Transaction(doc);
            tr.Start("Create a DirectShape");

            // Create a direct shape.

            var ds = DirectShape.CreateElement(doc,
                new ElementId(BuiltInCategory.OST_GenericModel));

            ds.SetShape(list);

            tr.Commit();

            return Result.Succeeded;
        }

        /// <summary>
        ///     Create a cube 100 x 100 x 100, from
        ///     (0,0,0) to (100, 100, 100).
        /// </summary>
        public BRepBuilder CreateBrepSolid()
        {
            var b = new BRepBuilder(BRepType.Solid);

            // 1. Planes.
            // naming convention for faces and planes:
            // We are looking at this cube in an isometric view. 
            // X is down and to the left of us, Y is horizontal 
            // and points to the right, Z is up.
            // front and back faces are along the X axis, left 
            // and right are along the Y axis, top and bottom 
            // are along the Z axis.
            var bottom = Plane.CreateByOriginAndBasis(new XYZ(50, 50, 0), new XYZ(1, 0, 0), new XYZ(0, 1, 0)); // bottom. XY plane, Z = 0, normal pointing inside the cube.
            var top = Plane.CreateByOriginAndBasis(new XYZ(50, 50, 100), new XYZ(1, 0, 0), new XYZ(0, 1, 0)); // top. XY plane, Z = 100, normal pointing outside the cube.
            var front = Plane.CreateByOriginAndBasis(new XYZ(100, 50, 50), new XYZ(0, 0, 1), new XYZ(0, 1, 0)); // front side. ZY plane, X = 0, normal pointing inside the cube.
            var back = Plane.CreateByOriginAndBasis(new XYZ(0, 50, 50), new XYZ(0, 0, 1), new XYZ(0, 1, 0)); // back side. ZY plane, X = 0, normal pointing outside the cube.
            var left = Plane.CreateByOriginAndBasis(new XYZ(50, 0, 50), new XYZ(0, 0, 1), new XYZ(1, 0, 0)); // left side. ZX plane, Y = 0, normal pointing inside the cube
            var right = Plane.CreateByOriginAndBasis(new XYZ(50, 100, 50), new XYZ(0, 0, 1), new XYZ(1, 0, 0)); // right side. ZX plane, Y = 100, normal pointing outside the cube

            // 2. Faces.
            var faceId_Bottom = b.AddFace(BRepBuilderSurfaceGeometry.Create(bottom, null), true);
            var faceId_Top = b.AddFace(BRepBuilderSurfaceGeometry.Create(top, null), false);
            var faceId_Front = b.AddFace(BRepBuilderSurfaceGeometry.Create(front, null), true);
            var faceId_Back = b.AddFace(BRepBuilderSurfaceGeometry.Create(back, null), false);
            var faceId_Left = b.AddFace(BRepBuilderSurfaceGeometry.Create(left, null), true);
            var faceId_Right = b.AddFace(BRepBuilderSurfaceGeometry.Create(right, null), false);

            // 3. Edges.

            // 3.a (define edge geometry)
            // walk around bottom face
            var edgeBottomFront = BRepBuilderEdgeGeometry.Create(new XYZ(100, 0, 0), new XYZ(100, 100, 0));
            var edgeBottomRight = BRepBuilderEdgeGeometry.Create(new XYZ(100, 100, 0), new XYZ(0, 100, 0));
            var edgeBottomBack = BRepBuilderEdgeGeometry.Create(new XYZ(0, 100, 0), new XYZ(0, 0, 0));
            var edgeBottomLeft = BRepBuilderEdgeGeometry.Create(new XYZ(0, 0, 0), new XYZ(100, 0, 0));

            // now walk around top face
            var edgeTopFront = BRepBuilderEdgeGeometry.Create(new XYZ(100, 0, 100), new XYZ(100, 100, 100));
            var edgeTopRight = BRepBuilderEdgeGeometry.Create(new XYZ(100, 100, 100), new XYZ(0, 100, 100));
            var edgeTopBack = BRepBuilderEdgeGeometry.Create(new XYZ(0, 100, 100), new XYZ(0, 0, 100));
            var edgeTopLeft = BRepBuilderEdgeGeometry.Create(new XYZ(0, 0, 100), new XYZ(100, 0, 100));

            // sides
            var edgeFrontRight = BRepBuilderEdgeGeometry.Create(new XYZ(100, 100, 0), new XYZ(100, 100, 100));
            var edgeRightBack = BRepBuilderEdgeGeometry.Create(new XYZ(0, 100, 0), new XYZ(0, 100, 100));
            var edgeBackLeft = BRepBuilderEdgeGeometry.Create(new XYZ(0, 0, 0), new XYZ(0, 0, 100));
            var edgeLeftFront = BRepBuilderEdgeGeometry.Create(new XYZ(100, 0, 0), new XYZ(100, 0, 100));

            // 3.b (define the edges themselves)
            var edgeId_BottomFront = b.AddEdge(edgeBottomFront);
            var edgeId_BottomRight = b.AddEdge(edgeBottomRight);
            var edgeId_BottomBack = b.AddEdge(edgeBottomBack);
            var edgeId_BottomLeft = b.AddEdge(edgeBottomLeft);
            var edgeId_TopFront = b.AddEdge(edgeTopFront);
            var edgeId_TopRight = b.AddEdge(edgeTopRight);
            var edgeId_TopBack = b.AddEdge(edgeTopBack);
            var edgeId_TopLeft = b.AddEdge(edgeTopLeft);
            var edgeId_FrontRight = b.AddEdge(edgeFrontRight);
            var edgeId_RightBack = b.AddEdge(edgeRightBack);
            var edgeId_BackLeft = b.AddEdge(edgeBackLeft);
            var edgeId_LeftFront = b.AddEdge(edgeLeftFront);

            // 4. Loops.
            var loopId_Bottom = b.AddLoop(faceId_Bottom);
            var loopId_Top = b.AddLoop(faceId_Top);
            var loopId_Front = b.AddLoop(faceId_Front);
            var loopId_Back = b.AddLoop(faceId_Back);
            var loopId_Right = b.AddLoop(faceId_Right);
            var loopId_Left = b.AddLoop(faceId_Left);

            // 5. Co-edges. 
            // Bottom face. All edges reversed
            b.AddCoEdge(loopId_Bottom, edgeId_BottomFront, true); // other direction in front loop
            b.AddCoEdge(loopId_Bottom, edgeId_BottomLeft, true); // other direction in left loop
            b.AddCoEdge(loopId_Bottom, edgeId_BottomBack, true); // other direction in back loop
            b.AddCoEdge(loopId_Bottom, edgeId_BottomRight, true); // other direction in right loop
            b.FinishLoop(loopId_Bottom);
            b.FinishFace(faceId_Bottom);

            // Top face. All edges NOT reversed.
            b.AddCoEdge(loopId_Top, edgeId_TopFront, false); // other direction in front loop.
            b.AddCoEdge(loopId_Top, edgeId_TopRight, false); // other direction in right loop
            b.AddCoEdge(loopId_Top, edgeId_TopBack, false); // other direction in back loop
            b.AddCoEdge(loopId_Top, edgeId_TopLeft, false); // other direction in left loop
            b.FinishLoop(loopId_Top);
            b.FinishFace(faceId_Top);

            // Front face.
            b.AddCoEdge(loopId_Front, edgeId_BottomFront, false); // other direction in bottom loop
            b.AddCoEdge(loopId_Front, edgeId_FrontRight, false); // other direction in right loop
            b.AddCoEdge(loopId_Front, edgeId_TopFront, true); // other direction in top loop.
            b.AddCoEdge(loopId_Front, edgeId_LeftFront, true); // other direction in left loop.
            b.FinishLoop(loopId_Front);
            b.FinishFace(faceId_Front);

            // Back face
            b.AddCoEdge(loopId_Back, edgeId_BottomBack, false); // other direction in bottom loop
            b.AddCoEdge(loopId_Back, edgeId_BackLeft, false); // other direction in left loop.
            b.AddCoEdge(loopId_Back, edgeId_TopBack, true); // other direction in top loop
            b.AddCoEdge(loopId_Back, edgeId_RightBack, true); // other direction in right loop.
            b.FinishLoop(loopId_Back);
            b.FinishFace(faceId_Back);

            // Right face
            b.AddCoEdge(loopId_Right, edgeId_BottomRight, false); // other direction in bottom loop
            b.AddCoEdge(loopId_Right, edgeId_RightBack, false); // other direction in back loop
            b.AddCoEdge(loopId_Right, edgeId_TopRight, true); // other direction in top loop
            b.AddCoEdge(loopId_Right, edgeId_FrontRight, true); // other direction in front loop
            b.FinishLoop(loopId_Right);
            b.FinishFace(faceId_Right);

            // Left face
            b.AddCoEdge(loopId_Left, edgeId_BottomLeft, false); // other direction in bottom loop
            b.AddCoEdge(loopId_Left, edgeId_LeftFront, false); // other direction in front loop
            b.AddCoEdge(loopId_Left, edgeId_TopLeft, true); // other direction in top loop
            b.AddCoEdge(loopId_Left, edgeId_BackLeft, true); // other direction in back loop
            b.FinishLoop(loopId_Left);
            b.FinishFace(faceId_Left);

            b.Finish();

            return b;
        }

        /// <summary>
        ///     Create a cylinder to subtract from the cube.
        /// </summary>
        public BRepBuilder CreateBrepVoid()
        {
            // Naming convention for faces and edges: we 
            // assume that x is to the left and pointing down, 
            // y is horizontal and pointing to the right, 
            // z is up.

            var b = new BRepBuilder(BRepType.Solid);

            // The surfaces of the four faces.
            var basis = new Frame(new XYZ(50, 0, 0), new XYZ(0, 1, 0), new XYZ(-1, 0, 0), new XYZ(0, 0, 1));
            var cylSurf = CylindricalSurface.Create(basis, 40);
            var top1 = Plane.CreateByNormalAndOrigin(new XYZ(0, 0, 1), new XYZ(0, 0, 100)); // normal points outside the cylinder
            var bottom1 = Plane.CreateByNormalAndOrigin(new XYZ(0, 0, 1), new XYZ(0, 0, 0)); // normal points inside the cylinder

            // Add the four faces
            var frontCylFaceId = b.AddFace(BRepBuilderSurfaceGeometry.Create(cylSurf, null), false);
            var backCylFaceId = b.AddFace(BRepBuilderSurfaceGeometry.Create(cylSurf, null), false);
            var topFaceId = b.AddFace(BRepBuilderSurfaceGeometry.Create(top1, null), false);
            var bottomFaceId = b.AddFace(BRepBuilderSurfaceGeometry.Create(bottom1, null), true);

            // Geometry for the four semi-circular edges and two vertical linear edges
            var frontEdgeBottom = BRepBuilderEdgeGeometry.Create(Arc.Create(new XYZ(10, 0, 0), new XYZ(90, 0, 0), new XYZ(50, 40, 0)));
            var backEdgeBottom = BRepBuilderEdgeGeometry.Create(Arc.Create(new XYZ(90, 0, 0), new XYZ(10, 0, 0), new XYZ(50, -40, 0)));

            var frontEdgeTop = BRepBuilderEdgeGeometry.Create(Arc.Create(new XYZ(10, 0, 100), new XYZ(90, 0, 100), new XYZ(50, 40, 100)));
            var backEdgeTop = BRepBuilderEdgeGeometry.Create(Arc.Create(new XYZ(10, 0, 100), new XYZ(90, 0, 100), new XYZ(50, -40, 100)));

            var linearEdgeFront = BRepBuilderEdgeGeometry.Create(new XYZ(90, 0, 0), new XYZ(90, 0, 100));
            var linearEdgeBack = BRepBuilderEdgeGeometry.Create(new XYZ(10, 0, 0), new XYZ(10, 0, 100));

            // Add the six edges
            var frontEdgeBottomId = b.AddEdge(frontEdgeBottom);
            var frontEdgeTopId = b.AddEdge(frontEdgeTop);
            var linearEdgeFrontId = b.AddEdge(linearEdgeFront);
            var linearEdgeBackId = b.AddEdge(linearEdgeBack);
            var backEdgeBottomId = b.AddEdge(backEdgeBottom);
            var backEdgeTopId = b.AddEdge(backEdgeTop);

            // Loops of the four faces
            var loopId_Top = b.AddLoop(topFaceId);
            var loopId_Bottom = b.AddLoop(bottomFaceId);
            var loopId_Front = b.AddLoop(frontCylFaceId);
            var loopId_Back = b.AddLoop(backCylFaceId);

            // Add coedges for the loop of the front face
            b.AddCoEdge(loopId_Front, linearEdgeBackId, false);
            b.AddCoEdge(loopId_Front, frontEdgeTopId, false);
            b.AddCoEdge(loopId_Front, linearEdgeFrontId, true);
            b.AddCoEdge(loopId_Front, frontEdgeBottomId, true);
            b.FinishLoop(loopId_Front);
            b.FinishFace(frontCylFaceId);

            // Add coedges for the loop of the back face
            b.AddCoEdge(loopId_Back, linearEdgeBackId, true);
            b.AddCoEdge(loopId_Back, backEdgeBottomId, true);
            b.AddCoEdge(loopId_Back, linearEdgeFrontId, false);
            b.AddCoEdge(loopId_Back, backEdgeTopId, true);
            b.FinishLoop(loopId_Back);
            b.FinishFace(backCylFaceId);

            // Add coedges for the loop of the top face
            b.AddCoEdge(loopId_Top, backEdgeTopId, false);
            b.AddCoEdge(loopId_Top, frontEdgeTopId, true);
            b.FinishLoop(loopId_Top);
            b.FinishFace(topFaceId);

            // Add coedges for the loop of the bottom face
            b.AddCoEdge(loopId_Bottom, frontEdgeBottomId, false);
            b.AddCoEdge(loopId_Bottom, backEdgeBottomId, false);
            b.FinishLoop(loopId_Bottom);
            b.FinishFace(bottomFaceId);

            b.Finish();

            return b;
        }
    }
}