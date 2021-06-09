FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as build
WORKDIR /app

COPY model/Domain/. ./model/Domain
COPY model/Application/. ./model/Application
COPY model/Infrastructure/. ./model/Infrastructure
COPY tracking/ADI.UpdateTracking.Service/. ./tracking/ADI.UpdateTracking.Service

RUN dotnet publish tracking/ADI.UpdateTracking.Service -c Release -o out

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS runtime
WORKDIR /app
COPY --from=build /app/out ./

RUN useradd -d /app updatetracking
RUN chown -R updatetracking /app
USER updatetracking

ENV ASPNETCORE_URLS=http://*:8081
EXPOSE 8081/tcp

ENTRYPOINT ["dotnet", "ADI.UpdateTracking.Service.dll"]