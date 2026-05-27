FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Itm.Price.Api/Itm.Price.Api.csproj", "Itm.Price.Api/"]
COPY ["Itm.Shared.Events/Itm.Shared.Events.csproj", "Itm.Shared.Events/"]
RUN dotnet restore "./Itm.Price.Api/Itm.Price.Api.csproj"
COPY . .
WORKDIR "/src/Itm.Price.Api"
RUN dotnet build "./Itm.Price.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Itm.Price.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Itm.Price.Api.dll"]