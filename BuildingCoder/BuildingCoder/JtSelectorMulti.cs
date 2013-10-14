#region Namespaces
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Diagnostics;
#endregion

namespace BuildingCoder
{
  /// <summary>
  /// Select multiple elements of the same type using
  /// either pre-selection, before launching the 
  /// command, or post-selection, afterwards.
  /// The element type is determined by the template 
  /// parameter. A filtering method must be provided
  /// and is reused for both testing the pre-selection
  /// and defining allowable elements for the post-
  /// selection.
  /// </summary>
  class JtSelectorMulti<T> where T : Element
  {
    /// <summary>
    /// Error message in case of invalid pre-selection.
    /// </summary>
    const string _usage_error = "Please pre-select "
      + "only {0}s before launching this command.";

    /// <summary>
    /// Determine whether the given element is a valid 
    /// selectable object. The method passed in is 
    /// reused for both the interactive selection
    /// filter and the pre-selection validation.
    /// See below for a sample method.
    /// </summary>
    public delegate bool IsSelectable( Element e );

    #region Sample common filtering helper method
    /// <summary>
    /// Determine whether the given element is valid.
    /// This specific implementation requires a family 
    /// instance element of the furniture category 
    /// belonging to the named family.
    /// </summary>
    static public bool IsTable( Element e )
    {
      bool rc = false;

      Category cat = e.Category;

      if( null != cat )
      {
        if( cat.Id.IntegerValue.Equals(
          (int) BuiltInCategory.OST_Furniture ) )
        {
          FamilyInstance fi = e as FamilyInstance;

          if( null != fi )
          {
            string fname = fi.Symbol.Family.Name;

            rc = fname.Equals( "SampleTableFamilyName" );
          }
        }
      }
      return rc;
    }
    #endregion // Common filtering helper method

    #region JtSelectionFilter
    class JtSelectionFilter : ISelectionFilter
    {
      Type _t;
      BuiltInCategory? _bic;
      IsSelectable _f;

      public JtSelectionFilter(
        Type t,
        BuiltInCategory? bic,
        IsSelectable f )
      {
        _t = t;
        _bic = bic;
        _f = f;
      }

      bool HasBic( Element e )
      {
        return null == _bic
          || ( null != e.Category
            && e.Category.Id.IntegerValue.Equals(
              (int) _bic ) );
      }

      public bool AllowElement( Element e )
      {
        return e is T
          && HasBic( e )
          && _f( e );
      }

      public bool AllowReference( Reference r, XYZ p )
      {
        return true;
      }
    }
    #endregion // JtSelectionFilter

    List<T> _selected;
    string _msg;
    Result _result;

    /// <summary>
    /// Instantiate and run element selector.
    /// </summary>
    /// <param name="uidoc">UIDocument.</param>
    /// <param name="bic">Built-in category or null.</param>
    /// <param name="description">Description of the elements to select.</param>
    /// <param name="f">Validation method.</param>
    public JtSelectorMulti(
      UIDocument uidoc,
      BuiltInCategory? bic,
      string description,
      IsSelectable f )
    {
      _selected = null;
      _msg = null;

      Document doc = uidoc.Document;

      if( null == doc )
      {
        _msg = "Please run this command in a valid"
          + " Revit project document.";
        _result = Result.Failed;
      }

      // Check for pre-selected elements

      Selection sel = uidoc.Selection;

      int n = sel.Elements.Size;

      if( 0 < n )
      {
        //if( 1 != n )
        //{
        //  _msg = _usage_error;
        //  _result = Result.Failed;
        //}

        foreach( Element e in sel.Elements )
        {
          if( !f( e ) )
          {
            _msg = string.Format(
              _usage_error, description );

            _result = Result.Failed;
          }

          if( null == _selected )
          {
            _selected = new List<T>( n );
          }

          _selected.Add( e as T );
        }
      }

      // If no elements were pre-selected, 
      // prompt for post-selection

      if( null == _selected
        || 0 == _selected.Count )
      {
        IList<Reference> refs = null;

        try
        {
          refs = sel.PickObjects(
            ObjectType.Element,
            new JtSelectionFilter( typeof( T ), bic, f ),
            string.Format(
              "Please select {0}s.",
              description ) );
        }
        catch( Autodesk.Revit.Exceptions
          .OperationCanceledException )
        {
          _result = Result.Cancelled;
        }

        if( null != refs && 0 < refs.Count )
        {
          _selected = new List<T>(
            refs.Select<Reference, T>(
              r => doc.GetElement( r.ElementId )
                as T ) );
        }
      }

      Debug.Assert(
        null == _selected || 0 < _selected.Count,
        "ensure we return only non-empty collections" );

      _result = ( null == _selected )
        ? Result.Cancelled
        : Result.Succeeded;
    }

    /// <summary>
    /// Return true if nothing was selected.
    /// </summary>
    public bool IsEmpty
    {
      get
      {
        return null == _selected
          || 0 == _selected.Count;
      }
    }

    /// <summary>
    /// Return the cancellation or failure code
    /// to Revit and display a message if there
    /// is anything to say.
    /// </summary>
    public Result ShowResult()
    {
      if( Result.Failed == _result )
      {
        Debug.Assert( 0 < _msg.Length,
          "expected a non-empty error message" );

        Util.ErrorMsg( _msg );
      }
      return _result;
    }

    /// <summary>
    /// Return selected elements or null.
    /// </summary>
    public IList<T> Selected
    {
      get
      {
        return _selected;
      }
    }

    // There is no need to provide external access to 
    // the error message or result code, since that 
    // can all be encapsulated in the call to ShowResult.

    /// <summary>
    /// Return error message in case
    /// of failure or cancellation
    /// </summary>
    //public string ErrorMessage
    //{
    //  get
    //  {
    //    return _msg;
    //  }
    //}

    /// <summary>
    /// Return selection result
    /// </summary>
    //public Result Result
    //{
    //  get
    //  {
    //    return _result;
    //  }
    //}
  }
}
