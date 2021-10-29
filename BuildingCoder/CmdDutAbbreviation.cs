#region Header

//
// CmdDutAbbreviation.cs - Test the display unit type abbreviation array
//
// Copyright (C) 2013-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

#endregion // Namespaces

namespace BuildingCoder
{
    #region Using Obsolete pre-Forge Unit API Functionality Deprecated in Revit 2021

#if USE_PRE_FORGE_UNIT_FUNCTIONALITY
  /// <summary>
  /// Map each DisplayUnitType to a list of all the 
  /// UnitType values that it might be used for, e.g.
  /// Meters is mapped to the following 21 values:
  /// Length, SheetLength, HVAC_DuctSize, HVAC_Roughness, 
  /// PipeSize, Piping_Roughness, WireSize, DecSheetLength,
  /// Electrical_CableTraySize, Electrical_ConduitSize, 
  /// Reinforcement_Length, HVAC_DuctInsulationThickness, 
  /// HVAC_DuctLiningThickness, PipeInsulationThickness, 
  /// Bar_Diameter, Crack_Width, Displacement_Deflection, 
  /// Reinforcement_Cover, Reinforcement_Spacing, 
  /// Section_Dimension, Section_Property.
  /// </summary>
  class MapDutToUt : Dictionary<DisplayUnitType, List<UnitType>>
  {
    public MapDutToUt()
    {
      IList<DisplayUnitType> duts;

      Array a = Enum.GetValues( typeof( UnitType ) );

      foreach( UnitType ut in a )
      {
        // Skip the UT_Undefined and UT_Custom entries; 
        // GetValidDisplayUnits throws ArgumentException 
        // on them, saying "unitType is an invalid unit 
        // type.  See UnitUtils.IsValidUnitType() and 
        // UnitUtils.GetValidUnitTypes()."

        if( UnitType.UT_Undefined == ut
          || UnitType.UT_Custom == ut )
        {
          continue;
        }

        duts = UnitUtils.GetValidDisplayUnits( ut );

        foreach( DisplayUnitType dut in duts )
        {
          //Debug.Assert( !ContainsKey( dut ), 
          //  "unexpected duplicate DisplayUnitType key" );

          if( !ContainsKey( dut ) )
          {
            Add( dut, new List<UnitType>( 1 ) );
          }
          this[dut].Add( ut );
        }
      }
    }
  }

  [Transaction( TransactionMode.ReadOnly )]
  class CmdDutAbbreviation : IExternalCommand
  {
    const string _s = "unexpected display unit type "
      + "enumeration sequence";

    public Result Execute(
      ExternalCommandData commandData,
      ref string message,
      ElementSet elements )
    {
#region Assertions
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
#endregion // Assertions

      MapDutToUt map_dut_to_ut = new MapDutToUt();

      DisplayUnitType n
        = DisplayUnitType.DUT_GALLONS_US;

      Debug.Print( "Here is a list of the first {0} "
        + "display unit types with official Revit API "
        + "LabelUtils, hard-coded The Building Coder "
        + "abbreviations and valid unit symbols:\n",
        (int) n - 1 );

      string unit_types, valid_unit_symbols;

      for( DisplayUnitType i = DisplayUnitType
        .DUT_METERS; i < n; ++i )
      {
        List<string> uts = new List<string>(
          map_dut_to_ut[i]
            .Select<UnitType, string>(
              u => u.ToString().Substring( 3 ) ) );

        int m = uts.Count;

        unit_types = 4 > m
          ? string.Join( ", ", uts )
          : string.Format( "{0}, {1} and {2} more",
            uts[0], uts[1], m - 2 );

        valid_unit_symbols = string.Join( ", ",
          FormatOptions.GetValidUnitSymbols( i )
            .Where( u => UnitSymbolType.UST_NONE != u )
            .Select<UnitSymbolType, string>(
              u => LabelUtils.GetLabelFor( u )
                + "/" + Util.UnitSymbolTypeString( u ) ) );

        Debug.Print( "{0,6} - {1} - {2}: {3}",
          Util.DisplayUnitTypeAbbreviation[(int) i],
          LabelUtils.GetLabelFor( i ),
          unit_types,
          //i
          //UnitFormatUtils.Format( UnitType. ???
          //UnitUtils.ConvertFromInternalUnits( 1, i ),
          valid_unit_symbols );
      }
      return Result.Succeeded;
    }
  }
#endif // USE_PRE_FORGE_UNIT_FUNCTIONALITY

    #endregion // Using Obsolete pre-Forge Unit API Functionality Deprecated in Revit 2021
}