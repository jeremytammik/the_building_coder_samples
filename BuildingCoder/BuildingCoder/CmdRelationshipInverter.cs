#region Header
//
// CmdRelationshipInverter.cs
//
// Determine door and window to wall relationships,
// i.e. hosted --> host, and invert it to obtain
// a map host --> list of hosted elements.
//
// Copyright (C) 2008-2013 by Jeremy Tammik,
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
  [Transaction( TransactionMode.ReadOnly )]
  class CmdRelationshipInverter : IExternalCommand
  {
    private Document m_doc;

    string ElementDescription( ElementId id )
    {
      Element e = m_doc.GetElement( id );
      return Util.ElementDescription( e );
    }

    /// <summary>
    /// From a list of openings, determine
    /// the wall hoisting each and return a mapping
    /// of element ids from host to all hosted.
    /// </summary>
    /// <param name="elements">Hosted elements</param>
    /// <returns>Map of element ids from host to
    /// hosted</returns>
    private Dictionary<ElementId, List<ElementId>>
      GetElementIds( FilteredElementCollector elements )
    {
      Dictionary<ElementId, List<ElementId>> dict =
        new Dictionary<ElementId, List<ElementId>>();

      string fmt = "{0} is hosted by {1}";

      foreach( FamilyInstance fi in elements )
      {
        ElementId id = fi.Id;
        ElementId idHost = fi.Host.Id;

        Debug.Print( fmt,
          Util.ElementDescription( fi ),
          ElementDescription( idHost ) );

        if( !dict.ContainsKey( idHost ) )
        {
          dict.Add( idHost, new List<ElementId>() );
        }
        dict[idHost].Add( id );
      }
      return dict;
    }

    private void DumpHostedElements(
      Dictionary<ElementId, List<ElementId>> ids )
    {
      foreach( ElementId idHost in ids.Keys )
      {
        string s = string.Empty;

        foreach( ElementId id in ids[idHost] )
        {
          if( 0 < s.Length )
          {
            s += ", ";
          }
          s += ElementDescription( id );
        }

        int n = ids[idHost].Count;

        Debug.Print(
          "{0} hosts {1} opening{2}: {3}",
          ElementDescription( idHost ),
          n, Util.PluralSuffix( n ), s );
      }
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      m_doc = app.ActiveUIDocument.Document;

      // filter for family instance and (door or window):

      ElementClassFilter fFamInstClass = new ElementClassFilter( typeof( FamilyInstance ) );
      ElementCategoryFilter fDoorCat = new ElementCategoryFilter( BuiltInCategory.OST_Doors );
      ElementCategoryFilter fWindowCat = new ElementCategoryFilter( BuiltInCategory.OST_Windows );
      LogicalOrFilter fCat = new LogicalOrFilter( fDoorCat, fWindowCat );
      LogicalAndFilter f = new LogicalAndFilter( fCat, fFamInstClass );
      FilteredElementCollector openings = new FilteredElementCollector( m_doc );
      openings.WherePasses( f );

      // map with key = host element id and
      // value = list of hosted element ids:

      Dictionary<ElementId, List<ElementId>> ids =
        GetElementIds( openings );

      DumpHostedElements( ids );
      m_doc = null;

      return Result.Succeeded;
    }
  }
}
