#region Header
//
// AbSelFilter.cs - enhanced simplified selection functionality class
//
// Copyright (C) 2014-2015 Alexander Buschmann, IDAT GmbH
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to 
// deal in the Software without restriction, including without limitation the 
// rights to use, copy, modify, merge, publish, distribute, sublicense, and/or 
// sell copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in 
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
// DEALINGS IN THE SOFTWARE.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
#endregion // Namespaces

namespace BuildingCoder
{
  class AbSelFilter
  {
    //
    // Element-Filters: Will filter only for 
    // elements, not for references
    //

    /// <summary>
    /// Creates a selection filter that only 
    /// elements of type T will pass
    /// </summary>
    public static IElementSelectionFilter GetElementFilter<T>()
    {
      return new ElementTypeFilter( typeof( T ) );
    }

    /// <summary>
    /// Creates a selection filter that elements of 
    /// any type in the collection will pass
    /// </summary>
    public static IElementSelectionFilter GetElementFilter(
      IEnumerable<Type> allowedTypes )
    {
      return new ElementTypeFilter( allowedTypes );
    }

    /// <summary>
    /// Creates a selection filter that elements 
    /// of any of the given types  will pass
    /// </summary>
    public static IElementSelectionFilter GetElementFilter(
      Type type,
      params Type[] types )
    {
      return new ElementTypeFilter( type, types );
    }

    /// <summary>
    /// Creates a selection filter from an ElementFilter
    /// </summary>
    public static IElementSelectionFilter GetElementFilter(
      ElementFilter filter )
    {
      return new ElementFilterFilter( filter );
    }

    /// <summary>
    /// Creates a selection filter that will use 
    /// the "filterMethod" to filter the elements
    /// </summary>
    public static IElementSelectionFilter GetElementFilter(
      Func<Element, bool> filterMethod )
    {
      return new DelegatesFilter( filterMethod, DelegatesFilter.AllReferences );
    }

    /// <summary>
    /// Creates a selection filter that will use 
    /// the "filterMethod" to filter the elements
    /// </summary>
    public static IElementSelectionFilter GetElementFilter(
      Predicate<Element> filterMethod )
    {
      return new DelegatesFilter(
        new Func<Element, bool>( filterMethod ),
        DelegatesFilter.AllReferences );
    }

    /// <summary>
    ///  Creates a selection filter that will 
    ///  let pass only the elements defined by the ids
    /// </summary>
    public static IElementSelectionFilter GetElementFilter(
      ElementId id,
      params ElementId[] ids )
    {
      return new ElementIdFilter( id, ids );
    }

    /// <summary>
    ///  Creates a selection filter that will 
    ///  let pass only the elements defined by the ids
    /// </summary>
    public static IElementSelectionFilter GetElementFilter(
      IEnumerable<ElementId> ids )
    {
      return new ElementIdFilter( ids );
    }

    //
    // Reference-Filters: Will filter only for 
    // references, let all elements pass
    //

    /// <summary>
    /// Creates a selection filter that will use 
    /// the "filterMethod" to filter the references
    /// </summary>
    public static IReferenceSelectionFilter GetReferenceFilter(
      Func<Reference, XYZ, bool> filterMethod )
    {
      return new DelegatesFilter( DelegatesFilter.AllElements,
        filterMethod );
    }

    /// <summary>
    /// Creates a selection filter that will let only 
    /// PlanarFace-References pass
    /// </summary>
    public static IReferenceSelectionFilter GetPlanarFaceFilter( Document doc )
    {
      return GetReferenceFilter( ( reference, xyz ) =>
      {
        Element element = doc.GetElement( reference );
        PlanarFace planarFace = element.GetGeometryObjectFromReference(
          reference ) as PlanarFace;
        return planarFace != null;
      } );
    }

