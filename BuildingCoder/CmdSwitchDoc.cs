#region Header

//
// CmdSwitchDoc.cs - switch document or view
//
// Copyright (C) 2018-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdSwitchDoc : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var app = uiapp.Application;
            var doc = uidoc.Document;
            var rc = Result.Succeeded;

            var ids
                = uidoc.Selection.GetElementIds();

            var zoomToPreselectedElements
                = 0 < ids.Count;

            if (zoomToPreselectedElements)
            {
                rc = ZoomToElements(uidoc, ids,
                    ref message, elements);
            }
            else
            {
                var filepath = "C:/test/xyz.rfa";

                ToggleViews(doc.ActiveView, filepath);
            }

            return rc;
        }

        /// <summary>
        ///     Zoom to the given elements, switching view if needed.
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="message">Error message on failure</param>
        /// <param name="elements">Elements causing failure</param>
        /// <returns></returns>
        private Result ZoomToElements(
            UIDocument uidoc,
            ICollection<ElementId> ids,
            ref string message,
            ElementSet elements)
        {
            var n = ids.Count;

            if (0 == n)
            {
                message = "Please select at least one element to zoom to.";
                return Result.Cancelled;
            }

            try
            {
                uidoc.ShowElements(ids);
            }
            catch
            {
                var doc = uidoc.Document;

                foreach (var id in ids)
                {
                    var e = doc.GetElement(id);
                    elements.Insert(e);
                }

                message = $"Cannot zoom to element{(1 == n ? "" : "s")}.";

                return Result.Failed;
            }

            return Result.Succeeded;
        }

        /// <summary>
        ///     Toggle back and forth between two different documents
        /// </summary>
        private void ToggleViews(
            View view1,
            string filepath2)
        {
            var doc = view1.Document;
            var uidoc = new UIDocument(doc);
            var app = doc.Application;
            var uiapp = new UIApplication(app);

            // Select some elements in the first document

            var idsView1
                = new FilteredElementCollector(doc, view1.Id)
                    .WhereElementIsNotElementType()
                    .ToElementIds();

            // Open the second file

            var uidoc2 = uiapp
                .OpenAndActivateDocument(filepath2);

            var doc2 = uidoc2.Document;

            // Do something in second file

            using (var tx = new Transaction(doc2))
            {
                tx.Start("Change Scale");
                doc2.ActiveView.get_Parameter(
                        BuiltInParameter.VIEW_SCALE_PULLDOWN_METRIC)
                    .Set(20);
                tx.Commit();
            }

            // Save modified second file

            var opt = new SaveAsOptions
            {
                OverwriteExistingFile = true
            };

            doc2.SaveAs(filepath2, opt);

            // Switch back to original file;
            // in a new file, doc.PathName is empty

            if (!string.IsNullOrEmpty(doc.PathName))
            {
                uiapp.OpenAndActivateDocument(
                    doc.PathName);

                doc2.Close(false); // no problem here, says Remy
            }
            else
            {
                // Avoid using OpenAndActivateDocument

                uidoc.ShowElements(idsView1);
                uidoc.RefreshActiveView();

                //doc2.Close( false ); // Remy says: Revit throws the exception and doesn't close the file
            }
        }

        #region Zoom to linked element

        // zoom active view to element in linked doc
        // how to zoom elements in linked document using revit api?
        // https://forums.autodesk.com/t5/revit-api-forum/how-to-zoom-elements-in-linked-document-using-revit-api/m-p/9778123
        // use LinkElementId class?

        private void ZoomToLinkedElement(
            UIDocument uidoc,
            RevitLinkInstance link,
            ElementId id)
        {
            var doc = uidoc.Document;
            var view = doc.ActiveView;

            // Determine active UIView to use

            var uiView = uidoc
                .GetOpenUIViews()
                .FirstOrDefault(uv
                    => uv.ViewId.Equals(view.Id));

            var e = doc.GetElement(id);
            var lp = e.Location as LocationPoint;
            var transform1 = link.GetTransform();
            var newLocation2 = transform1.OfPoint(lp.Point);
            var bb = e.get_BoundingBox(doc.ActiveView);

            uiView.ZoomAndCenterRectangle(
                new XYZ(newLocation2.X - 4, newLocation2.Y - 4, newLocation2.Z - 4),
                new XYZ(newLocation2.X + 4, newLocation2.Y + 4, newLocation2.Z + 4));
        }

        #endregion // Zoom to linked element
    }
}