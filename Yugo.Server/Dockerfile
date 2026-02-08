# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy projects and restore
COPY ["Yugo.Server/Yugo.Server.csproj", "Yugo.Server/"]
COPY ["Yugo.Core/Yugo.Core.csproj", "Yugo.Core/"]
RUN dotnet restore "Yugo.Server/Yugo.Server.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/Yugo.Server"
RUN dotnet publish "Yugo.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Create volume for SQLite database
VOLUME /app/data
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/yugo.db"

ENTRYPOINT ["dotnet", "Yugo.Server.dll"]
