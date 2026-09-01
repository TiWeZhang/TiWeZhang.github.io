# Findings

- GitHub Actions run `31707238652` failed because the generated post retained an HTML image source prefixed with `ESDTVS选型.assets/`.
- The copied asset is stored directly under `assets/img/posts/2026-08-13-ESDTVS选型/`, so the generated link must use only the image filename with the existing `media_subpath`.
- The repository has no existing C# project. Only the .NET 6 runtime is installed; .NET 8 SDK and Desktop Runtime are required for the chosen framework-dependent WinForms build.
- The current script names undated source files with the current date, which can create multiple generated posts when the same source is republished on a later day.
- The new WinForms project builds successfully with .NET SDK 8.0.424 and publishes to tools/BlogPublisher/BlogPublisher.exe as a framework-dependent, single-file win-x64 application.
- The GUI starts successfully when given a .NET 8 runtime. Its normal target machines need the .NET 8 Desktop Runtime.
- `SplitterDistance` cannot be set while a `SplitContainer` still has its construction-time width; the valid range only exists after the form is shown.
- The current GUI source has no `SplitContainer`, `SplitterDistance`, `Panel1MinSize`, or `Panel2MinSize` code paths; any repeat of that exact message would therefore be from an older executable copy that has not been replaced.
- The source article model already reads both inline and block-list YAML tags, so the GUI can build its reusable tag list directly from loaded `ArticleInfo.Tags` without changing the Markdown parser.
- Chirpy's normal Jekyll command works on Windows with a portable Ruby + DevKit installation. The local `start-preview.cmd` uses `_Local/local-config.yml` so output, cache, metadata, and all Gem dependencies remain isolated under `_Local/`; `_Local/` is ignored by Git.
- `FrontMatterDocument.SetScalar()` and `SetSequence()` currently call `Quote()`, which is why the publisher emits double quotes for `title`, `date`, `categories`, and `tags`. The reference post `_posts/2026-08-14-ESDTVS选型.md` instead uses plain scalars and unquoted inline sequences.
- On 2026-08-24, this computer has the .NET 8 Desktop Runtime but no .NET SDK available on `PATH`; the publisher must be built using an SDK installed privately under `_Local/` or an SDK supplied by the user.
- The private `dotnet-install.ps1` attempt remained running without child download processes or an install directory. Do not repeat that route; use the official SDK ZIP with a direct download instead.
- The direct official SDK ZIP route succeeds. A pre-existing empty `_Local/dotnet-sdk` directory from the failed attempt must be removed before extraction, after which `_Local/dotnet-sdk/dotnet.exe --version` returns `8.0.424`.
- A portable .NET SDK needs `DOTNET_CLI_HOME` redirected into `_Local/`; otherwise first-run initialization writes to the user profile and fails with `UnauthorizedAccessException`.
- `DOTNET_CLI_HOME` alone is insufficient here because NuGet probes `%AppData%\NuGet\NuGet.Config`; pass `--configfile _Local/NuGet.Config` to keep restore configuration inside the workspace.
- `api.nuget.org` is not reachable from this environment (`NU1301`), but `BlogPublisher.WinForms.csproj` has no third-party package references. A NuGet configuration with an empty `packageSources` list allows an offline restore using the portable SDK framework packs.
- Framework-dependent `PublishSingleFile=true` injects a restore dependency on `Microsoft.NET.ILLink.Tasks >= 8.0.30`; the portable SDK ZIP does not contain that NuGet package, so an offline source with the matching package is required to preserve the existing single-file distribution.
