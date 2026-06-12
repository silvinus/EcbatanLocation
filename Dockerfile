FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY EcbatanLocation.slnx ./
COPY src/EcbatanLocation.Domain/EcbatanLocation.Domain.csproj src/EcbatanLocation.Domain/
COPY src/EcbatanLocation.Application/EcbatanLocation.Application.csproj src/EcbatanLocation.Application/
COPY src/EcbatanLocation.Infrastructure/EcbatanLocation.Infrastructure.csproj src/EcbatanLocation.Infrastructure/
COPY src/EcbatanLocation.Web/EcbatanLocation.Web.csproj src/EcbatanLocation.Web/
RUN dotnet restore EcbatanLocation.slnx

COPY src/ src/
RUN dotnet publish src/EcbatanLocation.Web/EcbatanLocation.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ConnectionStrings__DefaultConnection="Data Source=/data/ecbatanelocation.db"
EXPOSE 8080

ENTRYPOINT ["dotnet", "EcbatanLocation.Web.dll"]
