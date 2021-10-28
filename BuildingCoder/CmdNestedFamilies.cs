#region Header

//
// CmdNestedFamilies.cs - list nested family files and instances in a family document
//
// Copyright (C) 2010-2021 by Jeremy Tammik, Autodesk Inc. All rights reserved.
//
// Keywords: The Building Coder Revit API C# .NET add-in.
//

#endregion // Header

#region Namespaces

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

#endregion // Namespaces

namespace BuildingCoder
{
    /// <summary>
    ///     This class contains functions for dealing with
    ///     nested families within a Revit family document.
    /// </summary>
    public class NestedFamilyFunctions
    {
        #region Public Methods

        /// <summary>
        ///     Returns a list of the nested family files in the
        ///     given family document whose name matches the given
        ///     family file name filter.  Useful for checking to
        ///     see if a family desired for nesting into the host
        ///     family document is already nested in.
        ///     Filtering is done with a simple Contains (substring)
        ///     check, so wildcards don't work.
        /// </summary>
        /// <param name="familyFileNameFilter">The portion of the family file loaded into the family document</param>
        /// <param name="familyDocument">The family document being queried</param>
        /// <param name="caseSensitiveFiltering">Whether or not the filter checking is case-sensitive</param>
        /// <example>
        ///     GetFilteredNestedFamilyFiles("window", document, false);
        /// </example>
        /// <remarks>
        ///     Because standard Revit filtering techniques fail when searching for nested families in a
        ///     family document, we have no choice but to iterate over all elements in the family.
        ///     While there usually aren't that many elements at the family level, nonetheless this method
        ///     has been built for speed.
        /// </remarks>
        /// <returns>
        ///     A collection of family file definitions nested into the given family document.
        /// </returns>
        public static IEnumerable<Family>
            GetFilteredNestedFamilyDefinitions(
                string familyFileNameFilter,
                Document familyDocument,
                bool caseSensitiveFiltering)
        {
            // Following good SOA practices, verify the
            // incoming data can be worked with.

            ValidateFamilyDocument(familyDocument); // Throws an exception if not a family doc

            // The filter can be null, the filter matching function checks for that.

#if _2010
      List<Family> oResult = new List<Family>();

      ElementIterator it = familyDocument.Elements;

      while( it.MoveNext() )
      {
        Element oElement = it.Current as Element;

        if( ( oElement is Family )
          && FilterMatches( oElement.Name,
            familyFileNameFilter, caseSensitiveFiltering ) )
        {
          oResult.Add( oElement as Family );
        }
      }
#endif // _2010

            var collector
                = new FilteredElementCollector(familyDocument);

            collector.OfClass(typeof(Family));

            var familiesMatching =
                from f in collector
                where FilterMatches(f.Name, familyFileNameFilter, caseSensitiveFiltering)
                select f;

            return familiesMatching.Cast<Family>();
        }

