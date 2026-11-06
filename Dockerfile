# ==========================================================
# Dockerfile root: membangun /frontend (Blazor Server).
# /frontend mereferensikan /backend (ProjectReference), jadi satu image
# sudah berisi UI + seluruh service bisnis backend. Tidak butuh nginx.
#
# Build : docker build -t aumo .
# Run   : docker run -p 8080:8080 --env-file .env aumo
# ==========================================================

# 1. Build (SDK .NET 10)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Salin csproj dulu agar layer restore ter-cache
COPY ["frontend/AumoBlazor.csproj", "frontend/"]
COPY ["backend/AumoBackend.csproj", "backend/"]
RUN dotnet restore "frontend/AumoBlazor.csproj"

# Salin sisa source (lihat .dockerignore)
COPY backend/ backend/
COPY frontend/ frontend/
WORKDIR /src/frontend
RUN dotnet publish "AumoBlazor.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

# 2. Runtime (ASP.NET 10)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

# Hosting seperti Render/Railway/Heroku mengirim port lewat env PORT; default 8080.
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} exec dotnet AumoBlazor.dll"]
