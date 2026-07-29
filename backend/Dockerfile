FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Marketify.csproj ./
RUN dotnet restore Marketify.csproj

COPY . ./
RUN dotnet publish Marketify.csproj -c Release -o /app/publish /p:UseAppHost=false
RUN test -d /app/publish/wwwroot && test -d /app/publish/wwwroot/catalog && test -d /app/publish/wwwroot/images

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_URLS=http://+:4000
EXPOSE 4000

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "Marketify.dll"]
