# QuickTools_SLS
WIP package of tools for faster analysis and generation of geometry for Arkitektskolen Aarhus' SLS printer.
Must be installed via the PlugInManager

## CalcBoundingBoxCost
Calculate cost for to-be-printed geometry, according to specified material cost per cubic centimeter.

## CreateWirePouch
Create geometry encapsulating selected objects from convex hull; Can be used for small objects for eased de-powdering. Utilizes DesignEngrLab's MIConvexHull (https://github.com/DesignEngrLab/MIConvexHull).

## CreateRunner
Create runner geometry below selected objects.

## LoadDimFile
Button for quick access to file showing print-bed extents for eased positioning and verification.
File can be located in shared folder (specified in LoadDimFile.cs) or at current document location.