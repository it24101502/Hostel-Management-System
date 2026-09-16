FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/AccommodationService/AccommodationService.csproj \
    backend/AccommodationService/

RUN dotnet restore \
    backend/AccommodationService/AccommodationService.csproj

COPY backend/AccommodationService/ \
    backend/AccommodationService/

WORKDIR /src/backend/AccommodationService

RUN dotnet publish \
    AccommodationService.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "AccommodationService.dll"]