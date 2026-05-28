# Git Flow - API Security Scanner

## Branches en place

- `main`: production stable
- `develop`: intégration continue
- `feature/scan-url`: feature en cours

## Workflow recommandé

1. Partir de `develop`
2. Créer une branche feature:
   - `git checkout develop`
   - `git pull`
   - `git checkout -b feature/<nom-feature>`
3. Commits atomiques sur la feature
4. Merge request vers `develop`
5. Release plus tard de `develop` vers `main`

## Commandes utiles

```bash
# Voir la branche active
git branch --show-current

# Rebaser une feature sur develop
git checkout feature/scan-url
git fetch origin
git rebase origin/develop

# Fusionner localement (si besoin)
git checkout develop
git merge --no-ff feature/scan-url
```

## Push initial (à faire quand le remote est prêt)

```bash
git remote add origin <URL_DU_REPO>
git push -u origin main
git push -u origin develop
git push -u origin feature/scan-url
```
