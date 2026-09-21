# Build from the repository root:  docker build -f src/SciSales.Api/Dockerfile .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Project files first: this layer is cached until a dependency actually changes.
COPY Directory.Build.props .editorconfig ./
COPY src/SciSales.Domain/SciSales.Domain.csproj                 src/SciSales.Domain/
COPY src/SciSales.Application/SciSales.Application.csproj       src/SciSales.Application/
COPY src/SciSales.Infrastructure/SciSales.Infrastructure.csproj src/SciSales.Infrastructure/
COPY src/SciSales.Api/SciSales.Api.csproj                       src/SciSales.Api/
RUN dotnet restore src/SciSales.Api/SciSales.Api.csproj

COPY src/ src/
RUN dotnet publish src/SciSales.Api/SciSales.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# A non-root user: the API needs nothing that root gives it.
USER $APP_UID

COPY --from=build /app .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "SciSales.Api.dll"]
