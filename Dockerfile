# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy everything
COPY . .

# Restore dependencies
RUN dotnet restore "dapr-sample-net-project.csproj"

# Build the project
RUN dotnet build "dapr-sample-net-project.csproj" -c Release -o /app/build

# Publish the app
RUN dotnet publish "dapr-sample-net-project.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "dapr-sample-net-project.dll"]
