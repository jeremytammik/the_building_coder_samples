#region Header
//
// CmdTriangleCount.cs - Determine model total triangle count using custom exporter
//
// Copyright (C) 2021 by Sentio @techXMKH9 Solutionario and Jeremy Tammik,
// https://knowledge.autodesk.com/profile/LSWJANYYIEWMH and Autodesk Inc. 
// All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.ReadOnly )]
  public class CmdTriangleCount : IExternalCommand
  {
    class TriangleCounterContext : IExportContext
    {
      private Document document;

      private Func<bool> isCanceled;

      /// <summary>
      /// Callback at end with total count of model 
      /// geometry triangles and material ids
      /// </summary>
      private Action<long, int> callback;

      private long numTriangles;

      private List<ElementId> materialIds;

      private bool includeMaterials = true;

      public TriangleCounterContext( 
        Document document, 
        Func<bool> isCanceled, 
        Action<long, int> callback )
      {
        this.isCanceled = isCanceled;
        this.callback = callback;
        this.document = document;
        this.materialIds = new List<ElementId>();
      }

      public void OnPolymesh( PolymeshTopology polymesh )
      {
        this.numTriangles += (long) polymesh.NumberOfFacets;
      }

      public void Finish()
      {
        this.callback( this.numTriangles, this.materialIds.Count );
      }

      public bool IsCanceled()
      {
        return false;
      }

      public bool Start()
      {
        this.materialIds = new List<ElementId>();
        return true;
      }

      public void OnRPC( RPCNode node )
      {
      }

      public void OnLight( LightNode node )
      {
      }

      public RenderNodeAction OnViewBegin( ViewNode node )
      {
        node.LevelOfDetail = 8;
        return 0;
      }

      public void OnViewEnd( ElementId elementId )
      {
      }

      public RenderNodeAction OnFaceBegin( FaceNode node )
      {
        return 0;
      }

      public void OnFaceEnd( FaceNode node )
      {
      }

      public RenderNodeAction OnElementBegin( ElementId elementId )
      {
        return 0;
      }

      public void OnElementEnd( ElementId elementId )
      {
      }

      public RenderNodeAction OnInstanceBegin( InstanceNode node )
      {
        return 0;
      }

      public void OnInstanceEnd( InstanceNode node )
      {
      }

      public RenderNodeAction OnLinkBegin( LinkNode node )
      {
        return 0;
      }

      public void OnLinkEnd( LinkNode node )
      {
      }

      public void OnMaterial( MaterialNode node )
      {
        if( this.includeMaterials )
        {
          if( node.MaterialId == ElementId.InvalidElementId )
          {
            return;
          }
          if( this.materialIds.Contains( node.MaterialId ) )
          {
            return;
          }
          this.materialIds.Add( node.MaterialId );
        }
      }
    }

    void TriangleCountReport( long nTriangles, int nMaterials )
    {
      string s = string.Format(
        "Total number of model triangles and materials: {0}, {1}",
        nTriangles, nMaterials );

      Debug.Print( s );
      TaskDialog.Show( "Triangle Count", s );
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      var app = commandData.Application;
      var uidoc = app.ActiveUIDocument;
      var doc = uidoc.Document;

      TriangleCounterContext context 
        = new TriangleCounterContext( 
          doc, null, TriangleCountReport );

      CustomExporter exporter 
        = new CustomExporter(
          doc, context );

      //exporter.IncludeFaces = false;
      //exporter.ShouldStopOnError = false;

      exporter.Export( doc.ActiveView );

      return Result.Succeeded;
    }
  }
}
