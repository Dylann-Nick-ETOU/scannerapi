# API Security Scanner

Base du projet mise en place (phase 1) avec Clean Architecture, backend .NET 8, frontend Vue 3, Docker Compose et pipeline Jenkins.

## Structure

- `backend/src/ApiSecurityScanner.Domain`
- `backend/src/ApiSecurityScanner.Application`
- `backend/src/ApiSecurityScanner.Infrastructure`
- `backend/src/ApiSecurityScanner.API`
- `backend/tests/ApiSecurityScanner.Tests`
- `frontend`
- `samples`

## Lancement backend

```bash
cd backend
dotnet restore ApiSecurityScanner.sln
dotnet build ApiSecurityScanner.sln
dotnet run --project src/ApiSecurityScanner.API
```

## Lancement frontend

```bash
cd frontend
npm install
npm run dev
```

## Docker

```bash
docker compose up --build
```

## Base PostgreSQL

Connection string locale par défaut:

`Host=localhost;Port=5432;Database=apisecurityscanner;Username=postgres;Password=postgres`

## CI/CD Jenkins

Un `Jenkinsfile` est fourni pour:
1. restore/build/test backend
2. build frontend

URL Jenkins: `https://jenkins.dylannnick.fr`

## Next step (phase 2)

Implémenter le premier cas d'usage: scan OpenAPI depuis URL (`POST /api/scans/url`).
