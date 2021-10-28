#region Header

//
// CmdOmniClassParams.cs - extract OmniClass
// parameter data from all elements
//
// Copyright (C) 2009-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System.IO;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdOmniClassParams : IExternalCommand
    {
        private readonly BuiltInParameter _bipCode
            = BuiltInParameter.OMNICLASS_CODE;

        private readonly BuiltInParameter _bipDesc
            = BuiltInParameter.OMNICLASS_DESCRIPTION;

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var doc = app.ActiveUIDocument.Document;

#if _2010
      List<Element> set = new List<Element>();

      ParameterFilter f
        = app.Create.Filter.NewParameterFilter(
          _bipCode,
          CriteriaFilterType.NotEqual,
          string.Empty );

      ElementIterator it = doc.get_Elements( f );
#endif

            using var sw
                = File.CreateText("C:/omni.txt");
            var collector = new FilteredElementCollector(doc);
            collector.WhereElementIsNotElementType();

            // in 2011, we should probably add some more quick filters here ...
            // and make use of something like:

            //ParameterValueProvider provider = new ParameterValueProvider( new ElementId( Bip.SystemType ) );
            //FilterStringRuleEvaluator evaluator = new FilterStringEquals();
            //string ruleString = ParameterValue.SupplyAir;
            //FilterRule rule = new FilterStringRule( provider, evaluator, ruleString, false );
            //ElementParameterFilter filter = new ElementParameterFilter( rule );
            //collector.WherePasses( filter );

            foreach (var e in collector)
            {
                var p = e.get_Parameter(_bipCode);
                if (null != p) sw.WriteLine("{0} code {1} desc {2}", Util.ElementDescription(e), p.AsString(), e.get_Parameter(_bipDesc).AsString());
            }

            sw.Close();

            return Result.Failed;
        }
    }
}