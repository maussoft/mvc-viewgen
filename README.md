# mvc-viewgen

This is the view generator for the Simple C# web framework for .NET 6.

To build use the command

    dotnet build

This command will build a "net6.0" DLL called "Maussoft.Mvc.ViewGen.dll". This file can be executed on build by putting the following in your .csproj file:

    <UsingTask TaskName="GenerateViews" AssemblyFile="tools\Maussoft.Mvc.ViewGen.dll" />

    <Target Name="BeforeBeforeBuild" BeforeTargets="BeforeBuild">
      <GenerateViews />
    </Target>

For Windows and VisualStudio 2022 you will build a "net48" DLL. You can differentiate between the two environments like this:

    <UsingTask Condition="'$(MSBuildRuntimeType)' != 'Core'" TaskName="GenerateViews" AssemblyFile="tools\win\Maussoft.Mvc.ViewGen.dll" />
    <UsingTask Condition="'$(MSBuildRuntimeType)' == 'Core'" TaskName="GenerateViews" AssemblyFile="tools\Maussoft.Mvc.ViewGen.dll" />

This way the VisualStudio build tool will use the "net48" DLL, while the command line tool uses the "net6.0" DLL.
