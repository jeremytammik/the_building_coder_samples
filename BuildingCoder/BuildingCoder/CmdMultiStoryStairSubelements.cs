#region Header
//
// CmdMultistoryStairSubelements.cs - Access all subelements of all MultistoryStair instances
//
// Copyright (C) 2018 Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
#endregion // Header

#region Namespaces
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Diagnostics;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Manual )]
  class CmdMultistoryStairSubelements : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = commandData.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Document doc = uidoc.Document;

      // Retrieve selected multistory stairs, or all 
      // such elements, if nothing is pre-selected:

      List<Element> msss = new List<Element>();

      if( !Util.GetSelectedElementsOrAll(
        msss, uidoc, typeof( MultistoryStairs ) ) )
      {
        Selection sel = uidoc.Selection;
        message = ( 0 < sel.GetElementIds().Count )
          ? "Please select some floor elements."
          : "No floor elements found.";
        return Result.Failed;
      }

      int n = msss.Count;

      Debug.Print( "{0} multi story stair{1} selected{2}",
        n, Util.PluralSuffix( n ), Util.DotOrColon( n ) );

      foreach( MultistoryStairs mss in msss )
      {
        // Get the stairs by `GetAllStairsIds`, then 
        // call `Element.GetSubelements` to get the 
        // subelements of each stair.

        ISet<ElementId> ids = mss.GetAllStairsIds();

        n = ids.Count;

        Debug.Print(
          "Multi story stair '{0}' has {1} stair instance{2}{3}",
          Util.ElementDescription( mss ),
          n, Util.PluralSuffix( n ), Util.DotOrColon( n ) );

        foreach( ElementId id in ids )
        {
          Element e = doc.GetElement( id );

          Stairs stair = e as Stairs;

          Debug.Assert( null != stair, 
            "expected a stair element" );

          IList<Subelement> ses = e.GetSubelements();

          n = ses.Count;

          Debug.Print(
            "Multi story stair instance '{0}' has {1} subelement{2}{3}",
            Util.ElementDescription( e ),
            n, Util.PluralSuffix( n ), Util.DotOrColon( n ) );

          foreach( Subelement se in ses )
          {
            Debug.Print(
              "Subelement {0} of type {1}",
              se.UniqueId, se.TypeId.IntegerValue );

            Element e2 = doc.GetElement( se.UniqueId ); // null
            Element e2t = doc.GetElement( se.TypeId ); // StairsType
            IList<ElementId> ps = se.GetAllParameters(); // 24 parameters
            GeometryObject geo = se.GetGeometryObject( null );
          }
        }
      }
      return Result.Succeeded;
    }
  }
}
