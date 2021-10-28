#region Header

//
// CmdFlatten.cs - convert all Revit elements to DirectShapes retaining shape and category
//
// Copyright (C) 2015-2020 by Nikolay Shulga and Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Written by Nikolay Shulga.
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

using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.Exceptions;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdFlatten : IExternalCommand
    {
        private const string _direct_shape_appGUID = "Flatten";

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;

            // At the moment we convert to DirectShapes 
            // "in place" - that lets us preserve GStyles 
            // referenced by element shape without doing 
            // anything special.

            return Flatten(doc, uidoc.ActiveView.Id);
        }

        private Result Flatten(
            Document doc,
            ElementId viewId)
        {
            var col
                = new FilteredElementCollector(doc, viewId)
                    .WhereElementIsNotElementType();

            var geometryOptions = new Options();

            using var tx = new Transaction(doc);
            if (tx.Start("Convert elements to DirectShapes")
                == TransactionStatus.Started)
            {
                foreach (var e in col)
                {
                    var gelt = e.get_Geometry(
                        geometryOptions);

                    if (null != gelt)
                    {
                        var appDataGUID = e.Id.ToString();

                        // Currently create direct shape 
                        // replacement element in the original 
                        // document – no API to properly transfer 
                        // graphic styles to a new document.
                        // A possible enhancement: make a copy 
                        // of the current project and operate 
                        // on the copy.

                        //DirectShape ds = DirectShape.CreateElement(
                        //  doc, e.Category.Id,
                        //  _direct_shape_appGUID,
                        //  appDataGUID ); // 2016

                        var ds = DirectShape.CreateElement(
                            doc, e.Category.Id); //2017

                        ds.ApplicationId = _direct_shape_appGUID; // 2017
                        ds.ApplicationDataId = appDataGUID; // 2017

                        try
                        {
                            ds.SetShape(
                                new List<GeometryObject>(gelt));

                            // Delete original element

                            doc.Delete(e.Id);
                        }
                        catch (ArgumentException ex)
                        {
                            Debug.Print(
                                "Failed to replace {0}; exception {1} {2}",
                                Util.ElementDescription(e),
                                ex.GetType().FullName,
                                ex.Message);
                        }
                    }
                }

                tx.Commit();
            }

            return Result.Succeeded;
        }
    }
}