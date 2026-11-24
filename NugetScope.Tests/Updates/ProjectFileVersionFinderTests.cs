using NugetScope.Core.Updates;

namespace NugetScope.Tests.Updates;

public sealed class ProjectFileVersionFinderTests
{
    [Fact]
    public void Replacing_an_attribute_changes_only_that_value()
    {
        string text = "<Project>\r\n  <ItemGroup>\r\n    <!-- pinned -->\r\n"
            + "    <PackageReference Include=\"Newtonsoft.Json\"\r\n                      Version='13.0.1' />\r\n"
            + "    <PackageReference   Include=\"Humanizer.Core\" Version=\"2.13.14\" />\r\n"
            + "  </ItemGroup>\r\n</Project>";

        VersionLocation location = ProjectFileVersionFinder.Find(text, VersionDeclarationKind.PackageReference, "newtonsoft.json");

        Assert.Equal("13.0.1", location.Value);
        Assert.Equal(text.Replace("'13.0.1'", "'13.0.3'"), location.ReplaceIn(text, "13.0.3"));
    }

    [Fact]
    public void Finds_a_version_child_element_and_a_version_override()
    {
        const string text = """
            <Project>
              <ItemGroup>
                <PackageReference Include="A">
                  <Version> 1.0.0 </Version>
                </PackageReference>
                <PackageReference Include="B" VersionOverride="2.0.0" />
              </ItemGroup>
            </Project>
            """;

        VersionLocation child = ProjectFileVersionFinder.Find(text, VersionDeclarationKind.PackageReference, "A");
        VersionLocation versionOverride = ProjectFileVersionFinder.Find(text, VersionDeclarationKind.VersionOverride, "B");

        Assert.Contains("<Version> 1.1.0 </Version>", child.ReplaceIn(text, "1.1.0"));
        Assert.Equal("2.0.0", versionOverride.Value);
    }

    [Theory]
    [InlineData("""<Project><ItemGroup><PackageReference Include="A" /></ItemGroup></Project>""")]
    [InlineData("""
        <Project>
          <ItemGroup Condition="'$(TargetFramework)' == 'net8.0'"><PackageVersion Include="A" Version="1.0.0" /></ItemGroup>
          <ItemGroup Condition="'$(TargetFramework)' == 'net10.0'"><PackageVersion Include="A" Version="2.0.0" /></ItemGroup>
        </Project>
        """)]
    public void Refuses_when_the_version_is_missing_or_ambiguous(string text)
    {
        var kind = text.Contains("PackageVersion") ? VersionDeclarationKind.PackageVersion : VersionDeclarationKind.PackageReference;

        Assert.Throws<PackageUpdateException>(() => ProjectFileVersionFinder.Find(text, kind, "A"));
    }
}
