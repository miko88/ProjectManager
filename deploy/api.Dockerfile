# syntax=docker/dockerfile:1
# Build context is the repo root (see docker-compose.yml: context: ..)

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/ProjectManager.Api/ProjectManager.Api.csproj -c Release -o /app

# config.xml is marked CopyToOutputDirectory in ProjectManager.Api.csproj, so
# `dotnet publish` already copies it into /app alongside ProjectManager.Api.dll.

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ProjectManager.Api.dll"]
