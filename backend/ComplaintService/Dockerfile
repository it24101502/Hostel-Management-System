FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/ComplaintService/ComplaintService.csproj \
    backend/ComplaintService/

RUN dotnet restore \
    backend/ComplaintService/ComplaintService.csproj

COPY backend/ComplaintService/ \
    backend/ComplaintService/

WORKDIR /src/backend/ComplaintService

RUN dotnet publish \
    ComplaintService.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "ComplaintService.dll"]
