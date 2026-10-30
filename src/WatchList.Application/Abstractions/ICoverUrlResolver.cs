using WatchList.Domain.ValueObjects;

namespace WatchList.Application.Abstractions;

public interface ICoverUrlResolver
{
    string Resolve(MediaType mediaType, CoverImage coverImage);
}