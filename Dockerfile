# InventoryHub — multi-stage Docker build
# Stage 1: build with the full .NET SDK
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first (cached layer while only source files change)
COPY InventoryHub.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /app/publish --no-restore

# Stage 2: small runtime image with only the ASP.NET Core runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish ./

# Container port. In the cloud the provider injects its own PORT/hosting
# settings; locally and in Kubernetes the app listens on 8080.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "InventoryHub.dll"]
