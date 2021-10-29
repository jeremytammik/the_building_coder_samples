#region Header

//
// CmdFamilyParamValue.cs - list family parameter values
// defined on the types in a family document
//
// Copyright (C) 2009-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdFamilyParamValue : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var doc = app.ActiveUIDocument.Document;
            if (!doc.IsFamilyDocument)
            {
                message =
                    "Please run this command in a family document.";
            }
            else
            {
                var mgr = doc.FamilyManager;

                var n = mgr.Parameters.Size;

                Debug.Print(
                    "\nFamily {0} has {1} parameter{2}.",
                    doc.Title, n, Util.PluralSuffix(n));

                var fps
                    = new Dictionary<string, FamilyParameter>(n);

                foreach (FamilyParameter fp in mgr.Parameters)
                {
                    var name = fp.Definition.Name;
                    fps.Add(name, fp);

                    #region Look at associated parameters

#if LOOK_AT_ASSOCIATED_PARAMETERS
          ParameterSet ps = fp.AssociatedParameters;
          n = ps.Size;

          string values = string.Empty;
          foreach( Parameter p in ps )
          {
            if( 0 == values.Length )
            {
              values = " ";
            }
            else
            {
              values += ", ";
            }
            values += p.AsValueString();
          }

          Debug.Print(
            "Parameter {0} has {1} associated parameter{2}{3}{4}.",
            name,
            n,
            PluralSuffix( n ),
            ( 0 < n ? ":" : "" ),
            values );
#endif // LOOK_AT_ASSOCIATED_PARAMETERS

                    #endregion // Look at associated parameters
                }

                var keys = new List<string>(fps.Keys);
                keys.Sort();

                n = mgr.Types.Size;

                Debug.Print(
                    "Family {0} has {1} type{2}{3}",
                    doc.Title,
                    n,
                    Util.PluralSuffix(n),
                    Util.DotOrColon(n));

                foreach (FamilyType t in mgr.Types)
                {
                    var name = t.Name;
                    Debug.Print("  {0}:", name);
                    foreach (var key in keys)
                    {
                        var fp = fps[key];
                        if (t.HasValue(fp))
                        {
                            var value
                                = FamilyParamValueString(t, fp, doc);

                            Debug.Print("    {0} = {1}", key, value);
                        }
                    }
                }
            }

            #region Exercise ExtractPartAtomFromFamilyFile

            // by the way, here is a completely different way to
            // get at all the parameter values, and all the other
            // family information as well:

            var exercise_this_method = false;

            if (doc.IsFamilyDocument && exercise_this_method)
            {
                var path = doc.PathName;
                if (0 < path.Length)
                    app.Application.ExtractPartAtomFromFamilyFile(
                        path, $"{path}.xml");
            }

            #endregion // Exercise ExtractPartAtomFromFamilyFile

            return Result.Failed;
        }

        private static string FamilyParamValueString(
            FamilyType t,
            FamilyParameter fp,
            Document doc)
        {
            var value = t.AsValueString(fp);
            switch (fp.StorageType)
            {
                case StorageType.Double:
                    value = $"{Util.RealString((double) t.AsDouble(fp))} (double)";
                    break;

                case StorageType.ElementId:
                    var id = t.AsElementId(fp);
                    var e = doc.GetElement(id);
                    value = $"{id.IntegerValue} ({Util.ElementDescription(e)})";
                    break;

                case StorageType.Integer:
                    value = $"{t.AsInteger(fp)} (int)";
                    break;

                case StorageType.String:
                    value = $"'{t.AsString(fp)}' (string)";
                    break;
            }

            return value;
        }

        #region SetFamilyParameterValue

        /// <summary>
        ///     Non-working sample code for
        ///     http://forums.autodesk.com/t5/revit-api/family-types-amp-shared-parameter-values/m-p/6218767
        /// </summary>
        private void SetFamilyParameterValueFails(
            Document doc,
            string paramNameToAmend)
        {
            var mgr = doc.FamilyManager;
            var familyTypes = mgr.Types;
            var familyTypeItor
                = familyTypes.ForwardIterator();
            familyTypeItor.Reset();
            while (familyTypeItor.MoveNext())
            {
                var familyParam
                    = mgr.get_Parameter(paramNameToAmend);

                if (familyParam != null)
                {
                    var familyType = familyTypeItor.Current as FamilyType;
                    Debug.Print(familyType.Name);
                    mgr.Set(familyParam, 2);
                }
            }
        }

        /// <summary>
        ///     Working sample code for
        ///     http://forums.autodesk.com/t5/revit-api/family-types-amp-shared-parameter-values/m-p/6218767
        /// </summary>
        private void SetFamilyParameterValueWorks(
            Document doc,
            string paramNameToAmend)
        {
            var mgr = doc.FamilyManager;
            var familyParam
                = mgr.get_Parameter(paramNameToAmend);

            if (familyParam != null)
                foreach (FamilyType familyType in mgr.Types)
                {
                    Debug.Print(familyType.Name);
                    mgr.CurrentType = familyType;
                    mgr.Set(familyParam, 2);
                }
        }

        #endregion // SetFamilyParameterValue
    }
}