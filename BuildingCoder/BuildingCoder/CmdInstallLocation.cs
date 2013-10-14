#region Header
//
// CmdInstallLocation.cs - determine Revit product install location
//
// Copyright (C) 2009-2013 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Win32;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.Automatic )]
  class CmdInstallLocation : IExternalCommand
  {
    const string _reg_path_uninstall
      = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

    const string _reg_path_for_flavour
      = @"SOFTWARE\Autodesk\Revit\Autodesk Revit {0} 2010";

    string RegPathForFlavour( ProductType flavour )
    {
      return string.Format( _reg_path_for_flavour, flavour );
    }

    /// <summary>
    /// Return a specific string value from a specific subkey of a given registry key.
    /// </summary>
    /// <param name="reg_path_key">Registry key path</param>
    /// <param name="subkey_name">Subkey name.</param>
    /// <param name="value_name">Value name.</param>
    /// <returns>Registry string value.</returns>
    string GetSubkeyValue(
      string reg_path_key,
      string subkey_name,
      string value_name )
    {
      using( RegistryKey key
        = Registry.LocalMachine.OpenSubKey( reg_path_key ) )
      {
        using( RegistryKey subkey
          = key.OpenSubKey( subkey_name ) )
        {
          return subkey.GetValue( value_name ) as string;
        }
      }
    }

    string GetRevitProductCode( string reg_path_product )
    {
      return GetSubkeyValue( reg_path_product,
        "Components", "ProductCode" );
    }

    string GetRevitInstallLocation( string product_code )
    {
      return GetSubkeyValue( _reg_path_uninstall,
        product_code, "InstallLocation" );
    }

    string FormatData(
      string description,
      string version_name,
      string product_code,
      string install_location )
    {
      return string.Format
        ( "{0}: {1}"
        + "\nProduct code: {2}"
        + "\nInstall location: {3}",
        description,
        version_name,
        product_code,
        install_location );
    }

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      Application app = commandData.Application.Application;

      string reg_path_product
        = RegPathForFlavour( app.Product );

      string product_code
        = GetRevitProductCode( reg_path_product );

      string install_location
        = GetRevitInstallLocation( product_code );

      string msg = FormatData(
        "Running application",
        app.VersionName,
        product_code,
        install_location );

      foreach( ProductType p in
        Enum.GetValues( typeof( ProductType ) ) )
      {
        try
        {
          reg_path_product = RegPathForFlavour( p );

          product_code = GetRevitProductCode(
            reg_path_product );

          install_location = GetRevitInstallLocation(
            product_code );

          msg += FormatData(
            "\n\nInstalled product",
            p.ToString(),
            product_code,
            install_location );
        }
        catch( Exception )
        {
        }
      }

      Util.InfoMsg( msg );

      return Result.Failed;
    }
  }
}
