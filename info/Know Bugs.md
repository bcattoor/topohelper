A user reports this bug: 

```
Command: IAMTopo_RailsToRailwayCenterLine
Please select the polyline3d you would like to use for the left rail.:
Please select the polyline3d you would like to use for the right rail.:
 =>Polyline3d has been selected with 67 vertices.
Calculating, please wait ...Could not load file or assembly 'Microsoft.Bcl.AsyncInterfaces, Version=1.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51' or one of its dependencies. The system cannot find the file specified..
TopoHelper.
   at Infrabel.AutodeskPlatform.TopoHelper.Csv.ReadWrite.WriteMeasuredSections[T](IEnumerable`1 records)
   at Infrabel.AutodeskPlatform.TopoHelper.Commands.WriteResultToFile(IEnumerable`1 correctedResult, IList`1 sections) in C:\Users\cwn8400\Documents\GitHub\Infrabel\Infrabel.Topohelper\TopoHelper\Commands.cs:line 1108
   at Infrabel.AutodeskPlatform.TopoHelper.Commands.IAMTopo_RailsToRailwayCenterLine() in C:\Users\cwn8400\Documents\GitHub\Infrabel\Infrabel.Topohelper\TopoHelper\Commands.cs:line 825.
Void WriteMeasuredSections[T](System.Collections.Generic.IEnumerable`1[T]).
```