#region Header
//
// CmdItemExecuted.cs - list all ribbon panel command buttons and subscribe to Autodesk.Windows.ComponentManager.ItemExecuted
//
// Copyright (C) 2015-2018 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//
#endregion // Header

#region Namespaces
using System;
using System.Diagnostics;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Windows;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.ReadOnly )]
  class CmdItemExecuted : IExternalCommand
  {
    static bool _subscribed = false;

    static void OnItemExecuted(
      object sender,
      Autodesk.Internal.Windows
        .RibbonItemExecutedEventArgs e )
    {
      string s = ( null == sender )
        ? "<nul>"
        : sender.ToString();

      Autodesk.Windows.RibbonItem parent = e.Parent;

      string p = ( null == parent )
        ? "<nul>"
        : parent.AutomationName.ToString();

      Debug.Print(
        "OnItemExecuted: {0} '{1}' in '{2}' cookie {3}",
        s, p, e.Item.AutomationName, e.Item.Cookie );
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      if( _subscribed )
      {
        Autodesk.Windows.ComponentManager.ItemExecuted
          -= OnItemExecuted;

        _subscribed = false;
      }
      else
      {
        RibbonTabCollection tabs
          = ComponentManager.Ribbon.Tabs;

        foreach( RibbonTab tab in tabs )
        {
          Debug.Print( "  {0} {1} '{2}'", tab,
            tab.GetType().Name, tab.AutomationName );

          if( tab.KeyTip == null )
          {
            // This tab is user defined.

            foreach( var panel in tab.Panels )
            {
              // Cannot convert type 'Autodesk.Windows.RibbonPanel' 
              // to 'Autodesk.Revit.UI.RibbonPanel' via a reference 
              // conversion, boxing conversion, unboxing conversion, 
              // wrapping conversion, or null type conversion.
              //
              //Autodesk.Revit.UI.RibbonPanel rp 
              //  = panel as Autodesk.Revit.UI.RibbonPanel;

              Autodesk.Windows.RibbonPanel rp
                = panel as Autodesk.Windows.RibbonPanel;

              Debug.Print( "    {0} {1}",
                panel.ToString(), panel.GetType().Name );

              foreach( var item in panel.Source.Items )
              {
                Autodesk.Windows.RibbonItem ri = item
                  as Autodesk.Windows.RibbonItem;

                string automationName = ri.AutomationName;

                Debug.Print( "      {0} {1} '{2}' {3}",
                  item.ToString(), item.GetType().Name,
                  automationName, ri.Cookie );
              }
            }
          }
        }

        Autodesk.Windows.ComponentManager.ItemExecuted
          += OnItemExecuted;

        _subscribed = true;
      }
      return Result.Succeeded;
    }
  }
}
