namespace oxvxoro.Models;

public sealed record Note(
    string Title,
    DateOnly Date,
    string Category,
    string Slug,
    string Route,
    string ResourceName);
