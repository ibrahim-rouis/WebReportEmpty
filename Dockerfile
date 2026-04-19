# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# 1. Base image for runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

# Switch to root temporarily to install native dependencies
USER root
RUN apt-get update \
    && apt-get install -y libldap-2.5-0 \
    && rm -rf /var/lib/apt/lists/*

# Switch back to the less-privileged app user
USER app
WORKDIR /app
# .NET 8 uses port 8080 by default
EXPOSE 8080 
EXPOSE 8081

# 2. Build image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Copy the csproj and restore dependencies. 
# Update "WebReport.csproj" if your project file has a different name
COPY ["WebReport.csproj", "."]
RUN dotnet restore "./WebReport.csproj"

# Copy the rest of the code and build
COPY . .
WORKDIR "/src/."
RUN dotnet build "./WebReport.csproj" -c $BUILD_CONFIGURATION -o /app/build

# 3. Publish image
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./WebReport.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# 4. Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebReport.dll"]