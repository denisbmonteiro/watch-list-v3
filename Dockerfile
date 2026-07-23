# Build

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY WatchList.sln .
COPY WatchList/WatchList.csproj WatchList/

RUN dotnet restore

COPY . .

RUN dotnet publish WatchList/WatchList.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    -p:UseAppHost=false \
    -p:DebugType=none \
    -p:DebugSymbols=false

# Runtime

FROM mcr.microsoft.com/dotnet/aspnet:8.0-noble-chiseled

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

USER app

ENTRYPOINT ["dotnet", "WatchList.dll"]