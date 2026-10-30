using System.ComponentModel.DataAnnotations;
using WatchList.Application.Common;

namespace WatchList.Api.Contracts;

public sealed class ListQuery
{
    public const int DefaultPageSize = 12;
    public const int MaxPageSize = 100;
    public string? Search { get; init; }

    [Range(0, int.MaxValue)]
    public int PageIndex { get; init; }

    [Range(1, MaxPageSize)]
    public int PageSize { get; init; } = DefaultPageSize;

    public PageRequest ToPageRequest() => new(PageIndex, PageSize);
}