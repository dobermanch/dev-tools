FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine AS base
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
WORKDIR /app

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS publish
ARG TARGETARCH
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
COPY ["src/Dev.Tools.Console/Dev.Tools.Console.csproj", "src/Dev.Tools.Console/"]
RUN RID=$([ "$TARGETARCH" = "arm64" ] && echo linux-musl-arm64 || echo linux-musl-x64) && \
    dotnet restore -r $RID "src/Dev.Tools.Console/Dev.Tools.Console.csproj"
COPY . .
WORKDIR "/build/src/Dev.Tools.Console"
RUN RID=$([ "$TARGETARCH" = "arm64" ] && echo linux-musl-arm64 || echo linux-musl-x64) && \
    dotnet publish "Dev.Tools.Console.csproj" --no-restore -r $RID \
      -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false /p:Version=$VERSION

FROM base AS final
WORKDIR /app
COPY --from=publish --chown=app:app /app/publish .
USER app
ENTRYPOINT ["dotnet", "Dev.Tools.Console.dll"]
