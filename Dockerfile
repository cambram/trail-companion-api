FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props .
COPY src/ src/
RUN dotnet publish src/TrailCompanion.Api/TrailCompanion.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
# Npgsql probes GSSAPI during authentication; the runtime image does not ship it.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=10000
EXPOSE 10000
ENTRYPOINT ["dotnet", "TrailCompanion.Api.dll"]
