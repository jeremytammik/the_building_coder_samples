#region Header
//
// CmdDuplicateElement.cs - duplicate selected elements
//
// Copyright (C) 2010-2013 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System.Collections.Generic;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Manual )]
  class CmdDuplicateElements : IExternalCommand
  {
    #region Which target view
    /// <summary>
    /// Create a new group of the specified elements 
    /// in the current active view at the given offset.
    /// </summary>
    void CreateGroup( 
      Document doc,
      ICollection<ElementId> ids,
      XYZ offset )
    {
      Group group = doc.Create.NewGroup( ids );

      LocationPoint location = group.Location 
        as LocationPoint;

      XYZ p = location.Point + offset;

      Group newGroup = doc.Create.PlaceGroup( 
        p, group.GroupType );

      group.UngroupMembers();
    }
    #endregion // Which target view

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      UIDocument uidoc = app.ActiveUIDocument;
      Document doc = uidoc.Document;

      Transaction trans = new Transaction( doc,
        "Duplicate Elements" );

      trans.Start();

      //Group group = doc.Create.NewGroup( // 2012
      //  uidoc.Selection.Elements );

      Group group = doc.Create.NewGroup( // 2013
        uidoc.Selection.GetElementIds() );

      LocationPoint location = group.Location
        as LocationPoint;

      XYZ p = location.Point;
      XYZ newPoint = new XYZ( p.X, p.Y + 10, p.Z );

      Group newGroup = doc.Create.PlaceGroup(
        newPoint, group.GroupType );

      //group.Ungroup(); // 2012
      group.UngroupMembers(); // 2013

      //ElementSet eSet = newGroup.Ungroup(); // 2012

      ICollection<ElementId> eIds 
        = newGroup.UngroupMembers(); // 2013

      // change the property or parameter values
      // of the member elements as required...

      trans.Commit();

      return Result.Succeeded;
    }
  }
}
