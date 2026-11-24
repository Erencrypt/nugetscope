namespace NugetScope.Core.Packages;

/// <summary>An error or warning reported while scanning packages (restore failure, missing assets file, feed error).</summary>
/// <param name="Code">NuGet code such as <c>NU1101</c>, when known.</param>
public sealed record PackageProblem(ProblemLevel Level, string Message, string? Code = null);
