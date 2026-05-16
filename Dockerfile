FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY AmarTools.Bankcheck.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish AmarTools.Bankcheck.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends \
    libgdiplus \
    libc6-dev \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish ./
RUN mkdir -p /root/.aspnet/DataProtection-Keys \
    && mkdir -p /app/wwwroot/images/cheques \
    && mkdir -p /app/wwwroot/images/logos
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080
ENV PORT=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "AmarTools.Bankcheck.dll"]
