FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/EcbatanLocation.Domain/EcbatanLocation.Domain.csproj src/EcbatanLocation.Domain/
COPY src/EcbatanLocation.Application/EcbatanLocation.Application.csproj src/EcbatanLocation.Application/
COPY src/EcbatanLocation.Infrastructure/EcbatanLocation.Infrastructure.csproj src/EcbatanLocation.Infrastructure/
COPY src/EcbatanLocation.Infrastructure.Migrations.Sqlite/EcbatanLocation.Infrastructure.Migrations.Sqlite.csproj src/EcbatanLocation.Infrastructure.Migrations.Sqlite/
COPY src/EcbatanLocation.Infrastructure.Migrations.PostgreSQL/EcbatanLocation.Infrastructure.Migrations.PostgreSQL.csproj src/EcbatanLocation.Infrastructure.Migrations.PostgreSQL/
COPY src/EcbatanLocation.Web/EcbatanLocation.Web.csproj src/EcbatanLocation.Web/
RUN dotnet restore src/EcbatanLocation.Web/EcbatanLocation.Web.csproj

COPY src/ src/
RUN dotnet publish src/EcbatanLocation.Web/EcbatanLocation.Web.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV DatabaseProvider="PostgreSQL"
EXPOSE 8080

ENTRYPOINT ["dotnet", "EcbatanLocation.Web.dll"]
