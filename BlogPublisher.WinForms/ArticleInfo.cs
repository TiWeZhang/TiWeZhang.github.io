using System.Globalization;

namespace BlogPublisher.WinForms;

internal enum PublicationStatus
{
    Unpublished,
    Published,
    Uncertain
}

internal sealed class ArticleInfo
{
    private const string DateFormat = "yyyy-MM-dd HH:mm:ss zzz";

    public required string SourcePath { get; init; }
    public required string RelativePath { get; init; }
    public required string Title { get; init; }
    public required DateTimeOffset Date { get; init; }
    public required IReadOnlyList<string> Categories { get; init; }
    public required IReadOnlyList<string> Tags { get; init; }
    public string? PublishTarget { get; init; }
    public PublicationStatus PublicationStatus { get; set; }
    public string? MatchedPostRelativePath { get; set; }

    public static ArticleInfo Load(string repositoryRoot, string sourcePath)
    {
        var document = FrontMatterDocument.Load(sourcePath);
        var sourceName = Path.GetFileNameWithoutExtension(sourcePath);
        var relativePath = Path.GetRelativePath(repositoryRoot, sourcePath).Replace('\\', '/');
        var publishTarget = document.GetScalar("publish_target");
        var parsedDate = DateTimeOffset.TryParseExact(
            document.GetScalar("date"),
            DateFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date)
            ? date
            : new DateTimeOffset(File.GetLastWriteTime(sourcePath));

        return new ArticleInfo
        {
            SourcePath = sourcePath,
            RelativePath = relativePath,
            Title = document.GetScalar("title") ?? sourceName,
            Date = parsedDate,
            Categories = document.GetSequence("categories"),
            Tags = document.GetSequence("tags"),
            PublishTarget = publishTarget
        };
    }
}
