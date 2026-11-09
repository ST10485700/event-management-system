# ---------- Build stage: compile and publish the MVC app ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first so this layer is cached until the project file changes
COPY RaceDay_Web.csproj ./
RUN dotnet restore RaceDay_Web.csproj

COPY . .
RUN dotnet publish RaceDay_Web.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- Runtime stage: small image with only the ASP.NET runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
# Where the RaceDay API can be reached (docker-compose sets this to the api container)
ENV ApiBaseUrl=http://api:8080/

# The .NET 8 images listen on port 8080 and include a non-root "app" user
EXPOSE 8080
USER app

ENTRYPOINT ["dotnet", "RaceDay_Web.dll"]
