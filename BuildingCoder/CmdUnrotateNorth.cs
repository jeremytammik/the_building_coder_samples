#region Header

//
// CmdUnrotateNorth.cs - transform element location back to
// original coordinates to cancel effect of rotating project north
//
// Copyright (C) 2009-2021 by Jeremy Tammik,
// Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdUnrotateNorth : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var uidoc = app.ActiveUIDocument;
            var doc = uidoc.Document;

            #region Determine true north rotation

            var projectInfoElement
                = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_ProjectBasePoint)
                    .FirstElement();

            var bipAtn
                = BuiltInParameter.BASEPOINT_ANGLETON_PARAM;

            var patn = projectInfoElement.get_Parameter(
                bipAtn);

            var atn = patn.AsDouble();

            Debug.Print(
                "Angle to north from project info: {0}",
                Util.AngleString(atn));

            #endregion // Determine true north rotation

            //ElementSet els = uidoc.Selection.Elements; // 2014

            var ids = uidoc.Selection.GetElementIds(); // 2015

            if (1 != ids.Count)
            {
                message = "Please select a single element.";
            }
            else
            {
                //ElementSetIterator it = els.ForwardIterator();
                //it.MoveNext();
                //Element e = it.Current as Element; // 2014

                var e = doc.GetElement(ids.First()); // 2015

                XYZ p;
                if (!Util.GetElementLocation(out p, e))
                {
                    message
                        = "Selected element has no location defined.";

                    Debug.Print(message);
                }
                else
                {
                    var msg
                        = $"Selected element location: {Util.PointString(p)}";

                    XYZ pnp;
                    double x, y, pna;

                    foreach (ProjectLocation location
                        in doc.ProjectLocations)
                    {
                        //ProjectPosition projectPosition
                        //  = location.get_ProjectPosition( XYZ.Zero ); // 2017

                        var projectPosition
                            = location.GetProjectPosition(XYZ.Zero); // 2018

                        x = projectPosition.EastWest;
                        y = projectPosition.NorthSouth;
                        pnp = new XYZ(x, y, 0.0);
                        pna = projectPosition.Angle;

                        msg +=
                            $"\nAngle between project north and true north: {Util.AngleString(pna)}";

                        // Transform tr = Transform.get_Rotation( XYZ.Zero, XYZ.BasisZ, pna ); // 2013
                        var tr = Transform.CreateRotation(XYZ.BasisZ, pna); // 2014

                        //Transform tt = Transform.get_Translation( pnp ); // 2013
                        var tt = Transform.CreateTranslation(pnp); // 2014

                        var t = tt.Multiply(tr);

                        msg +=
                            $"\nUnrotated element location: {Util.PointString(tr.OfPoint(p))} {Util.PointString(tt.OfPoint(p))} {Util.PointString(t.OfPoint(p))}";

                        Util.InfoMsg(msg);
                    }
                }
            }

            return Result.Failed;
        }

        #region Get Sun Direction Adjusted for Project True North

        // Shared by Mohsen Assaqqaf in a comment on The Building Coder:
        // https://thebuildingcoder.typepad.com/blog/2013/06/sun-direction-shadow-calculation-and-wizard-update.html#comment-4614771756
        // I found that this method for getting the vector 
        // of the sun does not take into account the True 
        // North angle of the project, so I updated it 
        // myself using the following code:
        /// <summary>
        ///     Get sun direction adjusted for project true north
        /// </summary>
        private static XYZ GetSunDirection(View view)
        {
            var doc = view.Document;

            // Get sun and shadow settings from the 3D View

            var sunSettings
                = view.SunAndShadowSettings;

            // Set the initial direction of the sun 
            // at ground level (like sunrise level)

            var initialDirection = XYZ.BasisY;

            // Get the altitude of the sun from the sun settings

            var altitude = sunSettings.GetFrameAltitude(
                sunSettings.ActiveFrame);

            // Create a transform along the X axis 
            // based on the altitude of the sun

            var altitudeRotation = Transform
                .CreateRotation(XYZ.BasisX, altitude);

            // Create a rotation vector for the direction 
            // of the altitude of the sun

            var altitudeDirection = altitudeRotation
                .OfVector(initialDirection);

            // Get the azimuth from the sun settings of the scene

            var azimuth = sunSettings.GetFrameAzimuth(
                sunSettings.ActiveFrame);

            // Correct the value of the actual azimuth with true north

            // Get the true north angle of the project

            var projectInfoElement
                = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_ProjectBasePoint)
                    .FirstElement();

            var bipAtn
                = BuiltInParameter.BASEPOINT_ANGLETON_PARAM;

            var patn = projectInfoElement.get_Parameter(
                bipAtn);

            var trueNorthAngle = patn.AsDouble();

            // Add the true north angle to the azimuth

            var actualAzimuth = 2 * Math.PI - azimuth + trueNorthAngle;

            // Create a rotation vector around the Z axis

            var azimuthRotation = Transform
                .CreateRotation(XYZ.BasisZ, actualAzimuth);

            // Finally, calculate the direction of the sun

            var sunDirection = azimuthRotation.OfVector(
                altitudeDirection);

            // https://github.com/jeremytammik/the_building_coder_samples/issues/14
            // The resulting sun vector is pointing from the 
            // ground towards the sun and not from the sun 
            // towards the ground. I recommend reversing the 
            // vector at the end before it is returned so it 
            // points in the same direction as the sun rays.

            return -sunDirection;
        }

        #endregion // Get Sun Direction Adjusted for Project True North

        #region Set project location to city location

        private void SetSiteLocationToCity(Document doc)
        {
            var cities = doc.Application.Cities;
            var nCount = cities.Size;
            try
            {
                var item = cities.ForwardIterator();
                while (item != null)
                {
                    item.MoveNext();
                    var city = item.Current as City;
                    if (city.Name.Contains("中国") ||
                        city.Name.Contains("China"))
                    {
                        var transaction = new Transaction(doc, "Create Wall");
                        transaction.Start();

                        var projectLocation = doc.ActiveProjectLocation;

                        //SiteLocation site = projectLocation.SiteLocation; // 2017
                        var site = projectLocation.GetSiteLocation(); // 2018

                        // site.PlaceName = city.Name;
                        site.Latitude = city.Latitude; // latitude information
                        site.Longitude = city.Longitude; // longitude information
                        site.TimeZone = city.TimeZone; // TimeZone information
                        transaction.Commit();
                        break;
                    }
                }
            }
            catch (Exception ret)
            {
                Debug.Print(ret.Message);
            }
        }

        private void SetSiteLocationToCity2(Document doc)
        {
            var cities = doc.Application.Cities;

            foreach (City city in cities)
            {
                var s = city.Name;

                if (s.Contains("中国") || s.Contains("China"))
                {
                    using var t = new Transaction(doc);
                    t.Start("Set Site Location to City");

                    var projectLocation = doc.ActiveProjectLocation;

                    //SiteLocation site = projectLocation.SiteLocation; // 2017
                    var site = projectLocation.GetSiteLocation(); // 2018

                    // site.PlaceName = city.Name;
                    site.Latitude = city.Latitude; // latitude information
                    site.Longitude = city.Longitude; // longitude information
                    site.TimeZone = city.TimeZone; // TimeZone information

                    // SiteLocation property is read-only:
                    //projectLocation.SiteLocation = site;

                    t.Commit();

                    break;
                }
            }
        }

        #endregion // Set project location to city location
    }
}