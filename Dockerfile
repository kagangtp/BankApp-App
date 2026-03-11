# ---- Build Stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore (layer caching)
COPY IlkProjem.API/IlkProjem.API.csproj IlkProjem.API/
COPY IlkProjem.BLL/IlkProjem.BLL.csproj IlkProjem.BLL/
COPY IlkProjem.Core/IlkProjem.Core.csproj IlkProjem.Core/
COPY IlkProjem.DAL/IlkProjem.DAL.csproj IlkProjem.DAL/
RUN dotnet restore IlkProjem.API/IlkProjem.API.csproj

# Copy everything and publish
COPY . .
RUN dotnet publish IlkProjem.API/IlkProjem.API.csproj -c Release -o /app/publish

# ---- Runtime Stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# Create uploads directory
RUN mkdir -p /app/uploads

# Railway injects PORT at runtime
EXPOSE ${PORT}

ENTRYPOINT ["dotnet", "IlkProjem.API.dll"]
