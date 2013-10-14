#region Header
//
// CmdDisallowJoin.cs - allow or disallow join at wall ends
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
  /// <summary>
  /// For case 1253888 [Allow Join / Disallow Join via Revit API].
  /// </summary>
  [Transaction( TransactionMode.Automatic )]
  class CmdDisallowJoin : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      Debug.Assert( false,
        "setting the disallow join property was not possible prior to Revit 2012. "
        + "In Revit 2012, you can use the WallUtils.DisallowWallJoinAtEnd method." );

      UIApplication app = commandData.Application;
      UIDocument uidoc = app.ActiveUIDocument;
      Document doc = uidoc.Document;

      string s = "a wall, to retrieve its join types";

      Wall wall = Util.SelectSingleElementOfType(
        uidoc, typeof( Wall ), s, false ) as Wall;

      if( null == wall )
      {
        message = "Please select a wall.";
      }
      else
      {
        JoinType [] a1 = ( JoinType [] ) Enum.GetValues( typeof( JoinType ) );
        List<JoinType> a = new List<JoinType>( (JoinType[]) Enum.GetValues( typeof( JoinType ) ) );
        int n = a.Count;

        LocationCurve lc = wall.Location as LocationCurve;

        s = Util.ElementDescription( wall ) + ":\n";

        /*for( int i = 0; i < 2; ++i )
        {
          JoinType jt = lc.get_JoinType( i );
          int j = a.IndexOf( jt ) + 1;
          JoinType jtnew = a[ j < n ? j : 0];
          lc.set_JoinType( j, jtnew );
          s += string.Format(
            "\nChanged join type at {0} from {1} to {2}.",
            ( 0 == i ? "start" : "end" ), jt, jtnew );
        }
        // wall.Location = lc; // Property or indexer 'Autodesk.Revit.Element.Location' cannot be assigned to -- it is read only
        */

        for( int i = 0; i < 2; ++i )
        {
          JoinType jt = ( (LocationCurve) wall.Location ).get_JoinType( i );
          int j = a.IndexOf( jt ) + 1;
          JoinType jtnew = a[j < n ? j : 0];
          ( (LocationCurve) wall.Location ).set_JoinType( j, jtnew );
          s += string.Format(
            "\nChanged join type at {0} from {1} to {2}.",
            ( 0 == i ? "start" : "end" ), jt, jtnew );
        }
      }
      Util.InfoMsg( s );
      return Result.Succeeded;
    }
  }
}
