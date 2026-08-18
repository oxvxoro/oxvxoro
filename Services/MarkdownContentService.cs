using System.Reflection;
using Markdig;
using oxvxoro.Models;

namespace oxvxoro.Services;

public sealed class MarkdownContentService
{
    private const string ResourcePrefix = "Content/";

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .DisableHtml()
        .Build();

    private readonly Assembly _assembly = typeof(MarkdownContentService).Assembly;
    private readonly Lazy<List<Note>> _notes;
    private readonly Dictionary<string, string> _htmlCache = new();

    public MarkdownContentService()
    {
        _notes = new Lazy<List<Note>>(LoadNotes);
    }

    public IReadOnlyList<Note> GetNotes()
        => _notes.Value
            .OrderByDescending(x => x.Date)
            .ThenBy(x => x.Title, StringComparer.Ordinal)
            .ToList();

    public Note? GetNote(string route)
        => _notes.Value.FirstOrDefault(x =>
            string.Equals(x.Route, route, StringComparison.Ordinal));

    public string RenderHtml(Note note)
    {
        if (_htmlCache.TryGetValue(note.ResourceName, out var cached))
        {
            return cached;
        }

        using var stream = _assembly.GetManifestResourceStream(note.ResourceName);
        if (stream is null)
        {
            return string.Empty;
        }

        using var reader = new StreamReader(stream);
        var markdown = reader.ReadToEnd();
        var html = Markdown.ToHtml(markdown, Pipeline);

        _htmlCache[note.ResourceName] = html;
        return html;
    }

    private List<Note> LoadNotes()
    {
        var notes = new List<Note>();

        var resourceNames = _assembly
            .GetManifestResourceNames()
            .Where(x => x.StartsWith(ResourcePrefix, StringComparison.Ordinal))
            .Where(x => x.EndsWith(".md", StringComparison.OrdinalIgnoreCase));

        foreach (var resourceName in resourceNames)
        {
            var note = TryCreateNote(resourceName);
            if (note is not null)
            {
                notes.Add(note);
            }
        }

        return notes;
    }

    private Note? TryCreateNote(string resourceName)
    {
        var path = resourceName[ResourcePrefix.Length..].Replace('\\', '/');
        var segments = path.Split('/');
        if (segments.Length < 2)
        {
            return null;
        }

        var category = segments[0];
        var fileName = segments[^1];
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);

        var slug = fileNameWithoutExtension;
        var date = DateOnly.MinValue;

        if (fileNameWithoutExtension.Length > 10
            && DateOnly.TryParse(fileNameWithoutExtension[..10], out var parsedDate))
        {
            date = parsedDate;
            slug = fileNameWithoutExtension[11..];
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = fileNameWithoutExtension;
        }

        var title = ReadTitle(resourceName) ?? slug;
        var route = $"{category}/{fileNameWithoutExtension}";

        return new Note(title, date, category, slug, route, resourceName);
    }

    private string? ReadTitle(string resourceName)
    {
        using var stream = _assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return null;
        }

        using var reader = new StreamReader(stream);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.StartsWith("# ", StringComparison.Ordinal))
            {
                return line[2..].Trim();
            }
        }

        return null;
    }
}
