using System.Text.RegularExpressions;

namespace BlogPublisher.WinForms;

internal static class PublicationMatcher
{
    private static readonly Regex PublishedNamePattern = new(
        @"^\d{4}-\d{2}-\d{2}-(?<name>.+)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static void Match(IReadOnlyList<ArticleInfo> articles, IReadOnlyList<PublishedPostInfo> posts)
    {
        var articlesByName = articles
            .GroupBy(article => Path.GetFileNameWithoutExtension(article.SourcePath), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        foreach (var article in articles)
        {
            article.PublicationStatus = PublicationStatus.Unpublished;
            article.MatchedPostRelativePath = null;

            var directMatches = FindDirectMatches(article, posts);
            if (directMatches.Count > 0)
            {
                SetMatch(article, directMatches);
                continue;
            }

            var sourceName = Path.GetFileNameWithoutExtension(article.SourcePath);
            var filenameMatches = posts
                .Where(post => GetSourceNameFromPost(post.FileStem)?.Equals(sourceName, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();

            if (filenameMatches.Count > 0)
            {
                if (articlesByName[sourceName].Count != 1)
                {
                    article.PublicationStatus = PublicationStatus.Uncertain;
                }
                else
                {
                    SetMatch(article, filenameMatches);
                }
                continue;
            }

            var normalizedSourceBody = NormalizeSourceBody(article.SourcePath, sourceName);
            var bodyMatches = posts
                .Where(post => NormalizeBody(post.Body, null).Equals(normalizedSourceBody, StringComparison.Ordinal))
                .ToList();
            SetMatch(article, bodyMatches);
        }

        foreach (var collision in articles
                     .Where(article => article.PublicationStatus == PublicationStatus.Published)
                     .GroupBy(article => article.MatchedPostRelativePath!, StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Count() > 1))
        {
            foreach (var article in collision)
            {
                article.PublicationStatus = PublicationStatus.Uncertain;
                article.MatchedPostRelativePath = null;
            }
        }
    }

    private static List<PublishedPostInfo> FindDirectMatches(ArticleInfo article, IReadOnlyList<PublishedPostInfo> posts) =>
        posts.Where(post =>
                (!string.IsNullOrWhiteSpace(article.PublishTarget) &&
                 article.PublishTarget.Equals(post.RelativePath, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(post.SourcePath) &&
                 post.SourcePath.Replace('\\', '/').Equals(article.RelativePath, StringComparison.OrdinalIgnoreCase)))
            .ToList();

    private static void SetMatch(ArticleInfo article, IReadOnlyList<PublishedPostInfo> candidates)
    {
        if (candidates.Count == 0)
        {
            return;
        }

        if (candidates.Count == 1)
        {
            article.PublicationStatus = PublicationStatus.Published;
            article.MatchedPostRelativePath = candidates[0].RelativePath;
            return;
        }

        article.PublicationStatus = PublicationStatus.Uncertain;
    }

    private static string? GetSourceNameFromPost(string postFileStem)
    {
        var match = PublishedNamePattern.Match(postFileStem);
        return match.Success ? match.Groups["name"].Value : null;
    }

    private static string NormalizeSourceBody(string path, string sourceName)
    {
        var document = FrontMatterDocument.Load(path);
        return NormalizeBody(document.Body, sourceName);
    }

    private static string NormalizeBody(string body, string? sourceName)
    {
        var normalized = body.Replace("\r\n", "\n").Replace('\r', '\n');
        if (!string.IsNullOrWhiteSpace(sourceName))
        {
            var escapedAssets = Regex.Escape(sourceName + ".assets");
            normalized = Regex.Replace(
                normalized,
                @"!\[([^\]]*)\]\((?:\./)?" + escapedAssets + @"/([^\s\)]+)([^\)]*)\)",
                "![$1]($2$3)");
            normalized = Regex.Replace(
                normalized,
                @"(<img\b[^>]*?\bsrc\s*=\s*[""'])(?:\./)?" + escapedAssets + @"/([^""']+)([""'])",
                "$1$2$3",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        return string.Join("\n", normalized.Split('\n').Select(line => line.TrimEnd())).Trim();
    }
}
