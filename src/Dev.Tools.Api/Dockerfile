FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
USER dev-tools
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "src/Directory.Build.props"]
COPY ["Directory.Build.targets", "src/Directory.Build.targets"]
COPY ["src/Dev.Tools.Api/Dev.Tools.Api.csproj", "src/Dev.Tools.Api/"]
COPY ["src/Dev.Tools.CodeAnalysis/Dev.Tools.CodeAnalysis.csproj", "src/Dev.Tools.CodeAnalysis/"]
COPY ["src/Dev.Tools/Dev.Tools.csproj", "src/Dev.Tools/"]
COPY ["src/Dev.Tools.Core/Dev.Tools.Core.csproj", "src/Dev.Tools.Core/"]
RUN dotnet restore -r linux-musl-x64 "src/Dev.Tools.Api/Dev.Tools.Api.csproj"
COPY . .
WORKDIR "/src/src/Dev.Tools.Api"
RUN dotnet publish "Dev.Tools.Api.csproj" --no-restore  -r linux-musl-x64 -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
#ENV ASPNETCORE_URLS="http://${DEVTOOLS_HOSTNAME:-localhost}:${DEVTOOLS_HTTP_PORT:-80};https://${DEVTOOLS_HOSTNAME:-localhost}:${DEVTOOLS_HTTPS_PORT:-443}"
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Dev.Tools.Api.dll"]
