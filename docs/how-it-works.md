# How nugetscope decides

This page explains two features in more detail than the [README](../README.md): **package updates**, which edit your files, and **possibly unused** detection, which is a best-effort guess. Both are designed to fail on the safe side. An update skips anything it can't edit exactly, and the unused check treats anything it can't judge as used.

## Package updates

### 1. Version list

Versions come from `dotnet package search <id> --exact-match --prerelease --format json`, which queries every source in your `nuget.config`. Unlisted versions are left out. If one source fails (for example, an authenticated feed you aren't signed in to), the other sources still return versions and the failure is reported.

### 2. Finding where the version is declared

A package's version isn't always in the project file. It can come from `Directory.Packages.props`, `Directory.Build.props`, an imported `.props`/`.targets` file, or a `VersionOverride`. Rather than guessing, nugetscope asks MSBuild:

```sh
dotnet msbuild <project> -getItem:PackageReference -getItem:PackageVersion \
  -getProperty:ManagePackageVersionsCentrally -p:TargetFramework=<tfm>
```

- This runs for **every project that references the package directly**, not just the ones you selected. A shared declaration then lists every project it affects.
- A multi-targeted project is evaluated **once per target framework it uses the package in**, because versions can be set conditionally per framework.
- MSBuild reports the file each item was defined in. When an item appears more than once, the last one wins, as in MSBuild.

The declaration is picked in this order:

| Project setup                                                 | File edited                                                                 | What is edited                     |
| ------------------------------------------------------------- | --------------------------------------------------------------------------- | ---------------------------------- |
| `PackageReference` has a `VersionOverride`                    | the file defining that reference                                            | `VersionOverride`                  |
| Central Package Management (`ManagePackageVersionsCentrally`) | the file defining the `PackageVersion` (usually `Directory.Packages.props`) | `<PackageVersion Version="...">`   |
| Otherwise                                                     | the file defining the `PackageReference`                                    | `<PackageReference Version="...">` |

Up to four projects are evaluated at a time. Evaluation doesn't restore, so it takes well under a second per project.

### 3. Locating the exact text

In each file, nugetscope parses the XML and looks for the single `PackageReference` or `PackageVersion` element whose `Include` or `Update` matches the package id (case-insensitive), and reads its `Version`/`VersionOverride`. That value can be an attribute or a child element. It then finds the exact characters of that value in the original text.

The file is left alone, and the plan says why, when:

| Case                                                             | Reason                                                              |
| ---------------------------------------------------------------- | ------------------------------------------------------------------- |
| The version is an MSBuild property (`Version="$(MyVersion)"`)    | Changing the property could affect other packages; edit it yourself |
| The package has several entries in the file (e.g. per condition) | It's not clear which one is meant                                   |
| The raw text differs from the parsed value (e.g. XML entities)   | The edit can't be done exactly                                      |
| The file isn't valid XML, or no version is found                 | Nothing safe to edit                                                |
| The value is already the target version                          | Nothing to do                                                       |
| The package is only transitive in that project                   | There is no declaration; add a direct reference first               |

### 4. Confirmation

The dialog lists each file, the old and new value, the projects affected (including projects you didn't select that share a central version), and every skipped item with its reason. **Cancel** is the default button, so pressing Enter alone never writes anything.

### 5. Writing

- Each file is read again, and every planned value is located again. If any value differs from what the preview showed, **nothing is written** and you're asked to reload.
- All edits are prepared in memory before the first file is written.
- Only the version characters are replaced. Formatting, attribute order, comments, encoding (including a BOM), line endings and the final newline stay exactly as they were.
- If writing a file fails (for example, it's locked), the error says which files were already written, so you can check or undo them.
- After a successful update, nugetscope restores and rescans.

**Why not `dotnet add package` or `dotnet package update`?** On SDK 10.0.404 they reformat the attributes of every `PackageReference` in the file, drop the final newline of `Directory.Packages.props`, replace `$(Property)` versions with literal values, and (`package update`) restore immediately without a preview. Editing only the version text avoids all of that.

## Possibly unused packages

### What is checked

The check runs after every scan, in the background in the dashboard and before the report in CI mode. Each project gets one MSBuild evaluation that returns its restore output (`project.assets.json`), its `Compile` items, its `Using`/VB `Import` items and its `PackageReference` items with asset metadata.

For each **direct** package reference (implicit SDK references are ignored):

1. **The package's namespaces.** From `project.assets.json`, nugetscope takes the package's compile assemblies (the ones your code builds against) and reads the namespaces of their **public types** with `System.Reflection.Metadata`. Nothing is loaded or run.
2. **The names your project mentions.** Every dotted name in the files below, plus each of its prefixes. `using Serilog.Sinks.File;` adds `Serilog`, `Serilog.Sinks` and `Serilog.Sinks.File`.
   - `Compile` items (C#, F#, VB source)
   - global usings: `Using` items, VB `Import` items, and generated `obj/**/*.GlobalUsings.g.cs`
   - markup and config files anywhere under the project folder: `*.razor`, `*.cshtml`, `*.vbhtml`, `*.xaml`, `*.axaml`, `*.aspx`, `*.ascx`, `*.master`, `*.asax`, `*.ashx`, `*.asmx`, `*.config`, `appsettings*.json`
   - skipped: `bin`, `obj` (apart from the generated global usings), `node_modules` and `.git` folders, plus `nuget.config` and `packages.config`, which list package ids that often equal namespaces
3. **The verdict.** A package is flagged when it has at least one public namespace and **none** of them appears in that set of names.

Comments and string literals are read on purpose. A name in a comment can make a package look used when it isn't, but that mistake is harmless. The opposite one isn't.

### Never flagged

These references count as used without checking, because code isn't expected to name them:

- **Development-only or compile-excluded references:** `PrivateAssets="all"` (analyzers, Source Link, design-time tools), an `IncludeAssets` without `compile`, or an `ExcludeAssets` with `compile` or `all`.
- **Packages with nothing to compile against:** analyzers, meta-packages, test adapters, native-only packages.
- **Packages that hook into the build:** anything shipping its own `.props`/`.targets` (code generators, build tasks, tools).

A project that failed to restore, or has no `project.assets.json`, isn't checked at all. The reason shows in its details and under `Ctrl+P`, and in the report's problems.

### When it's wrong

**Flagged but needed** (check before removing):

- The package is used only through **reflection or by name**: `Type.GetType("...")` with a name that doesn't contain its namespace, plugin loading, or a provider chosen by a short name in configuration.
- **DI assembly scanning** that picks up its types without your code naming them.
- The reference exists only to **raise the version of a transitive dependency** (for example, to fix a vulnerable version pulled in by another package). Your code never names it, but removing it brings the old version back.
- It's needed **at run time only**, by another package or by the host.

**Used but not flagged:**

- The package shares a namespace with another package you do use (common with `*.Abstractions` packages).
- Its namespace appears only in a comment, a string or an unrelated name with the same spelling.

That's why possibly-unused packages are a hint, not an issue. They don't count towards **Any issue**, can't be used with `--fail-on`, and nugetscope never removes them for you.
