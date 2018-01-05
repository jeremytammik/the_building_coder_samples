#region Header
//
// CmdViewsShowingElements.cs - determine all views displaying a given set of elements
//
// By Colin, cshha, 
// http://forums.autodesk.com/t5/user/viewprofilepage/user-id/1162312
// published in 
// http://forums.autodesk.com/t5/Revit-API/Revision-help-which-views-show-this-object/m-p/5029772
//
// Copyright (C) 2014-2018 by Jeremy Tammik,
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
  /// <summary>
  /// Revit Document and IEnumerable<Element>
  /// extension methods.
  /// </summary>
  static class ExtensionMethods
  {
    /// <summary>
    /// View extension predicate method: does 
    /// this view intersect the given bounding box?
    /// </summary>
    public static bool IntersectsBoundingBox(
      this View view,
      BoundingBoxXYZ targetBoundingBox )
    {
      Document doc = view.Document;
      var viewBoundingBox = view.CropBox;

      if( !view.CropBoxActive )
      {
        using( Transaction tr = new Transaction( doc ) )
        {
          //If the cropbox is not active we can't 
          //extract the boundingbox (we rollback so we 
          //don't change anything and also increase 
          //performance)
          tr.Start( "Temp" );
          view.CropBoxActive = true;
          viewBoundingBox = view.CropBox;
          tr.RollBack();
        }
      }

      Outline viewOutline = null;

      if( view is ViewPlan )
      {
        var viewRange = ( view as ViewPlan ).GetViewRange();

        //We need to change the boundingbox Z-values because 
        //they are not correct (for some reason).

        var bottomXYZ = ( doc.GetElement( viewRange
          .GetLevelId( PlanViewPlane.BottomClipPlane ) )
            as Level ).Elevation
          + viewRange.GetOffset( PlanViewPlane.BottomClipPlane );

        var topXYZ = ( doc.GetElement( viewRange
          .GetLevelId( PlanViewPlane.CutPlane ) )
            as Level ).Elevation
          + viewRange.GetOffset( PlanViewPlane.CutPlane );

        viewOutline = new Outline( new XYZ(
          viewBoundingBox.Min.X, viewBoundingBox.Min.Y,
          bottomXYZ ), new XYZ( viewBoundingBox.Max.X,
          viewBoundingBox.Max.Y, topXYZ ) );
      }

      //this is where I try to handle viewsections. 
      //But I can't get it to work!!

      if( !viewBoundingBox.Transform.BasisY.IsAlmostEqualTo(
        XYZ.BasisY ) )
      {
        viewOutline = new Outline(
          new XYZ( viewBoundingBox.Min.X,
           viewBoundingBox.Min.Z, viewBoundingBox.Min.Y ),
          new XYZ( viewBoundingBox.Max.X,
            viewBoundingBox.Max.Z, viewBoundingBox.Max.Y ) );
      }

      using( var boundingBoxAsOutline = new Outline(
        targetBoundingBox.Min, targetBoundingBox.Max ) )
      {
        return boundingBoxAsOutline.Intersects(
          viewOutline, 0 );
      }
    }

    /// <summary>
    /// Return an enumeration of all views in this
    /// document that can display elements at all.
    /// </summary>
    static IEnumerable<View>
    FindAllViewsThatCanDisplayElements(
      this Document doc )
    {
      ElementMulticlassFilter filter
        = new ElementMulticlassFilter(
          new List<Type> {
            typeof( View3D ),
            typeof( ViewPlan ),
            typeof( ViewSection ) } );

      return new FilteredElementCollector( doc )
        .WherePasses( filter )
        .Cast<View>()
        .Where( v => !v.IsTemplate );
    }

    /// <summary>
    /// Return all views that display 
    /// any of the given elements.
    /// </summary>
    public static IEnumerable<View>
    FindAllViewsWhereAllElementsVisible(
      this IEnumerable<Element> elements )
    {
      if( null == elements )
      {
        throw new ArgumentNullException( "elements" );
      }

      //if( 0 == elements.Count )
      //{
      //  return new List<View>();
      //}

      Element e1 = elements.FirstOrDefault<Element>();

      if( null == e1 )
      {
        return new List<View>();
      }

      Document doc = e1.Document;

      IEnumerable<View> relevantViewList
        = doc.FindAllViewsThatCanDisplayElements();

      IEnumerable<ElementId> idsToCheck
        = ( from e in elements select e.Id );

      return (
        from v in relevantViewList
        let idList
          = new FilteredElementCollector( doc, v.Id )
            .WhereElementIsNotElementType()
            .ToElementIds()
        where !idsToCheck.Except( idList ).Any()
        select v );
    }

    /// <summary>
    /// Determine whether an element is visible in a view, 
    /// by Colin Stark, described in
    /// http://stackoverflow.com/questions/44012630/determine-is-a-familyinstance-is-visible-in-a-view
    /// </summary>
    public static bool IsElementVisibleInView( 
      this View view, 
      Element el )
    {
      if( view == null )
      {
        throw new ArgumentNullException( nameof( view ) );
      }

      if( el == null )
      {
        throw new ArgumentNullException( nameof( el ) );
      }

      // Obtain the element's document.

      Document doc = el.Document;

      ElementId elId = el.Id;

      // Create a FilterRule that searches 
      // for an element matching the given Id.
    
      FilterRule idRule = ParameterFilterRuleFactory
        .CreateEqualsRule( 
          new ElementId( BuiltInParameter.ID_PARAM ), 
          elId );

      var idFilter = new ElementParameterFilter( idRule );

      // Use an ElementCategoryFilter to speed up the 
      // search, as ElementParameterFilter is a slow filter.

      Category cat = el.Category;
      var catFilter = new ElementCategoryFilter( cat.Id );

      // Use the constructor of FilteredElementCollector 
      // that accepts a view id as a parameter to only 
      // search that view.
      // Also use the WhereElementIsNotElementType filter 
      // to eliminate element types.

      FilteredElementCollector collector =
          new FilteredElementCollector( doc, view.Id )
            .WhereElementIsNotElementType()
            .WherePasses( catFilter )
            .WherePasses( idFilter );

      // If the collector contains any items, then 
      // we know that the element is visible in the
      // given view.

      return collector.Any();
    }
  }

  /// <summary>
  /// Determine all views displaying 
  /// a given set of elements.
  /// </summary>
  [Transaction( TransactionMode.ReadOnly )]
  class CmdViewsShowingElements : IExternalCommand
  {
    public Result Execute(
      ExternalCommandData revit,
      ref string message,
      ElementSet elements )
    {
      UIApplication uiapp = revit.Application;
      UIDocument uidoc = uiapp.ActiveUIDocument;
      Document doc = uidoc.Document;

      // Retrieve pre-selected elements.

      ICollection<ElementId> ids
        = uidoc.Selection.GetElementIds();

      if( 0 == ids.Count )
      {
        message = "Please pre-select some elements "
          + "before launching this command to list "
          + "the views displaying them.";

        return Result.Failed;
      }

      // Determine views displaying them.

      IEnumerable<Element> targets
        = from id in ids select doc.GetElement( id );

      IEnumerable<View> views = targets
        .FindAllViewsWhereAllElementsVisible();

      // Report results.

      string names = string.Join( ", ",
        ( from v in views select v.Name ) );

      int nElems = targets.Count<Element>();

      int nViews = names.Count<char>(
        c => ',' == c ) + 1;

      TaskDialog dlg = new TaskDialog( string.Format(
        "{0} element{1} are visible in {2} view{3}",
        nElems, Util.PluralSuffix( nElems ),
        nViews, Util.PluralSuffix( nViews ) ) );

      dlg.MainInstruction = names;

      dlg.Show();

      return Result.Succeeded;
    }
  }

  #region Align two views
  /// <summary>
  /// http://forums.autodesk.com/t5/Revit-API/Align-views-on-sheet/td-p/5048740
  /// </summary>
  class CmdAlignTwoViews : IExternalCommand
  {
    const BuiltInParameter _bipFarOffset
      = BuiltInParameter.VIEWER_BOUND_OFFSET_FAR;

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      UIApplication app = commandData.Application;
      Document doc = app.ActiveUIDocument.Document;

      using( Transaction trans = new Transaction( doc ) )
      {
        trans.Start( "Place views" );

        View frontView = doc.GetElement( new ElementId( 180041 ) ) as View;
        View leftView = doc.GetElement( new ElementId( 180032 ) ) as View;

        AssemblyInstance assemblyInst = doc.GetElement( new ElementId( 179915 ) ) as AssemblyInstance;

        ViewSheet vSheet = doc.GetElement( new ElementId( 180049 ) ) as ViewSheet;

        // Assume that the scale is the same for both views

        int scale = frontView.Scale;
        leftView.Scale = scale;

        // Save current crop box values

        BoundingBoxXYZ savedBoxFront = null, savedBoxLeft = null;
        bool frontCropActive, frontCropVisible, leftCropActive, leftCropVisible;
        double farClipFront = 0, farClipLeft = 0;
        Parameter param;
        Transform transformFront = frontView.CropBox.Transform;
        Transform transformLeft = leftView.CropBox.Transform;

        // Save old values. I have to store the farclip and reset it later

        savedBoxFront = frontView.CropBox;
        frontCropActive = frontView.CropBoxActive;
        frontCropVisible = frontView.CropBoxVisible;
        param = frontView.get_Parameter( _bipFarOffset );
        if( param != null )
        {
          farClipFront = param.AsDouble();
        }
        savedBoxLeft = leftView.CropBox;
        leftCropActive = leftView.CropBoxActive;
        leftCropVisible = leftView.CropBoxVisible;
        param = leftView.get_Parameter( _bipFarOffset );
        if( param != null )
        {
          farClipLeft = param.AsDouble();
        }

        // Here my approach differs from yours. 
        // I'm starting from the old bounding box to 
        // ensure that I get the correct transformation.
        // I tried to create a new Transformation but 
        // this didn't work.

        BoundingBoxXYZ newBoxFront = frontView.CropBox;
        newBoxFront.set_MinEnabled( 0, true );
        newBoxFront.set_MinEnabled( 1, true );
        newBoxFront.set_MinEnabled( 2, true );
        newBoxFront.Min = new XYZ( -2000, -2000, 0 );
        newBoxFront.set_MaxEnabled( 0, true );
        newBoxFront.set_MaxEnabled( 1, true );
        newBoxFront.set_MaxEnabled( 2, true );
        newBoxFront.Max = new XYZ( 2000, 2000, 0 );

        BoundingBoxXYZ newBoxLeft = leftView.CropBox;
        newBoxLeft.set_MinEnabled( 0, true );
        newBoxLeft.set_MinEnabled( 1, true );
        newBoxLeft.set_MinEnabled( 2, true );
        newBoxLeft.Min = new XYZ( -2000, -2000, 0 );
        newBoxLeft.set_MaxEnabled( 0, true );
        newBoxLeft.set_MaxEnabled( 1, true );
        newBoxLeft.set_MaxEnabled( 2, true );
        newBoxLeft.Max = new XYZ( 2000, 2000, 0 );

        frontView.CropBox = newBoxFront;
        leftView.CropBox = newBoxLeft;
        doc.Regenerate();
        frontView.CropBoxActive = true;
        leftView.CropBoxActive = true;

        doc.Regenerate();

        ElementId vid = vSheet.Id;
        XYZ p = XYZ.Zero;

        var vpFront = Viewport.Create( doc, vid, frontView.Id, p );
        var vpLeft = Viewport.Create( doc, vid, leftView.Id, p );

        doc.Regenerate();

        // Align lower left - works 
        // because crop boxes are same

        Outline outline1 = vpFront.GetBoxOutline();
        Outline outline2 = vpLeft.GetBoxOutline();
        XYZ min1 = outline1.MinimumPoint;
        XYZ min2 = outline2.MinimumPoint;
        XYZ diffToMove = min1 - min2;
        ElementTransformUtils.MoveElement( doc, vpLeft.Id, diffToMove );

        // Tranform the view such that the origin 
        // of the assemblyInstance for each view is 
        // on the middle of the sheet
        // 1) Move the views such that the assembly
        // Origin lies on the same on the origin of sheet

        p = assemblyInst.GetTransform().Origin;

        XYZ v = transformFront.Origin - p;

        XYZ translation = new XYZ(
          v.DotProduct( transformFront.BasisX ),
          v.DotProduct( transformFront.BasisY ), 0 )
            / scale;

        ElementTransformUtils.MoveElement( doc, vpFront.Id, translation );

        v = transformLeft.Origin - p;

        translation = new XYZ(
          v.DotProduct( transformLeft.BasisX ),
          v.DotProduct( transformLeft.BasisY ), 0 )
            / scale;

        ElementTransformUtils.MoveElement( doc, vpLeft.Id, translation );

        // 2) Move the views such that the assembly 
        // origin lies on the center of the sheet

        double width = 840 * 0.0032808399;
        double height = 594 * 0.0032808399;

        XYZ sheetMidpoint = ( vSheet.Origin + XYZ.BasisX * width + XYZ.BasisY * height ) / 2.0;
        ElementTransformUtils.MoveElement( doc, vpFront.Id, sheetMidpoint );
        ElementTransformUtils.MoveElement( doc, vpLeft.Id, sheetMidpoint );

        // Once the views are on the middle, move the 
        // left view to the left of the front view:
        // Do the correct translations to suit the 
        // defined layout

        translation = XYZ.BasisX * (
          -( ( outline1.MinimumPoint.X - outline1.MinimumPoint.X )
          / 2 + ( outline2.MinimumPoint.X - outline2.MinimumPoint.X ) + 1 ) );

        ElementTransformUtils.MoveElement( doc, vpLeft.Id, translation );

        doc.Regenerate();

        // Restore view crop boxes

        frontView.CropBox = savedBoxFront;
        frontView.CropBoxActive = frontCropActive;
        frontView.CropBoxVisible = frontCropVisible;

        param = frontView.get_Parameter( _bipFarOffset );

        if( param != null )
        {
          param.Set( farClipFront );
        }

        leftView.CropBox = savedBoxLeft;
        leftView.CropBoxActive = leftCropActive;
        leftView.CropBoxVisible = leftCropVisible;

        param = leftView.get_Parameter( _bipFarOffset );

        if( param != null )
        {
          param.Set( farClipLeft );
        }

        trans.Commit();
      }
      return Result.Succeeded;
    }

    /// <summary>
    /// Bring viewport to front by 
    /// deleting and recreating it.
    /// </summary>
    void ViewportBringToFront(
      ViewSheet sheet,
      Viewport viewport )
    {
      Document doc = sheet.Document;

      // Element id of the view in the viewport.

      ElementId viewId = viewport.ViewId;
      ElementId typeId = viewport.GetTypeId();
      XYZ boxCenter = viewport.GetBoxCenter();

      // The viewport might be pinned. Most overlayed
      // viewports are maintained pinned to prevent
      // accidental displacement. Record that state so 
      // the replacement viewport can reproduce it.

      bool pinnedState = viewport.Pinned;

      //View view = doc.ActiveView;

      using( Transaction t = new Transaction( doc ) )
      {
        t.Start( "Delete and Recreate Viewport" );

        // At least in Revit 2016, pinned viewports 
        // can be deleted without error.

        sheet.DeleteViewport( viewport );

        Viewport vvp = Viewport.Create( doc,
          sheet.Id, viewId, boxCenter );

        vvp.ChangeTypeId( typeId );
        vvp.Pinned = pinnedState;

        t.Commit();
      }
    }
  }
  #endregion // Align two views
}
