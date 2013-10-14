#region Header
//
// CmdPreprocessFailure.cs - suppress warning message by implementing the IFailuresPreprocessor interface
//
// Copyright (C) 2010-2013 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  /// <summary>
  /// Here is some code that is in the RevitAPI.chm
  /// as a snippet for IFailuresPreprocessor Interface).
  /// It creates an unbounded room and suppresses the
  /// warning ("Room is not in a properly enclosed region")
  /// that would otherwise be given.
  ///
  /// The duration for this implementation is only for
  /// the transaction in the external command, so after
  /// the command is executed manually placed unbounded
  /// rooms do result in the warning.
  ///
  /// However, it is also possible with the new failure
  /// API to suppress warnings for the entire Revit session.
  ///
  /// By Harry Mattison.
  /// </summary>
  [Transaction( TransactionMode.Manual )]
  class CmdPreprocessFailure : IExternalCommand
  {
    public class RoomWarningSwallower : IFailuresPreprocessor
    {
      public FailureProcessingResult PreprocessFailures(
        FailuresAccessor a )
      {
        // inside event handler, get all warnings

        IList<FailureMessageAccessor> failures
          = a.GetFailureMessages();

        foreach( FailureMessageAccessor f in failures )
        {
          // check failure definition ids
          // against ones to dismiss:

          FailureDefinitionId id
            = f.GetFailureDefinitionId();

          if( BuiltInFailures.RoomFailures.RoomNotEnclosed
            == id )
          {
            a.DeleteWarning( f );
          }
        }
        return FailureProcessingResult.Continue;
      }
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      Document doc = commandData.Application
        .ActiveUIDocument.Document;

      FilteredElementCollector collector
        = new FilteredElementCollector( doc );

      collector.OfClass( typeof( Level ) );
      Level level = collector.FirstElement() as Level;

      Transaction t = new Transaction( doc );

      t.Start( "Create unbounded room" );

      FailureHandlingOptions failOpt
        = t.GetFailureHandlingOptions();

      failOpt.SetFailuresPreprocessor(
        new RoomWarningSwallower() );

      t.SetFailureHandlingOptions( failOpt );

      doc.Create.NewRoom( level, new UV( 0, 0 ) );

      t.Commit();

      return Result.Succeeded;
    }
  }
}
