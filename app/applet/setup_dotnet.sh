#!/bin/bash
set -e
echo "Downloading .NET SDK..."
curl -sSL https://download.visualstudio.microsoft.com/download/pr/0fbc1473-b31c-4395-92db-a50d46dd86fd/411c5f3c52402120e363943fcfbcbc25/dotnet-sdk-8.0.403-linux-x64.tar.gz -o dotnet.tar.gz
mkdir -p ./dotnet-sdk
tar -zxf dotnet.tar.gz -C ./dotnet-sdk
export DOTNET_ROOT=$(pwd)/dotnet-sdk
export PATH=$PATH:$DOTNET_ROOT

echo "Installing dotnet-ef..."
export DOTNET_CLI_HOME=$(pwd)/.dotnet_home
mkdir -p $DOTNET_CLI_HOME
dotnet tool install --global dotnet-ef --tool-path ./dotnet-sdk/tools

export PATH=$PATH:$(pwd)/dotnet-sdk/tools

echo "Generating migrations..."
dotnet ef migrations add InitialCreate --project backend/SpeedAlert.Infrastructure --startup-project backend/SpeedAlert.Api