    /// <summary>
    /// Creates a selection filter that will let faces 
    /// pass if their normal vector at (0/0) is 
    /// codirectional or parallel to the given normal 
    /// vector
    /// </summary>
    public static IReferenceSelectionFilter GetFaceNormalFilter(
      Document doc,
      XYZ normal,
      bool acceptParallel = false )
    {
      XYZ _normal = normal.Normalize();
      XYZ minusNormal = -1 * _normal;
      return GetReferenceFilter( ( reference, xyz ) =>
      {
        Element element = doc.GetElement( reference );
        Face face = element.GetGeometryObjectFromReference( reference ) as Face;
        if( face == null ) return false;
        XYZ faceNormal = face.ComputeNormal( UV.Zero );
        bool erg = faceNormal.IsAlmostEqualTo( _normal );
        if( acceptParallel ) erg |= faceNormal.IsAlmostEqualTo( minusNormal );
        return erg;
      } );
    }

    //
    // Filter for Elements and References
    //

    public static ISelectionFilter GetFilter(
      Func<Element, bool> elementFilterMethod,
      Func<Reference, XYZ, bool> referencesFilterMethod )
    {
      return new DelegatesFilter( elementFilterMethod,
        referencesFilterMethod );
    }

    //
    // Logical-Filters: Will call other filters
    //

    /// <summary>
    /// Creates a logical "or" filter
    /// </summary>
    public static ILogicalCombinationFilter GetLogicalOrFilter(
      ISelectionFilter first,
      ISelectionFilter second,
      bool executeAll = false )
    {
      return new LogicalOrFilter( first, second, executeAll );
    }

    /// <summary>
    /// Creates a logical "or" filter
    /// </summary>
    public static ILogicalCombinationFilter GetLogicalOrFilter(
      ISelectionFilter first,
      params ISelectionFilter[] filters )
    {
      return new LogicalOrFilter( first, filters );
    }

    /// <summary>
    /// Creates a logical "or" filter
    /// </summary>
    public static ILogicalCombinationFilter GetLogicalOrFilter(
      IEnumerable<ISelectionFilter> filters )
    {
      return new LogicalOrFilter( filters );
    }

    /// <summary>
    /// Creates a logical "and" filter
    /// </summary>
    public static ILogicalCombinationFilter GetLogicalAndFilter(
      ISelectionFilter first,
      ISelectionFilter second,
      bool executeAll = false )
    {
      return new LogicalAndFilter( first, second, executeAll );
    }

    /// <summary>
    /// Creates a logical "and" filter
    /// </summary>
    public static ILogicalCombinationFilter GetLogicalAndFilter(
      ISelectionFilter first,
      params ISelectionFilter[] filters )
    {
      return new LogicalAndFilter( first, filters );
    }

    /// <summary>
    /// Creates a logical "and" filter
    /// </summary>
    public static ILogicalCombinationFilter GetLogicalAndFilter(
      IEnumerable<ISelectionFilter> filters )
    {
      return new LogicalAndFilter( filters );
    }

    /// <summary>
    /// Creates a logical "not" filter
    /// </summary>
    public static ISelectionFilter GetLogicalNotFilter(
      ISelectionFilter filter )
    {
      return new LogicalNotFilter( filter );
    }

    //
    // Extension Methods for SelectionFilters
    // 

    /// <summary>
    /// Creates a logical "or" filter
    /// </summary>
    /// <param name="_this"></param>
    /// <param name="filters"></param>
    /// <returns></returns>
    public static ILogicalCombinationFilter Or(
      this ISelectionFilter _this,
      params ISelectionFilter[] filters )
    {
      return new LogicalOrFilter( _this, filters );
    }

    /// <summary>
    /// Creates a logical "and" filter
    /// </summary>
    public static ILogicalCombinationFilter And(
      this ISelectionFilter _this,
      params ISelectionFilter[] filters )
    {
      return new LogicalAndFilter( _this, filters );
    }

    /// <summary>
    /// Creates a logical "not" filter
    /// </summary>
    public static ISelectionFilter Not(
      this ISelectionFilter _this )
    {
      return new LogicalNotFilter( _this );
    }

    //
    // Interfaces
    //

    //
    // Private classes will do the actual work - 
    // don't need to be visible.
    // ISelectionFilter is not strictly neccessary on 
    //each class, but I like to explicitly say what I 
    // am doing.
    //

