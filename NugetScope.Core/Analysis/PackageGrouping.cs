namespace NugetScope.Core.Analysis;

/// <summary>How the dashboard groups its rows.</summary>
public enum PackageGrouping
{
    /// <summary>One row per package; its projects are listed in the details.</summary>
    ByPackage,

    /// <summary>One row per project, expandable to the packages it references.</summary>
    ByProject,
}
