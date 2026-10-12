FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/NoticeService/NoticeService.csproj \
    backend/NoticeService/

RUN dotnet restore \
    backend/NoticeService/NoticeService.csproj

COPY backend/NoticeService/ \
    backend/NoticeService/

WORKDIR /src/backend/NoticeService

RUN dotnet publish \
    NoticeService.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "NoticeService.dll"]
