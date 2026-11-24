using NugetScope.Core.Cli.PackageSearch;
using NugetScope.Core.Updates;

namespace NugetScope.Tests.Cli;

public sealed class PackageSearchParserTests
{
    [Fact]
    public void Merges_versions_across_sources_newest_first_and_reports_failed_sources()
    {
        const string output = """
            {
              "version": 2,
              "problems": [],
              "searchResult": [
                { "sourceName": "offline", "packages": [ { "id": "Newtonsoft.Json", "version": "13.0.1" } ] },
                { "sourceName": "nuget.org", "packages": [
                    { "id": "Newtonsoft.Json", "version": "9.0.1" },
                    { "id": "Newtonsoft.Json", "version": "13.0.1" },
                    { "id": "Newtonsoft.Json", "version": "14.0.1-beta2" } ] },
                { "sourceName": "dead", "packages": [],
                  "problems": [ { "text": "Unable to load the service index.", "problemType": "Error" } ] }
              ]
            }
            """;

        PackageVersionList list = PackageSearchParser.Parse(output, "newtonsoft.json");

        Assert.Equal(["14.0.1-beta2", "13.0.1", "9.0.1"], list.Versions);
        Assert.Equal("dead: Unable to load the service index.", Assert.Single(list.Problems).Message);
    }
}
