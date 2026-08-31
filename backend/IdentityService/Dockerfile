FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY backend/IdentityService/IdentityService.csproj backend/IdentityService/
RUN dotnet restore backend/IdentityService/IdentityService.csproj

COPY backend/IdentityService/ backend/IdentityService/
WORKDIR /source/backend/IdentityService

RUN dotnet publish IdentityService.csproj --configuration Release --output /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "IdentityService.dll"]