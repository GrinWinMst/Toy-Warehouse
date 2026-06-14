FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY ["WarehouseAPI.csproj", "./"]
RUN dotnet restore "WarehouseAPI.csproj"

# Copy everything else and build
COPY . .
RUN dotnet publish "WarehouseAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "WarehouseAPI.dll"]
