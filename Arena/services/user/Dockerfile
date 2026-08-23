# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

COPY services/user/src/UserService.csproj services/user/src/
COPY shared/EventContracts/src/EventContracts.csproj shared/EventContracts/src/

RUN dotnet restore services/user/src/UserService.csproj

COPY services/user/src/ services/user/src/
COPY shared/EventContracts/src/ shared/EventContracts/src/

RUN dotnet publish services/user/src/UserService.csproj -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "UserService.dll"]