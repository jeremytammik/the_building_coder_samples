using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace BuildingCoder
{
    /// <summary>
    ///     Allow selection of elements of type T only.
    /// </summary>
    internal class JtElementsOfClassSelectionFilter<T>
        : ISelectionFilter where T : Element
    {
        public bool AllowElement(Element e)
        {
            return e is T;
        }

        public bool AllowReference(Reference r, XYZ p)
        {
            return true;
        }

        #region Compare element category to target category list

        private bool CompareCategoryToTargetList(Element e)
        {
            var rc = null != e.Category;
            if (rc)
            {
                var targets = new[]
                {
                    (int) BuiltInCategory.OST_StructuralColumns,
                    (int) BuiltInCategory.OST_StructuralFraming,
                    (int) BuiltInCategory.OST_Walls
                };
                var icat = e.Category.Id.IntegerValue;
                rc = targets.Any(i => i.Equals(icat));
            }

            return rc;
        }

        #endregion // Compare element category to target category list
    }
}