// Common file for storing version data. On what this trick is and what to do if you add a new project see:
// https://stackoverflow.com/questions/980753/change-assembly-version-for-multiple-projects/980763#980763 When creating
// a new project remove the attributes listed here from its AssemblyInfo.cs file and link this file to the project.

using System.Reflection;

[assembly: AssemblyCompany("Lombiq Technologies Ltd.")]
[assembly: AssemblyProduct("Hastlayer (hastlayer.com)")]
[assembly: AssemblyCopyright("Copyright © 2015-2023")]
[assembly: AssemblyTrademark("")]
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("2.0.0")]
[assembly: AssemblyFileVersion("2.0.0")]
