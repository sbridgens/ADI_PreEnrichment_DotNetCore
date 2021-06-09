FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as build
WORKDIR /app

COPY model/Domain/. ./model/Domain
COPY model/Application/. ./model/Application
COPY model/Infrastructure/. ./model/Infrastructure
COPY service/ADI.PreEnrichment.Service/. ./service/ADI.PreEnrichment.Service

RUN dotnet publish service/ADI.PreEnrichment.Service -c Release -o out

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS runtime
WORKDIR /app
COPY --from=build /app/out ./

# Creating airflow users
RUN groupadd --gid 5000 flowusers \
    && useradd -d /app preenricher -g 5000

RUN chown -R preenricher:flowusers /app

USER preenricher

ENV ASPNETCORE_URLS=http://*:8081
EXPOSE 8081/tcp

ENTRYPOINT ["dotnet", "ADI.PreEnrichment.Service.dll"]