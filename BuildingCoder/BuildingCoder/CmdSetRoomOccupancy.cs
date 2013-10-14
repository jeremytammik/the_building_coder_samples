#region Header
//
// CmdSetRoomOccupancy.cs - read and set room occupancy
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
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
//using Autodesk.Revit.Collections;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Automatic )]
  class CmdSetRoomOccupancy : IExternalCommand
  {
    static char[] _digits = null;

    /// <summary>
    /// Analyse the given string.
    /// If it ends in a sequence of digits representing a number,
    /// return a string with the number oincremented by one.
    /// Otherwise, return a string with a suffix "1" appended.
    /// </summary>
    static string BumpStringSuffix( string s )
    {
      if( null == s || 0 == s.Length )
      {
        return "1";
      }
      if( null == _digits )
      {
        _digits = new char[] {
          '0', '1', '2', '3', '4',
          '5', '6', '7', '8', '9'
        };
      }
      int n = s.Length;
      string t = s.TrimEnd( _digits );
      if( t.Length == n )
      {
        t += "1";
      }
      else
      {
        n = t.Length;
        n = int.Parse( s.Substring( n ) );
        ++n;
        t += n.ToString();
      }
      return t;
    }

    /// <summary>
    /// Read the value of the element ROOM_OCCUPANCY parameter.
    /// If it ends in a number, increment the number, else append "1".
    /// </summary>
    static void BumpOccupancy( Element e )
    {
      Parameter p = e.get_Parameter(
        BuiltInParameter.ROOM_OCCUPANCY );

      if( null == p )
      {
        Debug.Print(
          "{0} has no room occupancy parameter.",
          Util.ElementDescription( e ) );
      }
      else
      {
        string occupancy = p.AsString();

        string newOccupancy = BumpStringSuffix(
          occupancy );

        p.Set( newOccupancy );
      }
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      UIDocument uidoc = app.ActiveUIDocument;
      Document doc = uidoc.Document;

      List<Element> rooms = new List<Element>();
      if( !Util.GetSelectedElementsOrAll(
        rooms, uidoc, typeof( Room ) ) )
      {
        Selection sel = uidoc.Selection;
        message = ( 0 < sel.Elements.Size )
          ? "Please select some room elements."
          : "No room elements found.";
        return Result.Failed;
      }
      foreach( Room room in rooms )
      {
        BumpOccupancy( room );
      }
      return Result.Succeeded;
    }
  }
}
