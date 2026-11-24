using NugetScope.Core.Cli.MsBuild;
using NugetScope.Core.Updates;

namespace NugetScope.Tests.Cli;

public sealed class MsBuildEvaluationParserTests
{
    [Fact]
    public void Central_versions_resolve_to_the_props_file_and_overrides_to_the_project()
    {
        const string output = """
            {
              "Properties": { "ManagePackageVersionsCentrally": "true" },
              "Items": {
                "PackageReference": [
                  { "Identity": "A", "DefiningProjectFullPath": "C:\\repo\\P\\P.csproj" },
                  { "Identity": "B", "VersionOverride": "2.0.0", "DefiningProjectFullPath": "C:\\repo\\P\\P.csproj" }
                ],
                "PackageVersion": [
                  { "Identity": "A", "Version": "1.0.0", "DefiningProjectFullPath": "C:\\repo\\Directory.Packages.props" },
                  { "Identity": "B", "Version": "1.0.0", "DefiningProjectFullPath": "C:\\repo\\Directory.Packages.props" }
                ]
              }
            }
            """;

        ProjectEvaluation evaluation = MsBuildEvaluationParser.Parse(output);

        Assert.Equal(
            new VersionDeclaration("C:/repo/Directory.Packages.props", VersionDeclarationKind.PackageVersion),
            evaluation.FindDeclaration("a"));
        Assert.Equal(
            new VersionDeclaration("C:/repo/P/P.csproj", VersionDeclarationKind.VersionOverride),
            evaluation.FindDeclaration("B"));
        Assert.Null(evaluation.FindDeclaration("NotReferenced"));
    }
}
