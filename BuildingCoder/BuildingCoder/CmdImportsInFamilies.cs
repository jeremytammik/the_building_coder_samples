#region Header
//
// CmdImportsInFamilies.cs - list all families
// used in current project containing imported
// CAD files.
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
  class CmdImportsInFamilies : IExternalCommand
  {
    #region First version to list import instances non-recursively
    /// <summary>
    /// Non-recursively list all import instances
    /// in all families used in the current project document.
    /// </summary>
    public Result ExecuteWithoutRecursion(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements)
    {
      UIApplication app = commandData.Application;
      Document doc = app.ActiveUIDocument.Document;

      FilteredElementCollector instances
        = new FilteredElementCollector( doc );

      instances.OfClass( typeof( FamilyInstance ) );

      Dictionary<string, Family> families
        = new Dictionary<string, Family>();

      foreach ( FamilyInstance i in instances )
      {
        Family family = i.Symbol.Family;
        if ( !families.ContainsKey( family.Name ) )
        {
          families[family.Name] = family;
        }
      }

      List<string> keys = new List<string>(
        families.Keys );

      keys.Sort();

      foreach ( string key in keys )
      {
        Family family = families[key];
        if ( family.IsInPlace )
        {
          Debug.Print( "Family '{0}' is in-place.", key );
        }
        else
        {
          Document fdoc = doc.EditFamily( family );

          FilteredElementCollector c
            = new FilteredElementCollector( doc );

          c.OfClass( typeof( ImportInstance ) );

          IList<Element> imports = c.ToElements();

          int n = imports.Count;

          Debug.Print(
            "Family '{0}' contains {1} import instance{2}{3}",
            key, n, Util.PluralSuffix( n ),
            Util.DotOrColon( n ) );

          if ( 0 < n )
          {
            foreach ( ImportInstance i in imports )
            {
              //string name = i.ObjectType.Name; // 2011
              string name = doc.GetElement( i.GetTypeId() ).Name; // 2012

              Debug.Print( "  '{0}'", name );
            }
          }
        }
      }
      return Result.Failed;
    }
    #endregion // First version to list import instances non-recursively

    /// <summary>
    /// Retrieve all families used by the family instances
    /// and annotation symbols in the given document.
    /// Return a dictionary mapping the family name
    /// to the corresponding family object.
    /// </summary>
    Dictionary<string, Family> GetFamilies( Document doc )
    {
      Dictionary<string, Family> families
        = new Dictionary<string, Family>();

      // collect all family instances and determine their families:

      FilteredElementCollector instances
        = new FilteredElementCollector( doc );

      instances.OfClass( typeof( FamilyInstance ) );

      foreach ( FamilyInstance i in instances )
      {
        Family family = i.Symbol.Family;
        if ( !families.ContainsKey( family.Name ) )
        {
          families[family.Name] = family;
        }
      }


      // collect all annotation symbols and determine their families:

      FilteredElementCollector annotations
        = new FilteredElementCollector( doc );

      annotations.OfClass( typeof( AnnotationSymbol ) );

      foreach ( AnnotationSymbol a in annotations )
      {
        //Family family = a.AsFamilyInstance.Symbol.Family; // 2011
        Family family = a.Symbol.Family; // 2012

        if ( !families.ContainsKey( family.Name ) )
        {
          families[family.Name] = family;
        }
      }
      return families;
    }

    /// <summary>
    /// List all import instances in all the given families.
    /// Retrieve nested families and recursively search in these as well.
    /// </summary>
    void ListImportsAndSearchForMore(
      int recursionLevel,
      Document doc,
      Dictionary<string, Family> families )
    {
      string indent
        = new string( ' ', 2 * recursionLevel );

      List<string> keys = new List<string>(
        families.Keys );

      keys.Sort();

      foreach ( string key in keys )
      {
        Family family = families[key];

        if ( family.IsInPlace )
        {
          Debug.Print( indent
            + "Family '{0}' is in-place.",
            key );
        }
        else
        {
          Document fdoc = doc.EditFamily( family );

          FilteredElementCollector c
            = new FilteredElementCollector( doc );

          c.OfClass( typeof( ImportInstance ) );

          IList<Element> imports = c.ToElements();

          int n = imports.Count;

          Debug.Print( indent
            + "Family '{0}' contains {1} import instance{2}{3}",
            key, n, Util.PluralSuffix( n ),
            Util.DotOrColon( n ) );

          if ( 0 < n )
          {
            foreach ( ImportInstance i in imports )
            {
              string s = i.Pinned ? "" : "not ";

              //string name = i.ObjectType.Name; // 2011
              string name = doc.GetElement( i.GetTypeId() ).Name; // 2012

              Debug.Print( indent
                + "  '{0}' {1}pinned",
                name, s );

              i.Pinned = !i.Pinned;
            }
          }

          Dictionary<string, Family> nestedFamilies
            = GetFamilies( fdoc );

          ListImportsAndSearchForMore(
            recursionLevel + 1, fdoc, nestedFamilies );
        }
      }
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      Document doc = app.ActiveUIDocument.Document;

      Dictionary<string, Family> families
        = GetFamilies( doc );

      ListImportsAndSearchForMore( 0, doc, families );

      return Result.Failed;
    }
  }
}
