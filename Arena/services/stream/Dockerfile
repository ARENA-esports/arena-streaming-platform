# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

COPY services/stream/src/StreamService.csproj services/stream/src/
COPY shared/EventContracts/src/EventContracts.csproj shared/EventContracts/src/

RUN dotnet restore services/stream/src/StreamService.csproj

COPY services/stream/src/ services/stream/src/
COPY shared/EventContracts/src/ shared/EventContracts/src/

RUN dotnet publish services/stream/src/StreamService.csproj -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "StreamService.dll"]