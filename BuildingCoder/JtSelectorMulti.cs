#region Namespaces

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using OperationCanceledException = Autodesk.Revit.Exceptions.OperationCanceledException;

#endregion

namespace BuildingCoder
{
    /// <summary>
    ///     Select multiple elements of the same type using
    ///     either pre-selection, before launching the
    ///     command, or post-selection, afterwards.
    ///     The element type is determined by the template
    ///     parameter. A filtering method must be provided
    ///     and is reused for both testing the pre-selection
    ///     and defining allowable elements for the post-
    ///     selection.
    /// </summary>
    internal class JtSelectorMulti<T> where T : Element
    {
        /// <summary>
        ///     Determine whether the given element is a valid
        ///     selectable object. The method passed in is
        ///     reused for both the interactive selection
        ///     filter and the pre-selection validation.
        ///     See below for a sample method.
        /// </summary>
        public delegate bool IsSelectable(Element e);

        /// <summary>
        ///     Error message in case of invalid pre-selection.
        /// </summary>
        private const string _usage_error = "Please pre-select "
                                            + "only {0}s before launching this command.";

        private readonly string _msg;
        private readonly Result _result;

        private readonly List<T> _selected;

        /// <summary>
        ///     Instantiate and run element selector.
        /// </summary>
        /// <param name="uidoc">UIDocument.</param>
        /// <param name="bic">Built-in category or null.</param>
        /// <param name="description">Description of the elements to select.</param>
        /// <param name="f">Validation method.</param>
        public JtSelectorMulti(
            UIDocument uidoc,
            BuiltInCategory? bic,
            string description,
            IsSelectable f)
        {
            _selected = null;
            _msg = null;

            var doc = uidoc.Document;

            if (null == doc)
            {
                _msg = "Please run this command in a valid"
                       + " Revit project document.";
                _result = Result.Failed;
            }

            // Check for pre-selected elements

            var sel = uidoc.Selection;
            var ids = sel.GetElementIds();
            var n = ids.Count;

            if (0 < n)
                //if( 1 != n )
                //{
                //  _msg = _usage_error;
                //  _result = Result.Failed;
                //}

                foreach (var id in ids)
                {
                    var e = doc.GetElement(id);

                    if (!f(e))
                    {
                        _msg = string.Format(
                            _usage_error, description);

                        _result = Result.Failed;
                    }

                    if (null == _selected) _selected = new List<T>(n);

                    _selected.Add(e as T);
                }

            // If no elements were pre-selected, 
            // prompt for post-selection

            if (null == _selected
                || 0 == _selected.Count)
            {
                IList<Reference> refs = null;

                try
                {
                    refs = sel.PickObjects(
                        ObjectType.Element,
                        new JtSelectionFilter(typeof(T), bic, f),
                        $"Please select {description}s.");
                }
                catch (OperationCanceledException)
                {
                    _result = Result.Cancelled;
                }

                if (refs is {Count: > 0})
                    _selected = new List<T>(
                        refs.Select(
                            r => doc.GetElement(r.ElementId)
                                as T));
            }

            Debug.Assert(
                null == _selected || 0 < _selected.Count,
                "ensure we return only non-empty collections");

            _result = null == _selected
                ? Result.Cancelled
                : Result.Succeeded;
        }

        /// <summary>
        ///     Return true if nothing was selected.
        /// </summary>
        public bool IsEmpty =>
            null == _selected
            || 0 == _selected.Count;

        /// <summary>
        ///     Return selected elements or null.
        /// </summary>
        public IList<T> Selected => _selected;

        #region Sample common filtering helper method

        /// <summary>
        ///     Determine whether the given element is valid.
        ///     This specific implementation requires a family
        ///     instance element of the furniture category
        ///     belonging to the named family.
        /// </summary>
        public static bool IsTable(Element e)
        {
            var rc = false;

            var cat = e.Category;

            if (null != cat)
                if (cat.Id.IntegerValue.Equals(
                    (int) BuiltInCategory.OST_Furniture))
                    if (e is FamilyInstance fi)
                    {
                        var fname = fi.Symbol.Family.Name;

                        rc = fname.Equals("SampleTableFamilyName");
                    }

            return rc;
        }

        #endregion // Common filtering helper method

        /// <summary>
        ///     Return the cancellation or failure code
        ///     to Revit and display a message if there
        ///     is anything to say.
        /// </summary>
        public Result ShowResult()
        {
            if (Result.Failed == _result)
            {
                Debug.Assert(0 < _msg.Length,
                    "expected a non-empty error message");

                Util.ErrorMsg(_msg);
            }

            return _result;
        }

        #region JtSelectionFilter

        private class JtSelectionFilter : ISelectionFilter
        {
            private readonly BuiltInCategory? _bic;
            private readonly IsSelectable _f;
            private Type _t;

            public JtSelectionFilter(
                Type t,
                BuiltInCategory? bic,
                IsSelectable f)
            {
                _t = t;
                _bic = bic;
                _f = f;
            }

            public bool AllowElement(Element e)
            {
                return e is T
                       && HasBic(e)
                       && _f(e);
            }

            public bool AllowReference(Reference r, XYZ p)
            {
                return true;
            }

            private bool HasBic(Element e)
            {
                return null == _bic
                       || null != e.Category
                       && e.Category.Id.IntegerValue.Equals(
                           (int) _bic);
            }
        }

        #endregion // JtSelectionFilter

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