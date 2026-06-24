# Phase 7 : Finalisation & Déploiement

**Objectif** : Application prête pour la production sur VPS.

## Tâches

1. **Administration (optionnel)**
   - Page gestion des tarifs (édition grille annuelle)
   - Page gestion des studios (si besoin de modifier le catalogue)

2. **Polish UI**
   - Vues additionnelles : Semaine, Liste
   - Animations / transitions
   - Messages de confirmation / erreur
   - Loading states

3. **Sécurité**
   - HTTPS forcé
   - Anti-forgery tokens
   - Rate limiting basique
   - Headers de sécurité (CSP, HSTS)

4. **Configuration déploiement**
   - `Dockerfile` (optionnel) ou publication standalone
   - Configuration `appsettings.Production.json`
   - Script de déploiement (systemd service sur Linux)
   - Backup automatique SQLite (cron)

5. **Tests end-to-end**
   - Scénario complet : consultation publique → login → créer réservation → accepter → confirmer
   - Test responsive
   - Test des règles métier depuis l'UI

6. **Documentation**
   - Guide de déploiement
   - Guide utilisateur basique

**Livrable** : Application en production sur le VPS.
