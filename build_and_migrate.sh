#!/bin/bash
cd backend
dotnet build
dotnet ef database update -p SpeedAlert.Infrastructure/SpeedAlert.Infrastructure.csproj -s SpeedAlert.Api/SpeedAlert.Api.csproj
