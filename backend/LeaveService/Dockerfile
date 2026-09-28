FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/LeaveService/LeaveService.csproj \
    backend/LeaveService/

RUN dotnet restore \
    backend/LeaveService/LeaveService.csproj

COPY backend/LeaveService/ \
    backend/LeaveService/

WORKDIR /src/backend/LeaveService

RUN dotnet publish \
    LeaveService.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "LeaveService.dll"]