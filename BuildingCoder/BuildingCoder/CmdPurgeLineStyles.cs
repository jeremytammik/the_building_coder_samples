#region Header
//
// CmdPurgeLineStyles.cs - purge specific line styles
//
// Copyright (C) 2010-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
#endregion // Header

#region Namespaces
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Manual )]
  class CmdPurgeLineStyles : IExternalCommand
  {
    const string _line_style_name = "_Solid-Red-1";

    /// <summary>
    /// Purge all graphic styles whose name contains 
    /// the given substring. Watch out what you do!
    /// If your substring is empty, this might delete 
    /// all graphic styles in the entire project!
    /// </summary>
    void PurgeGraphicStyles( 
      Document doc, 
      string name_substring )
    {
      FilteredElementCollector graphic_styles
        = new FilteredElementCollector( doc )
          .OfClass( typeof( GraphicsStyle ) );

      int n1 = graphic_styles.Count<Element>();

      IEnumerable<Element> red_line_styles
        = graphic_styles.Where<Element>( e
          => e.Name.Contains( name_substring ) );

      int n2 = red_line_styles.Count<Element>();

      if( 0 < n2 )
      {
        using( Transaction tx = new Transaction( doc ) )
        {
          tx.Start( "Delete Line Styles" );

          doc.Delete( red_line_styles
            .Select<Element, ElementId>( e => e.Id )
            .ToArray<ElementId>() );

          tx.Commit();

          TaskDialog.Show( "Purge line styles",
            string.Format(
              "Deleted {0} graphic style{1} named '*{2}*' "
              + "from {3} total graohic styles.",
              n2, ( 1 == n2 ? "" : "s" ), 
              name_substring, n1 ) );
        }
      }
    }

    /// <summary>
    /// External command Execute method.
    /// </summary>
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      Document doc = uiapp.ActiveUIDocument.Document;

      PurgeGraphicStyles( doc, _line_style_name );

      return Result.Succeeded;
    }

    /// <summary>
    /// Revit macro mainline. 
    /// Uncomment the line referencing 'this'.
    /// </summary>
    public void PurgeLineStyles_macro_mainline()
    {
      Document doc = null; // in a macro, use this.Document
      string name = "_Solid-Red-1";
      PurgeGraphicStyles( doc, name );
    }
  }
}
