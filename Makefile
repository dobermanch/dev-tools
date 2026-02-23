VERSION ?= 0.0.1

.PHONY: all
all: nuget tool mcp docker

.PHONY: docker
docker: docker-api docker-web docker-mcp docker-console

.PHONY: docker-api
docker-api:
	docker build -f src/Dev.Tools.Api/Dockerfile --build-arg VERSION=$(VERSION) -t dev.tools.api:$(VERSION) .

.PHONY: docker-web
docker-web:
	docker build -f src/Dev.Tools.Web/Dockerfile --build-arg VERSION=$(VERSION) -t dev.tools.web:$(VERSION) .

.PHONY: docker-mcp
docker-mcp:
	docker build -f src/Dev.Tools.Mcp/Dockerfile --build-arg VERSION=$(VERSION) -t dev.tools.mcp:$(VERSION) .

.PHONY: docker-console
docker-console:
	docker build -f src/Dev.Tools.Console/Dockerfile --build-arg VERSION=$(VERSION) -t dev.tools.console:$(VERSION) .

.PHONY: nuget
nuget:
	dotnet pack src/Dev.Tools/Dev.Tools.csproj --include-symbols --configuration Release -o .build/nugets /p:Version=$(VERSION)

.PHONY: tool
tool:
	dotnet pack src/Dev.Tools.Console/Dev.Tools.Console.csproj --include-symbols --configuration Release -o .build/tools /p:Version=$(VERSION)
	dotnet tool uninstall --global Dev.Tools.Console 2> /dev/null || true
	dotnet tool install --global --add-source ./.build/tools Dev.Tools.Console

.PHONY: mcp
mcp:
	dotnet pack src/Dev.Tools.Mcp/Dev.Tools.Mcp.csproj --include-symbols --configuration Release -o .build/tools /p:Version=$(VERSION)
	dotnet tool uninstall --global Dev.Tools.Mcp 2> /dev/null || true
	dotnet tool install --global --add-source ./.build/tools Dev.Tools.Mcp

.PHONY: clean
clean:
	rm -fr .build
	dotnet clean
