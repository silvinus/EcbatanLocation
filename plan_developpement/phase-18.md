# Phase 18 : Durcissement authentification

**Objectif** : Corriger les faiblesses identifiées dans le système d'authentification — session persistante sans limite, absence de lockout sur échec de connexion, et absence de choix « Se souvenir de moi ».

## Problèmes identifiés

1. **Session sans fin** : le cookie expire après 30 jours avec sliding expiration (se réinitialise à chaque requête). Combiné à `isPersistent: true` codé en dur, l'utilisateur n'est **jamais** déconnecté tant qu'il utilise l'app régulièrement.
2. **Pas de lockout** : `lockoutOnFailure: false` dans le login — un attaquant peut tenter des mots de passe indéfiniment sans blocage du compte.
3. **Pas de checkbox « Se souvenir de moi »** : `isPersistent: true` est codé en dur — le cookie survit toujours à la fermeture du navigateur, même si l'utilisateur ne le souhaite pas.

## Tâches

### 1. Réduire la durée du cookie et désactiver le sliding

- **`Program.cs`** — Modifier `ConfigureApplicationCookie` :
  - `ExpireTimeSpan` : `TimeSpan.FromDays(30)` → `TimeSpan.FromHours(2)` (2 heures)
  - `SlidingExpiration` : `true` → `false` (expiration absolue, pas de renouvellement automatique)
- **Effet** : reconnexion obligatoire toutes les 2 heures maximum, même en cas d'utilisation régulière.

### 2. Activer le lockout sur échec de connexion

- **`DependencyInjection.cs`** (Infrastructure) — Ajouter la config lockout dans `AddIdentity` :
  - `options.Lockout.MaxFailedAccessAttempts = 5`
  - `options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15)`
  - `options.Lockout.AllowedForNewUsers = true`
- **`Login.razor`** — Passer `lockoutOnFailure: true` dans `PasswordSignInAsync`.
- **Effet** : après 5 tentatives échouées, le compte est verrouillé pendant 15 minutes.

### 3. Ajouter la checkbox « Se souvenir de moi »

- **`Login.razor`** — Ajouter un champ `RememberMe` au `LoginModel` (booléen, défaut `false`).
- Ajouter une checkbox dans le formulaire.
- Passer `isPersistent: Input.RememberMe` au lieu de `isPersistent: true`.
- **Effet** : sans la case cochée, le cookie est un cookie de session (supprimé à la fermeture du navigateur). Avec la case, le cookie persiste 2 heures.

## Analyse d'impact

| Couche | Fichiers impactés | Nature de l'impact |
|--------|-------------------|--------------------|
| **Web** | `Program.cs` | Durée cookie réduite, sliding désactivé |
| **Web** | `Login.razor` | Checkbox « Se souvenir de moi », lockout activé |
| **Infrastructure** | `DependencyInjection.cs` | Config lockout Identity |

## Ce qui ne change PAS

- `IdentityRevalidatingAuthenticationStateProvider` : la revalidation du security stamp (30 min) reste en place.
- Logout (`/api/auth/logout`) : inchangé.
- Cookie sécurité (HttpOnly, Secure, SameSite=Strict) : inchangé.
- Mot de passe policy : inchangé.
- Headers de sécurité (CSP, HSTS, X-Frame-Options) : inchangés.

**Livrable** : Authentification durcie — cookie à durée fixe de 2 heures, lockout après 5 tentatives, choix « Se souvenir de moi ».
