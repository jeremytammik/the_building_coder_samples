#region Header

//
// CmdListMarks.cs - list all door marks
//
// Copyright (C) 2009-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    internal class CmdListMarks : IExternalCommand
    {
        private const string _the_answer = "42";
        private static readonly bool _modify_existing_marks = true;

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;
            var doc = uidoc.Document;

            //Autodesk.Revit.Creation.Application creApp = app.Application.Create;
            //Autodesk.Revit.Creation.Document creDoc = doc.Create;

            var doors
                = Util.GetElementsOfType(doc,
                    typeof(FamilyInstance),
                    BuiltInCategory.OST_Doors);

            var n = doors.Count();

            Debug.Print("{0} door{1} found.",
                n, Util.PluralSuffix(n));

            if (0 < n)
            {
                var marks
                    = new Dictionary<string, List<Element>>();

                foreach (FamilyInstance door in doors)
                {
                    var mark = door.get_Parameter(
                            BuiltInParameter.ALL_MODEL_MARK)
                        .AsString();

                    if (!marks.ContainsKey(mark)) marks.Add(mark, new List<Element>());
                    marks[mark].Add(door);
                }

                var keys = new List<string>(
                    marks.Keys);

                keys.Sort();

                n = keys.Count;

                Debug.Print("{0} door mark{1} found{2}",
                    n, Util.PluralSuffix(n),
                    Util.DotOrColon(n));

                foreach (var mark in keys)
                {
                    n = marks[mark].Count;

                    Debug.Print("  {0}: {1} door{2}",
                        mark, n, Util.PluralSuffix(n));
                }
            }

            n = 0; // count how many elements are modified

            if (_modify_existing_marks)
            {
                using var tx = new Transaction(doc);
                tx.Start("Modify Existing Door Marks");

                //ElementSet els = uidoc.Selection.Elements; // 2014

                var ids = uidoc.Selection
                    .GetElementIds(); // 2015

                //foreach( Element e in els ) // 2014

                foreach (var id in ids) // 2015
                {
                    var e = doc.GetElement(id); // 2015

                    if (e is FamilyInstance
                        && null != e.Category
                        && (int) BuiltInCategory.OST_Doors
                        == e.Category.Id.IntegerValue)
                    {
                        e.get_Parameter(
                                BuiltInParameter.ALL_MODEL_MARK)
                            .Set(_the_answer);

                        ++n;
                    }
                }

                tx.Commit();
            }

            // return Succeeded only if we wish to commit
            // the transaction to modify the database:
            //
            //return 0 < n
            //  ? Result.Succeeded
            //  : Result.Failed;
            //
            // That was only useful before the introduction
            // of the manual and read-only transaction modes.

            return Result.Succeeded;
        }

        /*
        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements )
        {
          UIApplication app = commandData.Application;
          Document doc = app.ActiveUIDocument.Document;
    
          Element e;
          int num = 1;
          ElementIterator it = doc.Elements;
          while( it.MoveNext() )
          {
            e = it.Current as Element;
            try
            {
              // get the BuiltInParameter.ALL_MODEL_MARK paremeter.
              // If the element does not have this paremeter,
              // get_Parameter method returns null:
    
              Parameter p = e.get_Parameter(
                BuiltInParameter.ALL_MODEL_MARK );
    
              if( p != null )
              {
                // we found an element with the
                // BuiltInParameter.ALL_MODEL_MARK
                // parameter. Change the value and
                // increment our value:
    
                p.Set( num.ToString() );
                ++num;
              }
            }
            catch( Exception ex )
            {
              Util.ErrorMsg( "Exception: " + ex.Message );
            }
          }
          doc.EndTransaction();
          return Result.Succeeded;
        }
    
        /// <summary>
        /// Retrieve all elements in the current active document
        /// having a non-empty value for the given parameter.
        /// </summary>
        static int GetElementsWithParameter(
          List<Element> elements,
          BuiltInParameter bip,
          Application app )
        {
          Document doc = app.ActiveUIDocument.Document;
    
          Autodesk.Revit.Creation.Application a
            = app.Create;
    
          Filter f = a.Filter.NewParameterFilter(
            bip, CriteriaFilterType.NotEqual, "" );
    
          return doc.get_Elements( f, elements );
        }
        */

        /// <summary>
        ///     Set the 'Mark' parameter value to sequential
        ///     numbers on all structural framing elements.
        ///     https://forums.autodesk.com/t5/revit-api-forum/set-different-value-to-a-set-of-elements/td-p/8004141
        /// </summary>
        private void NumberStructuralFraming(Document doc)
        {
            var beams
                = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_StructuralFraming)
                    .WhereElementIsNotElementType();

            using var t = new Transaction(doc);
            t.Start("Renumber marks");

            var mark_number = 3;

            foreach (FamilyInstance beam in beams)
            {
                var p = beam.get_Parameter(
                    BuiltInParameter.ALL_MODEL_MARK);

                p.Set((mark_number++).ToString());
            }

            t.Commit();
        }
    }
}