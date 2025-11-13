FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Dev.Tools.Mcp/Dev.Tools.Mcp.csproj", "src/Dev.Tools.Mcp/"]
RUN dotnet restore "src/Dev.Tools.Mcp/Dev.Tools.Mcp.csproj"
COPY . .
WORKDIR "/src/src/Dev.Tools.Mcp"
RUN dotnet build "Dev.Tools.Mcp.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Dev.Tools.Mcp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Dev.Tools.Mcp.dll"]
