FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as build
WORKDIR /app

COPY model/Domain/. ./model/Domain
COPY model/Application/. ./model/Application
COPY model/Infrastructure/. ./model/Infrastructure
COPY generator/ADI.UpdateGenerator.Service/. ./generator/ADI.UpdateGenerator.Service

RUN dotnet publish generator/ADI.UpdateGenerator.Service -c Release -o out

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS runtime
WORKDIR /app
COPY --from=build /app/out ./

# Creating airflow users
RUN groupadd --gid 5000 flowusers \
    && useradd -d /app updategenerator -g 5000

RUN chown -R updategenerator:flowusers /app

USER updategenerator

ENV ASPNETCORE_URLS=http://*:8081
EXPOSE 8081/tcp

ENTRYPOINT ["dotnet", "ADI.UpdateGenerator.Service.dll"]