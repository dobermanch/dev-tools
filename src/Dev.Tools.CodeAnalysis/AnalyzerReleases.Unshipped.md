; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md~~

## Release 1.0.0

### New Rules

| Rule ID | Category    | Severity | Notes                                                                 |
|---------|-------------|----------|-----------------------------------------------------------------------|
| DT1001  | Naming      | Error    | The tool name should be unique                                        |
| DT1002  | Naming      | Error    | The tool name is not valid                                            |
| DT1003  | Declaration | Error    | Class marked with ToolDefinitionAttribute should be public and sealed |
| DT1004  | Documentation | Error  | A tool must describe itself                                           |
| DT1005  | Documentation | Error  | A tool title must be a single line                                    |
| DT1006  | Documentation | Error  | Tool arguments and results must be described                          |
| DT1007  | Documentation | Error  | Tool enum values must be described                                    |
