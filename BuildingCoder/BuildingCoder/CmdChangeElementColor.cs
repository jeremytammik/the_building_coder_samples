#region Header
//
// CmdChangeElementColor.cs - Change element colour using OverrideGraphicSettings for active view
//
// Also change its category's material to a random material
//
// Copyright (C) 2020-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
#endregion // Header

#region Namespaces
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Manual )]
  public class CmdChangeElementColor : IExternalCommand
  {
    void ChangeElementColor( Document doc, ElementId id )
    {
      Color color = new Color(
        (byte) 200, (byte) 100, (byte) 100 );

      OverrideGraphicSettings ogs = new OverrideGraphicSettings();
      ogs.SetProjectionLineColor( color );

      using( Transaction tx = new Transaction( doc ) )
      {
        tx.Start( "Change Element Color" );
        doc.ActiveView.SetElementOverrides( id, ogs );
        tx.Commit();
      }
    }

    void ChangeElementMaterial( Document doc, ElementId id )
    {
      Element e = doc.GetElement( id );

      if( null != e.Category )
      {
        int im = e.Category.Material.Id.IntegerValue;

        List<Material> materials = new List<Material>(
          new FilteredElementCollector( doc )
            .WhereElementIsNotElementType()
            .OfClass( typeof( Material ) )
            .ToElements()
            .Where<Element>( m
              => m.Id.IntegerValue != im )
            .Cast<Material>() );

        Random r = new Random();
        int i = r.Next( materials.Count );

        using( Transaction tx = new Transaction( doc ) )
        {
          tx.Start( "Change Element Material" );
          e.Category.Material = materials[ i ];
          tx.Commit();
        }
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
      View view = doc.ActiveView;
      ElementId id;

      try
      {
        Selection sel = uidoc.Selection;
        Reference r = sel.PickObject(
          ObjectType.Element,
          "Pick element to change its colour" );
        id = r.ElementId;
      }
      catch( Autodesk.Revit.Exceptions.OperationCanceledException )
      {
        return Result.Cancelled;
      }

      ChangeElementColor( doc, id );

      ChangeElementMaterial( doc, id );

      return Result.Succeeded;
    }

    #region Paint Stairs
    // https://forums.autodesk.com/t5/revit-api-forum/paint-stair-faces/m-p/10388359
    void PaintStairs( UIDocument uidoc, Material mat )
    {
      Document doc = uidoc.Document;
      Selection sel = uidoc.Selection;

      //FaceSelectionFilter filter = new FaceSelectionFilter();
      Reference pickedRef = sel.PickObject(
        ObjectType.PointOnElement,
        //filter, 
        "Please select a Face" );

      Element elem = doc.GetElement( pickedRef );

      GeometryObject geoObject = elem
        .GetGeometryObjectFromReference( pickedRef );

      Face fc = geoObject as Face;

      if( elem.Category.Id.IntegerValue == -2000120 ) // Stairs
      {
        bool flag = false;
        Stairs str = elem as Stairs;
        ICollection<ElementId> landings = str.GetStairsLandings();
        ICollection<ElementId> runs = str.GetStairsLandings();
        using( Transaction transaction = new Transaction( doc ) )
        {
          transaction.Start( "Paint Material" );
          foreach( ElementId id in landings )
          {
            doc.Paint( id, fc, mat.Id );
            flag = true;
            break;
          }
          if( !flag )
          {
            foreach( ElementId id in runs )
            {
              doc.Paint( id, fc, mat.Id );
              break;
            }
          }
          transaction.Commit();
        }
      }
    }

    /// <summary>
    /// Prompt user to pick a face and paint it 
    /// with the given material. If the face belongs
    /// to a stair run or landing, paint that part
    /// of the stair specifically.
    /// </summary>
    void PaintSelectedFace( UIDocument uidoc, Material mat )
    {
      Document doc = uidoc.Document;
      Selection sel = uidoc.Selection;
      List<String> errors = new List<string>();
      //FaceSelectionFilter filter = new FaceSelectionFilter();
      Reference pickedRef = sel.PickObject(
        ObjectType.PointOnElement,
        //filter, 
        "Please select a face to paint" );

      Element elem = doc.GetElement( pickedRef );

      GeometryObject geoObject = elem
        .GetGeometryObjectFromReference( pickedRef );

      Face selected_face = geoObject as Face;

      using( Transaction transaction = new Transaction( doc ) )
      {
        transaction.Start( "Paint Selected Face" );

        if( elem.Category.Id.IntegerValue.Equals(
          (int) BuiltInCategory.OST_Stairs ) )
        {
          Stairs str = elem as Stairs;
          bool IsLand = false;

          ICollection<ElementId> landings = str.GetStairsLandings();
          ICollection<ElementId> runs = str.GetStairsRuns();

          foreach( ElementId id in landings )
          {

            Element land = doc.GetElement( id );
            List<Solid> solids = GetElemSolids(
              land.get_Geometry( new Options() ) );

            IsLand = SolidsContainFace( solids, selected_face );

            if( IsLand )
            {
              break;
            }
          }

          if( IsLand )
          {
            foreach( ElementId id in landings )
            {
              doc.Paint( id, selected_face, mat.Id );
              break;
            }
          }
          else
          {
            foreach( ElementId id in runs )
            {
              doc.Paint( id, selected_face, mat.Id );
              break;
            }
          }
        }
        else
        {
          try
          {
            doc.Paint( elem.Id, selected_face, mat.Id );
          }
          catch( Exception ex )
          {
            TaskDialog.Show( "Error painting selected face",
              ex.Message );
          }
        }
        transaction.Commit();
      }
    }

    /// <summary>
    /// Does the given face belong to one of the given solids?
    /// </summary>
    private bool SolidsContainFace( List<Solid> solids, Face face )
    {
      foreach( Solid s in solids )
      {
        if( null != s
          && 0 < s.Volume )
        {
          foreach( Face f in s.Faces )
          {
            if( f == face )
            {
              return true;
            }
            else if( f.HasRegions )
            {
              foreach( Face f2 in f.GetRegions() )
              {
                if( f2 == face )
                {
                  return true;
                }
              }
            }
          }
        }
      }
      return false;
    }

    /// <summary>
    /// Recursively collect all solids 
    /// contained in the given element geomety
    /// </summary>
    List<Solid> GetElemSolids( GeometryElement geomElem )
    {
      List<Solid> solids = new List<Solid>();

      if( null != geomElem )
      {
        foreach( GeometryObject geomObj in geomElem )
        {
          if( geomObj is Solid solid )
          {
            if( solid.Faces.Size > 0 )
            {
              solids.Add( solid );
              continue;
            }
          }
          if( geomObj is GeometryInstance geomInst )
          {
            solids.AddRange( GetElemSolids(
              geomInst.GetInstanceGeometry() ) );
          }
        }
      }
      return solids;
    }
    #endregion // Paint Stairs
  }
}
