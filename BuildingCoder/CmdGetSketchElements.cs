#region Header

//
// CmdGetSketchElements.cs - retrieve sketch elements for a selected wall, floor, roof, filled region, etc.
//
// Copyright (C) 2010-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
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
using Autodesk.Revit.UI.Selection;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdGetSketchElements : IExternalCommand
    {
        private const string _caption = "Retrieve Sketch Elements";

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var app = uiapp.Application;
            var doc = uidoc.Document;
            var sel = uidoc.Selection;

            var r = sel.PickObject(ObjectType.Element,
                "Please pick an element");

            // 'Autodesk.Revit.DB.Reference.Element' is
            // obsolete: Property will be removed. Use
            // Document.GetElement(Reference) instead.
            //Element e = r.Element; // 2011

            var e = doc.GetElement(r); // 2012

            var tx = new Transaction(doc);

            tx.Start(_caption);

            var ids = doc.Delete(e.Id);

            tx.RollBack();

            var showOnlySketchElements = true;

            /*
            StringBuilder s = new StringBuilder(
              _caption
              + " for host element "
              + Util.ElementDescription( e )
              + ": " );
      
            foreach( ElementId id in ids )
            {
              Element e = doc.GetElement( id );
      
              if( !showOnlySketchElements
                || e is Sketch
                || e is SketchPlane )
              {
                s.Append( Util.ElementDescription( e ) + ", " );
              }
            }
            */

            var a = new List<Element>(
                ids.Select(id => doc.GetElement(id)));

            var s = $"{_caption} for host element {Util.ElementDescription(e)}: ";

            s += string.Join(", ",
                a.Where(e2 => !showOnlySketchElements
                              || e2 is Sketch or SketchPlane)
                    .Select(e2 => Util.ElementDescription(e2))
                    .ToArray());

            Util.InfoMsg(s);

            return Result.Succeeded;
        }
    }
}