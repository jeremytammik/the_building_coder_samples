using System.Diagnostics;
using Autodesk.Revit.DB;

namespace BuildingCoder
{
    #region Obsolete JtRevision using parameter display names

    /// <summary>
    ///     A Revision parameter wrapper class by Max.
    /// </summary>
    internal class JtRevisionUsingDisplayName
    {
        /// <summary>
        ///     The BIM element.
        /// </summary>
        private readonly Element _e;

        /// <summary>
        ///     Create a Revision parameter accessor
        ///     for the given BIM element.
        /// </summary>
        public JtRevisionUsingDisplayName(Element e)
        {
            _e = e;
        }

        public string Date
        {
            get => _p("Revision Date").AsString();
            set => _p("Revision Date").Set(value);
        }

        public string IssuedTo
        {
            get => _p("Issued to").AsString();
            set => _p("Issued to").Set(value);
        }

        public string Number
        {
            get => _p("Revision Number").AsString();
            set => _p("Revision Number").Set(value);
        }

        public int Issued
        {
            get => _p("Issued").AsInteger();
            set => _p("Issued").Set(value);
        }

        public int Numbering
        {
            get => _p("Numbering").AsInteger();
            set => _p("Numbering").Set(value);
        }

        public int Sequence
        {
            get => _p("Revision Sequence").AsInteger();
            set => _p("Revision Sequence").Set(value);
        }

        public string Description
        {
            get => _p("Revision Description").AsString();
            set => _p("Revision Description").Set(value);
        }

        public string IssuedBy
        {
            get => _p("Issued by").AsString();
            set => _p("Issued by").Set(value);
        }

        /// <summary>
        ///     Internal access to the named parameter.
        /// </summary>
        private Parameter _p(string parameter_name)
        {
            //return _e.get_Parameter( parameter_name ); // 2014

            Debug.Assert(
                1 == _e.GetParameters(parameter_name).Count,
                $"expected only one parameters named '{parameter_name}'");

            return _e.LookupParameter(parameter_name); // 2015
        }
    }

    #endregion // Obsolete JtRevision using parameter display names

    /// <summary>
    ///     A Revision parameter wrapper class avoiding
    ///     use of display names to access the data by
    ///     Jose Ignacio Montes.
    /// </summary>
    internal class JtRevision
    {
        /// <summary>
        ///     The BIM element.
        /// </summary>
        private readonly Element _e;

        /// <summary>
        ///     Create a Revision parameter accessor
        ///     for the given BIM element.
        /// </summary>
        public JtRevision(Element e)
        {
            _e = e;
        }

        public string Date
        {
            get => _p(BuiltInParameter.PROJECT_REVISION_REVISION_DATE).AsString();
            set => _p(BuiltInParameter.PROJECT_REVISION_REVISION_DATE).Set(value);
        }

        public string IssuedTo
        {
            get => _p(BuiltInParameter.PROJECT_REVISION_REVISION_ISSUED_TO).AsString();
            set => _p(BuiltInParameter.PROJECT_REVISION_REVISION_ISSUED_TO).Set(value);
        }

        public string Number
        {
            get => _p(BuiltInParameter.PROJECT_REVISION_REVISION_NUM).AsString();
            set => _p(BuiltInParameter.PROJECT_REVISION_REVISION_NUM).Set(value);
        }

        public int Issued
        {
            get => _p(BuiltInParameter.PROJECT_REVISION_REVISION_ISSUED).AsInteger();
            set => _p(BuiltInParameter.PROJECT_REVISION_REVISION_ISSUED).Set(value);
        }

        public int Numbering
        {
            get => _p(BuiltInParameter.PROJECT_REVISION_ENUMERATION).AsInteger();
            set => _p(BuiltInParameter.PROJECT_REVISION_ENUMERATION).Set(value);
        }

        public int Sequence
        {
            get => _p(BuiltInParameter.PROJECT_REVISION_SEQUENCE_NUM).AsInteger();
            set => _p(BuiltInParameter.PROJECT_REVISION_SEQUENCE_NUM).Set(value);
        }

        public string Description
        {
            get => _p(BuiltInParameter.PROJECT_REVISION_REVISION_DESCRIPTION).AsString();
            set => _p(BuiltInParameter.PROJECT_REVISION_REVISION_DESCRIPTION).Set(value);
        }

        public string IssuedBy
        {
            get => _p(BuiltInParameter.PROJECT_REVISION_REVISION_ISSUED_BY).AsString();
            set => _p(BuiltInParameter.PROJECT_REVISION_REVISION_ISSUED_BY).Set(value);
        }

        /// <summary>
        ///     Internal access to the named parameter.
        /// </summary>
        private Parameter _p(BuiltInParameter bip)
        {
            return _e.get_Parameter(bip);
        }
    }
}