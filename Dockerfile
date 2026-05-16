FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
 
# Copy csproj and restore (layer-cached separately from source)
COPY AmarTools.Bankcheck.csproj ./
RUN dotnet restore
 
# Copy everything else and publish
COPY . ./
RUN dotnet publish AmarTools.Bankcheck.csproj \
    -c Release \
    -o /app/publish \
    --no-restore
 
# ── Stage 2: Runtime ─────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
 
# iTextSharp needs libgdiplus (GDI+ for image handling on Linux)
RUN apt-get update && apt-get install -y --no-install-recommends \
    libgdiplus \
    libc6-dev \
    && rm -rf /var/lib/apt/lists/*
 
# Copy published output
COPY --from=build /app/publish ./
 
# Railway injects PORT env var; ASP.NET Core reads ASPNETCORE_URLS
ENV ASPNETCORE_URLS=http://+:$PORT
ENV ASPNETCORE_ENVIRONMENT=Production
 
EXPOSE 8080
 
ENTRYPOINT ["dotnet", "AmarTools.Bankcheck.dll"]
