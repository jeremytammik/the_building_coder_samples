#region Namespaces

#endregion // Namespaces

namespace BuildingCoder
{
    #region Using Obsolete pre-Forge Unit API Functionality Deprecated in Revit 2021

#if USE_PRE_FORGE_UNIT_FUNCTIONALITY
  /// <summary>
  /// Implement extension methods for 
  /// the Revit Parameter class:
  /// - AsProjectUnitTypeDouble 
  /// - AsMetersValue
  /// Implement static method
  /// - ConvertParameterTypeToUnitType
  /// Written by Victor Chekalin and posted to 
  /// http://pastebin.com/ULHxU95E
  /// with a test command posted to
  /// http://pastebin.com/Pu26SPAN
  /// </summary>
  public static class ParameterUnitConverter
  {
    private const double METERS_IN_FEET = 0.3048;

  #region AsProjectUnitTypeDouble
    /// <summary>
    /// Get double value parameter in ProjectUnits
    /// </summary>
    public static double AsProjectUnitTypeDouble(
      this Parameter param)
    {
      if (param.StorageType != StorageType.Double)
        throw new NotSupportedException(
          "Parameter does not have double value");

      double imperialValue = param.AsDouble();

      Document document = param.Element.Document;

      UnitType ut = ConvertParameterTypeToUnitType(
        param.Definition.ParameterType);

      //FormatOptions fo = document.ProjectUnit // 2013
      //  .get_FormatOptions(ut);

      //DisplayUnitType dut = fo.Units; // 2014

      FormatOptions fo = document.GetUnits() // 2014
        .GetFormatOptions(ut);

      DisplayUnitType dut = fo.DisplayUnits; // 2014

      // Unit Converter
      // http://www.asknumbers.com

      switch (dut)
      {
  #region Length

        case DisplayUnitType.DUT_METERS:
          return imperialValue * METERS_IN_FEET; //feet
        case DisplayUnitType.DUT_CENTIMETERS:
          return imperialValue * METERS_IN_FEET * 100;
        case DisplayUnitType.DUT_DECIMAL_FEET:
          return imperialValue;
        case DisplayUnitType.DUT_DECIMAL_INCHES:
          return imperialValue * 12;
        case DisplayUnitType.DUT_FEET_FRACTIONAL_INCHES:
          NotSupported(dut);
          break;
        case DisplayUnitType.DUT_FRACTIONAL_INCHES:
          NotSupported(dut);
          break;
        case DisplayUnitType.DUT_METERS_CENTIMETERS:
          return imperialValue * METERS_IN_FEET; //feet
        case DisplayUnitType.DUT_MILLIMETERS:
          return imperialValue * METERS_IN_FEET * 1000;

  #endregion // Length

  #region Area

        case DisplayUnitType.DUT_SQUARE_FEET:
          return imperialValue;
        case DisplayUnitType.DUT_ACRES:
          return imperialValue * 1 / 43560.039;
        case DisplayUnitType.DUT_HECTARES:
          return imperialValue * 1 / 107639.104;
        case DisplayUnitType.DUT_SQUARE_CENTIMETERS:
          return imperialValue * Math.Pow(METERS_IN_FEET * 100, 2);
        case DisplayUnitType.DUT_SQUARE_INCHES:
          return imperialValue * Math.Pow(12, 2);
        case DisplayUnitType.DUT_SQUARE_METERS:
          return imperialValue * Math.Pow(METERS_IN_FEET, 2);
        case DisplayUnitType.DUT_SQUARE_MILLIMETERS:
          return imperialValue * Math.Pow(METERS_IN_FEET * 1000, 2);

  #endregion // Area

  #region Volume
        case DisplayUnitType.DUT_CUBIC_FEET:
          return imperialValue;
        case DisplayUnitType.DUT_CUBIC_CENTIMETERS:
          return imperialValue * Math.Pow(METERS_IN_FEET * 100, 3);
        case DisplayUnitType.DUT_CUBIC_INCHES:
          return imperialValue * Math.Pow(12, 3);
        case DisplayUnitType.DUT_CUBIC_METERS:
          return imperialValue * Math.Pow(METERS_IN_FEET, 3);
        case DisplayUnitType.DUT_CUBIC_MILLIMETERS:
          return imperialValue * Math.Pow(METERS_IN_FEET * 1000, 3);
        case DisplayUnitType.DUT_CUBIC_YARDS:
          return imperialValue * 1 / Math.Pow(3, 3);
        case DisplayUnitType.DUT_GALLONS_US:
          return imperialValue * 7.5;
        case DisplayUnitType.DUT_LITERS:
          return imperialValue * 28.31684;

  #endregion // Volume

        default:
          NotSupported(dut);
          break;
      }
      throw new NotSupportedException();
    }
  #endregion // AsProjectUnitTypeDouble

