#region Header

//
// CmdNewSpotElevation.cs - insert a new spot elevation on top surface of beam
//
// Copyright (C) 2010-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.Creation;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Document = Autodesk.Revit.DB.Document;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdNewSpotElevation : IExternalCommand
    {
        #region Language Independent View Type Id

#if NOT_NEEDED
    void f()
    {
      NyViewSectionB = ViewSection.CreateSection(
        doc.Document, SectionId, bbNewSectionF );
    }

    public RevitElement GetElementByName<RevitElement>(
      string name ) where RevitElement : Element
    {
      if( string.IsNullOrEmpty( name ) )
        return null;

      FilteredElementCollector fec
        = new FilteredElementCollector( Doc() );

      fec.OfClass( typeof( RevitElement ) );

      IList<Element> elements = fec.ToElements();

      Element result = null;

      result = GetElementByName( elements, name );

      return result as RevitElement;
    }

    private Element GetElementByName(
      IList<Element> elements,
      string name )
    {
      foreach( Element e in elements )
      {
        System.Diagnostics.Debug.WriteLine( e.Name );
      }

      Element result = null;

      try
      {
        result = elements.First(
          element => element.Name == name );
      }
      catch( Exception )
      {
        return null;
      }
      return result;
    }

    void g()
    {
      ElementId SectionId = GetViewTypeIdByViewType(
        ViewFamily.Section );

      ViewSection NyViewSectionR
        = ViewSection.CreateSection(
          doc.Document, SectionId, bbNewSectionR );
    }

    ElementId GetViewTypeIdByViewType(
      ViewFamily viewFamily )
    {
      FilteredElementCollector fec
        = new FilteredElementCollector(
          m_app.ActiveUIDocument.Document );

      fec.OfClass( typeof( ViewFamilyType ) );

      foreach( ViewFamilyType e in fec )
      {
        System.Diagnostics.Debug.WriteLine( e.Name );

        if( e.ViewFamily == viewFamily )
        {
          return e.Id;
        }
      }
      return null;
    }
#endif // 0

        #endregion // Language Independent View Type Id

        #region Create Perspective View to Match Forge Viewer Camera Settings

        /// <summary>
        ///     Create perspective view with camera settings
        ///     matching the Forge Viewer.
        /// </summary>
        private void CreatePerspectiveViewMatchingCamera(
            Document doc,
            XYZ camera_position,
            XYZ target)
        {
            using var trans = new Transaction(doc);
            trans.Start("Map Forge Viewer Camera");

            var typ
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(ViewFamilyType))
                    .Cast<ViewFamilyType>()
                    .First(
                        x => x.ViewFamily.Equals(
                            ViewFamily.ThreeDimensional));

            // Create a new perspective 3D view

            var view3D = View3D.CreatePerspective(
                doc, typ.Id);

            var rnd = new Random();
            view3D.Name = $"Camera{rnd.Next()}";

            // By default, the 3D view uses a default 
            // orientation. Change that by creating and 
            // setting up a suitable ViewOrientation3D.

            //var position = new XYZ( -15.12436009332275,
            //  -8.984616232971192, 4.921260089050291 );

            var up = XYZ.BasisZ;

            //var target = new XYZ( -15.02436066552734,
            //  -8.984211875061035, 4.921260089050291 );

            var sightDir = target.Subtract(camera_position).Normalize();

            var orientation = new ViewOrientation3D(
                camera_position, up, sightDir);

            view3D.SetOrientation(orientation);

            // Turn off the far clip plane, etc.

            view3D.LookupParameter("Far Clip Active")
                .Set(0);

            view3D.LookupParameter("Crop Region Visible")
                .Set(1);

            view3D.LookupParameter("Crop View")
                .Set(1);

            trans.Commit();
        }

        #endregion // Create Perspective View to Match Forge Viewer Camera Settings

        #region Create Isometric View from Revit 2013 What's New sample code

        /// <summary>
        ///     Sample code from Revit API help file What's New
        ///     section on View API and View Creation.
        /// </summary>
        private void ViewApiCreateViewSample()
        {
            Document doc = null;
            Level level = null;
            var viewFamily3d = ElementId.InvalidElementId;

            var viewFamilyTypes
                = from e in new FilteredElementCollector(doc)
                    .OfClass(typeof(ViewFamilyType))
                let type = e as ViewFamilyType
                where type.ViewFamily == ViewFamily.CeilingPlan
                select type;

            var ceilingPlan = ViewPlan.Create(doc,
                viewFamilyTypes.First().Id, level.Id);

            ceilingPlan.Name = $"New Ceiling Plan for {level.Name}";

            ceilingPlan.DetailLevel = ViewDetailLevel.Fine;

            // 3D views can be created with 
            // View3D.CreateIsometric and 
            // View3D.CreatePerspective. 
            // The new ViewOrientation3D object is used to 
            // get or set the orientation of 3D views.

            var view = View3D.CreateIsometric(
                doc, viewFamily3d);

            var eyePosition = new XYZ(10, 10, 10);
            var upDirection = new XYZ(-1, 0, 1);
            var forwardDirection = new XYZ(1, 0, 1);

            view.SetOrientation(new ViewOrientation3D(
                eyePosition, upDirection, forwardDirection));
        }

        #endregion // Create Isometric View from Revit 2013 What's New sample code

        /// <summary>
        ///     Simulate VSTA macro Application class member variable:
        /// </summary>
        private Application Create;

        /// <summary>
        ///     Return a view with the given name in the document.
        /// </summary>
        private View FindView(Document doc, string name)
        {
            // todo: check whether this includes derived types,
            // which is what we need here. in revit 2009, we used
            // TypeFilter filter = Create.Filter.NewTypeFilter(
            //   typeof( View ), true );

            return Util.GetFirstElementOfTypeNamed(
                doc, typeof(View), name) as View;
        }

        /// <summary>
        ///     Return a reference to the topmost face of the given element.
        /// </summary>
        private Reference FindTopMostReference(Element e)
        {
            Reference ret = null;
            var doc = e.Document;

            #region Revit 2012

#if _2012
        using (SubTransaction t = new SubTransaction(doc))
        {
            t.Start();

        // Create temporary 3D view

        //View3D view3D = doc.Create.NewView3D( // 2012
        //  viewDirection ); // 2012

        ViewFamilyType vft
            = new FilteredElementCollector( doc )
            .OfClass( typeof( ViewFamilyType ) )
            .Cast<ViewFamilyType>()
            .FirstOrDefault<ViewFamilyType>( x =>
                ViewFamily.ThreeDimensional == x.ViewFamily );

        Debug.Assert( null != vft,
            "expected to find a valid 3D view family type" );

        View3D view = View3D.CreateIsometric( doc, vft.Id ); // 2013

        XYZ eyePosition = XYZ.BasisZ;
        XYZ upDirection = XYZ.BasisY;
        XYZ forwardDirection = -XYZ.BasisZ;

        view.SetOrientation( new ViewOrientation3D(
            eyePosition, upDirection, forwardDirection ) );

      XYZ viewDirection = -XYZ.BasisZ;

      BoundingBoxXYZ bb = e.get_BoundingBox( view );

      XYZ max = bb.Max;

      XYZ minAtMaxElevation = Create.NewXYZ(
        bb.Min.X, bb.Min.Y, max.Z );

      XYZ centerOfTopOfBox = 0.5
        * (minAtMaxElevation + max);

      centerOfTopOfBox += 10 * XYZ.BasisZ;

        // Cast a ray through the model 
        // to find the topmost surface

#if DEBUG
        //ReferenceArray references
        //  = doc.FindReferencesByDirection(
        //    centerOfTopOfBox, viewDirection, view3D ); // 2011

        IList<ReferenceWithContext> references
          = doc.FindReferencesWithContextByDirection(
            centerOfTopOfBox, viewDirection, view ); // 2012

        double closest = double.PositiveInfinity;

        //foreach( Reference r in references )
        //{
        //  // 'Autodesk.Revit.DB.Reference.Element' is
        //  // obsolete: Property will be removed. Use
        //  // Document.GetElement(Reference) instead.
        //  //Element re = r.Element; // 2011

        //  Element re = doc.GetElement( r ); // 2012

        //  if( re.Id.IntegerValue == e.Id.IntegerValue
        //    && r.ProximityParameter < closest )
        //  {
        //    ret = r;
        //    closest = r.ProximityParameter;
        //  }
        //}

        foreach( ReferenceWithContext r in references )
        {
          Element re = doc.GetElement(
            r.GetReference() ); // 2012

          if( re.Id.IntegerValue == e.Id.IntegerValue
            && r.Proximity < closest )
          {
            ret = r.GetReference();
            closest = r.Proximity;
          }
        }

        string stable_reference = null == ret ? null 
          : ret.ConvertToStableRepresentation( doc );
#endif // DEBUG

        ReferenceIntersector ri
          = new ReferenceIntersector( 
            e.Id, FindReferenceTarget.Element, view );

        ReferenceWithContext r2 = ri.FindNearest(
          centerOfTopOfBox, viewDirection );

        if( null == r2 )
        {
          Debug.Print( "ReferenceIntersector.FindNearest returned null!" );
        }
        else
        {
          ret = r2.GetReference();

          Debug.Assert( stable_reference.Equals( ret
            .ConvertToStableRepresentation( doc ) ),
            "expected same reference from "
            + "FindReferencesWithContextByDirection and "
            + "ReferenceIntersector" );
        }
        t.RollBack();
      }
#endif // _2012

            #endregion // Revit 2012

            var opt = doc.Application.Create
                .NewGeometryOptions();

            opt.ComputeReferences = true;

            var geo = e.get_Geometry(opt);

            foreach (var obj in geo)
            {
                var inst = obj as GeometryInstance;

                if (null != inst)
                {
                    geo = inst.GetSymbolGeometry();
                    break;
                }
            }

            var solid = geo.OfType<Solid>()
                .First(sol => null != sol);

            var z = double.MinValue;

            foreach (Face f in solid.Faces)
            {
                var b = f.GetBoundingBox();
                var p = b.Min;
                var q = b.Max;
                var midparam = p + 0.5 * (q - p);
                var midpoint = f.Evaluate(midparam);
                var normal = f.ComputeNormal(midparam);

                if (Util.PointsUpwards(normal))
                    if (midpoint.Z > z)
                    {
                        z = midpoint.Z;
                        ret = f.Reference;
                    }
            }

            return ret;
        }

        /// <summary>
        ///     Create three new spot elevations on the top
        ///     surface of a beam, at its midpoint and both
        ///     endpoints.
        /// </summary>
        private bool NewSpotElevation(Document doc)
        {
            //Document doc = ActiveDocument; // for VSTA macro version

            var westView = FindView(doc, "West");

            if (null == westView)
            {
                Util.ErrorMsg("No view found named 'West'.");
                return false;
            }

            // define the hard-coded beam element id:

            //ElementId instanceId = Create.NewElementId();
            //instanceId.IntegerValue = 230298;

            var instanceId = new ElementId(230298);

            if (doc.GetElement(
                instanceId) is not FamilyInstance beam)
            {
                Util.ErrorMsg("Beam 230298 not found.");
                return false;
            }

            //doc.BeginTransaction(); // for VSTA macro version

            using var t = new Transaction(doc);
            t.Start("Create Spot Elevation");

            var topReference
                = FindTopMostReference(beam);

            var lCurve = beam.Location
                as LocationCurve;

            for (var i = 0; i < 3; ++i)
            {
                var lCurvePnt = lCurve.Curve.Evaluate(
                    0.5 * i, true);

                var bendPnt = lCurvePnt.Add(
                    Create.NewXYZ(0, 1, 4));

                var endPnt = lCurvePnt.Add(
                    Create.NewXYZ(0, 2, 4));

                // NewSpotElevation arguments:
                // View view, Reference reference,
                // XYZ origin, XYZ bend, XYZ end, XYZ refPt,
                // bool hasLeader

                var d = doc.Create.NewSpotElevation(
                    westView, topReference, lCurvePnt, bendPnt,
                    endPnt, lCurvePnt, true);
            }

            //doc.EndTransaction(); // for VSTA macro version

            t.Commit();

            return true;
        }

        /// <summary>
        ///     External command mainline method for non-VSTA solution.
        /// </summary>
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var doc = app.ActiveUIDocument.Document;
            Create = app.Application.Create;

            return NewSpotElevation(doc)
                ? Result.Succeeded
                : Result.Failed;
        }
    }
}

// Y:\j\tmp\rvt\rac_empty.rvt 
// Y:\a\doc\revit\blog\zip\ForSpotElevation.rvt