#!/usr/bin/env bash

sudo chown -R vscode:vscode /home/vscode/.dotnet

dotnet dev-certs https
sudo -E dotnet dev-certs https --export-path /usr/local/share/ca-certificates/dotnet-dev-cert.crt --format pem
sudo update-ca-certificates