        /// <summary>
        ///     Returns a list of family instances found in the given family document whose family file
        ///     name matches the given familyFileNameFilter and whose type name matches the given
        ///     typeNameFilter.  If no filter values are provided (or they evaluate to the empty string
        ///     when trimmed) then all instances will be evaluated.
        ///     Filtering is done with a simple Contains (substring) check, so wildcards don't work.
        /// </summary>
        /// <param name="familyFileNameFilter">The portion of the nested family file name (or exact match) to find</param>
        /// <param name="typeNameFilter">The portion of the type name (or exact match) to find</param>
        /// <param name="familyDocument">The family document to search.</param>
        /// <param name="caseSensitiveFiltering">Whether or not the filter checking is case-sensitive</param>
        /// <example>
        ///     GetFilteredNestedFamilyInstances("window", "double-hung", document, false);
        /// </example>
        /// <remarks>
        ///     Because standard Revit filtering techniques fail when searching for nested families in a
        ///     family document, we have no choice but to iterate over all elements in the family.
        ///     While there usually aren't that many elements at the family level, nonetheless this method
        ///     has been built for MAXIMUM SPEED.
        /// </remarks>
        /// <returns>
        ///     A collection of matching nested family file instances.
        /// </returns>
        public static List<FamilyInstance>
            GetFilteredNestedFamilyInstances(
                string familyFileNameFilter,
                string typeNameFilter,
                Document familyDocument,
                bool caseSensitiveFiltering)
        {
            // Following good SOA practices, verify the
            // incoming data can be worked with.

            ValidateFamilyDocument(familyDocument); // Throws an exception if not a family doc

            // The filters can be null

            var oResult
                = new List<FamilyInstance>();

            FamilyInstance oFamilyInstanceCandidate;
            FamilySymbol oFamilySymbolCandidate;

            var oMatchingNestedFamilies
                = new List<Family>();

            var oAllFamilyInstances
                = new List<FamilyInstance>();

            var bFamilyFileNameFilterExists = true;
            var bTypeNameFilterExists = true;

            // Set up some fast-to-test boolean values, which will be
            // used for short-circuit Boolean evaluation later.

            if (string.IsNullOrEmpty(familyFileNameFilter)) bFamilyFileNameFilterExists = false;

            if (string.IsNullOrEmpty(typeNameFilter)) bTypeNameFilterExists = false;

            // Unfortunately detecting nested families in a family document requires iterating
            // over all the elements in the document, because the built-in filtering mechanism
            // doesn't work for this case.  However, families typically don't have nearly as many
            // elements as a whole project, so the performance hit shouldn't be too bad.

            // Still, the fastest performance should come by iterating over all elements in the given
            // family document exactly once, keeping subsets of the family instances found for
            // later testing against the nested family file matches found.

            var fFamilyClass = new ElementClassFilter(typeof(Family));
            var fFamInstClass = new ElementClassFilter(typeof(FamilyInstance));
            var f = new LogicalOrFilter(fFamilyClass, fFamInstClass);
            var collector = new FilteredElementCollector(familyDocument);
            collector.WherePasses(f);

            foreach (var e in collector)
                // See if this is a family file nested into the current family document.

                if (e is Family oNestedFamilyFileCandidate)
                {
                    // Must ask the "Element" version for it's name, because the Family object's
                    // name is always the empty string.
                    if (!bFamilyFileNameFilterExists
                        || FilterMatches(oNestedFamilyFileCandidate.Name,
                            familyFileNameFilter, caseSensitiveFiltering))
                        // This is a nested family file, and either no valid family file name filter was
                        // given, or the name of this family file matches the filter.

                        oMatchingNestedFamilies.Add(oNestedFamilyFileCandidate);
                }
                else
                {
                    // This element is not a nested family file definition, see if it's a
                    // nested family instance.

                    oFamilyInstanceCandidate
                        = e as FamilyInstance;

                    if (oFamilyInstanceCandidate != null)
                        // Just add the family instance to our "all" collection for later testing
                        // because we may not have yet found all the matching nested family file
                        // definitions.
                        oAllFamilyInstances.Add(oFamilyInstanceCandidate);
                }

            // See if any matching nested family file definitions were found.  Only do any
            // more work if at least one was found.
            foreach (var oMatchingNestedFamilyFile
                    in oMatchingNestedFamilies)
                // Count backwards through the all family instances list.  As we find
                // matches on this iteration through the matching nested families, we can
                // delete them from the candidates list to reduce the number of family
                // instance candidates to test for later matching nested family files to be tested
                for (var iCounter = oAllFamilyInstances.Count - 1;
                    iCounter >= 0;
                    iCounter--)
                {
                    oFamilyInstanceCandidate
                        = oAllFamilyInstances[iCounter];

#if _2010
          oFamilySymbolCandidate
            = oFamilyInstanceCandidate.ObjectType
              as FamilySymbol;
#endif // _2010

                    var id = oFamilyInstanceCandidate.GetTypeId();
                    oFamilySymbolCandidate = familyDocument.GetElement(id)
                        as FamilySymbol;

                    if (oFamilySymbolCandidate.Family.UniqueId
                        == oMatchingNestedFamilyFile.UniqueId)
                    {
                        // Only add this family instance to the results if there was no type name
                        // filter, or this family instance's type matches the given filter.

                        if (!bTypeNameFilterExists
                            || FilterMatches(oFamilyInstanceCandidate.Name,
                                typeNameFilter, caseSensitiveFiltering))
                            oResult.Add(oFamilyInstanceCandidate);

                        // No point in testing this one again,
                        // since we know its family definition
                        // has already been processed.

                        oAllFamilyInstances.RemoveAt(iCounter);
                    }
                } // Next family instance candidate

            return oResult;
        }

