# Base image for running the app
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 7221
#EXPOSE 5191

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project file and restore dependencies
COPY ["FullPost.csproj", "./"]
RUN dotnet restore "./FullPost.csproj"

# Copy the rest of the app and build
COPY . .
RUN dotnet build "./FullPost.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the app
FROM build AS publish
RUN dotnet publish "./FullPost.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish ./

# Automatically apply EF Core migrations on container start
# Assumes you have Database.Migrate() in Program.cs
ENTRYPOINT ["dotnet", "FullPost.dll"]
