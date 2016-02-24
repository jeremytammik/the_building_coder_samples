#region Header
//
// CmdFlatten.cs - convert all Revit elements to DirectShapes retaining shape and category
//
// Written by Nikolay Shulga.
// Copyright (C) 2015-2016 by Nikolay Shulga and Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
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
  [Transaction( TransactionMode.Manual )]
  class CmdFlatten : IExternalCommand
  {
    const string _direct_shape_appGUID = "Flatten";

    Result Flatten(
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
                Debug.Print(
                  "Failed to replace {0}; exception {1} {2}",
                  Util.ElementDescription( e ),
                  ex.GetType().FullName,
                  ex.Message );
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

      return Flatten( doc, uidoc.ActiveView.Id );
    }
  }
}
