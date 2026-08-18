namespace oxvxoro.Models;

public sealed record Note(
    string Title,
    DateOnly Date,
    string Category,
    string Route,
    string ResourceName);
