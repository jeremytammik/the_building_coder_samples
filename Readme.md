# The Building Coder Samples

<p align="center">
  <img src="https://img.shields.io/badge/Revit%20API-2023-blue.svg?style=for-the-badge">
  <img src="https://img.shields.io/badge/platform-Windows-lightgray.svg?style=for-the-badge">
  <img src="https://img.shields.io/badge/.NET-4.8-blue.svg?style=for-the-badge">
  <a href="http://opensource.org/licenses/MIT"><img src="https://img.shields.io/github/license/jeremytammik/RevitLookup?style=for-the-badge"></a>
</p>

The Building Coder Samples illustrate numerous aspects and example usages of the Revit API.

Please refer to [The Building Coder](http://thebuildingcoder.typepad.com) for further information.

Keywords: Revit API C# .NET add-in.

## Installation

You can install each individual command separately by creating an add-in manifest file for it,
e.g. [TheBuildingCoderSingleSample.addin](TheBuildingCoderSingleSample.addin) is
set up to load the external command CmdDemoCheck.

Simply copy both `BuildingCoder.dll` and `TheBuildingCoderSingleSample.addin` to
the [Revit Add-Ins folder](http://help.autodesk.com/view/RVT/2015/ENU/?guid=GUID-4FFDB03E-6936-417C-9772-8FC258A261F7).

To save implementing a separate add-in manifest entry for each individual command, you can also use the Revit SDK sample RvtSamples.

It is an external application that reads a list of external commands from a text file and creates a ribbon menu to launch them all.

[BcSamples.txt](BcSamples.txt) lists all The Building Coder sample commands in the required format to be used as
an [RvtSamples include file](http://thebuildingcoder.typepad.com/blog/2008/11/loading-the-building-coder-samples.html).


## Author

Mainly implemented and maintained by
Jeremy Tammik,
[The Building Coder](http://thebuildingcoder.typepad.com) and
[The 3D Web Coder](http://the3dwebcoder.typepad.com),
[Forge](http://forge.autodesk.com) [Platform](https://developer.autodesk.com) Development,
[ADN](http://www.autodesk.com/adn)
[Open](http://www.autodesk.com/adnopen),
[Autodesk Inc.](http://www.autodesk.com),
with lots of help from the entire
[Revit add-in developer community](http://forums.autodesk.com/t5/revit-api/bd-p/160).


## License

This sample is licensed under the terms of the [MIT License](http://opensource.org/licenses/MIT).
Please see the [LICENSE](LICENSE) file for full details.
