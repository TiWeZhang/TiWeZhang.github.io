# Publish Script HTML Image Support

## Goal

Update `tools/publish-post.ps1` so Typora-generated HTML `<img>` tags publish with correct Chirpy media paths.

## Plan

1. [complete] Inspect the script and the failing article transformation.
2. [complete] Add HTML image `src` rewriting while preserving attributes.
3. [complete] Verify with the ESDTVS source article and inspect the generated post paths.
