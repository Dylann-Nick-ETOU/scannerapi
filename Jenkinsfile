pipeline {
  agent { label 'scannerapi' }

  environment {
    DEPLOY_ENV_FILE = '.env.production'
    DEPLOY_COMPOSE_FILE = 'deploy/vps/docker-compose.yml'
  }

  stages {
    stage('Checkout') {
      steps { checkout scm }
    }

    stage('Backend Build & Test') {
      steps {
        dir('backend') {
          sh 'dotnet restore ApiSecurityScanner.sln'
          sh 'dotnet build ApiSecurityScanner.sln -c Release --no-restore'
          sh 'dotnet test ApiSecurityScanner.sln -c Release --no-build'
        }
      }
    }

    stage('Frontend Build') {
      steps {
        dir('frontend') {
          sh 'npm ci'
          sh 'npm run build'
        }
      }
    }

    stage('Deploy VPS') {
      steps {
        withCredentials([
          string(credentialsId: 'api-security-scanner-db-password', variable: 'POSTGRES_PASSWORD'),
          string(credentialsId: 'api-security-scanner-jwt-signing-key', variable: 'JWT_SIGNING_KEY')
        ]) {
          sh '''
            cat > "$DEPLOY_ENV_FILE" <<EOF
ASPNETCORE_ENVIRONMENT=Production
POSTGRES_DB=apisecurityscanner
POSTGRES_USER=apisecurityscanner
POSTGRES_PASSWORD=$POSTGRES_PASSWORD
JWT_ISSUER=ApiSecurityScanner
JWT_AUDIENCE=ApiSecurityScanner.Frontend
JWT_SIGNING_KEY=$JWT_SIGNING_KEY
VITE_API_BASE_URL=/api
EOF

            docker compose --env-file "$DEPLOY_ENV_FILE" -f "$DEPLOY_COMPOSE_FILE" up -d --build --remove-orphans
          '''
        }
      }
    }
  }
}
