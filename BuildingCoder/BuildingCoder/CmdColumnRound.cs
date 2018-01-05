#region Header
//
// CmdColumnRound.cs - determine whether a
// selected column instance is cylindrical
//
// Copyright (C) 2009-2018 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
#endregion // Header

#region Namespaces
using System;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.IO;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.ReadOnly )]
  class CmdColumnRound : IExternalCommand
  {
    /// <summary>
    /// Determine the height of a vertical column from 
    /// its top and bottom level.
    /// </summary>
    public Double GetColumHeightFromLevels( 
      Element e ) 
    {
      if( !IsColumn( e ) )
      {
        throw new ArgumentException(
          "Expected a column argument." );
      }

      Document doc = e.Document;

      double height = 0;

      if( e != null )
      {
        // Get top level of the column

        Parameter topLevel = e.get_Parameter( 
          BuiltInParameter.FAMILY_TOP_LEVEL_PARAM );

        ElementId ip = topLevel.AsElementId();
        Level top = doc.GetElement( ip ) as Level;
        double t_value = top.ProjectElevation;

        // Get base level of the column 

        Parameter BotLevel = e.get_Parameter( 
          BuiltInParameter.FAMILY_BASE_LEVEL_PARAM );

        ElementId bip = BotLevel.AsElementId();
        Level bot = doc.GetElement( bip ) as Level;
        double b_value = bot.ProjectElevation;

        // At this point, there are a number of 
        // additional Z offsets that may also affect
        // the result.

        height = ( t_value - b_value );
      }
      return height;
    }


    /// <summary>
    /// Determine the height of any given element 
    /// from its bounding box.
    /// </summary>
    public Double GetElementHeightFromBoundingBox( 
      Element e )
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

      BoundingBoxXYZ bb = e.get_BoundingBox( null );

      if( null == bb )
      {
        throw new ArgumentException(
          "Expected Element 'e' to have a valid bounding box." );
      }

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
    /// Return true if the given Revit element looks
    /// like it might be a column family instance.
    /// </summary>
    bool IsColumn( Element e )
    {
      return e is FamilyInstance
        && null != e.Category
        && e.Category.Name.ToLower().Contains( "column" );

      // Optional stronger test:
      //
      //  && (int) BuiltInCategory.OST_Columns
      //    == e.Category.Id.IntegerValue )
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      UIDocument uidoc = app.ActiveUIDocument;
      Document doc = uidoc.Document;

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

      Result rc = Result.Failed;

      Element column = Util.SelectSingleElementOfType(
        uidoc, typeof( FamilyInstance ), "column", true );

      if( null == column || !IsColumn( column ) )
      {
        message = "Please select a single column instance";
      }
      else
      {
        Options opt = app.Application.Create.NewGeometryOptions();
        GeometryElement geo = column.get_Geometry( opt );
        GeometryInstance i = null;

        //GeometryObjectArray objects = geo.Objects; // 2012
        //foreach( GeometryObject obj in objects ) // 2012

        foreach( GeometryObject obj in geo ) // 2013
        {
          i = obj as GeometryInstance;
          if( null != i )
          {
            break;
          }
        }
        if( null == i )
        {
          message = "Unable to obtain geometry instance";
        }
        else
        {
          bool isCylindrical = false;
          geo = i.SymbolGeometry;

          //objects = geo.Objects; // 2012
          //foreach( GeometryObject obj in objects ) // 2012

          foreach( GeometryObject obj in geo )
          {
            Solid solid = obj as Solid;
            if( null != solid )
            {
              foreach( Face face in solid.Faces )
              {
                if( face is CylindricalFace )
                {
                  isCylindrical = true;
                  break;
                }
              }
            }
          }
          message = string.Format(
            "Selected column instance is{0} cylindrical",
            ( isCylindrical ? "" : " NOT" ) );
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
