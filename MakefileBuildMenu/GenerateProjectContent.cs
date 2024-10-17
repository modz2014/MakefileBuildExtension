using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MakefileBuild
{
    public class ProjectContentGenerator
    {
        public string GenerateProjectContent(IEnumerable<(string fullPath, string relativePath)> sourceFiles)
        {
            var clCompileItems = string.Join(Environment.NewLine, sourceFiles
                .Where(f => f.relativePath.EndsWith(".c") || f.relativePath.EndsWith(".cpp"))
                .Select(f => $"    <ClCompile Include=\"{f.relativePath}\" />"));

            var clIncludeItems = string.Join(Environment.NewLine, sourceFiles
                .Where(f => f.relativePath.EndsWith(".h"))
                .Select(f => $"    <ClInclude Include=\"{f.relativePath}\" />"));

            var noneItems = string.Join(Environment.NewLine, sourceFiles
                .Where(f => f.relativePath.EndsWith("Makefile"))
                .Select(f => $"    <None Include=\"{f.relativePath}\" />"));

            return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project DefaultTargets=""Build"" ToolsVersion=""15.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <ItemGroup Label=""ProjectConfigurations"">
    <ProjectConfiguration Include=""Debug|x64"">
      <Configuration>Debug</Configuration>
      <Platform>x64</Platform>
    </ProjectConfiguration>
    <ProjectConfiguration Include=""Release|x64"">
      <Configuration>Release</Configuration>
      <Platform>x64</Platform>
    </ProjectConfiguration>
  </ItemGroup>
  <PropertyGroup Label=""Globals"">
    <ProjectGuid>{{336BD91B-BF97-4DA8-A9A4-345EE63C8E16}}</ProjectGuid>
    <RootNamespace>MakefileProject</RootNamespace>
    <Keyword>MakeFileProj</Keyword>
    <Platform>x64</Platform>
    <ProjectName>MakefileBuildTemplate</ProjectName>
  </PropertyGroup>
  <Import Project=""$(VCTargetsPath)\Microsoft.Cpp.Default.props"" />
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)'=='Debug|x64'"" Label=""Configuration"">
    <ConfigurationType>Makefile</ConfigurationType>
    <PlatformToolset>v143</PlatformToolset>
    <BuildCommandLine>make -f ""$(ProjectDir)Makefile""</BuildCommandLine>
    <CleanCommandLine>make -f ""$(ProjectDir)Makefile"" clean</CleanCommandLine>
    <RebuildCommandLine>make -f ""$(ProjectDir)Makefile"" clean &amp;&amp; make -f ""$(ProjectDir)Makefile""</RebuildCommandLine>
    <OutDir>$(ProjectDir)Debug\</OutDir>
    <IntDir>$(ProjectDir)obj\</IntDir>
    <TargetName>hello</TargetName>
    <UseDebugLibraries>true</UseDebugLibraries>
  </PropertyGroup>
  <PropertyGroup Condition=""'$(Configuration)|$(Platform)'=='Release|x64'"" Label=""Configuration"">
    <ConfigurationType>Makefile</ConfigurationType>
    <PlatformToolset>v143</PlatformToolset>
    <BuildCommandLine>make -f ""$(ProjectDir)Makefile""</BuildCommandLine>
    <CleanCommandLine>make -f ""$(ProjectDir)Makefile"" clean</CleanCommandLine>
    <RebuildCommandLine>make -f ""$(ProjectDir)Makefile"" clean &amp;&amp; make -f ""$(ProjectDir)Makefile""</RebuildCommandLine>
    <OutDir>$(ProjectDir)bin\</OutDir>
    <IntDir>$(ProjectDir)obj\</IntDir>
    <TargetName>hello</TargetName>
    <UseDebugLibraries>false</UseDebugLibraries>
  </PropertyGroup>
  <Import Project=""$(VCTargetsPath)\Microsoft.Cpp.targets"" />
  <ItemGroup>
    {clCompileItems}
    {clIncludeItems}
    {noneItems}
  </ItemGroup>
  <Target Name=""Build"" Inputs=""@(ClCompile)"" Outputs=""$(OutDir)$(TargetName).exe"">
    <Exec Command=""$(BuildCommandLine)"" />
  </Target>
  <Target Name=""Clean"">
    <Exec Command=""$(CleanCommandLine)"" />
  </Target>
  <Target Name=""Rebuild"" DependsOnTargets=""Clean;Build"">
    <Exec Command=""$(RebuildCommandLine)"" />
  </Target>
</Project>";
        }

        public string GenerateFiltersContent(IEnumerable<(string fullPath, string relativePath)> sourceFiles)
        {
            var filterItems = string.Join(Environment.NewLine, sourceFiles
                .Where(f => f.relativePath.EndsWith(".c") || f.relativePath.EndsWith(".cpp"))
                .Select(f => $"    <ClCompile Include=\"{f.relativePath}\"><Filter>{Path.GetDirectoryName(f.relativePath)}</Filter></ClCompile>"));

            var headerFilterItems = string.Join(Environment.NewLine, sourceFiles
                .Where(f => f.relativePath.EndsWith(".h"))
                .Select(f => $"    <ClInclude Include=\"{f.relativePath}\"><Filter>{Path.GetDirectoryName(f.relativePath)}</Filter></ClInclude>"));

            var makefileFilterItems = string.Join(Environment.NewLine, sourceFiles
                .Where(f => f.relativePath.EndsWith("Makefile"))
                .Select(f => $"    <None Include=\"{f.relativePath}\"><Filter>{Path.GetDirectoryName(f.relativePath)}</Filter></None>"));

            var uniqueFilters = sourceFiles
                .Select(f => Path.GetDirectoryName(f.relativePath))
                .Distinct()
                .Where(f => !string.IsNullOrEmpty(f))
                .Select(f => $"    <Filter Include=\"{f}\"><UniqueIdentifier>{{{Guid.NewGuid().ToString().ToUpper()}}}</UniqueIdentifier></Filter>");

            var filterGroups = string.Join(Environment.NewLine, uniqueFilters);

            return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <ItemGroup>
    {filterGroups}
  </ItemGroup>
  <ItemGroup>
    {filterItems}
    {headerFilterItems}
    {makefileFilterItems}
  </ItemGroup>
</Project>";
        }

    }
}