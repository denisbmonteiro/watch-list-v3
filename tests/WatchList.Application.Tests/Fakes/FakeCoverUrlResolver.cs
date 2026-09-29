using WatchList.Application.Abstractions;
using WatchList.Domain.ValueObjects;

namespace WatchList.Application.Tests.Fakes;

internal sealed class FakeCoverUrlResolver : ICoverUrlResolver
{
    public string Resolve(MediaType mediaType, CoverImage coverImage) => $"covers/{mediaType}/{coverImage.FileName}";
}
