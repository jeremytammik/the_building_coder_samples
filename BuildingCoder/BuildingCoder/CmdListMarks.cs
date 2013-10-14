#region Header
//
// CmdListMarks.cs - list all door marks
//
// Copyright (C) 2009-2013 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Automatic )]
  class CmdListMarks : IExternalCommand
  {
    static bool _modify_existing_marks = true;
    const string _the_answer = "42";

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      UIDocument uidoc = app.ActiveUIDocument;
      Document doc = uidoc.Document;

      //Autodesk.Revit.Creation.Application creApp = app.Application.Create;
      //Autodesk.Revit.Creation.Document creDoc = doc.Create;

      IList<Element> doors = Util.GetElementsOfType(
        doc, typeof( FamilyInstance ), BuiltInCategory.OST_Doors ).ToElements();

      int n = doors.Count;

      Debug.Print( "{0} door{1} found.",
        n, Util.PluralSuffix( n ) );

      if( 0 < n )
      {
        Dictionary<string, List<Element>> marks
          = new Dictionary<string, List<Element>>();

        foreach( FamilyInstance door in doors )
        {
          string mark = door.get_Parameter(
            BuiltInParameter.ALL_MODEL_MARK )
            .AsString();

          if( !marks.ContainsKey( mark ) )
          {
            marks.Add( mark, new List<Element>() );
          }
          marks[mark].Add( door );
        }

        List<string> keys = new List<string>(
          marks.Keys );

        keys.Sort();

        n = keys.Count;

        Debug.Print( "{0} door mark{1} found{2}",
          n, Util.PluralSuffix( n ),
          Util.DotOrColon( n ) );

        foreach( string mark in keys )
        {
          n = marks[mark].Count;

          Debug.Print( "  {0}: {1} door{2}",
            mark, n, Util.PluralSuffix( n ) );
        }
      }

      n = 0; // count how many elements are modified

      if( _modify_existing_marks )
      {
        ElementSet els = uidoc.Selection.Elements;

        foreach( Element e in els )
        {
          if( e is FamilyInstance
            && null != e.Category
            && (int) BuiltInCategory.OST_Doors
              == e.Category.Id.IntegerValue )
          {
            e.get_Parameter(
              BuiltInParameter.ALL_MODEL_MARK )
              .Set( _the_answer );

            ++n;
          }
        }
      }

      // return Succeeded only if we wish to commit
      // the transaction to modify the database:

      return 0 < n
        ? Result.Succeeded
        : Result.Failed;
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

  }
}
