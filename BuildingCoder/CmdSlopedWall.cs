#region Header

//
// CmdSlopedWall.cs - create a sloped wall
//
// Copyright (C) 2009-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using View = Autodesk.Revit.DB.View;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdSlopedWall : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var doc = app.ActiveUIDocument.Document;

            //Autodesk.Revit.Creation.Application ac
            //  = app.Application.Create;

            //CurveArray profile = ac.NewCurveArray(); // 2012
            var profile = new List<Curve>(4); // 2012

            double length = 10;
            double heightStart = 5;
            double heightEnd = 8;

            var p = XYZ.Zero;
            var q = new XYZ(length, 0.0, 0.0);

            //profile.Append( ac.NewLineBound( p, q ) ); // 2012
            profile.Add(Line.CreateBound(p, q)); // 2014

            p = q;
            q += heightEnd * XYZ.BasisZ;

            //profile.Append( ac.NewLineBound( p, q ) ); // 2012
            profile.Add(Line.CreateBound(p, q)); // 2014

            p = q;
            q = new XYZ(0.0, 0.0, heightStart);

            //profile.Append( ac.NewLineBound( p, q ) ); // 2012
            //profile.Add( ac.NewLineBound( p, q ) ); // 2013
            profile.Add(Line.CreateBound(p, q)); // 2014

            p = q;
            q = XYZ.Zero;

            //profile.Append( ac.NewLineBound( p, q ) ); // 2012
            //profile.Add( ac.NewLineBound( p, q ) ); // 2013
            profile.Add(Line.CreateBound(p, q)); // 2014

            using var t = new Transaction(doc);
            t.Start("Create Sloped Wall");

            //Wall wall = doc.Create.NewWall( profile, false ); // 2012
            var wall = Wall.Create(doc, profile, false); // 2013

            t.Commit();

            return Result.Succeeded;
        }
    }

    #region TestWall

    /// <summary>
    ///     Answer to http://forums.autodesk.com/t5/Autodesk-Revit-API/Why-cannot-create-the-wall-by-following-profiles/m-p/3186912/highlight/false#M2351
    ///     1. Please look at the following two commands in The Building Coder samples: CmdCreateGableWall and CmdSlopedWall.
    ///     2. I actually went and tested your code, and I see that you are checking the verticality using an epsilon value of 1.e-5. I would suggest raising that to 1.e-9, which is more
    ///     like what Revit used internally. I did so, and then the IsVertical test fails on your three given points, so the message box saying "not vertical" is displayed. After that,
    ///     Revit displays an error message saying "Can't make Extrusion.я" At the same time, I temporarily see three model lines on the graphics screen. I have to select Cancel in the
    ///     Revit message box, though, and afterwards the three model lines disappear again.
    ///     So I would say the problem lies in your three points. They are not well enough aligned. You need higher precision.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class TestWall : IExternalCommand
    {
        public virtual Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var doc = app.ActiveUIDocument.Document;

            var ac
                = app.Application.Create;

            var transaction = new Transaction(doc);
            transaction.Start("TestWall");

            var pts = new[]
            {
                new(5.675844469, 8.769334161, -5.537348007),
                new XYZ(5.665137820, 8.771332255, 2.956630685),
                new XYZ(7.672569880, 8.396698044, 2.959412671)
            };

            //CurveArray profile = new CurveArray(); // 2012
            var profile = new List<Curve>(pts.Length); // 2013

            var q = pts[pts.Length - 1];

            foreach (var p in pts)
            {
                //profile.Append( CreateLine( ac, q, p, true ) ); // 2012
                //profile.Add( CreateLine( ac, q, p, true ) ); // 2013

                profile.Add(Line.CreateBound(q, p)); // 2014

                q = p;
            }

            var t1 = pts[0] - pts[1];
            var t2 = pts[1] - pts[2];
            var normal2 = t1.CrossProduct(t2);
            normal2 = normal2.Normalize();

            // Verify this plane is vertical to plane XOY

            if (!IsVertical(normal2, XYZ.BasisZ)) MessageBox.Show("not vertical");

            var sketchPlane = CreateSketchPlane(
                doc, normal2, pts[0]);

            //CreateModelCurveArray( // 2012
            //  doc, profile, sketchPlane );

            foreach (var c in profile) // 2013
                doc.Create.NewModelCurve(c, sketchPlane);

            var wallType
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(WallType))
                    .First() as WallType;

            var level
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .First(
                        e => e.Name.Equals("Level 1")) as Level;

            //Wall wall = doc.Create.NewWall( // 2012
            //  profile, wallType, level, true, normal2 );

            var wall = Wall.Create(doc,
                profile, wallType.Id, level.Id, true, normal2);

            transaction.Commit();

            return Result.Succeeded;
        }

        //Line CreateLine(
        //  Autodesk.Revit.Creation.Application ac,
        //  XYZ point,
        //  XYZ endOrDirection,
        //  bool bound )
        //{
        //  return ac.NewLine( point, endOrDirection, 
        //    bound ); // 2013
        //}

        private bool IsVertical(XYZ v1, XYZ v2)
        {
            return 1e-9 > Math.Abs(
                v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z);
        }

        private SketchPlane CreateSketchPlane(
            Document doc,
            XYZ normal,
            XYZ origin)
        {
            //Plane geometryPlane = doc.Application.Create.NewPlane( normal, origin ); // 2016
            var geometryPlane = Plane.CreateByNormalAndOrigin(
                normal, origin); // 2017

            //return doc.Create.NewSketchPlane( geometryPlane ); // 2013

            return SketchPlane.Create(doc, geometryPlane); // 2014
        }

        private ModelCurveArray CreateModelCurveArray(
            Document doc,
            CurveArray geometryCurveArray,
            SketchPlane sketchPlane)
        {
            return doc.Create.NewModelCurveArray(
                geometryCurveArray, sketchPlane);
        }

        private DetailCurveArray CreateDetailCurveArray(
            View view,
            CurveArray geometryCurveArray)
        {
            var doc = view.Document;
            return doc.Create.NewDetailCurveArray(
                view, geometryCurveArray);
        }

        private CurveArray ConvertLoopToArray(CurveLoop loop)
        {
            var a = new CurveArray();
            foreach (var c in loop) a.Append(c);
            return a;
        }
    }

    #endregion // TestWall
}