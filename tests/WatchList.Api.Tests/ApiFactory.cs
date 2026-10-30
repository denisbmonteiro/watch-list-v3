using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace WatchList.Api.Tests;

/// <summary>The real API, reading the files in <c>Fixtures/</c> instead of <c>data/</c>.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("Storage:DataPath", Path.Combine(AppContext.BaseDirectory, "Fixtures"));
}