    /// <summary>
    /// Private class that represents a selection 
    /// filter that will filter only for elements
    /// and lets all references pass
    /// </summary>
    private abstract class ElementSelectionFilter
      : IElementSelectionFilter,
      IReferenceSelectionFilter,
      ISelectionFilter
    {
      #region Implementation of ISelectionFilter

      public abstract bool AllowElement( Element elem );

      /// <summary>
      /// This class does not filter for references
      /// </summary>
      public bool AllowReference(
        Reference reference,
        XYZ position )
      {
        return true;
      }
      #endregion
    }

    /// <summary>
    /// Private class that represents a 
    /// filter for one or more ElementTypes
    /// </summary>
    private class ElementTypeFilter
      : ElementSelectionFilter,
      IElementSelectionFilter,
      IReferenceSelectionFilter,
      ISelectionFilter
    {
      /// <summary>
      /// List of the allowed types 
      /// </summary>
      private readonly List<Type> m_types
        = new List<Type>();

      /// <summary>
      /// Constructs a Filter for a single Type
      /// </summary>
      public ElementTypeFilter( Type type )
      {
        m_types.Add( type );
      }

      /// <summary>
      /// Constructs a Filter for a list of Types
      /// </summary>
      public ElementTypeFilter( IEnumerable<Type> types )
      {
        m_types.AddRange( types );
      }

      /// <summary>
      /// Constructs a Filter for a number of Types
      /// </summary>
      public ElementTypeFilter(
        Type type,
        params Type[] types )
        : this( type )
      {
        m_types.AddRange( types );
      }

      #region Implementation of ISelectionFilter

      public override bool AllowElement( Element elem )
      {
        return m_types.Any( type
          => type.IsInstanceOfType( elem ) );
      }
      #endregion
    }

    /// <summary>
    /// Private class that represents a 
    /// filter using an ElementFilter
    /// </summary>
    private class ElementFilterFilter
      : ElementSelectionFilter,
      IElementSelectionFilter,
      IReferenceSelectionFilter,
      ISelectionFilter
    {
      /// <summary>
      /// The ElementFilter that is used
      /// </summary>
      private readonly ElementFilter m_filter;

      /// <summary>
      /// Constructs a SelectionFilter for an ElementFilter
      /// </summary>
      public ElementFilterFilter( ElementFilter filter )
      {
        m_filter = filter;
      }

      #region Overrides of ElementSelectionFilter

      /// <summary>
      /// An element passes if it passes the ElementFilter
      /// </summary>
      public override bool AllowElement( Element elem )
      {
        return m_filter.PassesFilter( elem );
      }
      #endregion
    }

    /// <summary>
    /// Private class that represents a filter 
    /// for one or more specific elements.
    /// Especially usefull when and-combined 
    /// with a ReferenceFilter
    /// </summary>
    private class ElementIdFilter
      : ElementSelectionFilter,
      IElementSelectionFilter,
      IReferenceSelectionFilter,
      ISelectionFilter
    {

      /// <summary>
      /// The list of valid ElementIds
      /// </summary>
      private readonly List<ElementId> m_ids
        = new List<ElementId>();

      /// <summary>
      /// Constructs a ElementIdFilter for one or more ElementIds
      /// </summary>
      public ElementIdFilter(
        ElementId id,
        params ElementId[] ids )
      {
        m_ids.Add( id );
        m_ids.AddRange( ids );
      }

      /// <summary>
      /// Constructs a ElementIdFilter for a 
      /// collection of ElementIds
      /// </summary>
      public ElementIdFilter( IEnumerable<ElementId> ids )
      {
        m_ids.AddRange( ids );
      }

      #region Overrides of ElementSelectionFilter

      /// <summary>
      /// An Element passes if its Id is 
      /// in the list of valid Ids
      /// </summary>
      public override bool AllowElement( Element elem )
      {
        return m_ids.Contains( elem.Id );
      }
      #endregion
    }

