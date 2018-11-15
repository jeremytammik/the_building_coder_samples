using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace BuildingCoder
{
  /// <summary>
  /// Allow selection of elements of type T only.
  /// </summary>
  class JtElementsOfClassSelectionFilter<T>
    : ISelectionFilter where T : Element
  {
    public bool AllowElement( Element e )
    {
      return e is T;

      #region Compare element category to target category list
      bool rc = null != e.Category;
      if( rc )
      {
        int[] targets = new int[] {
          (int) BuiltInCategory.OST_StructuralColumns,
          (int) BuiltInCategory.OST_StructuralFraming,
          (int) BuiltInCategory.OST_Walls
        };
        int icat = e.Category.Id.IntegerValue;
        rc = targets.Any<int>( i => i.Equals(icat) );
      }
      return rc;
      #endregion // Compare element category to target category list
    }

    public bool AllowReference( Reference r, XYZ p )
    {
      return true;
    }
  }
}
