using System.Text;
using System.Text.RegularExpressions;

namespace BlogPublisher.WinForms;

internal sealed class FrontMatterDocument
{
    private static readonly Regex HeaderPattern = new(
        "\\A---\\r?\\n(?<header>[\\s\\S]*?)\\r?\\n---\\r?\\n?",
        RegexOptions.Compiled);

    private string _header;

    private FrontMatterDocument(string header, string body)
    {
        _header = header;
        Body = body;
    }

    public string Body { get; }

    public static FrontMatterDocument Load(string path)
    {
        var content = File.ReadAllText(path, Encoding.UTF8);
        var match = HeaderPattern.Match(content);
        return match.Success
            ? new FrontMatterDocument(match.Groups["header"].Value, content[match.Length..])
            : new FrontMatterDocument(string.Empty, content);
    }

    public string? GetScalar(string key)
    {
        var match = Regex.Match(
            _header,
            $@"(?m)^{Regex.Escape(key)}:[ \t]*(?<value>[^\r\n]*)\r?$",
            RegexOptions.CultureInvariant);

        if (!match.Success)
        {
            return null;
        }

        var value = match.Groups["value"].Value.Trim();
        if (value.Length >= 2 && ((value[0] == '\"' && value[^1] == '\"') || (value[0] == '\'' && value[^1] == '\'')))
        {
            value = value[1..^1];
        }

        return value;
    }

    public IReadOnlyList<string> GetSequence(string key)
    {
        var scalar = GetScalar(key);
        if (string.IsNullOrWhiteSpace(scalar))
        {
            var blockMatch = Regex.Match(
                _header,
                $@"(?m)^{Regex.Escape(key)}:[ \t]*\r?\n(?<items>(?:[ \t]+-[ \t]*[^\r\n]*\r?\n?)*)",
                RegexOptions.CultureInvariant);
            if (!blockMatch.Success)
            {
                return Array.Empty<string>();
            }

            return Regex.Matches(blockMatch.Groups["items"].Value, @"(?m)^[ \t]+-[ \t]*(?<item>[^\r\n]*)\r?$")
                .Select(match => match.Groups["item"].Value.Trim().Trim('\"', '\''))
                .Where(value => value.Length > 0)
                .ToArray();
        }

        if (scalar.StartsWith('[') && scalar.EndsWith(']'))
        {
            scalar = scalar[1..^1];
        }

        return scalar
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(value => value.Trim().Trim('\"', '\''))
            .ToArray();
    }

    public void SetScalar(string key, string value)
    {
        RemoveKey(key);
        AppendLine($"{key}: {PlainScalar(value)}");
    }

    public void SetSequence(string key, IEnumerable<string> values)
    {
        RemoveKey(key);
        var serializedValues = values.Select(PlainScalar);
        AppendLine($"{key}: [{string.Join(", ", serializedValues)}]");
    }

    public void Save(string path)
    {
        var content = _header.Length == 0
            ? Body
            : $"---\n{_header.TrimEnd()}\n---\n\n{Body}";
        File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private void RemoveKey(string key)
    {
        var pattern = $@"(?m)^{Regex.Escape(key)}:\s*[^\r\n]*(?:\r?\n[ \t]+-\s*[^\r\n]*)*\r?\n?";
        _header = Regex.Replace(_header, pattern, string.Empty, RegexOptions.CultureInvariant);
    }

    private void AppendLine(string line)
    {
        _header = _header.TrimEnd();
        _header = _header.Length == 0 ? line : _header + "\n" + line;
    }

    // Chirpy accepts YAML plain scalars. This intentionally matches the reference
    // post format, e.g. `title: ESD防护TVS管选型` and `tags: [硬件, ESD防护]`.
    private static string PlainScalar(string value) =>
        value.Replace("\r", " ").Replace("\n", " ").Trim();
}
