# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

COPY services/tournament/src/TournamentService.csproj services/tournament/src/
COPY shared/EventContracts/src/EventContracts.csproj shared/EventContracts/src/

RUN dotnet restore services/tournament/src/TournamentService.csproj

COPY services/tournament/src/ services/tournament/src/
COPY shared/EventContracts/src/ shared/EventContracts/src/

RUN dotnet publish services/tournament/src/TournamentService.csproj -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "TournamentService.dll"]
