FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["CrudApp.Worker/CrudApp.Worker.csproj", "CrudApp.Worker/"]
RUN dotnet restore "CrudApp.Worker/CrudApp.Worker.csproj"
COPY . .
RUN dotnet publish "CrudApp.Worker/CrudApp.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish --no-restore /p:UseAppHost=false

FROM base AS final
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CrudApp.Worker.dll"]