  #region AsMetersValue
    /// <summary>
    /// Get double value of parameter in meters unit. 
    /// E.g. Length in meters, Area in square meters 
    /// and Volume in Cubic_meters.
    /// Other units not supported yet.
    /// </summary>
    public static double AsMetersValue(
      this Parameter param)
    {
      if (param.StorageType != StorageType.Double)
        throw new NotSupportedException(
          "Parameter does not have double value");

      double imperialValue = param.AsDouble();

      UnitType ut = ConvertParameterTypeToUnitType(
        param.Definition.ParameterType);

      switch (ut)
      {
        case UnitType.UT_Length:
          return imperialValue * METERS_IN_FEET;

        case UnitType.UT_Area:
          return imperialValue * Math.Pow(
            METERS_IN_FEET, 2);

        case UnitType.UT_Volume:
          return imperialValue * Math.Pow(
            METERS_IN_FEET, 3);
      }
      throw new NotSupportedException();
    }
  #endregion // AsMetersValue

  #region ConvertParameterTypeToUnitType
    /// <summary>
    /// Returns the UnitType for the given 
    /// ParameterType (where possible).
    /// </summary>
    /// <param name="parameterType">The ParameterType 
    /// for which we need to know the UnitType</param>
    /// <exception cref="ArgumentException">Thrown if 
    /// it is not possible to convert the ParameterType 
    /// to a UnitType.</exception>
    public static UnitType
      ConvertParameterTypeToUnitType(
        ParameterType parameterType)
    {
      // The conversion will from the in-memory dictionary.

      //foreach( KeyValuePair<UnitType, ParameterType> kvp 
      //  in BuildUnitTypeToParameterTypeMapping() )
      //{
      //  if( kvp.Value == parameterType )
      //  {
      //    return kvp.Key;
      //  }
      //}

      if (_map_parameter_type_to_unit_type.ContainsKey(
        parameterType))
      {
        return _map_parameter_type_to_unit_type[
          parameterType];
      }
      else
      {
        // If we made it this far, there's 
        // no entry in the dictionary.

        throw new ArgumentException(
          "Cannot convert ParameterType '"
            + parameterType.ToString()
            + "' to a UnitType.");
      }
    }
  #endregion // ConvertParameterTypeToUnitType

  #region Private Helper Functions
    static void NotSupported(DisplayUnitType dut)
    {
      throw new NotSupportedException(
        string.Format("Not supported type: {0} - {1}",
          dut.ToString(),
          LabelUtils.GetLabelFor(dut)));
    }

