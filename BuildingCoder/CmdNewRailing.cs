#region Header

//
// CmdNewRailing.cs - insert a new railing instance,
// in response to queries from Berria at
// http://thebuildingcoder.typepad.com/blog/2009/02/list-railing-types.html#comments
//
// Copyright (C) 2009-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System;
using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    /// <summary>
    ///     Currently, it is not possible to create a new railing instance:
    ///     http://thebuildingcoder.typepad.com/blog/2009/02/list-railing-types.html#comments
    ///     SPR #134260 [API - New Element Creation: Railing]
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    internal class CmdNewRailing : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var doc = app.ActiveUIDocument.Document;

            var levels = Util.GetElementsOfType(
                doc, typeof(Level), BuiltInCategory.OST_Levels);

            if (levels.FirstElement() is not Level level)
            {
                message = "No level found.";
                return Result.Failed;
            }

            // get symbol to use:

            BuiltInCategory bic;
            Type t;

            // this retrieves the railing baluster symbols
            // but they cannot be used to create a railing:

            bic = BuiltInCategory.OST_StairsRailingBaluster;
            t = typeof(FamilySymbol);

            // this retrieves all railing symbols,
            // but they are just Symbol instances,
            // not FamilySymbol ones:

            bic = BuiltInCategory.OST_StairsRailing;
            t = typeof(ElementType);

            var symbols
                = Util.GetElementsOfType(doc, t, bic);

            FamilySymbol sym = null;

            foreach (ElementType s in symbols)
            {
                var fs = s as FamilySymbol;

                Debug.Print(
                    "Family name={0}, symbol name={1},"
                    + " category={2}",
                    null == fs ? "<none>" : fs.Family.Name,
                    s.Name,
                    s.Category.Name);

                if (null == sym && s is ElementType)
                    // this does not work, of course:
                    sym = s as FamilySymbol;
            }

            if (null == sym)
            {
                message = "No railing family symbols found.";
                return Result.Failed;
            }

            using var tx = new Transaction(doc);
            tx.Start("Create New Railing");

            var p1 = new XYZ(17, 0, 0);
            var p2 = new XYZ(33, 0, 0);
            var line = Line.CreateBound(p1, p2);

            // we need a FamilySymbol instance here, but only have a Symbol:

            var Railing1
                = doc.Create.NewFamilyInstance(
                    line, sym, level, StructuralType.NonStructural);

            tx.Commit();

            return Result.Succeeded;
        }
    }
}