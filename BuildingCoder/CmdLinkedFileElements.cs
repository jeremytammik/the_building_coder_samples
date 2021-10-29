#region Header

//
// CmdLinkedFileElements.cs - list elements in linked files
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
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

#endregion // Namespaces

namespace BuildingCoder
{
    public class ElementData
    {
        private readonly double _x;
        private readonly double _y;
        private readonly double _z;

        public ElementData(
            string path,
            string elementName,
            int id,
            double x,
            double y,
            double z,
            string uniqueId)
        {
            var i = path.LastIndexOf("\\");
            Document = path.Substring(i + 1);
            Element = elementName;
            Id = id;
            _x = x;
            _y = y;
            _z = z;
            UniqueId = uniqueId;
            Folder = path.Substring(0, i);
        }

        public string Document { get; }

        public string Element { get; }

        public int Id { get; }

        public string X => Util.RealString(_x);

        public string Y => Util.RealString(_y);

        public string Z => Util.RealString(_z);

        public string UniqueId { get; }

        public string Folder { get; }
    }

    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdLinkedFileElements : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet highlightElements)
        {
            /*
      
            // retrieve all link elements:
      
            Document doc = app.ActiveUIDocument.Document;
            List<Element> links = GetElements(
              BuiltInCategory.OST_RvtLinks,
              typeof( Instance ), app, doc );
      
            // determine the link paths:
      
            DocumentSet docs = app.Documents;
            int n = docs.Size;
            Dictionary<string, string> paths
              = new Dictionary<string, string>( n );
      
            foreach( Document d in docs )
            {
              string path = d.PathName;
              int i = path.LastIndexOf( "\\" ) + 1;
              string name = path.Substring( i );
              paths.Add( name, path );
            }
            */

            // Retrieve lighting fixture element
            // data from linked documents:

            var data = new List<ElementData>();
            var app = commandData.Application;
            var docs = app.Application.Documents;

            foreach (Document doc in docs)
            {
                var a
                    = Util.GetElementsOfType(doc,
                        typeof(FamilyInstance),
                        BuiltInCategory.OST_LightingFixtures);

                foreach (FamilyInstance e in a)
                {
                    var name = e.Name;
                    if (e.Location is LocationPoint lp)
                    {
                        var p = lp.Point;
                        data.Add(new ElementData(doc.PathName, e.Name,
                            e.Id.IntegerValue, p.X, p.Y, p.Z, e.UniqueId));
                    }
                }
            }

            // Display data:

            using var dlg = new CmdLinkedFileElementsForm(data);
            dlg.ShowDialog();

            return Result.Succeeded;
        }

        #region AddFaceBasedFamilyToLinks

        public void AddFaceBasedFamilyToLinks(Document doc)
        {
            var alignedLinkId = new ElementId(125929);

            // Get symbol

            var symbolId = new ElementId(126580);

            var fs = doc.GetElement(symbolId)
                as FamilySymbol;

            // Aligned

            var linkInstance = doc.GetElement(
                alignedLinkId) as RevitLinkInstance;

            var linkDocument = linkInstance
                .GetLinkDocument();

            var wallCollector
                = new FilteredElementCollector(linkDocument);

            wallCollector.OfClass(typeof(Wall));

            var targetWall = wallCollector.FirstElement()
                as Wall;

            var exteriorFaceRef
                = HostObjectUtils.GetSideFaces(
                        targetWall, ShellLayerType.Exterior)
                    .First();

            var linkToExteriorFaceRef
                = exteriorFaceRef.CreateLinkReference(
                    linkInstance);

            var wallLine = (targetWall.Location
                as LocationCurve).Curve as Line;

            var wallVector = (wallLine.GetEndPoint(1)
                              - wallLine.GetEndPoint(0)).Normalize();

            using var t = new Transaction(doc);
            t.Start("Add to face");

            doc.Create.NewFamilyInstance(
                linkToExteriorFaceRef, XYZ.Zero,
                wallVector, fs);

            t.Commit();
        }

        #endregion // AddFaceBasedFamilyToLinks

        #region Tag elements in linked documents

        /// <summary>
        ///     Tag all walls in all linked documents
        /// </summary>
        private void TagAllLinkedWalls(Document doc)
        {
            // Point near my wall
            var xyz = new XYZ(-20, 20, 0);

            // At first need to find our links
            var collector
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(RevitLinkInstance));

            foreach (var elem in collector)
            {
                // Get linkInstance
                var instance = elem
                    as RevitLinkInstance;

                // Get linkDocument
                var linkDoc = instance.GetLinkDocument();

                // Get linkType
                var type = doc.GetElement(
                    instance.GetTypeId()) as RevitLinkType;

                // Check if link is loaded
                if (RevitLinkType.IsLoaded(doc, type.Id))
                {
                    // Find walls for tagging
                    var walls
                        = new FilteredElementCollector(linkDoc)
                            .OfCategory(BuiltInCategory.OST_Walls)
                            .OfClass(typeof(Wall));

                    // Create reference
                    foreach (Wall wall in walls)
                    {
                        var newRef = new Reference(wall)
                            .CreateLinkReference(instance);

                        // Create transaction
                        using var tx = new Transaction(doc);
                        tx.Start("Create tags");

                        var newTag = IndependentTag.Create(
                            doc, doc.ActiveView.Id, newRef, true,
                            TagMode.TM_ADDBY_MATERIAL,
                            TagOrientation.Horizontal, xyz);

                        // Use TaggedElementId.LinkInstanceId and 
                        // TaggedElementId.LinkInstanceId to retrieve 
                        // the id of the tagged link and element:

                        //LinkElementId linkId = newTag.TaggedElementId; // 2021
                        var linkIds = newTag.GetTaggedElementIds(); // 2022
                        var linkInstanceId = linkIds.First().LinkInstanceId;
                        var linkedElementId = linkIds.First().LinkedElementId;

                        tx.Commit();
                    }
                }
            }
        }

        #endregion // Tag elements in linked documents

        #region Pick face on element in linked document

        private static IEnumerable<Document> GetLinkedDocuments(
            Document doc)
        {
            throw new NotImplementedException();
        }

        public static Face SelectFace(UIApplication uiapp)
        {
            var doc = uiapp.ActiveUIDocument.Document;

            var doc2 = GetLinkedDocuments(
                doc);

            var sel
                = uiapp.ActiveUIDocument.Selection;

            var pickedRef = sel.PickObject(
                ObjectType.PointOnElement,
                "Please select a Face");

            var elem = doc.GetElement(pickedRef.ElementId);

            var et = elem.GetType();

            if (typeof(RevitLinkType) == et
                || typeof(RevitLinkInstance) == et
                || typeof(Instance) == et)
            {
                foreach (var d in doc2)
                    if (elem.Name.Contains(d.Title))
                    {
                        var pickedRefInLink = pickedRef
                            .CreateReferenceInLink();

                        var myElement = d.GetElement(
                            pickedRefInLink.ElementId);

                        var myGeometryObject = myElement
                            .GetGeometryObjectFromReference(
                                pickedRefInLink) as Face;

                        return myGeometryObject;
                    }
            }
            else
            {
                var myElement = doc.GetElement(
                    pickedRef.ElementId);

                var myGeometryObject = myElement
                        .GetGeometryObjectFromReference(pickedRef)
                    as Face;

                return myGeometryObject;
            }

            return null;
        }

        #endregion // Pick face on element in linked document
    }
}