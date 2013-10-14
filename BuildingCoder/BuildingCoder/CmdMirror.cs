#region Header
//
// CmdMirror.cs - mirror some elements.
//
// Copyright (C) 2009-2013 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
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
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Automatic )]
  class CmdMirror : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;

      Application app = uiapp.Application;
      Document doc = uidoc.Document;

      // 'Autodesk.Revit.DB.Document.Mirror(ElementSet, Line)' is obsolete:
      // Use one of the replace methods in ElementTransformUtils.
      //
      //Line line = app.Create.NewLine(
      //  XYZ.Zero, XYZ.BasisX, true ); // 2011
      //
      //ElementSet els = uidoc.Selection.Elements; // 2011
      //
      //doc.Mirror( els, line ); // 2011

      Plane plane = new Plane( XYZ.BasisY, XYZ.Zero ); // 2012

      ICollection<ElementId> elementIds
        = uidoc.Selection.GetElementIds(); // 2012

      ElementTransformUtils.MirrorElements(
        doc, elementIds, plane ); // 2012

      return Result.Succeeded;
    }
  }

  [Transaction( TransactionMode.Automatic )]
  class CmdMirrorListAdded : IExternalCommand
  {
    string _msg = "The following {0} element{1} were mirrored:\r\n";

    void Report( FilteredElementCollector a )
    {
      int n = 0;
      string s = _msg;

      foreach( Element e in a )
      {
        ++n;
        s += string.Format( "\r\n  {0}",
          Util.ElementDescription( e ) );
      }
      s = string.Format( s, n, Util.PluralSuffix( n ) );

      Util.InfoMsg( s );
    }

    /// <summary>
    /// Return all elements that are not ElementType objects.
    /// </summary>
    FilteredElementCollector GetElements( Document doc )
    {
      FilteredElementCollector collector
        = new FilteredElementCollector( doc );
      return collector.WhereElementIsNotElementType();
    }

    /// <summary>
    /// Return the current number of non-ElementType elements.
    /// </summary>
    int GetElementCount( Document doc )
    {
      return GetElements( doc ).ToElements().Count;
    }

    /// <summary>
    /// Return all database elements after the given number n.
    /// </summary>
    List<Element> GetElementsAfter( int n, Document doc )
    {
      List<Element> a = new List<Element>();
      FilteredElementCollector c = GetElements( doc );
      int i = 0;

      foreach( Element e in c )
      {
        ++i;

        if( n < i )
        {
          a.Add( e );
        }
      }
      return a;
    }

    /// <summary>
    /// Return all elements in the entire document
    /// whose element id is greater than 'lastId'.
    /// </summary>
    FilteredElementCollector GetElementsAfter(
      Document doc,
      ElementId lastId )
    {
      BuiltInParameter bip = BuiltInParameter.ID_PARAM;

      ParameterValueProvider provider
        = new ParameterValueProvider(
          new ElementId( bip ) );

      FilterNumericRuleEvaluator evaluator
        = new FilterNumericGreater();

      FilterRule rule = new FilterElementIdRule(
        provider, evaluator, lastId );

      ElementParameterFilter filter
        = new ElementParameterFilter( rule );

      FilteredElementCollector collector
        = new FilteredElementCollector( doc );

      return collector.WherePasses( filter );
    }

    /// <summary>
    /// Return all elements from the given collector
    /// whose element id is greater than 'lastId'.
    /// </summary>
    FilteredElementCollector GetElementsAfter(
      FilteredElementCollector input,
      ElementId lastId )
    {
      BuiltInParameter bip = BuiltInParameter.ID_PARAM;

      ParameterValueProvider provider
        = new ParameterValueProvider(
          new ElementId( bip ) );

      FilterNumericRuleEvaluator evaluator
        = new FilterNumericGreater();

      FilterRule rule = new FilterElementIdRule(
        provider, evaluator, lastId );

      ElementParameterFilter filter
        = new ElementParameterFilter( rule );

      return input.WherePasses( filter );
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

      //Line line = app.Create.NewLine(
      //  XYZ.Zero, XYZ.BasisX, true ); // 2011

      //ElementSet els = uidoc.Selection.Elements; // 2011

      Plane plane = new Plane( XYZ.BasisY, XYZ.Zero ); // 2012

      ICollection<ElementId> elementIds
        = uidoc.Selection.GetElementIds(); // 2012

      using( SubTransaction t = new SubTransaction( doc ) )
      {
        // determine newly added elements relying on the
        // element sequence as returned by the filtered collector.
        // this approach works in both Revit 2010 and 2011:

        t.Start();

        int n = GetElementCount( doc );

        //doc.Mirror( els, line ); // 2011

        ElementTransformUtils.MirrorElements(
          doc, elementIds, plane ); // 2012

        List<Element> a = GetElementsAfter( n, doc );


        t.RollBack();
      }

      using( SubTransaction t = new SubTransaction( doc ) )
      {
        // here is an idea for a new approach in 2011:
        // determine newly added elements relying on
        // monotonously increasing element id values:

        t.Start();

        FilteredElementCollector a = GetElements( doc );
        int i = a.Max<Element>( e => e.Id.IntegerValue );
        ElementId maxId = new ElementId( i );

        // doc.Mirror( els, line ); // 2011

        ElementTransformUtils.MirrorElements(
          doc, elementIds, plane ); // 2012

        // get all elements in document with an
        // element id greater than maxId:

        a = GetElementsAfter( doc, maxId );

        Report( a );

        t.RollBack();
      }

      using( SubTransaction t = new SubTransaction( doc ) )
      {
        // similar to the above approach relying on
        // monotonously increasing element id values,
        // but apply a quick filter first:

        t.Start();

        FilteredElementCollector a = GetElements( doc );
        int i = a.Max<Element>( e => e.Id.IntegerValue );
        ElementId maxId = new ElementId( i );

        //doc.Mirror( els, line ); // 2011

        ElementTransformUtils.MirrorElements(
          doc, elementIds, plane ); // 2012

        // only look at non-ElementType elements
        // instead of all document elements:

        a = GetElements( doc );
        a = GetElementsAfter( a, maxId );

        Report( a );

        t.RollBack();
      }

      using( SubTransaction t = new SubTransaction( doc ) )
      {
        // use a local and temporary DocumentChanged event
        // handler to directly obtain a list of all newly
        // created elements.
        // unfortunately, this canot be tested in this isolated form,
        // since the DocumentChanged event is only triggered when the
        // real outermost Revit transaction is committed, i.e. our
        // local sub-transaction makes no difference. since we abort
        // the sub-transaction before the command terminates and no
        // elements are really added to the database, our event
        // handler is never called:

        t.Start();

        app.DocumentChanged
          += new EventHandler<DocumentChangedEventArgs>(
            app_DocumentChanged );

        //doc.Mirror( els, line ); // 2011

        ElementTransformUtils.MirrorElements(
          doc, elementIds, plane ); // 2012

        app.DocumentChanged
          -= new EventHandler<DocumentChangedEventArgs>(
            app_DocumentChanged );

        Debug.Assert( null == _addedElementIds,
          "never expected the event handler to be called" );

        if( null != _addedElementIds )
        {
          int n = _addedElementIds.Count;

          string s = string.Format( _msg, n,
            Util.PluralSuffix( n ) );

          foreach( ElementId id in _addedElementIds )
          {
            Element e = doc.GetElement( id );

            s += string.Format( "\r\n  {0}",
              Util.ElementDescription( e ) );
          }
          Util.InfoMsg( s );
        }

        t.RollBack();
      }
      return Result.Succeeded;
    }

    static List<ElementId> _addedElementIds = null;

    void app_DocumentChanged(
      object sender,
      DocumentChangedEventArgs e )
    {
      if( null == _addedElementIds )
      {
        _addedElementIds = new List<ElementId>();
      }

      _addedElementIds.Clear();
      _addedElementIds.AddRange(
        e.GetAddedElementIds() );
    }
  }
}