        /// <summary>
        ///     Returns a reference to the FAMILY parameter (as a simple Parameter data type) on the given instance
        ///     for the parameter with the given name.  Will return the parameter
        ///     whether it is an instance or type parameter.
        ///     Returns null if no parameter on the instance was found.
        /// </summary>
        /// <param name="nestedFamilyInstance">An instance of a nested family file</param>
        /// <param name="parameterName">The name of the desired parameter to get a reference to</param>
        /// <remarks>
        ///     Even though the data type returned is the more generic Parameter type, it will
        ///     actually be for the data of the internal FamilyParameter object.
        /// </remarks>
        /// <returns></returns>
        public static Parameter GetFamilyParameter(
            FamilyInstance nestedFamilyInstance,
            string parameterName)
        {
            // Following good SOA practices, verify the
            // incoming parameters before attempting to proceed.

            if (nestedFamilyInstance == null)
                throw new ArgumentNullException(
                    "nestedFamilyInstance");

            if (string.IsNullOrEmpty(parameterName))
                throw new ArgumentNullException(
                    "parameterName");

            Parameter oResult = null;

            // See if the parameter is an Instance parameter

            //oResult = nestedFamilyInstance.get_Parameter( parameterName ); // 2014

            Debug.Assert(2 > nestedFamilyInstance.GetParameters(parameterName).Count,
                "ascertain that there are not more than one parameter of the given name");

            oResult = nestedFamilyInstance.LookupParameter(parameterName); // 2015

            // No?  See if it's a Type parameter

            if (oResult == null)
            {
                //oResult = nestedFamilyInstance.Symbol.get_Parameter( parameterName ); // 2014

                Debug.Assert(2 > nestedFamilyInstance.Symbol.GetParameters(parameterName).Count,
                    "ascertain that there are not more than one parameter of the given name");

                oResult = nestedFamilyInstance.Symbol.LookupParameter(parameterName); // 2015
            }

            return oResult;
        }

        /// <summary>
        ///     This method takes an instance of a nested family and links a parameter on it to
        ///     a parameter on the given host family instance.  This allows a change at the host
        ///     level to automatically be sent down and applied to the nested family instance.
        /// </summary>
        /// <param name="hostFamilyDocument">The host family document to have one of its parameters be linked to a parameter on the given nested family instance</param>
        /// <param name="nestedFamilyInstance">The nested family whose parameter should be linked to a parameter on the host family</param>
        /// <param name="nestedFamilyParameterName">The name of the parameter on the nested family to link to the host family parameter</param>
        /// <param name="hostFamilyParameterNameToLink">The name of the parameter on the host family to link to a parameter on the given nested family instance</param>
        public static void
            LinkNestedFamilyParameterToHostFamilyParameter(
                Document hostFamilyDocument,
                FamilyInstance nestedFamilyInstance,
                string nestedFamilyParameterName,
                string hostFamilyParameterNameToLink)
        {
            // Following good SOA practices, verify the incoming
            // parameters before attempting to proceed.

            ValidateFamilyDocument(hostFamilyDocument); // Throws an exception if is not valid family doc

            if (nestedFamilyInstance == null)
                throw new ArgumentNullException(
                    "nestedFamilyInstance");

            if (string.IsNullOrEmpty(nestedFamilyParameterName))
                throw new ArgumentNullException(
                    "nestedFamilyParameterName");

            if (string.IsNullOrEmpty(hostFamilyParameterNameToLink))
                throw new ArgumentNullException(
                    "hostFamilyParameterNameToLink");

            var oNestedFamilyParameter
                = GetFamilyParameter(nestedFamilyInstance,
                    nestedFamilyParameterName);

            if (oNestedFamilyParameter == null)
                throw new Exception($"Parameter '{nestedFamilyParameterName}' was not found on the nested family '{nestedFamilyInstance.Symbol.Name}'");

            var oHostFamilyParameter
                = hostFamilyDocument.FamilyManager.get_Parameter(
                    hostFamilyParameterNameToLink);

            if (oHostFamilyParameter == null)
                throw new Exception($"Parameter '{hostFamilyParameterNameToLink}' was not found on the host family.");

            hostFamilyDocument.FamilyManager
                .AssociateElementParameterToFamilyParameter(
                    oNestedFamilyParameter, oHostFamilyParameter);
        }

