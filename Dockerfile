# Build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Só o que o restore precisa, para manter a camada em cache enquanto nenhum .csproj mudar.
COPY WatchList.slnx ./
COPY src/WatchList.Presentation/WatchList.Presentation.csproj src/WatchList.Presentation/

RUN dotnet restore WatchList.slnx

COPY . .

RUN dotnet publish src/WatchList.Presentation/WatchList.Presentation.csproj \
    -c Release \
    --no-restore \
    -o /app/publish

# Runtime

FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

USER app

ENTRYPOINT ["dotnet", "WatchList.Presentation.dll"]
