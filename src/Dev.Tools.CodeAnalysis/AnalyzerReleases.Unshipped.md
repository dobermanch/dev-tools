; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md~~

## Release 1.0.0

### New Rules

| Rule ID | Category    | Severity | Notes                                                                 |
|---------|-------------|----------|-----------------------------------------------------------------------|
| DT1001  | Naming      | Error    | The tool name should be unique                                        |
| DT1002  | Naming      | Error    | The tool name is not valid                                            |
| DT1003  | Declaration | Error    | Class marked with ToolDefinitionAttribute should be public and sealed |
| DT2001  | Generation  | Error    | Class should be partial                                               |
