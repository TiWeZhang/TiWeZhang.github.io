<#
Usage examples:
  .\tools\publish-post.ps1 -Source .\writing\my-post.md
  .\tools\publish-post.ps1 -Source .\writing\2026-08-12-my-post\2026-08-12-my-post.md

The source file may have any name. If its name does not start with a date,
today's date is added to the published Jekyll post name.
#>

[CmdletBinding()]
param(
  [Parameter(Mandatory = $true)]
  [ValidateScript({ Test-Path -LiteralPath $_ -PathType Leaf })]
  [string]$Source,

  [ValidateScript({ Test-Path -LiteralPath $_ -PathType Container })]
  [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$sourcePath = (Resolve-Path -LiteralPath $Source).Path
$repositoryPath = (Resolve-Path -LiteralPath $RepositoryRoot).Path
$sourceName = [System.IO.Path]::GetFileNameWithoutExtension($sourcePath)

if ($sourceName -match '^\d{4}-\d{2}-\d{2}-.+') {
  $publishedName = $sourceName
}
else {
  $publishedName = ('{0}-{1}' -f (Get-Date -Format 'yyyy-MM-dd'), $sourceName)
}

$sourceDirectory = Split-Path -Parent $sourcePath
$sourceAssets = Join-Path $sourceDirectory ($sourceName + '.assets')
$postsDirectory = Join-Path $repositoryPath '_posts'
$imagesDirectory = Join-Path $repositoryPath ('assets\img\posts\' + $publishedName)
$publishedPost = Join-Path $postsDirectory ($publishedName + '.md')
$mediaSubpath = '/assets/img/posts/' + $publishedName + '/'

if (-not (Test-Path -LiteralPath $postsDirectory -PathType Container)) {
  throw "Publish directory not found: $postsDirectory"
}

$markdown = [System.IO.File]::ReadAllText($sourcePath)

# Convert Typora's sibling asset references to paths relative to media_subpath.
$escapedAssets = [regex]::Escape($sourceName + '.assets')
$imagePattern = '!\[([^\]]*)\]\((?:\./)?' + $escapedAssets + '/([^\s\)]+)([^\)]*)\)'
$markdown = [regex]::Replace($markdown, $imagePattern, '![$1]($2$3)')

$headerPattern = '^(---\r?\n)([\s\S]*?)(\r?\n---\r?\n)'
$headerMatch = [regex]::Match($markdown, $headerPattern)
if ($headerMatch.Success) {
  $frontMatter = $headerMatch.Groups[2].Value
  if ($frontMatter -match '(?m)^media_subpath:\s*.*$') {
    $frontMatter = [regex]::Replace($frontMatter, '(?m)^media_subpath:\s*.*$', ('media_subpath: ' + $mediaSubpath))
  }
  else {
    $frontMatter = $frontMatter + "`nmedia_subpath: " + $mediaSubpath
  }
  $markdown = $headerMatch.Groups[1].Value + $frontMatter + $headerMatch.Groups[3].Value + $markdown.Substring($headerMatch.Length)
}
else {
  $safeTitle = $sourceName.Replace('"', '\\"')
  $frontMatter = "---`ntitle: `"$safeTitle`"`ndate: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss +0800')`nmedia_subpath: $mediaSubpath`n---`n`n"
  $markdown = $frontMatter + $markdown
}

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($publishedPost, $markdown, $utf8NoBom)

if (Test-Path -LiteralPath $sourceAssets -PathType Container) {
  [System.IO.Directory]::CreateDirectory($imagesDirectory) | Out-Null
  Get-ChildItem -LiteralPath $sourceAssets -Force | ForEach-Object {
    Copy-Item -LiteralPath $_.FullName -Destination $imagesDirectory -Recurse -Force
  }
}

Write-Host "Published post: $publishedPost"
if (Test-Path -LiteralPath $sourceAssets -PathType Container) {
  Write-Host "Synced images: $imagesDirectory"
}
else {
  Write-Host 'No matching .assets directory was found; only the Markdown file was published.'
}
