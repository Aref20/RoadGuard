FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy all project files and restore as distinct layers
COPY ["SpeedAlert.Api/SpeedAlert.Api.csproj", "SpeedAlert.Api/"]
COPY ["SpeedAlert.Domain/SpeedAlert.Domain.csproj", "SpeedAlert.Domain/"]
COPY ["SpeedAlert.Application/SpeedAlert.Application.csproj", "SpeedAlert.Application/"]
COPY ["SpeedAlert.Infrastructure/SpeedAlert.Infrastructure.csproj", "SpeedAlert.Infrastructure/"]
RUN dotnet restore "SpeedAlert.Api/SpeedAlert.Api.csproj"

# Copy the rest of the code
COPY . .
WORKDIR "/src/SpeedAlert.Api"
RUN dotnet build "SpeedAlert.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SpeedAlert.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://*:${PORT:-8080}

EXPOSE 8080
ENTRYPOINT ["dotnet", "SpeedAlert.Api.dll"]
