# Publish Script HTML Image Support

## Goal

Update `tools/publish-post.ps1` so Typora-generated HTML `<img>` tags publish with correct Chirpy media paths.

## Plan

1. [complete] Inspect the script and the failing article transformation.
2. [complete] Add HTML image `src` rewriting while preserving attributes.
3. [complete] Verify with the ESDTVS source article and inspect the generated post paths.

# WinForms Blog Publisher

## Goal

Create a .NET 8 WinForms publisher that edits source-post metadata and invokes
the PowerShell transformer while enforcing a one-source-to-one-post mapping.

## Plan

1. [complete] Implement safe source/post mapping and migration in the PowerShell publisher.
2. [complete] Create the WinForms article selector and metadata editor.
3. [complete] Add the .NET 8 build/publish configuration and update workflow documentation.
4. [complete] Validate legacy adoption, republishing, image conversion, GUI build, and remove the Windows splitter-layout startup failure.

# Publisher Layout and Tag Suggestions

## Plan

1. [complete] Move the source list above the editor and size each list column from its content.
2. [complete] Aggregate source tags and expose them as toggleable suggestion buttons.
3. [complete] Build and republish the Windows EXE.
