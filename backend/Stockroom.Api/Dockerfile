FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json .
COPY backend/Stockroom.Api/Stockroom.Api.csproj backend/Stockroom.Api/
RUN dotnet restore backend/Stockroom.Api
COPY backend/Stockroom.Api backend/Stockroom.Api
RUN dotnet publish backend/Stockroom.Api -c Release --no-restore -o /out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Stockroom.Api.dll"]
