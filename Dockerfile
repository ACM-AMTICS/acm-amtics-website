# ------------------------------------------------------------------------------
# Stage 1: Build & Publish
# ------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file and restore dependencies (leveraging Docker layer caching)
COPY ["acm-amtics-website.csproj", "./"]
RUN dotnet restore "acm-amtics-website.csproj"

# Copy the remaining project files and publish release build
COPY . .
RUN dotnet publish "acm-amtics-website.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ------------------------------------------------------------------------------
# Stage 2: Runtime
# ------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Standard non-root container configuration for .NET 8+
ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

# Copy compiled artifacts from the build stage
COPY --from=build /app/publish .

# Run as non-root user for security
USER $APP_UID

ENTRYPOINT ["dotnet", "acm-amtics-website.dll"]
