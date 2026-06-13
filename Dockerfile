FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Appointly.slnx dotnet-tools.json ./
COPY src/Appointly.Domain/Appointly.Domain.csproj src/Appointly.Domain/
COPY src/Appointly.Infrastructure/Appointly.Infrastructure.csproj src/Appointly.Infrastructure/
COPY src/Appointly.Api/Appointly.Api.csproj src/Appointly.Api/
RUN dotnet restore src/Appointly.Api/Appointly.Api.csproj

COPY src/ src/
RUN dotnet publish src/Appointly.Api/Appointly.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

USER $APP_UID
ENTRYPOINT ["dotnet", "Appointly.Api.dll"]
