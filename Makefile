.PHONY: nuget
nuget:
	dotnet pack src/Dev.Tools/Dev.Tools.csproj --include-symbols --configuration Release -o .build/nugets

.PHONY: tool
tool:
	dotnet pack src/Dev.Tools.Console/Dev.Tools.Console.csproj --include-symbols --configuration Release -o .build/tools
	dotnet tool uninstall --global Dev.Tools.Console 2> /dev/null || true
	dotnet tool install --global --add-source ./.build/tools Dev.Tools.Console

.PHONY: clean
clean:
	rm -fr .build
	#dotnet clean
