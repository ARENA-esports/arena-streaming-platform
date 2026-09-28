# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

COPY services/chat/src/ChatService.csproj services/chat/src/
COPY shared/EventContracts/src/EventContracts.csproj shared/EventContracts/src/

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet restore services/chat/src/ChatService.csproj

COPY services/chat/src/ services/chat/src/
COPY shared/EventContracts/src/ shared/EventContracts/src/

RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
    dotnet publish services/chat/src/ChatService.csproj -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "ChatService.dll"]
