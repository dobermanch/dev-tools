# ─── Build: API ───────────────────────────────────────────────────────────────
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS publish-api
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
COPY ["src/Dev.Tools.Api/Dev.Tools.Api.csproj", "src/Dev.Tools.Api/"]
RUN RID=$([ "$TARGETARCH" = "arm64" ] && echo linux-musl-arm64 || echo linux-musl-x64) && \
    dotnet restore -r $RID "src/Dev.Tools.Api/Dev.Tools.Api.csproj"
COPY . .
RUN RID=$([ "$TARGETARCH" = "arm64" ] && echo linux-musl-arm64 || echo linux-musl-x64) && \
    dotnet publish "src/Dev.Tools.Api/Dev.Tools.Api.csproj" --no-restore -r $RID \
      -c $BUILD_CONFIGURATION -o /app/api /p:UseAppHost=false /p:Version=$VERSION

# ─── Build: MCP ───────────────────────────────────────────────────────────────
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS publish-mcp
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
COPY ["src/Dev.Tools.Mcp/Dev.Tools.Mcp.csproj", "src/Dev.Tools.Mcp/"]
RUN RID=$([ "$TARGETARCH" = "arm64" ] && echo linux-musl-arm64 || echo linux-musl-x64) && \
    dotnet restore -r $RID "src/Dev.Tools.Mcp/Dev.Tools.Mcp.csproj"
COPY . .
RUN RID=$([ "$TARGETARCH" = "arm64" ] && echo linux-musl-arm64 || echo linux-musl-x64) && \
    dotnet publish "src/Dev.Tools.Mcp/Dev.Tools.Mcp.csproj" --no-restore -r $RID \
      -c $BUILD_CONFIGURATION -o /app/mcp /p:UseAppHost=false /p:Version=$VERSION

# ─── Build: Web ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS publish-web
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
COPY ["src/Dev.Tools.Web/Dev.Tools.Web.csproj", "src/Dev.Tools.Web/"]
RUN dotnet restore "src/Dev.Tools.Web/Dev.Tools.Web.csproj"
COPY . .
RUN dotnet publish "src/Dev.Tools.Web/Dev.Tools.Web.csproj" --no-restore \
      -c $BUILD_CONFIGURATION -o /app/web /p:Version=$VERSION

# ─── Final ────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
RUN apk add --no-cache icu-libs nginx supervisor

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Web (nginx) on :80, API on :8080, MCP on :8081
EXPOSE 80 8080 8081

# Web static files
COPY --from=publish-web /app/web/wwwroot /usr/share/nginx/html
COPY deploy/nginx.conf /etc/nginx/nginx.conf

# API
COPY --from=publish-api /app/api /app/api

# MCP
COPY --from=publish-mcp /app/mcp /app/mcp

# Supervisor config
COPY deploy/supervisord.conf /etc/supervisord.conf

CMD ["/usr/bin/supervisord", "-c", "/etc/supervisord.conf"]
