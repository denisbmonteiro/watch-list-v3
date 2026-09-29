using WatchList.Domain.ValueObjects;

namespace WatchList.Application.Abstractions;

/// <summary>Turns a cover file name into the URL the browser loads; the folder layout belongs to Infrastructure.</summary>
public interface ICoverUrlResolver
{
    string Resolve(MediaType mediaType, CoverImage coverImage);
}