    /// <summary>
    /// Map ParameterType enum to corresponding
    /// UnitType enum value.
    /// </summary>
    static Dictionary<ParameterType, UnitType>
      _map_parameter_type_to_unit_type
        = new Dictionary<ParameterType, UnitType>()
    {
      // This data could come from a file, 
      // or (better yet) from Revit itself...

      {ParameterType.Angle, UnitType.UT_Angle},
      {ParameterType.Area, UnitType.UT_Area},
      {ParameterType.AreaForce, UnitType.UT_AreaForce},
      {ParameterType.AreaForcePerLength, UnitType.UT_AreaForcePerLength},
      //map.Add(UnitType.UT_AreaForceScale, ParameterType.???);
      {ParameterType.ColorTemperature, UnitType.UT_Color_Temperature},
      {ParameterType.Currency, UnitType.UT_Currency},
      //map.Add(UnitType.UT_DecSheetLength, ParameterType.???);
      {ParameterType.ElectricalApparentPower, UnitType.UT_Electrical_Apparent_Power},
      {ParameterType.ElectricalCurrent, UnitType.UT_Electrical_Current},
      {ParameterType.ElectricalEfficacy, UnitType.UT_Electrical_Efficacy},
      {ParameterType.ElectricalFrequency, UnitType.UT_Electrical_Frequency},
      {ParameterType.ElectricalIlluminance, UnitType.UT_Electrical_Illuminance},
      {ParameterType.ElectricalLuminance, UnitType.UT_Electrical_Luminance},
      {ParameterType.ElectricalLuminousFlux, UnitType.UT_Electrical_Luminous_Flux},
      {ParameterType.ElectricalLuminousIntensity, UnitType.UT_Electrical_Luminous_Intensity},
      {ParameterType.ElectricalPotential, UnitType.UT_Electrical_Potential},
      {ParameterType.ElectricalPower, UnitType.UT_Electrical_Power},
      {ParameterType.ElectricalPowerDensity, UnitType.UT_Electrical_Power_Density},
      {ParameterType.ElectricalWattage, UnitType.UT_Electrical_Wattage},
      {ParameterType.Force, UnitType.UT_Force},
      {ParameterType.ForceLengthPerAngle, UnitType.UT_ForceLengthPerAngle},
      {ParameterType.ForcePerLength, UnitType.UT_ForcePerLength},
      //map.Add(UnitType.UT_ForceScale, ParameterType.???);
      {ParameterType.HVACAirflow, UnitType.UT_HVAC_Airflow},
      {ParameterType.HVACAirflowDensity, UnitType.UT_HVAC_Airflow_Density},
      {ParameterType.HVACAirflowDividedByCoolingLoad, UnitType.UT_HVAC_Airflow_Divided_By_Cooling_Load},
      {ParameterType.HVACAirflowDividedByVolume, UnitType.UT_HVAC_Airflow_Divided_By_Volume},
      {ParameterType.HVACAreaDividedByCoolingLoad, UnitType.UT_HVAC_Area_Divided_By_Cooling_Load},
      {ParameterType.HVACAreaDividedByHeatingLoad, UnitType.UT_HVAC_Area_Divided_By_Heating_Load},
      {ParameterType.HVACCoefficientOfHeatTransfer, UnitType.UT_HVAC_CoefficientOfHeatTransfer},
      {ParameterType.HVACCoolingLoad, UnitType.UT_HVAC_Cooling_Load},
      {ParameterType.HVACCoolingLoadDividedByArea, UnitType.UT_HVAC_Cooling_Load_Divided_By_Area},
      {ParameterType.HVACCoolingLoadDividedByVolume, UnitType.UT_HVAC_Cooling_Load_Divided_By_Volume},
      {ParameterType.HVACCrossSection, UnitType.UT_HVAC_CrossSection},
      {ParameterType.HVACDensity, UnitType.UT_HVAC_Density},
      {ParameterType.HVACDuctSize, UnitType.UT_HVAC_DuctSize},
      {ParameterType.HVACEnergy, UnitType.UT_HVAC_Energy},
      {ParameterType.HVACFactor, UnitType.UT_HVAC_Factor},
      {ParameterType.HVACFriction, UnitType.UT_HVAC_Friction},
      {ParameterType.HVACHeatGain, UnitType.UT_HVAC_HeatGain},
      {ParameterType.HVACHeatingLoad, UnitType.UT_HVAC_Heating_Load},
      {ParameterType.HVACHeatingLoadDividedByArea, UnitType.UT_HVAC_Heating_Load_Divided_By_Area},
      {ParameterType.HVACHeatingLoadDividedByVolume, UnitType.UT_HVAC_Heating_Load_Divided_By_Volume},
      {ParameterType.HVACPower, UnitType.UT_HVAC_Power},
      {ParameterType.HVACPowerDensity, UnitType.UT_HVAC_Power_Density},
      {ParameterType.HVACPressure, UnitType.UT_HVAC_Pressure},
      {ParameterType.HVACRoughness, UnitType.UT_HVAC_Roughness},
      {ParameterType.HVACSlope, UnitType.UT_HVAC_Slope},
      {ParameterType.HVACTemperature, UnitType.UT_HVAC_Temperature},
      {ParameterType.HVACVelocity, UnitType.UT_HVAC_Velocity},
      {ParameterType.HVACViscosity, UnitType.UT_HVAC_Viscosity},
      {ParameterType.Length, UnitType.UT_Length},
      {ParameterType.LinearForce, UnitType.UT_LinearForce},
      {ParameterType.LinearForceLengthPerAngle, UnitType.UT_LinearForceLengthPerAngle},
      {ParameterType.LinearForcePerLength, UnitType.UT_LinearForcePerLength},
      // map.Add(UnitType.UT_LinearForceScale, ParameterType.???);
      {ParameterType.LinearMoment, UnitType.UT_LinearMoment},
      // map.Add(UnitType.UT_LinearMomentScale, ParameterType.???);
      {ParameterType.Moment, UnitType.UT_Moment},
      ///map.Add(UnitType.UT_MomentScale, ParameterType.???);
      {ParameterType.Number, UnitType.UT_Number},
      {ParameterType.PipeSize, UnitType.UT_PipeSize},
      {ParameterType.PipingDensity, UnitType.UT_Piping_Density},
      {ParameterType.PipingFlow, UnitType.UT_Piping_Flow},
      {ParameterType.PipingFriction, UnitType.UT_Piping_Friction},
      {ParameterType.PipingPressure, UnitType.UT_Piping_Pressure},
      {ParameterType.PipingRoughness, UnitType.UT_Piping_Roughness},
      {ParameterType.PipingSlope, UnitType.UT_Piping_Slope},
      {ParameterType.PipingTemperature, UnitType.UT_Piping_Temperature},
      {ParameterType.PipingVelocity, UnitType.UT_Piping_Velocity},
      {ParameterType.PipingViscosity, UnitType.UT_Piping_Viscosity},
      {ParameterType.PipingVolume, UnitType.UT_Piping_Volume},
      //map.Add(UnitType.UT_SheetLength, ParameterType.???);
      //map.Add(UnitType.UT_SiteAngle, ParameterType.???);
      {ParameterType.Slope, UnitType.UT_Slope},
      {ParameterType.Stress, UnitType.UT_Stress},
      //{ParameterType.TemperalExp, UnitType.UT_TemperalExp},
      {ParameterType.UnitWeight, UnitType.UT_UnitWeight},
      {ParameterType.Volume, UnitType.UT_Volume},
      {ParameterType.WireSize, UnitType.UT_WireSize},
    };

#if NEED_BuildUnitTypeToParameterTypeMapping_METHOD
    /// <summary>
    /// This method builds up the dictionary object 
    /// which relates the UnitType enum values to 
    /// their ParameterType enum counterpart values.
    /// </summary>
    /// <example>
    /// UnitType.UT_Angle = ParameterType.Angle
    /// </example>
    private static Dictionary<UnitType, ParameterType>
      BuildUnitTypeToParameterTypeMapping()
    {
      // This data could come from a file, or (better yet) from Revit itself...

      // Use a local variable with a shorter name as 
      // a quick reference to the module-level 
      // referenced dictionary

      Dictionary<UnitType, ParameterType> result = new Dictionary<UnitType, ParameterType>();

      result.Add( UnitType.UT_Angle, ParameterType.Angle );
      result.Add( UnitType.UT_Area, ParameterType.Area );
      result.Add( UnitType.UT_AreaForce, ParameterType.AreaForce );
      result.Add( UnitType.UT_AreaForcePerLength, ParameterType.AreaForcePerLength );
      //map.Add(UnitType.UT_AreaForceScale, ParameterType.???);
      result.Add( UnitType.UT_Color_Temperature, ParameterType.ColorTemperature );
      result.Add( UnitType.UT_Currency, ParameterType.Currency );
      //map.Add(UnitType.UT_DecSheetLength, ParameterType.???);
      result.Add( UnitType.UT_Electrical_Apparent_Power, ParameterType.ElectricalApparentPower );
      result.Add( UnitType.UT_Electrical_Current, ParameterType.ElectricalCurrent );
      result.Add( UnitType.UT_Electrical_Efficacy, ParameterType.ElectricalEfficacy );
      result.Add( UnitType.UT_Electrical_Frequency, ParameterType.ElectricalFrequency );
      result.Add( UnitType.UT_Electrical_Illuminance, ParameterType.ElectricalIlluminance );
      result.Add( UnitType.UT_Electrical_Luminance, ParameterType.ElectricalLuminance );
      result.Add( UnitType.UT_Electrical_Luminous_Flux, ParameterType.ElectricalLuminousFlux );
      result.Add( UnitType.UT_Electrical_Luminous_Intensity, ParameterType.ElectricalLuminousIntensity );
      result.Add( UnitType.UT_Electrical_Potential, ParameterType.ElectricalPotential );
      result.Add( UnitType.UT_Electrical_Power, ParameterType.ElectricalPower );
      result.Add( UnitType.UT_Electrical_Power_Density, ParameterType.ElectricalPowerDensity );
      result.Add( UnitType.UT_Electrical_Wattage, ParameterType.ElectricalWattage );
      result.Add( UnitType.UT_Force, ParameterType.Force );
      result.Add( UnitType.UT_ForceLengthPerAngle, ParameterType.ForceLengthPerAngle );
      result.Add( UnitType.UT_ForcePerLength, ParameterType.ForcePerLength );
      //map.Add(UnitType.UT_ForceScale, ParameterType.???);
      result.Add( UnitType.UT_HVAC_Airflow, ParameterType.HVACAirflow );
      result.Add( UnitType.UT_HVAC_Airflow_Density, ParameterType.HVACAirflowDensity );
      result.Add( UnitType.UT_HVAC_Airflow_Divided_By_Cooling_Load, ParameterType.HVACAirflowDividedByCoolingLoad );
      result.Add( UnitType.UT_HVAC_Airflow_Divided_By_Volume, ParameterType.HVACAirflowDividedByVolume );
      result.Add( UnitType.UT_HVAC_Area_Divided_By_Cooling_Load, ParameterType.HVACAreaDividedByCoolingLoad );
      result.Add( UnitType.UT_HVAC_Area_Divided_By_Heating_Load, ParameterType.HVACAreaDividedByHeatingLoad );
      result.Add( UnitType.UT_HVAC_CoefficientOfHeatTransfer, ParameterType.HVACCoefficientOfHeatTransfer );
      result.Add( UnitType.UT_HVAC_Cooling_Load, ParameterType.HVACCoolingLoad );
      result.Add( UnitType.UT_HVAC_Cooling_Load_Divided_By_Area, ParameterType.HVACCoolingLoadDividedByArea );
      result.Add( UnitType.UT_HVAC_Cooling_Load_Divided_By_Volume, ParameterType.HVACCoolingLoadDividedByVolume );
      result.Add( UnitType.UT_HVAC_CrossSection, ParameterType.HVACCrossSection );
      result.Add( UnitType.UT_HVAC_Density, ParameterType.HVACDensity );
      result.Add( UnitType.UT_HVAC_DuctSize, ParameterType.HVACDuctSize );
      result.Add( UnitType.UT_HVAC_Energy, ParameterType.HVACEnergy );
      result.Add( UnitType.UT_HVAC_Factor, ParameterType.HVACFactor );
      result.Add( UnitType.UT_HVAC_Friction, ParameterType.HVACFriction );
      result.Add( UnitType.UT_HVAC_HeatGain, ParameterType.HVACHeatGain );
      result.Add( UnitType.UT_HVAC_Heating_Load, ParameterType.HVACHeatingLoad );
      result.Add( UnitType.UT_HVAC_Heating_Load_Divided_By_Area, ParameterType.HVACHeatingLoadDividedByArea );
      result.Add( UnitType.UT_HVAC_Heating_Load_Divided_By_Volume, ParameterType.HVACHeatingLoadDividedByVolume );
      result.Add( UnitType.UT_HVAC_Power, ParameterType.HVACPower );
      result.Add( UnitType.UT_HVAC_Power_Density, ParameterType.HVACPowerDensity );
      result.Add( UnitType.UT_HVAC_Pressure, ParameterType.HVACPressure );
      result.Add( UnitType.UT_HVAC_Roughness, ParameterType.HVACRoughness );
      result.Add( UnitType.UT_HVAC_Slope, ParameterType.HVACSlope );
      result.Add( UnitType.UT_HVAC_Temperature, ParameterType.HVACTemperature );
      result.Add( UnitType.UT_HVAC_Velocity, ParameterType.HVACVelocity );
      result.Add( UnitType.UT_HVAC_Viscosity, ParameterType.HVACViscosity );
      result.Add( UnitType.UT_Length, ParameterType.Length );
      result.Add( UnitType.UT_LinearForce, ParameterType.LinearForce );
      result.Add( UnitType.UT_LinearForceLengthPerAngle, ParameterType.LinearForceLengthPerAngle );
      result.Add( UnitType.UT_LinearForcePerLength, ParameterType.LinearForcePerLength );
      // map.Add(UnitType.UT_LinearForceScale, ParameterType.???);
      result.Add( UnitType.UT_LinearMoment, ParameterType.LinearMoment );
      // map.Add(UnitType.UT_LinearMomentScale, ParameterType.???);
      result.Add( UnitType.UT_Moment, ParameterType.Moment );
      ///map.Add(UnitType.UT_MomentScale, ParameterType.???);
      result.Add( UnitType.UT_Number, ParameterType.Number );
      result.Add( UnitType.UT_PipeSize, ParameterType.PipeSize );
      result.Add( UnitType.UT_Piping_Density, ParameterType.PipingDensity );
      result.Add( UnitType.UT_Piping_Flow, ParameterType.PipingFlow );
      result.Add( UnitType.UT_Piping_Friction, ParameterType.PipingFriction );
      result.Add( UnitType.UT_Piping_Pressure, ParameterType.PipingPressure );
      result.Add( UnitType.UT_Piping_Roughness, ParameterType.PipingRoughness );
      result.Add( UnitType.UT_Piping_Slope, ParameterType.PipingSlope );
      result.Add( UnitType.UT_Piping_Temperature, ParameterType.PipingTemperature );
      result.Add( UnitType.UT_Piping_Velocity, ParameterType.PipingVelocity );
      result.Add( UnitType.UT_Piping_Viscosity, ParameterType.PipingViscosity );
      result.Add( UnitType.UT_Piping_Volume, ParameterType.PipingVolume );
      //map.Add(UnitType.UT_SheetLength, ParameterType.???);
      //map.Add(UnitType.UT_SiteAngle, ParameterType.???);
      result.Add( UnitType.UT_Slope, ParameterType.Slope );
      result.Add( UnitType.UT_Stress, ParameterType.Stress );
      result.Add( UnitType.UT_TemperalExp, ParameterType.TemperalExp );
      result.Add( UnitType.UT_UnitWeight, ParameterType.UnitWeight );
      result.Add( UnitType.UT_Volume, ParameterType.Volume );
      result.Add( UnitType.UT_WireSize, ParameterType.WireSize );

      return result;
    }
#endif // NEED_BuildUnitTypeToParameterTypeMapping_METHOD

  #endregion Private Helper Functions
  }
#endif // USE_PRE_FORGE_UNIT_FUNCTIONALITY

    #endregion // Using Obsolete pre-Forge Unit API Functionality Deprecated in Revit 2021
}