FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first to maximize layer cache for restore
COPY ApiSecurityScanner.sln ./
COPY src/ApiSecurityScanner.Domain/ApiSecurityScanner.Domain.csproj src/ApiSecurityScanner.Domain/
COPY src/ApiSecurityScanner.Application/ApiSecurityScanner.Application.csproj src/ApiSecurityScanner.Application/
COPY src/ApiSecurityScanner.Infrastructure/ApiSecurityScanner.Infrastructure.csproj src/ApiSecurityScanner.Infrastructure/
COPY src/ApiSecurityScanner.API/ApiSecurityScanner.API.csproj src/ApiSecurityScanner.API/
COPY tests/ApiSecurityScanner.Tests/ApiSecurityScanner.Tests.csproj tests/ApiSecurityScanner.Tests/

RUN dotnet restore ApiSecurityScanner.sln

# Then copy source code
COPY . .
RUN dotnet publish src/ApiSecurityScanner.API/ApiSecurityScanner.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "ApiSecurityScanner.API.dll"]