        #endregion // Public Methods

        #region Private Helper Methods

        /// <summary>
        ///     Returns whether or not the nameToCheck matches the given filter.
        ///     This is done with a simple Contains check, so wildcards won't work.
        /// </summary>
        /// <param name="nameToCheck">The name (e.g. type name or family file name) to check for a match with the filter</param>
        /// <param name="filter">The filter to compare to</param>
        /// <param name="caseSensitiveComparison">Whether or not the comparison is case-sensitive.</param>
        /// <returns></returns>
        private static bool FilterMatches(
            string nameToCheck,
            string filter,
            bool caseSensitiveComparison)
        {
            var bResult = false;

            if (string.IsNullOrEmpty(nameToCheck))
                // No name given, so the call must fail.
                return false;

            if (string.IsNullOrEmpty(filter))
                // No filter given, so the given name passes the test
                return true;

            if (!caseSensitiveComparison)
            {
                // Since the String.Contains function only does case-sensitive checks,
                // cheat with our copies of the values which we'll use for the comparison.
                nameToCheck = nameToCheck.ToUpper();
                filter = filter.ToUpper();
            }

            bResult = nameToCheck.Contains(filter);

            return bResult;
        }

        /// <summary>
        ///     This method will validate the provided Revit Document to make sure the reference
        ///     exists and is for a FAMILY document.  It will throw an ArgumentNullException
        ///     if nothing is sent, and will throw an ArgumentOutOfRangeException if the document
        ///     provided isn't a family document (e.g. is a project document)
        /// </summary>
        /// <param name="document">The Revit document being tested</param>
        private static void ValidateFamilyDocument(
            Document document)
        {
            if (null == document) throw new ArgumentNullException("document");

            if (!document.IsFamilyDocument)
                throw new ArgumentOutOfRangeException(
                    "The document provided is not a Family Document.");
        }

        #endregion Private Helper Methods
    }

    [Transaction(TransactionMode.ReadOnly)]
    internal class CmdNestedFamilies : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var app = commandData.Application;
            var doc = app.ActiveUIDocument.Document;

            var familyFilenameFilter = string.Empty;
            var typeNameFilter = string.Empty;
            var caseSensitive = false;

            var nestedFamilies
                = NestedFamilyFunctions.GetFilteredNestedFamilyDefinitions(
                    familyFilenameFilter, doc, caseSensitive);

            foreach (var f in nestedFamilies) Debug.WriteLine(f.Name);

            var instances
                = NestedFamilyFunctions.GetFilteredNestedFamilyInstances(
                    familyFilenameFilter, typeNameFilter, doc, caseSensitive);

            foreach (var fi in instances) Debug.WriteLine(Util.ElementDescription(fi));

            return Result.Failed;
        }
    }
}