    /// <summary>
    /// Private class that represents 
    /// a filter using delegate methods
    /// </summary>
    private class DelegatesFilter
      : IElementSelectionFilter,
      IReferenceSelectionFilter,
      ISelectionFilter
    {
      /// <summary>
      /// The delegate used to filter elements
      /// </summary>
      private readonly Func<Element, bool> m_elementFilter;

      /// <summary>
      /// The delegate used to filter References
      /// </summary>
      private readonly Func<Reference, XYZ, bool> m_referenceFilter;

      /// <summary>
      /// Constructs a filter that uses the element 
      /// and reference filter delegates
      /// </summary>
      public DelegatesFilter(
        Func<Element, bool> elementFilter,
        Func<Reference, XYZ, bool> referenceFilter )
      {
        m_elementFilter = elementFilter;
        m_referenceFilter = referenceFilter;
      }

      /// <summary>
      /// Use this if all Elements should pass the Filter
      /// </summary>
      public static Func<Element, bool> AllElements
      {
        get { return element => true; }
      }

      /// <summary>
      /// Use this, if all References should pass the Filter
      /// </summary>
      public static Func<Reference, XYZ, bool> AllReferences
      {
        get { return ( reference, xyz ) => true; }
      }

      #region Implementation of ISelectionFilter

      /// <summary>
      /// Elements that pass the element filter 
      /// predicate will pass
      /// </summary>
      public bool AllowElement( Element elem )
      {
        return m_elementFilter( elem );
      }

      /// <summary>
      /// Elements that pass the reference filter 
      /// predicate will pass
      /// </summary>
      public bool AllowReference(
        Reference reference,
        XYZ position )
      {
        return m_referenceFilter( reference, position );
      }
      #endregion
    }

    /// <summary>
    /// Private class that represents a logical or filter for selection filters.
    /// </summary>
    private abstract class LogicalCombinationFilter : ISelectionFilter, ILogicalCombinationFilter
    {
      /// <summary>
      /// List of the filters to check
      /// </summary>
      protected readonly List<ISelectionFilter> m_filterList
        = new List<ISelectionFilter>();

      /// <summary>
      /// If true, all filters are executed, 
      /// even if the result is already obvious
      /// </summary>
      public bool ExecuteAll { get; set; }

      /// <summary>
      /// Constructs a logical filter for two selectionFilters
      /// </summary>
      protected LogicalCombinationFilter(
        ISelectionFilter first,
        ISelectionFilter second,
        bool executeAll = false )
      {
        this.ExecuteAll = executeAll;
        m_filterList.Add( first );
        m_filterList.Add( second );
      }

      /// <summary>
      /// Constructs a logical filter for 
      /// a number of selectionsFilters
      /// executeAll is set to false
      /// </summary>
      protected LogicalCombinationFilter(
        ISelectionFilter first,
        params ISelectionFilter[] filters )
      {
        this.ExecuteAll = false;
        m_filterList.Add( first );
        m_filterList.AddRange( filters );
      }

      /// <summary>
      /// Constructs a logical filter for 
      /// a number of selectionsFilters
      /// executeAll is set to false
      /// </summary>
      /// <param name="filters"></param>
      protected LogicalCombinationFilter(
        IEnumerable<ISelectionFilter> filters )
      {
        ExecuteAll = false;
        m_filterList.AddRange( filters );
      }

      #region Implementation of ISelectionFilter

      public abstract bool AllowElement(
        Element elem );

      public abstract bool AllowReference(
        Reference reference,
        XYZ position );

      #endregion
    }

