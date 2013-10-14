#region Header
//
// CmdRemoveImportedJpgs.cs - Remove imported JPG image files
//
// Copyright (C) 2012-2013 by Jeremy Tammik, Autodesk Inc. All rights reserved.
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
  [Transaction( TransactionMode.Manual )]
  class CmdRemoveImportedJpgs : IExternalCommand
  {
    /// <summary>
    /// Return true if the given 
    /// element name ends in ".jpg".
    /// </summary>
    bool ElementNameEndsWithJpg( Element e )
    {
      string s = e.Name;

      return 3 < s.Length && s.EndsWith( ".jpg" );
    }

    /// <summary>
    /// Return true if the given element name seems to 
    /// indicate an image file refrerence, i.e. ends in 
    /// ".jpg", ".jpeg", or ".bmp". This is of course 
    /// still no guarantee, a user may create elements 
    /// named like this just to shoot herself in her 
    /// foot, so beware!
    /// </summary>
    bool ElementNameMayIndicateImageFileReference( 
      Element e )
    {
      string s = e.Name.ToLower();

      return s.EndsWith( ".jpg" )
        || s.EndsWith( ".jpeg" )
        || s.EndsWith( ".bmp" );
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Document doc = uidoc.Document;

      FilteredElementCollector col
        = new FilteredElementCollector( doc )
          .WhereElementIsNotElementType();

      List<ElementId> ids = new List<ElementId>();

      foreach( Element e in col )
      {
        if( ElementNameMayIndicateImageFileReference( e ) )
        {
          Debug.Print( Util.ElementDescription( e ) );
          ids.Add( e.Id );
        }
      }

      ICollection<ElementId> idsDeleted = null;
      Transaction t;

      int n = ids.Count;

      if( 0 < n )
      {
        using( t = new Transaction( doc ) )
        {
          t.Start( "Delete non-ElementType '.jpg' elements" );

          idsDeleted = doc.Delete( ids );

          t.Commit();
        }
      }

      int m = ( null == idsDeleted )
        ? 0
        : idsDeleted.Count;

      Debug.Print( string.Format(
        "Selected {0} non-ElementType element{1}, "
        + "{2} successfully deleted.",
        n, Util.PluralSuffix( n ), m ) );

      col = new FilteredElementCollector( doc )
        .WhereElementIsElementType();

      ids.Clear();

      foreach( Element e in col )
      {
        if( ElementNameMayIndicateImageFileReference( e ) )
        {
          Debug.Print( Util.ElementDescription( e ) );
          ids.Add( e.Id );
        }
      }

      n = ids.Count;

      if( 0 < n )
      {
        using( t = new Transaction( doc ) )
        {
          t.Start( "Delete element type '.jpg' elements" );

          idsDeleted = doc.Delete( ids );

          t.Commit();
        }
      }

      m = ( null == idsDeleted ) ? 0 : idsDeleted.Count;

      Debug.Print( string.Format(
        "Selected {0} element type{1}, "
        + "{2} successfully deleted.",
        n, Util.PluralSuffix( n ), m ) );

      return Result.Succeeded;
    }
  }
}
