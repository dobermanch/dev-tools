FROM nginx:stable-alpine AS base
WORKDIR /usr/share/nginx/html
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS publish
ARG BUILD_CONFIGURATION=Release
ARG VERSION=0.0.1
WORKDIR /build
COPY ["Directory.Build.props", "."]
COPY ["Directory.Build.targets", "."]
COPY ["Directory.Packages.props", "."]
COPY ["global.json", "."]
COPY ["src/Dev.Tools/Dev.Tools.csproj", "src/Dev.Tools/"]
COPY ["src/Dev.Tools.Localization/Dev.Tools.Localization.csproj", "src/Dev.Tools.Localization/"]
COPY ["src/Dev.Tools.CodeAnalysis/Dev.Tools.CodeAnalysis.csproj", "src/Dev.Tools.CodeAnalysis/"]
COPY ["src/Dev.Tools.Web/Dev.Tools.Web.csproj", "src/Dev.Tools.Web/"]
RUN dotnet restore "src/Dev.Tools.Web/Dev.Tools.Web.csproj"
COPY . .
WORKDIR "/build/src/Dev.Tools.Web"
RUN dotnet publish "Dev.Tools.Web.csproj" --no-restore -c $BUILD_CONFIGURATION -o /app/publish /p:Version=$VERSION

FROM base AS final
COPY deploy/nginx.conf /etc/nginx/nginx.conf
COPY --from=publish --chown=nginx:nginx /app/publish/wwwroot ./
ENTRYPOINT ["nginx", "-g", "daemon off;"]
