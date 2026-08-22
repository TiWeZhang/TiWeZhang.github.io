namespace BlogPublisher.WinForms;

internal sealed class PublishedPostInfo
{
    public required string PostPath { get; init; }
    public required string RelativePath { get; init; }
    public required string FileStem { get; init; }
    public required IReadOnlyList<string> Tags { get; init; }
    public string? SourcePath { get; init; }
    public required string Body { get; init; }

    public static PublishedPostInfo Load(string repositoryRoot, string postPath)
    {
        var document = FrontMatterDocument.Load(postPath);
        return new PublishedPostInfo
        {
            PostPath = postPath,
            RelativePath = Path.GetRelativePath(repositoryRoot, postPath).Replace('\\', '/'),
            FileStem = Path.GetFileNameWithoutExtension(postPath),
            Tags = document.GetSequence("tags"),
            SourcePath = document.GetScalar("source_path"),
            Body = document.Body
        };
    }
}
