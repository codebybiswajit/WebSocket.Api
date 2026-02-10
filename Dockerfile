# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["api.slnx", "."]
COPY ["api/api.csproj", "api/"]
COPY ["User/User.csproj", "User/"]
COPY ["Message/Message.csproj", "Message/"]

# Restore dependencies
RUN dotnet restore "api/api.csproj"

# Copy source code
COPY ["api/", "api/"]
COPY ["User/", "User/"]
COPY ["Message/", "Message/"]

# Build the project
RUN dotnet build "api/api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "api/api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copy published application
COPY --from=publish /app/publish .

# Expose ports
# EXPOSE 5000
# EXPOSE 443

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
    CMD dotnet /app/api.dll --version || exit 1

# Set environment variables
# Correct for Render
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}
ENV ASPNETCORE_ENVIRONMENT=Production


# Run the application
ENTRYPOINT ["dotnet", "api.dll"]
