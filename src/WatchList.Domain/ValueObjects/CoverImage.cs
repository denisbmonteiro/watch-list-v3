using System.Text.RegularExpressions;
using WatchList.Shared.Results;

namespace WatchList.Domain.ValueObjects;

public sealed partial record CoverImage
{
    public static readonly Error Invalid = new(
        "CoverImage.Invalid",
        "Cover image must be a lowercase slug with a .jpg extension (e.g. breaking-bad.jpg).");

    private CoverImage(string fileName) => FileName = fileName;

    public string FileName { get; }

    public static Result<CoverImage> Create(string? fileName)
    {
        var trimmed = fileName?.Trim();

        return trimmed is not null && SlugFileName().IsMatch(trimmed) ? new CoverImage(trimmed) : Invalid;
    }

    public static Result<CoverImage?> CreateOptional(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return Result.Success<CoverImage?>(null);
        }

        var cover = Create(fileName);

        return cover.IsSuccess ? cover.Value : cover.Error;
    }

    public override string ToString() => FileName;

    [GeneratedRegex(@"^[a-z0-9]+(-[a-z0-9]+)*\.jpg$")]
    private static partial Regex SlugFileName();
}
