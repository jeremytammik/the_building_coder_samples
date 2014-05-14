#region Header
//
// CmdViewsShowingElements.cs - determine all views displaying a given set of elements
//
// By cshha, 
// http://forums.autodesk.com/t5/user/viewprofilepage/user-id/1162312
// published in 
// http://forums.autodesk.com/t5/Revit-API/Revision-help-which-views-show-this-object/m-p/5029772
//
// Copyright (C) 2014 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  /// <summary>
  /// Revit Document and IEnumerable<Element>
  /// extension methods.
  /// </summary>
  static class ExtensionMethods
  {
    /// <summary>
    /// Return an enumeration of all views in this
    /// document that can display elements at all.
    /// </summary>
    static IEnumerable<View>
      FindAllViewsThatCanDisplayElements(
        this Document doc )
    {
      ElementMulticlassFilter filter
        = new ElementMulticlassFilter(
          new List<Type> { 
            typeof( View3D ), 
            typeof( ViewPlan ), 
            typeof( ViewSection ) } );

      return new FilteredElementCollector( doc )
        .WherePasses( filter )
        .Cast<View>()
        .Where( v => !v.IsTemplate );
    }

    /// <summary>
    /// Return all views that display 
    /// any of the given elements.
    /// </summary>
    public static IEnumerable<View>
      FindAllViewsWhereAllElementsVisible(
        this IEnumerable<Element> elements )
    {
      if( null == elements )
      {
        throw new ArgumentNullException( "elements" );
      }

      //if( 0 == elements.Count )
      //{
      //  return new List<View>();
      //}

      Element e1 = elements.FirstOrDefault<Element>();

      if( null == e1 )
      {
        return new List<View>();
      }

      Document doc = e1.Document;

      IEnumerable<View> relevantViewList
        = doc.FindAllViewsThatCanDisplayElements();

      IEnumerable<ElementId> idsToCheck
        = ( from e in elements select e.Id );

      return (
        from v in relevantViewList
        let idList
          = new FilteredElementCollector( doc, v.Id )
            .WhereElementIsNotElementType()
            .ToElementIds()
        where !idsToCheck.Except( idList ).Any()
        select v );
    }
  }

  /// <summary>
  /// Determine all views displaying 
  /// a given set of elements.
  /// </summary>
  [Transaction( TransactionMode.ReadOnly )]
  class CmdViewsShowingElements : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData revit,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = revit.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Document doc = uidoc.Document;

      // Retrieve pre-selected elements.

      ICollection<ElementId> ids
        = uidoc.Selection.GetElementIds();

      if( 0 == ids.Count )
      {
        message = "Please pre-select some elements "
          + "before launching this command to list "
          + "the views displaying them.";

        return Result.Failed;
      }

      // Determine views displaying them.

      IEnumerable<Element> targets
        = from id in ids select doc.GetElement( id );

      IEnumerable<View> views = targets
        .FindAllViewsWhereAllElementsVisible();

      // Report results.

      string names = string.Join( ", ",
        ( from v in views select v.Name ) );

      int nElems = targets.Count<Element>();

      int nViews = names.Count<char>(
        c => ',' == c ) + 1;

      TaskDialog dlg = new TaskDialog( string.Format(
        "{0} element{1} are visible in {2} view{3}",
        nElems, Util.PluralSuffix( nElems ),
        nViews, Util.PluralSuffix( nViews ) ) );

      dlg.MainInstruction = names;

      dlg.Show();

      return Result.Succeeded;
    }
  }
}
