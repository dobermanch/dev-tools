FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "src/Directory.Build.props"]
COPY ["Directory.Build.targets", "src/Directory.Build.targets"]
COPY ["src/Dev.Tools.Console/Dev.Tools.Console.csproj", "src/Dev.Tools.Console/"]
COPY ["src/Dev.Tools.CodeAnalysis/Dev.Tools.CodeAnalysis.csproj", "src/Dev.Tools.CodeAnalysis/"]
COPY ["src/Dev.Tools/Dev.Tools.csproj", "src/Dev.Tools/"]
COPY ["src/Dev.Tools.Core/Dev.Tools.Core.csproj", "src/Dev.Tools.Core/"]
RUN dotnet restore -r linux-musl-x64 "src/Dev.Tools.Console/Dev.Tools.Console.csproj"
COPY . .
WORKDIR "/src/src/Dev.Tools.Console"
RUN dotnet publish "Dev.Tools.Console.csproj" --no-restore -r linux-musl-x64 -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Dev.Tools.Console.dll"]
