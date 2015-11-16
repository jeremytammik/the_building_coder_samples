#region Header
//
// CmdFlatten.cs - convert all Revit elements to DirectShapes retaining shape and category
//
// Written by Nikolay Shulga.
// Copyright (C) 2015 by Nikolay Shulga and Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Name: Flatten
// Motivation: I wanted to see whether DirectShapes could be used to lock down a Revit design – remove most intelligence, make it read-only, perhaps improve performance.
// Spec: converts full Revit elements into DirectShapes that hold the same shape and have the same categories.
// Implementation: see below
// Cool API aspects: copy element’s geometry and use it elsewhere
// Cool ways to use it: lock down your project; make a copy of your element for presentation/export.
// How it can be enhanced: the sky is the limit.
// A suitable sample model: any Revit project. Note that the code changes the current project – make a backup copy.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  #region ExportAsDirectShape class
  class ExportAsDirectShape
  {
    // The document we will output to. Currently must be the original document – no API to properly transfer graphic styles to a new document.
    Document _projectAsDirectShapes;

    public ExportAsDirectShape( Document doc )
    {
      _projectAsDirectShapes = doc; // for now use the source doc. A possible enhancement: make a copy of the current project and operate on the copy.
    }

    public bool AddShape( IList<GeometryObject> nodes, ElementId categoryId, string elementId )
    {
      bool status = false;
      if( DirectShape.IsValidCategoryId( categoryId, _projectAsDirectShapes ) )
      {
        string appGUID = "Flatten";
        string appDataGUID = elementId;
        DirectShape ds = DirectShape.CreateElement( _projectAsDirectShapes, categoryId, appGUID, appDataGUID );

        //ds.ApplicationId = "Flatten";
        //ds.ApplicationDataId = elementId;
        //if( ds.IsValidShape( nodes ) )

        {
          ds.AppendShape( nodes );
          status = true;
        }
      }
      return status;
    }

    public bool AddShape( IEnumerator<GeometryObject> shapeAsEnumerator, ElementId categoryId, string elementId )
    {
      List<GeometryObject> shapes = new List<GeometryObject>();

      while( shapeAsEnumerator.MoveNext() )
      {
        shapes.Add( shapeAsEnumerator.Current );
      }

      return AddShape( shapes, categoryId, elementId );
    }
  }
  #endregion // ExportAsDirectShape class

  [Transaction( TransactionMode.Manual )]
  class CmdFlatten : IExternalCommand
  {
    const string _direct_shape_appGUID = "Flatten";

    #region Using ExportAsDirectShape class
    private Result Flatten1( Document myDoc, ElementId viewId )
    {
      ExportAsDirectShape exporter = new ExportAsDirectShape( myDoc );

      FilteredElementCollector col = new FilteredElementCollector( myDoc, viewId ).WhereElementIsNotElementType();

      Options geometryOptions = new Options();

      using( Transaction transaction = new Transaction( myDoc ) )
      {
        if( transaction.Start( "Convert elements to DirectShapes" ) == TransactionStatus.Started )
        {
          foreach( Element elt in col )
          {
            try
            {
              GeometryElement gelt = elt.get_Geometry( geometryOptions );

              if( null != gelt )
              {
                IEnumerator<GeometryObject> shapeAsEnumerator = gelt.GetEnumerator();
                if( exporter.AddShape( shapeAsEnumerator, elt.Category.Id, elt.Id.ToString() ) )
                {
                  // delete old one
                  myDoc.Delete( elt.Id );
                }
              }
            }
            catch( Autodesk.Revit.Exceptions.ArgumentException )
            {

            }
          }
          transaction.Commit();
        }
      }
      return Result.Succeeded;
    }
    #endregion // Using ExportAsDirectShape class

    Result Flatten2(
      Document doc,
      ElementId viewId )
    {
      FilteredElementCollector col
        = new FilteredElementCollector( doc, viewId )
          .WhereElementIsNotElementType();

      Options geometryOptions = new Options();

      using( Transaction tx = new Transaction( doc ) )
      {
        if( tx.Start( "Convert elements to DirectShapes" )
          == TransactionStatus.Started )
        {
          foreach( Element e in col )
          {
            GeometryElement gelt = e.get_Geometry( 
              geometryOptions );

            if( null != gelt )
            {
              string appDataGUID = e.Id.ToString();

              // Currently create direct shape 
              // replacement element in the original 
              // document – no API to properly transfer 
              // graphic styles to a new document.
              // A possible enhancement: make a copy 
              // of the current project and operate 
              // on the copy.

              DirectShape ds 
                = DirectShape.CreateElement( doc,
                  e.Category.Id, _direct_shape_appGUID,
                  appDataGUID );

              try
              {
                ds.SetShape(
                  new List<GeometryObject>( gelt ) );

                // Delete original element

                doc.Delete( e.Id );
              }
              catch( Autodesk.Revit.Exceptions
                .ArgumentException ex )
              {
                Debug.Print( ex.Message );
              }
            }
          }
          tx.Commit();
        }
      }
      return Result.Succeeded;
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Document doc = uidoc.Document;

      // At the moment we convert to DirectShapes 
      // "in place" - that lets us preserve GStyles 
      // referenced by element shape without doing 
      // anything special.

      return Flatten2( doc, uidoc.ActiveView.Id );
    }
  }
}
