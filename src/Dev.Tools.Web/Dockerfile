FROM nginx:alpine AS base
USER $APP_UID
WORKDIR /usr/share/nginx/html
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "src/Directory.Build.props"]
COPY ["Directory.Build.targets", "src/Directory.Build.targets"]
COPY ["src/Dev.Tools.Web/Dev.Tools.Web.csproj", "src/Dev.Tools.Web/"]
COPY ["src/Dev.Tools/Dev.Tools.csproj", "src/Dev.Tools/"]
COPY ["src/Dev.Tools.Core/Dev.Tools.Core.csproj", "src/Dev.Tools.Core/"]
COPY ["src/Dev.Tools.CodeAnalysis/Dev.Tools.CodeAnalysis.csproj", "src/Dev.Tools.CodeAnalysis/"]
RUN dotnet restore "src/Dev.Tools.Web/Dev.Tools.Web.csproj"
COPY . .
WORKDIR "/src/src/Dev.Tools.Web"
RUN dotnet publish "Dev.Tools.Web.csproj" --no-restore -c $BUILD_CONFIGURATION -o /app/publish

FROM base AS final
COPY nginx.conf /etc/nginx/nginx.conf
COPY --from=publish /app/publish/wwwroot ./
ENTRYPOINT ["nginx", "-g", "daemon off;"]

