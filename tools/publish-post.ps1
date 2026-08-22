<#
.SYNOPSIS
Publishes one Markdown source from writing/ to _posts/ with a one-to-one mapping.

.DESCRIPTION
The source Front Matter is authoritative. Its date selects the Jekyll file
name, while publish_target is a source-only bookkeeping field. The generated
post records source_path, so a source cannot silently overwrite another post.
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

function Test-IsChildPath {
  param([string]$Path, [string]$Parent)
  $fullPath = [System.IO.Path]::GetFullPath($Path)
  $fullParent = [System.IO.Path]::GetFullPath($Parent).TrimEnd('\', '/')
  return $fullPath.StartsWith($fullParent + [System.IO.Path]::DirectorySeparatorChar,
    [System.StringComparison]::OrdinalIgnoreCase)
}

function Get-RepositoryRelativePath {
  param([string]$Path, [string]$RepositoryPath)
  $fullPath = [System.IO.Path]::GetFullPath($Path)
  $fullRepository = [System.IO.Path]::GetFullPath($RepositoryPath).TrimEnd('\', '/')
  if (-not (Test-IsChildPath $fullPath $fullRepository)) { throw "Path is outside the repository: $fullPath" }
  return $fullPath.Substring($fullRepository.Length).TrimStart('\', '/').Replace('\', '/')
}

function Get-FrontMatter {
  param([string]$Text)
  $match = [regex]::Match($Text, '\A---\r?\n(?<content>[\s\S]*?)\r?\n---(?<after>\r?\n|\z)')
  if (-not $match.Success) { return $null }
  [pscustomobject]@{
    Content = $match.Groups['content'].Value
    HeaderLength = $match.Length
    Body = $Text.Substring($match.Length)
    NewLine = if ($Text.Contains("`r`n")) { "`r`n" } else { "`n" }
  }
}

function Get-FrontMatterValue {
  param([string]$Content, [string]$Name)
  $match = [regex]::Match($Content, '(?m)^\s*' + [regex]::Escape($Name) + '\s*:\s*(?<value>.*?)\s*$')
  if (-not $match.Success) { return $null }
  $value = $match.Groups['value'].Value.Trim()
  if ($value.Length -ge 2 -and (($value.StartsWith('"') -and $value.EndsWith('"')) -or
      ($value.StartsWith("'") -and $value.EndsWith("'")))) { $value = $value.Substring(1, $value.Length - 2) }
  return $value
}

function Remove-FrontMatterField {
  param([string]$Content, [string]$Name)
  return [regex]::Replace($Content, '(?m)^\s*' + [regex]::Escape($Name) + '\s*:\s*.*(?:\r?\n|$)', '')
}

function Set-FrontMatterField {
  param([string]$Content, [string]$Name, [string]$Value, [string]$NewLine)
  $withoutField = (Remove-FrontMatterField $Content $Name).TrimEnd("`r", "`n")
  if ($withoutField.Length -eq 0) { return "$Name`: $Value" }
  return $withoutField + $NewLine + "$Name`: $Value"
}

function Get-PostSourcePath {
  param([string]$Path)
  $postFrontMatter = Get-FrontMatter ([System.IO.File]::ReadAllText($Path))
  if ($null -eq $postFrontMatter) { return $null }
  return Get-FrontMatterValue $postFrontMatter.Content 'source_path'
}

function Get-CanonicalPublishTarget {
  param([string]$Value, [string]$RepositoryPath, [string]$PostsPath)
  if ([string]::IsNullOrWhiteSpace($Value)) { return $null }
  $candidate = [System.IO.Path]::GetFullPath((Join-Path $RepositoryPath $Value.Replace('/', '\')))
  if (-not (Test-IsChildPath $candidate $PostsPath) -or -not ([System.IO.Path]::GetExtension($candidate).Equals('.md', [System.StringComparison]::OrdinalIgnoreCase))) {
    throw "Invalid publish_target '$Value'. It must be a repository-relative Markdown file under _posts/."
  }
  return $candidate
}

function Get-DateFromFrontMatter {
  param([string]$Value)
  if ([string]::IsNullOrWhiteSpace($Value)) { throw 'The source Front Matter must contain a non-empty date (for example: 2026-08-22 09:30:00 +0800).' }
  $parsed = [DateTimeOffset]::MinValue
  $styles = [System.Globalization.DateTimeStyles]::AllowWhiteSpaces
  if (-not [DateTimeOffset]::TryParseExact($Value, 'yyyy-MM-dd HH:mm:ss zzz', [System.Globalization.CultureInfo]::InvariantCulture, $styles, [ref]$parsed)) {
    throw "Invalid source date '$Value'. Use yyyy-MM-dd HH:mm:ss +0800."
  }
  return $parsed
}

function Test-ContainsSiblingAssetReference {
  param([string]$Markdown, [string]$SourceName)
  $escaped = [regex]::Escape($SourceName + '.assets')
  return [regex]::IsMatch($Markdown, '(?:!\[[^\]]*\]\(|<img\b[^>]*\bsrc\s*=\s*["''])' + '(?:\./)?' + $escaped + '/')
}

function Convert-SiblingAssetReferences {
  param([string]$Markdown, [string]$SourceName)
  $escapedAssets = [regex]::Escape($SourceName + '.assets')
  $imagePattern = '!\[([^\]]*)\]\((?:\./)?' + $escapedAssets + '/([^\s\)]+)([^\)]*)\)'
  $result = [regex]::Replace($Markdown, $imagePattern, '![$1]($2$3)')
  $htmlImagePattern = '(<img\b[^>]*?\bsrc\s*=\s*["''])(?:\./)?' + $escapedAssets + '/([^"'']+)(["''])'
  return [regex]::Replace($result, $htmlImagePattern, '$1$2$3')
}

$sourcePath = (Resolve-Path -LiteralPath $Source).Path
$repositoryPath = (Resolve-Path -LiteralPath $RepositoryRoot).Path
$writingDirectory = Join-Path $repositoryPath 'writing'
$postsDirectory = Join-Path $repositoryPath '_posts'
$postImagesRoot = Join-Path $repositoryPath 'assets\img\posts'
if (-not (Test-Path -LiteralPath $writingDirectory -PathType Container)) { throw "Writing directory not found: $writingDirectory" }
if (-not (Test-Path -LiteralPath $postsDirectory -PathType Container)) { throw "Publish directory not found: $postsDirectory" }
if (-not (Test-IsChildPath $sourcePath $writingDirectory)) { throw "Source must be under writing/: $sourcePath" }
if (-not ([System.IO.Path]::GetExtension($sourcePath).Equals('.md', [System.StringComparison]::OrdinalIgnoreCase))) { throw "Source must be a Markdown file: $sourcePath" }

$sourceName = [System.IO.Path]::GetFileNameWithoutExtension($sourcePath)
$sourceDirectory = Split-Path -Parent $sourcePath
$sourceAssets = Join-Path $sourceDirectory ($sourceName + '.assets')
$sourceRelativePath = Get-RepositoryRelativePath $sourcePath $repositoryPath
$sourceText = [System.IO.File]::ReadAllText($sourcePath)
$sourceFrontMatter = Get-FrontMatter $sourceText
if ($null -eq $sourceFrontMatter) { throw "Source Front Matter is required: $sourcePath" }
$title = Get-FrontMatterValue $sourceFrontMatter.Content 'title'
if ([string]::IsNullOrWhiteSpace($title)) { throw 'The source Front Matter must contain a non-empty title.' }
$date = Get-DateFromFrontMatter (Get-FrontMatterValue $sourceFrontMatter.Content 'date')
$publishedName = '{0:yyyy-MM-dd}-{1}' -f $date, $sourceName
$publishedPost = Join-Path $postsDirectory ($publishedName + '.md')
$imagesDirectory = Join-Path $postImagesRoot $publishedName
$mediaSubpath = '/assets/img/posts/' + $publishedName + '/'

$referencesAssets = Test-ContainsSiblingAssetReference $sourceText $sourceName
$sourceAssetsExist = Test-Path -LiteralPath $sourceAssets -PathType Container
if ($referencesAssets -and -not $sourceAssetsExist) { throw "Image directory is missing: $sourceAssets. The Markdown references '$sourceName.assets/'. No files were changed." }

# Validate mappings before output is touched, including targets that do not exist yet.
$declaredTarget = Get-CanonicalPublishTarget (Get-FrontMatterValue $sourceFrontMatter.Content 'publish_target') $repositoryPath $postsDirectory
if ($null -ne $declaredTarget -and [System.IO.Path]::GetFileName($declaredTarget) -notmatch ('^\d{4}-\d{2}-\d{2}-' + [regex]::Escape($sourceName) + '\.md$')) {
  throw "Invalid publish_target '$((Get-RepositoryRelativePath $declaredTarget $repositoryPath))'. Its file name must match source '$sourceName'."
}
Get-ChildItem -LiteralPath $writingDirectory -Recurse -File -Filter '*.md' | ForEach-Object {
  if ($_.FullName -eq $sourcePath) { return }
  $otherFrontMatter = Get-FrontMatter ([System.IO.File]::ReadAllText($_.FullName))
  if ($null -eq $otherFrontMatter) { return }
  $otherTargetValue = Get-FrontMatterValue $otherFrontMatter.Content 'publish_target'
  if ([string]::IsNullOrWhiteSpace($otherTargetValue)) { return }
  $otherTarget = Get-CanonicalPublishTarget $otherTargetValue $repositoryPath $postsDirectory
  if (($null -ne $declaredTarget -and $otherTarget -eq $declaredTarget) -or $otherTarget -eq $publishedPost) {
    throw "Publish mapping conflict: '$($_.FullName)' already maps to '$(Get-RepositoryRelativePath $otherTarget $repositoryPath)'. No files were changed."
  }
}

$allPosts = @(Get-ChildItem -LiteralPath $postsDirectory -File -Filter '*.md')
$ownedPosts = @($allPosts | Where-Object {
  $postSource = Get-PostSourcePath $_.FullName
  $null -ne $postSource -and $postSource.Equals($sourceRelativePath, [System.StringComparison]::OrdinalIgnoreCase)
})
if ($ownedPosts.Count -gt 1) { throw "Publish mapping conflict: source '$sourceRelativePath' is recorded by multiple posts. No files were changed." }

$oldPost = $null
if ($null -ne $declaredTarget) {
  if ($ownedPosts.Count -eq 1 -and $ownedPosts[0].FullName -ne $declaredTarget) { throw 'Publish mapping conflict: publish_target and source_path point to different posts. No files were changed.' }
  if (Test-Path -LiteralPath $declaredTarget -PathType Leaf) {
    $mappedSource = Get-PostSourcePath $declaredTarget
    if ($null -ne $mappedSource -and -not $mappedSource.Equals($sourceRelativePath, [System.StringComparison]::OrdinalIgnoreCase)) { throw "Mapped target belongs to another source: $(Get-RepositoryRelativePath $declaredTarget $repositoryPath). No files were changed." }
    $oldPost = Get-Item -LiteralPath $declaredTarget
  }
}
elseif ($ownedPosts.Count -eq 1) { $oldPost = $ownedPosts[0] }
else {
  # A legacy post is claimable only by its exact, date-stripped source name.
  $legacyPattern = '^\d{4}-\d{2}-\d{2}-' + [regex]::Escape($sourceName) + '\.md$'
  $legacyCandidates = @($allPosts | Where-Object { $_.Name -match $legacyPattern })
  $foreignLegacy = @($legacyCandidates | Where-Object {
    $legacySource = Get-PostSourcePath $_.FullName
    $null -ne $legacySource -and -not $legacySource.Equals($sourceRelativePath, [System.StringComparison]::OrdinalIgnoreCase)
  })
  if ($foreignLegacy.Count -gt 0) { throw "A historical target with the same source name belongs to another source: '$($foreignLegacy[0].Name)'. No files were changed." }
  $claimableLegacy = @($legacyCandidates | Where-Object { $null -eq (Get-PostSourcePath $_.FullName) })
  if ($claimableLegacy.Count -gt 1) { throw "Cannot automatically claim '$sourceName': multiple historical posts have the exact same source name. No files were changed." }
  if ($claimableLegacy.Count -eq 1) { $oldPost = $claimableLegacy[0] }
}

# Even a valid explicit mapping must not conceal a second historical post for
# the same source file name. Keeping both would violate the one-source/one-post
# invariant, so require a manual resolution instead of selecting arbitrarily.
$sameNamePattern = '^\d{4}-\d{2}-\d{2}-' + [regex]::Escape($sourceName) + '\.md$'
$relatedSameNamePosts = @($allPosts | Where-Object {
  if ($_.Name -notmatch $sameNamePattern) { return $false }
  $relatedSource = Get-PostSourcePath $_.FullName
  return $null -eq $relatedSource -or $relatedSource.Equals($sourceRelativePath, [System.StringComparison]::OrdinalIgnoreCase)
})
if ($relatedSameNamePosts.Count -gt 1) {
  throw "Publish mapping conflict: multiple posts match source '$sourceName'. No files were changed."
}

if (Test-Path -LiteralPath $publishedPost -PathType Leaf) {
  $destinationSource = Get-PostSourcePath $publishedPost
  $sameAsOld = $null -ne $oldPost -and $oldPost.FullName -eq $publishedPost
  if (-not $sameAsOld) {
    if ($null -ne $destinationSource -and -not $destinationSource.Equals($sourceRelativePath, [System.StringComparison]::OrdinalIgnoreCase)) { throw "Target belongs to another source: $(Get-RepositoryRelativePath $publishedPost $repositoryPath). No files were changed." }
    throw "Target already exists and is not this source's mapped post: $(Get-RepositoryRelativePath $publishedPost $repositoryPath). No files were changed."
  }
}

$oldImagesDirectory = $null
if ($null -ne $oldPost) { $oldImagesDirectory = Join-Path $postImagesRoot $oldPost.BaseName }
$movingPost = $null -ne $oldPost -and $oldPost.FullName -ne $publishedPost
if ($movingPost -and (Test-Path -LiteralPath $imagesDirectory -PathType Container)) { throw "Image target already exists: $imagesDirectory. No files were changed." }
if ($null -eq $oldPost -and (Test-Path -LiteralPath $imagesDirectory -PathType Container)) { throw "Image target already exists without a mapped post: $imagesDirectory. No files were changed." }

# Build the complete image mirror before replacing a published directory.
$stagingImages = $null
if ($sourceAssetsExist) {
  [System.IO.Directory]::CreateDirectory($postImagesRoot) | Out-Null
  $stagingImages = Join-Path $postImagesRoot ('.' + $publishedName + '.sync-' + [guid]::NewGuid().ToString('N'))
  [System.IO.Directory]::CreateDirectory($stagingImages) | Out-Null
  try { Get-ChildItem -LiteralPath $sourceAssets -Force | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination $stagingImages -Recurse -Force } }
  catch { if (Test-Path -LiteralPath $stagingImages) { Remove-Item -LiteralPath $stagingImages -Recurse -Force }; throw }
}

try {
  if ($movingPost -and (Test-Path -LiteralPath $oldPost.FullName -PathType Leaf)) { Move-Item -LiteralPath $oldPost.FullName -Destination $publishedPost }
  if ($movingPost -and (Test-Path -LiteralPath $oldImagesDirectory -PathType Container)) { Move-Item -LiteralPath $oldImagesDirectory -Destination $imagesDirectory }

  $publishedFrontMatter = Remove-FrontMatterField $sourceFrontMatter.Content 'publish_target'
  $publishedFrontMatter = Remove-FrontMatterField $publishedFrontMatter 'source_path'
  $publishedFrontMatter = Set-FrontMatterField $publishedFrontMatter 'source_path' $sourceRelativePath $sourceFrontMatter.NewLine
  $publishedFrontMatter = Set-FrontMatterField $publishedFrontMatter 'media_subpath' $mediaSubpath $sourceFrontMatter.NewLine
  $convertedSource = Convert-SiblingAssetReferences $sourceText $sourceName
  $convertedFrontMatter = Get-FrontMatter $convertedSource
  $publishedMarkdown = '---' + $sourceFrontMatter.NewLine + $publishedFrontMatter + $sourceFrontMatter.NewLine + '---' + $sourceFrontMatter.NewLine + $convertedFrontMatter.Body
  $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
  [System.IO.File]::WriteAllText($publishedPost, $publishedMarkdown, $utf8NoBom)

  if ($sourceAssetsExist) {
    if (Test-Path -LiteralPath $imagesDirectory -PathType Container) { Remove-Item -LiteralPath $imagesDirectory -Recurse -Force }
    Move-Item -LiteralPath $stagingImages -Destination $imagesDirectory
    $stagingImages = $null
  }
  elseif (Test-Path -LiteralPath $imagesDirectory -PathType Container) { Remove-Item -LiteralPath $imagesDirectory -Recurse -Force }

  $updatedSourceFrontMatter = Set-FrontMatterField $sourceFrontMatter.Content 'publish_target' (Get-RepositoryRelativePath $publishedPost $repositoryPath) $sourceFrontMatter.NewLine
  $updatedSource = '---' + $sourceFrontMatter.NewLine + $updatedSourceFrontMatter + $sourceFrontMatter.NewLine + '---' + $sourceFrontMatter.NewLine + $sourceFrontMatter.Body
  [System.IO.File]::WriteAllText($sourcePath, $updatedSource, $utf8NoBom)
}
finally {
  if ($null -ne $stagingImages -and (Test-Path -LiteralPath $stagingImages)) { Remove-Item -LiteralPath $stagingImages -Recurse -Force }
}

Write-Host "Published post: $publishedPost"
Write-Host "Source mapping: $(Get-RepositoryRelativePath $publishedPost $repositoryPath)"
if ($sourceAssetsExist) { Write-Host "Synced images: $imagesDirectory" }
else { Write-Host 'No matching .assets directory was found; the published image mirror is empty.' }
