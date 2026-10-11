# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

COPY services/analytics/src/AnalyticsService.csproj services/analytics/src/
COPY shared/EventContracts/src/EventContracts.csproj shared/EventContracts/src/

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages,sharing=locked \
    dotnet restore services/analytics/src/AnalyticsService.csproj

COPY services/analytics/src/ services/analytics/src/
COPY shared/EventContracts/src/ shared/EventContracts/src/

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages,sharing=locked \
    dotnet publish services/analytics/src/AnalyticsService.csproj -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "AnalyticsService.dll"]
