#region Header
//
// CmdLinkedFileElements.cs - list elements in linked files
//
// Copyright (C) 2009-2020 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  public class ElementData
  {
    string _document;
    string _elementName;
    int _id;
    double _x;
    double _y;
    double _z;
    string _uniqueId;
    string _folder;

    public ElementData(
      string path,
      string elementName,
      int id,
      double x,
      double y,
      double z,
      string uniqueId )
    {
      int i = path.LastIndexOf( "\\" );
      _document = path.Substring( i + 1 );
      _elementName = elementName;
      _id = id;
      _x = x;
      _y = y;
      _z = z;
      _uniqueId = uniqueId;
      _folder = path.Substring( 0, i );
    }

    public string Document
    {
      get { return _document; }
    }
    public string Element
    {
      get { return _elementName; }
    }
    public int Id
    {
      get { return _id; }
    }
    public string X
    {
      get { return Util.RealString( _x ); }
    }
    public string Y
    {
      get { return Util.RealString( _y ); }
    }
    public string Z
    {
      get { return Util.RealString( _z ); }
    }
    public string UniqueId
    {
      get { return _uniqueId; }
    }
    public string Folder
    {
      get { return _folder; }
    }
  }

  [Transaction( TransactionMode.ReadOnly )]
  class CmdLinkedFileElements : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet highlightElements )
    {
      /*

      // retrieve all link elements:

      Document doc = app.ActiveUIDocument.Document;
      List<Element> links = GetElements(
        BuiltInCategory.OST_RvtLinks,
        typeof( Instance ), app, doc );

      // determine the link paths:

      DocumentSet docs = app.Documents;
      int n = docs.Size;
      Dictionary<string, string> paths
        = new Dictionary<string, string>( n );

      foreach( Document d in docs )
      {
        string path = d.PathName;
        int i = path.LastIndexOf( "\\" ) + 1;
        string name = path.Substring( i );
        paths.Add( name, path );
      }
      */

      // Retrieve lighting fixture element
      // data from linked documents:

      List<ElementData> data = new List<ElementData>();
      UIApplication app = commandData.Application;
      DocumentSet docs = app.Application.Documents;

      foreach( Document doc in docs )
      {
        FilteredElementCollector a
          = Util.GetElementsOfType( doc,
            typeof( FamilyInstance ),
            BuiltInCategory.OST_LightingFixtures );

        foreach( FamilyInstance e in a )
        {
          string name = e.Name;
          LocationPoint lp = e.Location as LocationPoint;
          if( null != lp )
          {
            XYZ p = lp.Point;
            data.Add( new ElementData( doc.PathName, e.Name,
              e.Id.IntegerValue, p.X, p.Y, p.Z, e.UniqueId ) );
          }
        }
      }

      // Display data:

      using( CmdLinkedFileElementsForm dlg = new CmdLinkedFileElementsForm( data ) )
      {
        dlg.ShowDialog();
      }

      return Result.Succeeded;
    }

    #region AddFaceBasedFamilyToLinks
    public void AddFaceBasedFamilyToLinks( Document doc )
    {
      ElementId alignedLinkId = new ElementId( 125929 );

      // Get symbol

      ElementId symbolId = new ElementId( 126580 );

      FamilySymbol fs = doc.GetElement( symbolId )
        as FamilySymbol;

      // Aligned

      RevitLinkInstance linkInstance = doc.GetElement(
        alignedLinkId ) as RevitLinkInstance;

      Document linkDocument = linkInstance
        .GetLinkDocument();

      FilteredElementCollector wallCollector
        = new FilteredElementCollector( linkDocument );

      wallCollector.OfClass( typeof( Wall ) );

      Wall targetWall = wallCollector.FirstElement()
        as Wall;

      Reference exteriorFaceRef
        = HostObjectUtils.GetSideFaces(
          targetWall, ShellLayerType.Exterior )
            .First<Reference>();

      Reference linkToExteriorFaceRef
        = exteriorFaceRef.CreateLinkReference(
          linkInstance );

      Line wallLine = ( targetWall.Location
        as LocationCurve ).Curve as Line;

      XYZ wallVector = ( wallLine.GetEndPoint( 1 )
        - wallLine.GetEndPoint( 0 ) ).Normalize();

      using( Transaction t = new Transaction( doc ) )
      {
        t.Start( "Add to face" );

        doc.Create.NewFamilyInstance(
          linkToExteriorFaceRef, XYZ.Zero,
          wallVector, fs );

        t.Commit();
      }
    }
    #endregion // AddFaceBasedFamilyToLinks

    #region Tag elements in linked documents
    /// <summary>
    /// Tag all walls in all linked documents
    /// </summary>
    void TagAllLinkedWalls( Document doc )
    {
      // Point near my wall
      XYZ xyz = new XYZ( -20, 20, 0 );

      // At first need to find our links
      FilteredElementCollector collector
        = new FilteredElementCollector( doc )
          .OfClass( typeof( RevitLinkInstance ) );

      foreach( Element elem in collector )
      {
        // Get linkInstance
        RevitLinkInstance instance = elem 
          as RevitLinkInstance;

        // Get linkDocument
        Document linkDoc = instance.GetLinkDocument();

        // Get linkType
        RevitLinkType type = doc.GetElement( 
          instance.GetTypeId() ) as RevitLinkType;

        // Check if link is loaded
        if( RevitLinkType.IsLoaded( doc, type.Id ) )
        {
          // Find walls for tagging
          FilteredElementCollector walls
            = new FilteredElementCollector( linkDoc )
              .OfCategory( BuiltInCategory.OST_Walls )
              .OfClass( typeof( Wall ) );

          // Create reference
          foreach( Wall wall in walls )
          {
            Reference newRef = new Reference( wall )
              .CreateLinkReference( instance );

            // Create transaction
            using( Transaction tx = new Transaction( doc ) )
            {
              tx.Start( "Create tags" );

              IndependentTag newTag = IndependentTag.Create( 
                doc, doc.ActiveView.Id, newRef, true, 
                TagMode.TM_ADDBY_MATERIAL, 
                TagOrientation.Horizontal, xyz );

              // Use TaggedElementId.LinkInstanceId and 
              // TaggedElementId.LinkInstanceId to retrieve 
              // the id of the tagged link and element:

              LinkElementId linkId = newTag.TaggedElementId;
              ElementId linkInstanceId = linkId.LinkInstanceId;
              ElementId linkedElementId = linkId.LinkedElementId;

              tx.Commit();
            }
          }
        }
      }
    }
    #endregion // Tag elements in linked documents

    #region Pick face on element in linked document
    static IEnumerable<Document> GetLinkedDocuments( 
      Document doc )
    {
      throw new NotImplementedException();
    }

    public static Face SelectFace( UIApplication uiapp )
    {
      Document doc = uiapp.ActiveUIDocument.Document;

      IEnumerable<Document> doc2 = GetLinkedDocuments( 
        doc );

      Autodesk.Revit.UI.Selection.Selection sel 
        = uiapp.ActiveUIDocument.Selection;

      Reference pickedRef = sel.PickObject( 
        Autodesk.Revit.UI.Selection.ObjectType.PointOnElement, 
        "Please select a Face" );

      Element elem = doc.GetElement( pickedRef.ElementId );

      Type et = elem.GetType();

      if( typeof( RevitLinkType ) == et 
        || typeof( RevitLinkInstance ) == et 
        || typeof( Instance ) == et )
      {
        foreach( Document d in doc2 )
        {
          if( elem.Name.Contains( d.Title ) )
          {
            Reference pickedRefInLink = pickedRef
              .CreateReferenceInLink();

            Element myElement = d.GetElement( 
              pickedRefInLink.ElementId );

            Face myGeometryObject = myElement
              .GetGeometryObjectFromReference( 
                pickedRefInLink ) as Face;

            return myGeometryObject;
          }
        }
      }
      else
      {
        Element myElement = doc.GetElement( 
          pickedRef.ElementId );

        Face myGeometryObject = myElement
          .GetGeometryObjectFromReference( pickedRef ) 
            as Face;

        return myGeometryObject;
      }
      return null;
    }
    #endregion // Pick face on element in linked document
  }
}
