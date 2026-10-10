#!/usr/bin/env bash

sudo chown -R vscode:vscode /home/vscode/.dotnet
sudo chown -R vscode:vscode /workspaces

dotnet dev-certs https
sudo -E dotnet dev-certs https --export-path /usr/local/share/ca-certificates/dotnet-dev-cert.crt --format pem
sudo update-ca-certificates

sudo dotnet workload update

# Language server for Claude Code's csharp-lsp plugin (.claude/settings.json)
dotnet tool install --global csharp-ls