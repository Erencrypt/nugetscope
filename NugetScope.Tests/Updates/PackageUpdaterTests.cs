using System.Text;
using NugetScope.Core.Updates;
using NugetScope.Tests.Support;

namespace NugetScope.Tests.Updates;

public sealed class PackageUpdaterTests : IDisposable
{
    private const string Csproj = "<Project>\r\n  <ItemGroup>\r\n    <PackageReference Include=\"X\" Version=\"1.0.0\" />\r\n  </ItemGroup>\r\n</Project>";

    private readonly TempDirectory _repo = new();

    public void Dispose() => _repo.Dispose();

    [Fact]
    public void Writes_only_the_new_version_and_keeps_the_bom()
    {
        string path = WriteWithBom("A.csproj", Csproj);

        PackageUpdater.Apply(Plan(path, oldValue: "1.0.0"));

        byte[] expected = [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(Csproj.Replace("1.0.0", "2.0.0"))];
        Assert.Equal(expected, File.ReadAllBytes(path));
    }

    [Fact]
    public void Writes_nothing_when_any_file_changed_since_the_plan()
    {
        string unchanged = WriteWithBom("A.csproj", Csproj);
        string changed = WriteWithBom("B.csproj", Csproj.Replace("1.0.0", "1.5.0"));
        var plan = new PackageUpdatePlan("X", "2.0.0", [Edit(unchanged, "1.0.0"), Edit(changed, "1.0.0")], []);

        Assert.Throws<PackageUpdateException>(() => PackageUpdater.Apply(plan));
        Assert.Contains("1.0.0", File.ReadAllText(unchanged));
    }

    private string WriteWithBom(string name, string content)
    {
        string path = Path.Combine(_repo.Path, name);
        File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        return path;
    }

    private static PackageUpdatePlan Plan(string path, string oldValue) => new("X", "2.0.0", [Edit(path, oldValue)], []);

    private static PlannedEdit Edit(string path, string oldValue) =>
        new(new VersionDeclaration(path, VersionDeclarationKind.PackageReference), oldValue, "2.0.0", []);
}
