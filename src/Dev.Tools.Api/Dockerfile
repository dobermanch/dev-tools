FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS publish
ARG BUILD_CONFIGURATION=Release
ARG VERSION=0.0.1
WORKDIR /build
COPY ["Directory.Build.props", "."]
COPY ["Directory.Build.targets", "."]
COPY ["Directory.Packages.props", "."]
COPY ["global.json", "."]
COPY ["src/Dev.Tools/Dev.Tools.csproj", "src/Dev.Tools/"]
COPY ["src/Dev.Tools.CodeAnalysis/Dev.Tools.CodeAnalysis.csproj", "src/Dev.Tools.CodeAnalysis/"]
COPY ["src/Dev.Tools.Localization/Dev.Tools.Localization.csproj", "src/Dev.Tools.Localization/"]
COPY ["src/Dev.Tools.Api/Dev.Tools.Api.csproj", "src/Dev.Tools.Api/"]
RUN dotnet restore -r linux-musl-x64 "src/Dev.Tools.Api/Dev.Tools.Api.csproj"
COPY . .
WORKDIR "/build/src/Dev.Tools.Api"
RUN dotnet publish "Dev.Tools.Api.csproj" --no-restore -r linux-musl-x64 -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false /p:Version=$VERSION

FROM base AS final
WORKDIR /app
COPY --from=publish --chown=app:app /app/publish .
USER app
ENTRYPOINT ["dotnet", "Dev.Tools.Api.dll"]