    /// <summary>
    /// Private class that represents a logical or filter
    /// </summary>
    private class LogicalOrFilter
      : LogicalCombinationFilter, ISelectionFilter
    {

      /// <summary>
      /// Constructs a logical or filter for two Filters
      /// </summary>
      public LogicalOrFilter(
        ISelectionFilter first,
        ISelectionFilter second,
        bool executeAll )
        : base( first, second, executeAll )
      {
      }

      /// <summary>
      /// Constructs a logical or filter for 
      /// a number of filters
      /// </summary>
      public LogicalOrFilter(
        ISelectionFilter first,
        params ISelectionFilter[] filters )
        : base( first, filters )
      {
      }

      /// <summary>
      /// Constructs a logical or filter for 
      /// a number of filters
      /// </summary>
      public LogicalOrFilter(
        IEnumerable<ISelectionFilter> filters )
        : base( filters )
      {
      }

      #region Implementation of ISelectionFilter

      /// <summary>
      /// Elements that pass at least on 
      /// of the filters will pass
      /// </summary>
      public override bool AllowElement( Element elem )
      {
        bool erg = false;
        if( ExecuteAll )
        {
          m_filterList.ForEach( filter
            => erg |= filter.AllowElement( elem ) );
        }
        else
        {
          erg = m_filterList.Any( filter
            => filter.AllowElement( elem ) );
        }
        return erg;
      }

      /// <summary>
      /// References that pass at least one 
      /// of the filters will pass
      /// </summary>
      public override bool AllowReference(
        Reference reference,
        XYZ position )
      {
        bool erg = false;
        if( ExecuteAll )
        {
          m_filterList.ForEach( filter
            => erg |= filter.AllowReference(
              reference, position ) );
        }
        else
        {
          erg = m_filterList.Any( filter
            => filter.AllowReference(
              reference, position ) );
        }
        return erg;
      }

      #endregion
    }

    /// <summary>
    /// Private class that represents a logical and filter
    /// </summary>
    private class LogicalAndFilter
      : LogicalCombinationFilter, ISelectionFilter
    {

      /// <summary>
      /// Constructs a logical and filter for two Filters
      /// </summary>
      public LogicalAndFilter(
        ISelectionFilter first,
        ISelectionFilter second,
        bool executeAll )
        : base( first, second, executeAll )
      {
      }

      /// <summary>
      /// Constructs a logical and filter 
      /// for a number of filters
      /// </summary>
      public LogicalAndFilter(
        ISelectionFilter first,
        params ISelectionFilter[] filters )
        : base( first, filters )
      {
      }

      /// <summary>
      /// Constructs a logical and 
      /// filter for a number of filters
      /// </summary>
      public LogicalAndFilter(
        IEnumerable<ISelectionFilter> filters )
        : base( filters )
      {
      }

      #region Implementation of ISelectionFilter

      /// <summary>
      /// Elements that pass all of the filters will pass
      /// </summary>
      public override bool AllowElement( Element elem )
      {
        bool erg = true;
        if( ExecuteAll )
        {
          m_filterList.ForEach( filter
            => erg = erg & filter.AllowElement( elem ) );
        }
        else
        {
          erg = m_filterList.All( filter
            => filter.AllowElement( elem ) );
        }
        return erg;
      }

      /// <summary>
      /// References that pass all of the filters will pass
      /// </summary>
      public override bool AllowReference(
        Reference reference,
        XYZ position )
      {
        bool erg = true;
        if( ExecuteAll )
        {
          m_filterList.ForEach( filter
            => erg = erg & filter.AllowReference(
              reference, position ) );
        }
        else
        {
          erg = m_filterList.All( filter
            => filter.AllowReference(
              reference, position ) );
        }
        return erg;
      }
      #endregion
    }

    /// <summary>
    /// Private class that represents a logical Not filter
    /// </summary>
    private class LogicalNotFilter : ISelectionFilter
    {
      /// <summary>
      /// The filter, that will get negated
      /// </summary>
      private readonly ISelectionFilter m_filter;

      /// <summary>
      /// constructs a not-Filter
      /// </summary>
      /// <param name="filter"> </param>
      public LogicalNotFilter( ISelectionFilter filter )
      {
        m_filter = filter;
      }

      #region Implementation of ISelectionFilter

      /// <summary>
      /// Elements will pass, if they don't 
      /// pass the original filter
      /// </summary>
      public bool AllowElement( Element elem )
      {
        return !m_filter.AllowElement( elem );
      }

      /// <summary>
      /// References will pass, if they 
      /// don't pass the original filter
      /// </summary>
      public bool AllowReference(
        Reference reference,
        XYZ position )
      {
        return !m_filter.AllowReference(
          reference, position );
      }
      #endregion
    }
  }
}
