# Stage 1 — Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore dependencies first (layer cache optimisation)
COPY YieldverdictApi.csproj .
RUN dotnet restore YieldverdictApi.csproj

# Copy everything and publish
COPY . .
RUN dotnet publish YieldverdictApi.csproj -c Release -o /app/publish --no-restore

# Stage 2 — Runtime (smaller image)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Railway injects PORT env var at runtime — app reads it via Program.cs
EXPOSE 3000

ENTRYPOINT ["dotnet", "YieldverdictApi.dll"]
