#region Header
//
// CmdPurgeTextNoteTypes.cs - purge TextNote types, i.e. delete all unused TextNote type instances
//
// Copyright (C) 2010-2013 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Manual )]
  class CmdPurgeTextNoteTypes : IExternalCommand
  {
    /// <summary>
    /// Return all unused text note types by collecting all
    /// existing types in the document and removing the
    /// ones that are used afterwards.
    /// </summary>
    ICollection<ElementId> GetUnusedTextNoteTypes(
      Document doc )
    {
      FilteredElementCollector collector
        = new FilteredElementCollector( doc );

      ICollection<ElementId> textNoteTypes
        = collector.OfClass( typeof( TextNoteType ) )
          .ToElementIds()
          .ToList();

      FilteredElementCollector textNotes
        = new FilteredElementCollector( doc )
          .OfClass( typeof( TextNote ) );

      foreach( TextNote textNote in textNotes )
      {
        bool removed = textNoteTypes.Remove(
          textNote.TextNoteType.Id );
      }
      return textNoteTypes;
    }

    /// <summary>
    /// Return all unused text note types by first
    /// determining all text note types in use and
    /// then collecting all the others using an
    /// exclusion filter.
    /// </summary>
    ICollection<ElementId>
      GetUnusedTextNoteTypesExcluding(
        Document doc )
    {
      ICollection<ElementId> usedTextNotesTypeIds
        = new Collection<ElementId>();

      FilteredElementCollector textNotes
        = new FilteredElementCollector( doc )
          .OfClass( typeof( TextNote ) );

      foreach( TextNote textNote in textNotes )
      {
        usedTextNotesTypeIds.Add(
          textNote.TextNoteType.Id );
      }

      FilteredElementCollector unusedTypeCollector
        = new FilteredElementCollector( doc )
          .OfClass( typeof( TextNoteType ) );

      if( 0 < usedTextNotesTypeIds.Count )
      {
        unusedTypeCollector.Excluding(
          usedTextNotesTypeIds );
      }

      ICollection<ElementId> unusedTypes
        = unusedTypeCollector.ToElementIds();

      return unusedTypes;
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      Document doc = app.ActiveUIDocument.Document;

      ICollection<ElementId> unusedTextNoteTypes
        = GetUnusedTextNoteTypes( doc );

      int n = unusedTextNoteTypes.Count;

      int nLoop = 100;

      Stopwatch sw = new Stopwatch();

      sw.Reset();
      sw.Start();

      for( int i = 0; i < nLoop; ++ i )
      {
        unusedTextNoteTypes
          = GetUnusedTextNoteTypes( doc );

        Debug.Assert( unusedTextNoteTypes.Count == n,
          "expected same number of unused texct note types" );
      }

      sw.Stop();
      double ms = (double) sw.ElapsedMilliseconds
        / (double) nLoop;

      sw.Reset();
      sw.Start();

      for( int i = 0; i < nLoop; ++ i )
      {
        unusedTextNoteTypes
          = GetUnusedTextNoteTypesExcluding( doc );

        Debug.Assert( unusedTextNoteTypes.Count == n,
          "expected same number of unused texct note types" );
      }

      sw.Stop();
      double msExcluding
        = (double) sw.ElapsedMilliseconds
          / (double) nLoop;

      Transaction t = new Transaction( doc,
        "Purging unused text note types" );

      t.Start();

      sw.Reset();
      sw.Start();

      doc.Delete( unusedTextNoteTypes );

      sw.Stop();
      double msDeleting
        = (double) sw.ElapsedMilliseconds
          / (double) nLoop;

      t.Commit();

      Util.InfoMsg( string.Format(
        "{0} text note type{1} purged. "
        + "{2} ms to collect, {3} ms to collect "
        + "excluding, {4} ms to delete.",
        n, Util.PluralSuffix( n ),
        ms, msExcluding, msDeleting ) );

      return Result.Succeeded;
    }
  }
}
