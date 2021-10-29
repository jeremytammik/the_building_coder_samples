#region Header

//
// CmdNewWallLayer.cs - create a new compound wall layer.
//
// Copyright (C) 2009-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    /// <summary>
    ///     Demonstrate that the compound layer structure can be edited
    ///     and added to prior from Revit 2012 onwards. This was previously
    ///     impossible, and the whole compound structure was read-only.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    internal class CmdNewWallLayer : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var doc = app.ActiveUIDocument.Document;

#if _2011
      //
      // code for the Revit 2011 API:
      //
      Debug.Assert( false,
        "Currently, no new wall layer can be created, because"
        + "there is no creation method available for it." );

      foreach( WallType wallType in doc.WallTypes )
      {
        if( 0 < wallType.CompoundStructure.Layers.Size )
        {
          CompoundStructureLayer oldLayer
            = wallType.CompoundStructure.Layers.get_Item( 0 );

          WallType newWallType
            = wallType.Duplicate( "NewWallType" ) as WallType;

          CompoundStructure structure
            = newWallType.CompoundStructure;

          CompoundStructureLayerArray layers
            = structure.Layers;


          // from here on, nothing works, as expected:
          // in the Revir 2010 API, we could call the constructor
          // even though it is for internal use only.
          // in 2011, it is not possible to call it either.

          CompoundStructureLayer newLayer = null;
          //  = new CompoundStructureLayer(); // for internal use only

          newLayer.DeckProfile = oldLayer.DeckProfile;
          //newLayer.DeckUsage = oldLayer.DeckUsage; // read-only
          //newLayer.Function = oldLayer.Function; // read-only
          newLayer.Material = oldLayer.Material;
          newLayer.Thickness = oldLayer.Thickness;
          newLayer.Variable = oldLayer.Variable;
          layers.Append( newLayer );

        }
      }
#endif // _2011

            using var t = new Transaction(doc);
            t.Start("Create New Wall Layer");

            //WallTypeSet wallTypes = doc.WallTypes; // 2013 

            var wallTypes
                = new FilteredElementCollector(doc)
                    .OfClass(typeof(WallType)); // 2014

            foreach (WallType wallType in wallTypes)
                if (0 < wallType.GetCompoundStructure().GetLayers().Count)
                {
                    var oldLayer
                        = wallType.GetCompoundStructure().GetLayers()[0];

                    var newWallType
                        = wallType.Duplicate("NewWallType") as WallType;

                    var structure
                        = newWallType.GetCompoundStructure();

                    var layers
                        = structure.GetLayers();

                    // in Revit 2012, we can create a new layer:

                    var width = 0.1;
                    var function = oldLayer.Function;
                    var materialId = oldLayer.MaterialId;

                    var newLayer
                        = new CompoundStructureLayer(width, function, materialId);

                    layers.Add(newLayer);
                    structure.SetLayers(layers);
                    newWallType.SetCompoundStructure(structure);
                }

            t.Commit();

            return Result.Succeeded;
        }
    }
}