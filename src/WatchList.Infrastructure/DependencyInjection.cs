using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using WatchList.Application.Abstractions;
using WatchList.Domain.Abstractions;
using WatchList.Domain.Catalog;
using WatchList.Domain.Tracking;
using WatchList.Infrastructure.Caching;
using WatchList.Infrastructure.Covers;
using WatchList.Infrastructure.Options;
using WatchList.Infrastructure.Persistence.TextFiles;
using WatchList.Infrastructure.Persistence.TextFiles.Parsers;

namespace WatchList.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers the cached <c>.txt</c> repositories and the cover resolver; reads <c>Storage:DataPath</c>.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<StorageOptions>()
            .Bind(configuration.GetSection(StorageOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.DataPath), "Storage:DataPath is required.")
            .ValidateOnStart();

        services.AddMemoryCache();
        services.AddKeyedSingleton<IFileProvider>(StorageOptions.SectionName, (provider, _) => new PhysicalFileProvider(DataDirectory(provider)));
        services.AddSingleton<ITextFileReader, TextFileReader>();
        services.AddSingleton<ICoverUrlResolver, CoverUrlResolver>();

        return services
            .AddTextFile<Anime, AnimeLineParser>()
            .AddTextFile<Book, BookLineParser>()
            .AddTextFile<Game, GameLineParser>()
            .AddTextFile<InProgressEntry, InProgressLineParser>()
            .AddTextFile<Manga, MangaLineParser>()
            .AddTextFile<Movie, MovieLineParser>()
            .AddTextFile<QueueEntry, QueueLineParser>()
            .AddTextFile<Series, SeriesLineParser>();
    }

    private static string DataDirectory(IServiceProvider provider)
    {
        var dataPath = provider.GetRequiredService<IOptions<StorageOptions>>().Value.DataPath;

        return Path.GetFullPath(dataPath, provider.GetRequiredService<IHostEnvironment>().ContentRootPath);
    }

    /// <summary>The repository, wrapped by the cache decorator (no Scrutor: one factory per type).</summary>
    private static IServiceCollection AddTextFile<T, TParser>(this IServiceCollection services)
        where TParser : class, ILineParser<T>
    {
        services.AddSingleton<ILineParser<T>, TParser>();
        services.AddSingleton<TextFileRepository<T>>();
        services.AddSingleton<IReadRepository<T>>(provider =>
            ActivatorUtilities.CreateInstance<CachedReadRepository<T>>(provider, provider.GetRequiredService<TextFileRepository<T>>()));

        return services;
    }
}