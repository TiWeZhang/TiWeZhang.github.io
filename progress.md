# Progress

- Started diagnosis and implementation for HTML image support in `publish-post.ps1`.
- Added `src` rewriting for Typora's HTML `<img>` tags while retaining their other attributes.
- Re-published the ESDTVS source locally and verified every generated image reference resolves to a copied asset.
- Began implementation of the WinForms blog publisher and one-to-one source/post mapping.
- Completed the mapping-aware PowerShell publisher, WinForms metadata editor, .NET 8 single-file EXE, and workflow documentation. Validated the script in an isolated repository and built/launched the GUI locally.
- Fixed the first-launch `SplitterDistance` exception by applying the initial split position only after the WinForms window has a measured client width, then rebuilt and republished the EXE.
- Replaced `SplitContainer` completely with a two-column `TableLayoutPanel`, removed every runtime splitter-property assignment, and republished the EXE after the exception persisted on the user's machine.
- Reworked the publisher into an upper source-list / lower editor layout, added content-width list columns, and added selectable tag suggestions aggregated from `writing/` source Front Matter.
