# syntax=docker/dockerfile:1
# Build context is the repo root (see docker-compose.yml: context: ..)

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/ProjectManager.Client/ProjectManager.Client.csproj -c Release -o /app

FROM nginx:alpine AS final
COPY --from=build /app/wwwroot /usr/share/nginx/html
COPY deploy/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
