using WatchList.Shared.Results;

namespace WatchList.Domain.ValueObjects;

public sealed record Title
{
    public static readonly Error Empty = new("Title.Empty", "Title cannot be empty.");

    private Title(string value) => Value = value;

    public string Value { get; }

    public static Result<Title> Create(string? value) =>
        string.IsNullOrWhiteSpace(value) ? Empty : new Title(value.Trim());

    public bool Equals(Title? other) =>
        other is not null && string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    public override string ToString() => Value;
}
