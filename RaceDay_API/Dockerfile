# ---------- Build stage ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first so this layer is cached until the project file changes
COPY RaceDay_API.csproj ./
RUN dotnet restore RaceDay_API.csproj

COPY . .
RUN dotnet publish RaceDay_API.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- Runtime stage ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production

# The .NET 8 images listen on port 8080 and include a non-root "app" user
EXPOSE 8080
USER app

ENTRYPOINT ["dotnet", "RaceDay_API.dll"]
