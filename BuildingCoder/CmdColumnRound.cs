#region Header

//
// CmdColumnRound.cs - determine whether a
// selected column instance is cylindrical
//
// Copyright (C) 2009-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdColumnRound : IExternalCommand
    {
        /// <summary>
        ///     Determine the height of a vertical column from
        ///     its top and bottom level.
        /// </summary>
        public double GetColumHeightFromLevels(
            Element e)
        {
            if (!IsColumn(e))
                throw new ArgumentException(
                    "Expected a column argument.");

            var doc = e.Document;

            double height = 0;

            if (e != null)
            {
                // Get top level of the column

                var topLevel = e.get_Parameter(
                    BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);

                var ip = topLevel.AsElementId();
                var top = doc.GetElement(ip) as Level;
                var t_value = top.ProjectElevation;

                // Get base level of the column 

                var BotLevel = e.get_Parameter(
                    BuiltInParameter.FAMILY_BASE_LEVEL_PARAM);

                var bip = BotLevel.AsElementId();
                var bot = doc.GetElement(bip) as Level;
                var b_value = bot.ProjectElevation;

                // At this point, there are a number of 
                // additional Z offsets that may also affect
                // the result.

                height = t_value - b_value;
            }

            return height;
        }


        /// <summary>
        ///     Determine the height of any given element
        ///     from its bounding box.
        /// </summary>
        public double GetElementHeightFromBoundingBox(
            Element e)
        {
            // No need to retrieve the full element geometry.
            // Even if there were, there would be no need to 
            // compute references, because they will not be
            // used anyway!

            //GeometryElement ge = e.get_Geometry( 
            //  new Options() { 
            //    ComputeReferences = true } );
            //
            //BoundingBoxXYZ boundingBox = ge.GetBoundingBox();

            var bb = e.get_BoundingBox(null);

            if (null == bb)
                throw new ArgumentException(
                    "Expected Element 'e' to have a valid bounding box.");

            return bb.Max.Z - bb.Min.Z;
        }

#if REQUIRES_REVIT_2009_API
    // works in Revit Structure 2009 API, but not in 2010:

    bool IsColumnRound(
      FamilySymbol symbol )
    {
      GenericFormSet solid = symbol.Family.SolidForms;
      GenericFormSetIterator i = solid.ForwardIterator();
      i.MoveNext();
      Extrusion extr = i.Current as Extrusion;
      CurveArray cr = extr.Sketch.CurveLoop;
      CurveArrayIterator i2 = cr.ForwardIterator();
      i2.MoveNext();
      String s = i2.Current.GetType().ToString();
      return s.Contains( "Arc" );
    }
#endif // REQUIRES_REVIT_2009_API

#if REQUIRES_REVIT_2010_API
    // works in Revit Structure 2010 API, but not in 2011:
    // works in Revit Structure, but not in other flavours of Revit:

    bool ContainsArc( AnalyticalModel a )
    {
      bool rc = false;
      AnalyticalModel amp = a.Profile;
      Profile p = amp.SweptProfile;
      foreach( Curve c in p.Curves )
      {
        if( c is Arc )
        {
          rc = true;
          break;
        }
      }
      return rc;
    }
#endif // REQUIRES_REVIT_2010_API

        /// <summary>
        ///     Return true if the given Revit element looks
        ///     like it might be a column family instance.
        /// </summary>
        private bool IsColumn(Element e)
        {
            return e is FamilyInstance
                   && null != e.Category
                   && e.Category.Name.ToLower().Contains("column");

            // Optional stronger test:
            //
            //  && (int) BuiltInCategory.OST_Columns
            //    == e.Category.Id.IntegerValue )
        }

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;
            var doc = uidoc.Document;

            //Element column = null;
            //Selection sel = uidoc.Selection;
            //
            //if( 1 == sel.GetElementIds().Count )
            //{
            //  foreach( Element e in sel.Elements )
            //  {
            //    column = e;
            //  }
            //  if( !IsColumn( column ) )
            //  {
            //    column = null;
            //  }
            //}
            //
            //if( null == column )
            //{
            //
            //#if _2010
            //  sel.Elements.Clear();
            //  sel.StatusbarTip = "Please select a column";
            //  if( sel.PickOne() )
            //  {
            //    ElementSetIterator i
            //      = sel.Elements.ForwardIterator();
            //    i.MoveNext();
            //    column = i.Current as Element;
            //  }
            //#endif // _2010
            //
            //  Reference r = uidoc.Selection.PickObject( ObjectType.Element,
            //    "Please select a column" );
            //
            //  if( null != r )
            //  {
            //    // 'Autodesk.Revit.DB.Reference.Element' is
            //    // obsolete: Property will be removed. Use
            //    // Document.GetElement(Reference) instead.
            //    //column = r.Element; // 2011
            //
            //    column = doc.GetElement( r ); // 2012
            //
            //    if( !IsColumn( column ) )
            //    {
            //      message = "Please select a single column instance";
            //    }
            //  }
            //}

            var rc = Result.Failed;

            var column = Util.SelectSingleElementOfType(
                uidoc, typeof(FamilyInstance), "column", true);

            if (null == column || !IsColumn(column))
            {
                message = "Please select a single column instance";
            }
            else
            {
                var opt = app.Application.Create.NewGeometryOptions();
                var geo = column.get_Geometry(opt);
                GeometryInstance i = null;

                //GeometryObjectArray objects = geo.Objects; // 2012
                //foreach( GeometryObject obj in objects ) // 2012

                foreach (var obj in geo) // 2013
                {
                    i = obj as GeometryInstance;
                    if (null != i) break;
                }

                if (null == i)
                {
                    message = "Unable to obtain geometry instance";
                }
                else
                {
                    var isCylindrical = false;
                    geo = i.SymbolGeometry;

                    //objects = geo.Objects; // 2012
                    //foreach( GeometryObject obj in objects ) // 2012

                    foreach (var obj in geo)
                    {
                        var solid = obj as Solid;
                        if (null != solid)
                            foreach (Face face in solid.Faces)
                                if (face is CylindricalFace)
                                {
                                    isCylindrical = true;
                                    break;
                                }
                    }

                    message = $"Selected column instance is{(isCylindrical ? "" : " NOT")} cylindrical";
                }

                rc = Result.Succeeded;
            }

            return rc;
        }
    }

    #region Get geometry from joined beam

