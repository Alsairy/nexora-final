FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER 1001
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Nexora.Web.Api/Nexora.Web.Api.csproj", "src/Nexora.Web.Api/"]
COPY ["src/Nexora.Core/Nexora.Core.csproj", "src/Nexora.Core/"]
COPY ["src/Nexora.Infrastructure/Nexora.Infrastructure.csproj", "src/Nexora.Infrastructure/"]
RUN dotnet restore "src/Nexora.Web.Api/Nexora.Web.Api.csproj"
COPY . .
WORKDIR "/src/src/Nexora.Web.Api"
RUN dotnet build "Nexora.Web.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Nexora.Web.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Security: Run as non-root user
USER 1001

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:80/health || exit 1

ENTRYPOINT ["dotnet", "Nexora.Web.Api.dll"]
