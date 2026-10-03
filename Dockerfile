# Build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Só o que o restore precisa, para manter a camada em cache enquanto nenhum .csproj mudar.
# Restaura o projeto web (e o que ele referencia), não a solution: os testes ficam fora da imagem.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/WatchList.Shared/WatchList.Shared.csproj src/WatchList.Shared/
COPY src/WatchList.Domain/WatchList.Domain.csproj src/WatchList.Domain/
COPY src/WatchList.Application/WatchList.Application.csproj src/WatchList.Application/
COPY src/WatchList.Infrastructure/WatchList.Infrastructure.csproj src/WatchList.Infrastructure/
COPY src/WatchList.Presentation/WatchList.Presentation.csproj src/WatchList.Presentation/

RUN dotnet restore src/WatchList.Presentation/WatchList.Presentation.csproj

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
