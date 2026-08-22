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
