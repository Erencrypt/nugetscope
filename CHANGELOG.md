# Changelog

All notable changes to nugetscope are listed here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow [Semantic Versioning](https://semver.org/).

## [0.2.0] - 2026-11-25

### Changed

- License changed from GPL-3.0 to MIT. Version 0.1.0 stays under GPL-3.0.

### Added

- CI workflow that builds and tests on Linux, macOS and Windows.
- Each published version gets a GitHub Release, with notes taken from this changelog.
- NuGet, CI and license badges in the README.

## [0.1.0] - 2026-11-25

First release.

- Solution-wide dashboard for `.sln` / `.slnx`: outdated, vulnerable, deprecated, mismatched and possibly unused packages.
- Updates from the dashboard, CPM-aware, with a preview of the files that will change.
- Headless CI mode with JSON / text reports and meaningful exit codes.

[0.2.0]: https://github.com/Erencrypt/nugetscope/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/Erencrypt/nugetscope/releases/tag/v0.1.0
