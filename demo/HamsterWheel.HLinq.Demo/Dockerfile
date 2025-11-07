FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM base AS final
WORKDIR /app
COPY ["bin/Release/net9.0/publish/", "/app/"]
ENTRYPOINT ["dotnet", "HamsterWheel.HLinq.Demo.dll"]
