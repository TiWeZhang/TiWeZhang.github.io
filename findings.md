# Findings

- GitHub Actions run `31707238652` failed because the generated post retained an HTML image source prefixed with `ESDTVS选型.assets/`.
- The copied asset is stored directly under `assets/img/posts/2026-08-13-ESDTVS选型/`, so the generated link must use only the image filename with the existing `media_subpath`.
