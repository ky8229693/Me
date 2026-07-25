FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/WEB_Demo.Api/WEB_Demo.Api.csproj", "src/WEB_Demo.Api/"]
RUN dotnet restore "src/WEB_Demo.Api/WEB_Demo.Api.csproj"

COPY src/WEB_Demo.Api/ src/WEB_Demo.Api/
RUN dotnet publish "src/WEB_Demo.Api/WEB_Demo.Api.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "WEB_Demo.Api.dll"]