#if GET_GEOMETRY_FROM_JOINED_BEAM
  [Transaction( TransactionMode.Automatic )]
  public class CmdGetColumnGeometry1 : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIDocument uidoc = commandData.Application
        .ActiveUIDocument;

      Options options = new Options();

      options.ComputeReferences = true;

      GeometryElement geomElement
        = uidoc.Selection.PickObject(
          ObjectType.Element )
          .Element.get_Geometry( options );

      int edgeCount = 0;
      foreach( GeometryObject geomObj
        in geomElement.Objects )
      {
        if( geomObj is GeometryInstance )
        {
          GeometryInstance inst = geomObj
            as GeometryInstance;

          if( inst != null )
          {
            GeometryElement geomElem
              = inst.GetSymbolGeometry();

            foreach( Object o in geomElem.Objects )
            {
              Solid solid = o as Solid;
              if( solid != null )
              {
                foreach( Face face in solid.Faces )
                {
                  foreach( EdgeArray edgeArray
                    in face.EdgeLoops )
                  {
                    edgeCount += edgeArray.Size;
                  }
                }
              }
            }
          }
        }
      }
      TaskDialog.Show( "Revit", "Edges: "
        + edgeCount.ToString() );

      return Result.Succeeded;
    }

    void f( UIDocument uidoc )
    {
      Options options = new Options();
      options.ComputeReferences = true;

      GeometryElement geomElement = uidoc.Selection
        .PickObject( ObjectType.Element )
        .Element.get_Geometry( options );

      int ctr = 0;
      foreach( GeometryObject geomObj
        in geomElement.Objects )
      {
        if( geomObj is Solid )
        {
          FaceArray faces = ( ( Solid ) geomObj ).Faces;
          ctr += faces.Size;
        }

        if( geomObj is GeometryInstance )
        {
          GeometryInstance inst = geomObj
            as GeometryInstance;

          if( inst != null )
          {
            GeometryElement geomElem
              = inst.GetSymbolGeometry();

            foreach( Object o in geomElem.Objects )
            {
              Solid solid = o as Solid;
              if( solid != null )
              {
                ctr += solid.Faces.Size;
              }
            }
          }
        }
      }
      TaskDialog.Show( "Revit", "Faces: " + ctr );
    }
  }
#endif // GET_GEOMETRY_FROM_JOINED_BEAM

    #endregion // Get geometry from joined beam
}