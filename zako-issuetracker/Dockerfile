FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["zako-issuetracker/zako-issuetracker.csproj", "zako-issuetracker/"]
RUN dotnet restore "zako-issuetracker/zako-issuetracker.csproj"
COPY . .
WORKDIR "/src/zako-issuetracker"
RUN dotnet build "./zako-issuetracker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./zako-issuetracker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "zako-issuetracker.dll"]
