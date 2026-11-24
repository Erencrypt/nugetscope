using NugetScope.Core.Analysis;
using NugetScope.Core.Solutions;
using NugetScope.Core.Updates;
using NugetScope.Tests.Support;

namespace NugetScope.Tests.Updates;

public sealed class PackageUpdatePlannerTests : IDisposable
{
    private readonly TempDirectory _repo = new();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose() => _repo.Dispose();

    [Fact]
    public async Task Central_version_is_one_edit_that_lists_every_project_using_it()
    {
        string props = _repo.CreateFile("Directory.Packages.props", """
            <Project>
              <ItemGroup><PackageVersion Include="Newtonsoft.Json" Version="13.0.1" /></ItemGroup>
            </Project>
            """);
        ProjectInfo a = Project("A"), b = Project("B");
        var central = new ProjectEvaluation(
            UsesCentralPackageManagement: true,
            [new EvaluatedPackageItem("Newtonsoft.Json", null, null, "ignored.csproj")],
            [new EvaluatedPackageItem("Newtonsoft.Json", "13.0.1", null, props)]);
        var evaluator = new FakeProjectEvaluator().On(a.FilePath, central).On(b.FilePath, central);

        PackageUpdatePlan plan = await new PackageUpdatePlanner(evaluator)
            .PlanAsync(Package("Newtonsoft.Json", a, b), [a], "13.0.3", Ct);

        PlannedEdit edit = Assert.Single(plan.Edits);
        Assert.Equal(VersionDeclarationKind.PackageVersion, edit.Declaration.Kind);
        Assert.Equal(("13.0.1", "13.0.3"), (edit.OldValue, edit.NewValue));
        Assert.Equal([a, b], edit.AffectedProjects);
        Assert.Empty(plan.Notes);
    }

    [Fact]
    public async Task Property_versions_and_unreadable_projects_become_notes_instead_of_edits()
    {
        ProjectInfo a = Project("A", """
            <Project><ItemGroup><PackageReference Include="X" Version="$(XVersion)" /></ItemGroup></Project>
            """);
        ProjectInfo broken = Project("Broken");
        var evaluator = new FakeProjectEvaluator().On(a.FilePath, new ProjectEvaluation(
            false, [new EvaluatedPackageItem("X", "1.0.0", null, a.FilePath)], []));

        PackageUpdatePlan plan = await new PackageUpdatePlanner(evaluator)
            .PlanAsync(Package("X", a, broken), [a, broken], "2.0.0", Ct);

        Assert.False(plan.HasChanges);
        Assert.Equal(2, plan.Notes.Count);
        Assert.Contains(plan.Notes, n => n.Contains("$(XVersion)"));
        Assert.Contains(plan.Notes, n => n.StartsWith("Broken:"));
    }

    private ProjectInfo Project(string name, string content = "<Project />") =>
        new(name, _repo.CreateFile($"{name}/{name}.csproj", content), null);

    private static PackageSummary Package(string id, params ProjectInfo[] projects) =>
        new(id, projects.Select(p => new PackageUsage(p, ["net10.0"], "1.0.0", "1.0.0", IsTransitive: false)).ToList());
}
