# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Proje dosyasını kopyala ve restore et
COPY ["ailab-super-app/ailab-super-app.csproj", "ailab-super-app/"]
RUN dotnet restore "ailab-super-app/ailab-super-app.csproj"

# Tüm kaynak kodları kopyala
COPY . .
WORKDIR "/src/ailab-super-app"
RUN dotnet build "ailab-super-app.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "ailab-super-app.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app
EXPOSE 6161

# Non-root user oluştur (güvenlik için)
RUN addgroup -g 1000 appuser && adduser -u 1000 -G appuser -s /bin/sh -D appuser
USER appuser

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ailab-super-app.dll"]

