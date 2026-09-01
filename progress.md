# Progress

- Started diagnosis and implementation for HTML image support in `publish-post.ps1`.
- Added `src` rewriting for Typora's HTML `<img>` tags while retaining their other attributes.
- Re-published the ESDTVS source locally and verified every generated image reference resolves to a copied asset.
- Began implementation of the WinForms blog publisher and one-to-one source/post mapping.
- Completed the mapping-aware PowerShell publisher, WinForms metadata editor, .NET 8 single-file EXE, and workflow documentation. Validated the script in an isolated repository and built/launched the GUI locally.
- Fixed the first-launch `SplitterDistance` exception by applying the initial split position only after the WinForms window has a measured client width, then rebuilt and republished the EXE.
- Replaced `SplitContainer` completely with a two-column `TableLayoutPanel`, removed every runtime splitter-property assignment, and republished the EXE after the exception persisted on the user's machine.
- Reworked the publisher into an upper source-list / lower editor layout, added content-width list columns, and added selectable tag suggestions aggregated from `writing/` source Front Matter.
- Set up a portable Ruby 3.3 + DevKit, MSYS2, Bundler cache, Jekyll output, and local configuration entirely within `_Local/`.
- Installed the Chirpy bundle, started the live-reload preview server, and verified `http://127.0.0.1:4000` returns the site successfully without creating `_site`, `.jekyll-cache`, `.jekyll-metadata`, or `Gemfile.lock` at the repository root.
- Changed `FrontMatterDocument` so the GUI now writes plain YAML scalars and inline sequence entries instead of automatic double-quoted values, matching the ESDTVS reference post.
- Attempted to install the private .NET SDK with Microsoft’s `dotnet-install.ps1`; the process did not create an SDK directory or download artifact after several minutes, so the next attempt will use the official SDK ZIP directly.
- Downloaded Microsoft’s official .NET SDK 8.0.424 ZIP into `_Local/` and extracted it to `_Local/dotnet-sdk`; the SDK self-check reports version `8.0.424`.
- First build attempt failed before compilation because the portable SDK tried to create `C:\Users\ztw_2\.dotnet`, which is inaccessible in this environment. The retry will use `_Local` for `DOTNET_CLI_HOME` and the NuGet package cache.
- The redirected build reached package restore but NuGet still attempted to read the inaccessible user-profile `NuGet.Config`. A private `_Local/NuGet.Config` will be passed explicitly on the next build.
- The private restore attempted `api.nuget.org` and failed with `NU1301`. The WinForms project has no `PackageReference` items, so the final build will use an empty, private NuGet source list and restore from the SDK’s built-in framework packs only.
- Offline restore then showed that framework-dependent single-file publishing additionally requires the SDK packaging task `Microsoft.NET.ILLink.Tasks 8.0.30`. The next approach is to place that one package in a `_Local` NuGet feed rather than changing the requested single-file EXE format.
- Downloaded the required Microsoft publishing task to the private `_Local` NuGet feed, restored and republished the single-file `tools/BlogPublisher/BlogPublisher.exe`. A final Release build completed with 0 warnings and 0 errors.
