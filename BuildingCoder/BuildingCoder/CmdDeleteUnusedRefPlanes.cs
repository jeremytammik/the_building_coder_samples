#region Header
//
// CmdDeleteUnusedRefPlanes.cs - delete unnamed non-hosting reference planes
//
// Copyright (C) 2014-2016 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
#endregion // Namespaces

namespace BuildingCoder
{
  /// <summary>
  /// Delete all reference planes that have not been 
  /// named and are not hosting any elements.
  /// In other words, check whether the reference 
  /// plane has been named.
  /// If not, check whether it hosts any elements.
  /// If not, delete it.
  /// Actually, to check whether it hosts any 
  /// elements, we delete it temporarily anyway, as
  /// described in
  /// Object Relationships http://thebuildingcoder.typepad.com/blog/2010/03/object-relationships.html
  /// Object Relationships in VB http://thebuildingcoder.typepad.com/blog/2010/03/object-relationships-in-vb.html
  /// Temporary Transaction Trick Touchup http://thebuildingcoder.typepad.com/blog/2012/11/temporary-transaction-trick-touchup.html
  /// The deletion returns the number of elements 
  /// deleted. If this number is greater than one (the 
  /// ref plane itself), it hosted something. In that 
  /// case, roll back the transaction and do not delete.
  /// </summary>
  [Transaction( TransactionMode.Manual )]
  class CmdDeleteUnusedRefPlanes : IExternalCommand
  {
    static int _i = 0;

    /// <summary>
    /// Delete the given reference plane 
    /// if it is not hosting anything.
    /// </summary>
    /// <returns>True if the given reference plane
    /// was in fact deleted, else false.</returns>
    bool DeleteIfNotHosting( ReferencePlane rp )
    {
      bool rc = false;

      Document doc = rp.Document;

      using( Transaction tx = new Transaction( doc ) )
      {
        tx.Start( "Delete ReferencePlane "
          + ( ++_i ).ToString() );

        // Deletion simply fails if the reference 
        // plane hosts anything. If so, the return 
        // value ids collection is null.
        // In Revit 2014, in that case, the call 
        // throws an exception "ArgumentException: 
        // ElementId cannot be deleted."

        try
        {
          ICollection<ElementId> ids = doc.Delete(
            rp.Id );

          tx.Commit();
          rc = true;
        }
        catch( ArgumentException )
        {
          tx.RollBack();
        }
      }
      return rc;
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Application app = uiapp.Application;
      Document doc = uidoc.Document;

      // Construct a parameter filter to get only 
      // unnamed reference planes, i.e. reference 
      // planes whose name equals the empty string:

      BuiltInParameter bip
        = BuiltInParameter.DATUM_TEXT;

      ParameterValueProvider provider
        = new ParameterValueProvider(
          new ElementId( bip ) );

      FilterStringRuleEvaluator evaluator
        = new FilterStringEquals();

      FilterStringRule rule = new FilterStringRule(
        provider, evaluator, "", false );

      ElementParameterFilter filter
        = new ElementParameterFilter( rule );

      FilteredElementCollector col
        = new FilteredElementCollector( doc )
          .OfClass( typeof( ReferencePlane ) )
          .WherePasses( filter );

      int n = 0;
      int nDeleted = 0;

      // No need to cast ... this is pretty nifty,
      // I find ... grab the elements as ReferencePlane
      // instances, since the filter guarantees that 
      // only ReferencePlane instances are selected.
      // In Revit 2014, this attempt to delete the 
      // reference planes while iterating over the 
      // filtered element collector throws an exception:
      // Autodesk.Revit.Exceptions.InvalidOperationException:
      // HResult=-2146233088
      // Message=The iterator cannot proceed due to 
      // changes made to the Element table in Revit's 
      // database (typically, This can be the result 
      // of an Element deletion).
      //
      //foreach( ReferencePlane rp in col )
      //{
      //  ++n;
      //  nDeleted += DeleteIfNotHosting( rp ) ? 1 : 0;
      //}

      ICollection<ElementId> ids = col.ToElementIds();

      n = ids.Count();

      if( 0 < n )
      {
        using( Transaction tx = new Transaction( doc ) )
        {
          tx.Start( string.Format(
            "Delete {0} ReferencePlane{1}",
            n, Util.PluralSuffix( n ) ) );

          // This also causes the exception "One or more of 
          // the elementIds cannot be deleted. Parameter 
          // name: elementIds
          //
          //ICollection<ElementId> ids2 = doc.Delete(
          //  ids );
          //nDeleted = ids2.Count();

          List<ElementId> ids2 = new List<ElementId>(
            ids );

          foreach( ElementId id in ids2 )
          {
            try
            {
              ICollection<ElementId> ids3 = doc.Delete(
                id );

              nDeleted += ids3.Count;
            }
            catch( Autodesk.Revit.Exceptions.ArgumentException )
            {
            }
          }

          tx.Commit();
        }
      }

      Util.InfoMsg( string.Format(
        "{0} unnamed reference plane{1} examined, "
        + "{2} element{3} in total were deleted.",
        n, Util.PluralSuffix( n ),
        nDeleted, Util.PluralSuffix( nDeleted ) ) );

      return Result.Succeeded;
    }
  }
}
