# API Security Scanner

Application web d'analyse de sécurité OpenAPI/Swagger (MVP) avec backend .NET 8 Clean Architecture et frontend Vue 3.

## Architecture

- `backend/src/ApiSecurityScanner.Domain`
- `backend/src/ApiSecurityScanner.Application`
- `backend/src/ApiSecurityScanner.Infrastructure`
- `backend/src/ApiSecurityScanner.API`
- `backend/tests/ApiSecurityScanner.Tests`
- `frontend`
- `samples`

## Fonctionnalités MVP implémentées

- Scan depuis URL: `POST /api/scans/url`
- Scan depuis fichier: `POST /api/scans/file`
- Liste historique: `GET /api/scans`
- Détail scan: `GET /api/scans/{id}`
- Suppression scan: `DELETE /api/scans/{id}`
- Export JSON: `GET /api/scans/{id}/export`
- Health check: `GET /api/health`

## Règles de sécurité implémentées (5/5)

- `API-AUTH-001` Authentification absente
- `API-AUTHZ-001` Endpoint sensible non protégé
- `API-DATA-001` Données sensibles exposées
- `API-VALID-001` Validation insuffisante
- `API-CONFIG-001` Serveur HTTP non sécurisé

## Lancement local

### Backend

```bash
cd backend
dotnet restore ApiSecurityScanner.sln
dotnet build ApiSecurityScanner.sln
dotnet run --project src/ApiSecurityScanner.API
```

### Frontend

```bash
cd frontend
npm install
npm run dev
```

## Docker

```bash
docker compose up --build
```

Ports par défaut via `.env` (à la racine):

- `BACKEND_PORT=8082`
- `FRONTEND_PORT=5174`
- PostgreSQL: `5433`

## Tests

```bash
cd backend
dotnet test ApiSecurityScanner.sln
```

Tests ajoutés:

- scoring service
- règle auth absente
- règle serveur HTTP

## Base de données / Migrations

L'application applique désormais les migrations EF Core au démarrage avec `Migrate()`.

Si c'est le premier lancement, créez d'abord la migration initiale:

```bash
cd backend
dotnet ef migrations add InitialCreate --project src/ApiSecurityScanner.Infrastructure --startup-project src/ApiSecurityScanner.API
dotnet ef database update --project src/ApiSecurityScanner.Infrastructure --startup-project src/ApiSecurityScanner.API
```

## Exemples de scan

Fichiers de démo:

- `samples/vulnerable-api.openapi.json`
- `samples/secured-api.openapi.json`

## CI/CD Jenkins

Pipeline fourni dans `Jenkinsfile`:

1. restore/build/test backend
2. build frontend
3. déploiement Docker sur le VPS pour la branche `main`

### Déploiement VPS via Jenkins

Le dossier `deploy/vps` contient le compose de production attendu par Jenkins.

Prérequis côté VPS:

1. créer le réseau Docker partagé avec le reverse proxy:

```bash
docker network create reverse-proxy
```

2. faire joindre le conteneur `nginx` de ton stack principal à ce réseau externe
3. configurer Jenkins avec deux credentials de type `Secret text`:
   - `api-security-scanner-db-password`
   - `api-security-scanner-jwt-signing-key`

Le pipeline déploie ensuite:

- `scannerapi-frontend`
- `scannerapi-backend`
- `scannerapi-postgres`

### Reverse proxy Nginx du VPS

Ton Nginx public doit pointer vers `scannerapi-frontend:80` sur le réseau `reverse-proxy`.

Exemple de bloc serveur:

```nginx
server {
    server_name scanapi.dylannnick.fr;

    location / {
        proxy_pass http://scannerapi-frontend:80;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

Le frontend proxyfie déjà `/api/` vers le backend. Il n'y a donc pas besoin d'une seconde règle `/api` dans le Nginx public.
