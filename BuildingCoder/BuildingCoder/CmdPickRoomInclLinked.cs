#region Header
//
// CmdPickRoomInclLinked.cs - selection filter to pick a room either in current project or linked model
//
// Copyright (C) 2020-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
#endregion // Header

#region Namespaces
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Manual )]
  public class CmdPickRoomInclLinked : IExternalCommand
  {
    public class ElementInLinkSelectionFilter<T> : ISelectionFilter where T : Element
    {
      private Document _doc;

      public ElementInLinkSelectionFilter( Document doc )
      {
        _doc = doc;
      }

      public Document LinkedDocument { get; private set; } = null;

      public bool LastCheckedWasFromLink
      {
        get { return null != LinkedDocument; }
      }

      public bool AllowElement( Element e )
      {
        return true;
      }

      public bool AllowReference( Reference r, XYZ p )
      {
        LinkedDocument = null;

        Element e = _doc.GetElement( r );

        if( e is RevitLinkInstance )
        {
          RevitLinkInstance li = e as RevitLinkInstance;

          LinkedDocument = li.GetLinkDocument();

          e = LinkedDocument.GetElement( r.LinkedElementId );
        }
        return e is T;
      }
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Document doc = uidoc.Document;
      Reference r;

      ElementInLinkSelectionFilter<Room> filter
        = new ElementInLinkSelectionFilter<Room>(
          doc );

      try
      {
        r = uidoc.Selection.PickObject(
          ObjectType.PointOnElement,
          filter,
          "Please pick a room in current project or linked model" );
      }
      catch( Autodesk.Revit.Exceptions.OperationCanceledException )
      {
        return Result.Cancelled;
      }

      Element e;

      if( filter.LastCheckedWasFromLink )
      {
        e = filter.LinkedDocument.GetElement(
          r.LinkedElementId );
      }
      else
      {
        e = doc.GetElement( r );
      }

      TaskDialog.Show( "Picked", e.Name );

      return Result.Succeeded;
    }
  }
}
