using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace WatchList.Infrastructure.Tests.Fakes;

internal sealed class FakeHostEnvironment(string contentRootPath) : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Development;

    public string ApplicationName { get; set; } = "WatchList.Infrastructure.Tests";

    public string ContentRootPath { get; set; } = contentRootPath;

    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}