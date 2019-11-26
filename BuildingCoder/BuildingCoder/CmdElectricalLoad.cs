using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.Exceptions;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace BuildingCoder
{
    [Transaction(TransactionMode.Manual)]
    public class CmdElectricalLoad : IExternalCommand
    {
        class ElectricalApparentLoad
        {
            public ElectricalApparentLoad(ElectricalSystemType electricalSystemType, int connectorId, double apparentLoad)
            {
                ElectricalSystemType = electricalSystemType;

                ConnectorId = connectorId;

                ApparentLoad = apparentLoad;
            }

            public ElectricalSystemType ElectricalSystemType { get; }

            public int ConnectorId { get; }

            public double ApparentLoad { get; }

            public override string ToString() => $"{ElectricalSystemType}: {ConnectorId} - {ApparentLoad} V*A";
        }

        class ElectricalApparentLoadFactory
        {
            public IEnumerable<ElectricalApparentLoad> Create(FamilyInstance familyInstance)
            {
                return familyInstance
                    .MEPModel
                    .ConnectorManager
                    .Connectors
                    .Cast<Connector>()
                    .Select(Create)
                    .Where(x => x != null);
            }

            private static ElectricalApparentLoad Create(Connector connector)
            {
                if (connector.Domain != Domain.DomainElectrical)
                    return null;
                
                var mepConnectorInfo = connector.GetMEPConnectorInfo() as MEPFamilyConnectorInfo;
                
                var parameterValue = mepConnectorInfo?.GetConnectorParameterValue(new ElementId(BuiltInParameter.RBS_ELEC_APPARENT_LOAD)) as DoubleParameterValue;

                if (parameterValue == null)
                    return null;

                var load = UnitUtils.ConvertFromInternalUnits(parameterValue.Value, DisplayUnitType.DUT_VOLT_AMPERES);

                return new ElectricalApparentLoad(connector.ElectricalSystemType, connector.Id, load);
            }
        }

        class FamilyInstanceWithApparentLoadSelectionFilter : ISelectionFilter
        {
            private readonly ElectricalApparentLoadFactory electricalApparentLoadFactory;

            public FamilyInstanceWithApparentLoadSelectionFilter(ElectricalApparentLoadFactory electricalApparentLoadFactory)
            {
                this.electricalApparentLoadFactory = electricalApparentLoadFactory;
            }

            public bool AllowElement(Element elem)
            {
                var familyInstance = elem as FamilyInstance;

                if (familyInstance == null)
                    return false;

                return electricalApparentLoadFactory
                    .Create(familyInstance)
                    .Any();
            }

            public bool AllowReference(Reference reference, XYZ position) => false;
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;

            var familyInstance = SelectFamilyInstanceWithApparentLoad(uidoc);

            if (familyInstance == null)
                return Result.Cancelled;

            var electricalApparentLoadFactory = new ElectricalApparentLoadFactory();

            var apparentLoads = electricalApparentLoadFactory.Create(familyInstance);

            TaskDialog.Show("dev", string.Join("\n", apparentLoads));

            return Result.Succeeded;
        }

        private static FamilyInstance SelectFamilyInstanceWithApparentLoad(UIDocument uidoc)
        {
            var electricalApparentLoadFactory = new ElectricalApparentLoadFactory();

            var selectionFilter = new FamilyInstanceWithApparentLoadSelectionFilter(electricalApparentLoadFactory);

            try
            {
                return (FamilyInstance)uidoc.Document.GetElement(uidoc.Selection.PickObject(ObjectType.Element, selectionFilter));
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }
    }
}