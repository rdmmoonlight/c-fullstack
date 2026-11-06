# ==========================================
# 1. Build Stage (SDK .NET 10)
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy file solusi dan project untuk memanfaat simpanan cache layer (dotnet restore)
COPY *.sln ./
COPY backend/AumoBackend.csproj ./backend/
COPY frontend/AumoBlazor.csproj ./frontend/
COPY backend-fs/backend-fs.fsproj ./backend-fs/

# Restore dependensi untuk semua proyek
RUN dotnet restore

# Copy seluruh source code
COPY . .

# Publish C# Backend
RUN dotnet publish backend/AumoBackend.csproj -c Release -o /app/publish/backend

# Publish F# Backend
RUN dotnet publish backend-fs/backend-fs.fsproj -c Release -o /app/publish/backend-fs

# Publish Blazor Frontend
RUN dotnet publish frontend/AumoBlazor.csproj -c Release -o /app/publish/frontend/wwwroot

# ==========================================
# 2. Runtime Stage - C# Backend
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime-backend
WORKDIR /app
COPY --from=build /app/publish/backend .
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "AumoBackend.dll"]

# ==========================================
# 3. Runtime Stage - F# Backend
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime-backend-fs
WORKDIR /app
COPY --from=build /app/publish/backend-fs .
EXPOSE 5001
ENV ASPNETCORE_URLS=http://+:5001
ENTRYPOINT ["dotnet", "backend-fs.dll"]

# ==========================================
# 4. Final Stage - Nginx Reverse Proxy & Frontend Host
# ==========================================
FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html

# Hapus file default Nginx
RUN rm -rf ./*

# Copy hasil publish Blazor WASM ke direktori static file Nginx
COPY --from=build /app/publish/frontend/wwwroot/wwwroot .

# Copy konfigurasi Nginx dari folder /nginx/neginx.conf
COPY nginx/neginx.conf /etc/nginx/nginx.conf

EXPOSE 80 443
CMD ["nginx", "-g", "daemon off;"]
