#region Header
//
// CmdDutAbbreviation.cs - Test the display unit type abbreviation array
//
// Copyright (C) 2013 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
#endregion // Header

#region Namespaces
using System;
//using System.Collections.Generic;
//using System.IO;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Diagnostics;
//using Autodesk.Revit.UI.Selection;
#endregion // Namespaces

namespace BuildingCoder
{
  [Transaction( TransactionMode.ReadOnly )]
  class CmdDutAbbreviation : IExternalCommand
  {
    const string _s = "unexpected display unit type enumeration sequence";


    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
      Debug.Assert( 0 == (int) DisplayUnitType.DUT_METERS, _s );
      Debug.Assert( 1 == (int) DisplayUnitType.DUT_CENTIMETERS, _s );
      Debug.Assert( 2 == (int) DisplayUnitType.DUT_MILLIMETERS, _s );
      Debug.Assert( 3 == (int) DisplayUnitType.DUT_DECIMAL_FEET, _s );
      Debug.Assert( 4 == (int) DisplayUnitType.DUT_FEET_FRACTIONAL_INCHES, _s );
      Debug.Assert( 5 == (int) DisplayUnitType.DUT_FRACTIONAL_INCHES, _s );
      Debug.Assert( 6 == (int) DisplayUnitType.DUT_DECIMAL_INCHES, _s );
      Debug.Assert( 7 == (int) DisplayUnitType.DUT_ACRES, _s );
      Debug.Assert( 8 == (int) DisplayUnitType.DUT_HECTARES, _s );
      Debug.Assert( 9 == (int) DisplayUnitType.DUT_METERS_CENTIMETERS, _s );
      Debug.Assert( 10 == (int) DisplayUnitType.DUT_CUBIC_YARDS, _s );
      Debug.Assert( 11 == (int) DisplayUnitType.DUT_SQUARE_FEET, _s );
      Debug.Assert( 12 == (int) DisplayUnitType.DUT_SQUARE_METERS, _s );
      Debug.Assert( 13 == (int) DisplayUnitType.DUT_CUBIC_FEET, _s );
      Debug.Assert( 14 == (int) DisplayUnitType.DUT_CUBIC_METERS, _s );
      Debug.Assert( 15 == (int) DisplayUnitType.DUT_DECIMAL_DEGREES, _s );
      Debug.Assert( 16 == (int) DisplayUnitType.DUT_DEGREES_AND_MINUTES, _s );
      Debug.Assert( 17 == (int) DisplayUnitType.DUT_GENERAL, _s );
      Debug.Assert( 18 == (int) DisplayUnitType.DUT_FIXED, _s );
      Debug.Assert( 19 == (int) DisplayUnitType.DUT_PERCENTAGE, _s );
      Debug.Assert( 20 == (int) DisplayUnitType.DUT_SQUARE_INCHES, _s );
      Debug.Assert( 21 == (int) DisplayUnitType.DUT_SQUARE_CENTIMETERS, _s );
      Debug.Assert( 22 == (int) DisplayUnitType.DUT_SQUARE_MILLIMETERS, _s );
      Debug.Assert( 23 == (int) DisplayUnitType.DUT_CUBIC_INCHES, _s );
      Debug.Assert( 24 == (int) DisplayUnitType.DUT_CUBIC_CENTIMETERS, _s );
      Debug.Assert( 25 == (int) DisplayUnitType.DUT_CUBIC_MILLIMETERS, _s );
      Debug.Assert( 26 == (int) DisplayUnitType.DUT_LITERS, _s );

      DisplayUnitType n 
        = DisplayUnitType.DUT_GALLONS_US;

      Debug.Print( "Here is a list of the first {0} "
        + "display unit types with The Building Coder "
        + "abbreviation and the valid unit symbols:\n",
        (int) n );

      for( DisplayUnitType i = DisplayUnitType.DUT_METERS;
        i < n; ++i )
      {
        Debug.Print( "{0,6} {1} valid unit symbols: {2}", 
          Util.DisplayUnitTypeAbbreviation[(int)i],
          LabelUtils.GetLabelFor( i ),
          //UnitFormatUtils.Format( UnitType. ???
          //UnitUtils.ConvertFromInternalUnits( 1, i ),
          string.Join( ", ", FormatOptions
            .GetValidUnitSymbols( i )
            .Select<UnitSymbolType,string>( 
              u => Util.UnitSymbolTypeString( u ) ) ),
          
          i );

      }
      return Result.Succeeded;
    }
  }
}
