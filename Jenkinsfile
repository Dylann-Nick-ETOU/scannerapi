pipeline {
  agent any

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
          sh 'if [ -f package-lock.json ]; then npm ci; else npm install; fi'
          sh 'npm run build'
        }
      }
    }
  }
}
