#region Header
//
// CmdColumnRound.cs - determine whether a
// selected column instance is cylindrical
//
// Copyright (C) 2009-2013 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.ReadOnly )]
  class CmdColumnRound : IExternalCommand
  {

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
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      UIDocument uidoc = app.ActiveUIDocument;
      Document doc = uidoc.Document;

      Selection sel = uidoc.Selection;
      Element column = null;

      if( 1 == sel.Elements.Size )
      {
        foreach( Element e in sel.Elements )
        {
          column = e;
        }
        if( !IsColumn( column ) )
        {
          column = null;
        }
      }

      if( null == column )
      {

#if _2010
        sel.Elements.Clear();
        sel.StatusbarTip = "Please select a column";
        if( sel.PickOne() )
        {
          ElementSetIterator i
            = sel.Elements.ForwardIterator();
          i.MoveNext();
          column = i.Current as Element;
        }
#endif // _2010

        Reference r = uidoc.Selection.PickObject( ObjectType.Element,
          "Please select a column" );

        if( null != r )
        {
          // 'Autodesk.Revit.DB.Reference.Element' is
          // obsolete: Property will be removed. Use
          // Document.GetElement(Reference) instead.
          //column = r.Element; // 2011

          column = doc.GetElement( r ); // 2012

          if( !IsColumn( column ) )
          {
            message = "Please select a single column instance";
          }
        }
      }

      if( null != column )
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
      }
      return Result.Failed;
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
