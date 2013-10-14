#region Header
//
// CmdNewDuctSystem.cs - create a new duct system via the NewMechanicalSystem API call
//
// Copyright (C) 2010-2013 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Manual )]
  class CmdNewDuctSystem : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      UIDocument uidoc = app.ActiveUIDocument;
      Document doc = uidoc.Document;

      Transaction trans = new Transaction( doc,
        "New Duct System" );

      trans.Start();

      ConnectorSet connectorSet = new ConnectorSet();

      Connector baseConnector = null;

      ConnectorSetIterator csi;

      // select a Parallel Fan Powered VAV
      // and some Supply Diffusers prior to running
      // this command

      ElementSet selection = uidoc.Selection.Elements;

      foreach( Element e in selection )
      {
        if( e is FamilyInstance )
        {
          FamilyInstance fi = e as FamilyInstance;

          Family family = fi.Symbol.Family;

          // assume the selected Mechanical Equipment
          // is the base equipment for new system:

          if( family.FamilyCategory.Name
            == "Mechanical Equipment" )
          {
            // find the "Out" and "SupplyAir" connectors
            // on the base equipment

            if( null != fi.MEPModel )
            {
              csi = fi.MEPModel.ConnectorManager
                .Connectors.ForwardIterator();

              while( csi.MoveNext() )
              {
                Connector conn = csi.Current as Connector;

                if( conn.Direction == FlowDirectionType.Out
                  && conn.DuctSystemType == DuctSystemType.SupplyAir )
                {
                  baseConnector = conn;
                  break;
                }
              }
            }
          }
          else if( family.FamilyCategory.Name == "Air Terminals" )
          {
            // add selected Air Terminals to
            // connector set for new mechanical system

            csi = fi.MEPModel.ConnectorManager
              .Connectors.ForwardIterator();

            csi.MoveNext();

            connectorSet.Insert( csi.Current as Connector );
          }
        }
      }

      // create a new SupplyAir mechanical system

      MechanicalSystem ductSystem = doc.Create.NewMechanicalSystem(
        baseConnector, connectorSet, DuctSystemType.SupplyAir );

      trans.Commit();
      return Result.Succeeded;
    }
  }
}
