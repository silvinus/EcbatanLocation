# Phase 9 : Durcissement (sécurité, intégrité, architecture, UI)

**Objectif** : Corriger les points d'amélioration identifiés lors de la revue de code. Chaque item ci-dessous porte une décision validée avec le client (✅ à faire / ⛔ pas d'action / ❓ non tranché).

## Sécurité & autorisations

1. ✅ **Autorisation par rôle (`Admin`)**
   - `AuthorizationBehavior` ne vérifie aujourd'hui que `IsAuthenticated`, jamais les rôles. La grille tarifaire doit être réservée aux admins.
   - Introduire un marqueur de rôle requis (ex. `IRequireRole` / `IRequireAdmin`) et faire échouer la commande si l'utilisateur n'a pas le rôle.
   - Page `Admin.razor` : passer de `[Authorize]` à `[Authorize(Roles="Admin")]`.
   - **Christophe est propriétaire ET admin** : son compte doit cumuler les rôles `Owner` + `Admin`.

2. ✅ **Comptes Admin seedés**
   - Donner le rôle `Admin` au compte Christophe (en plus de `Owner`).
   - Ajouter un nouvel utilisateur **Sylvain** avec le rôle `Admin` (administrateur technique, pas forcément propriétaire).
   - Masquer le bouton/lien « Administration » aux non-admins dans `MainLayout`.

3. ⛔ **Mots de passe par défaut** — pas d'action pour le moment (à revoir avant mise en production réelle).

4. ⛔ **Contrôle de propriété sur les réservations** — pas de contrôle pour le moment : tout propriétaire peut modifier/valider toute réservation (y compris la sienne). Décision assumée.

## Intégrité des données

5. ✅ **Race condition de double-réservation (TOCTOU)**
   - Encadrer la vérification de chevauchement + persistance dans **une transaction**.
   - Ajouter en complément un **index** (contrainte d'exclusion / index sur Studio + dates) garantissant l'absence de chevauchement au niveau base.

6. ⛔ **Tarif enfants -3 ans hors « Connaissance »** — comportement actuel conservé (plein tarif), pas d'action.

7. ⛔ **Réservation à cheval sur deux années** — non supporté volontairement : faire **deux réservations** distinctes. Pas de réservation à cheval sur l'année. À documenter dans l'UI/aide si besoin.

## Architecture

8. ✅ **Déplacer la logique de tarification dans le Domain**
   - Sortir le calcul de `EstimateAmountQueryHandler` vers le Domain (ex. `PricingGrid.CalculateAmount(personLines, numberOfDays)`).
   - Le handler ne fait plus que charger la grille et déléguer.

9. ✅ **Uniformiser les DTOs en anglais**
   - Renommer les DTOs francisés : `PlanningMensuelDto` → `MonthlyPlanningDto`, `OccupationJourDto` → `DailyOccupationDto`, `OccupationRangeDto` → `RangeOccupationDto` (et propager dans Queries/UI).
   - Cohérence totale avec la convention « tout le code en anglais ».

10. ✅ **Mettre en place des domain events**
    - Infrastructure de domain events (ex. `IDomainEvent`, collection d'events sur l'aggregate `Reservation`, dispatch après `SaveChanges`).
    - Premiers events : `ReservationCreated`, `ReservationAccepted`, `ReservationConfirmed`, `ReservationDeleted`.
    - Prépare les futures notifications / audit (nice-to-have ultérieurs).

11. ✅ **Tester la couche Application**
    - Le projet `EcbatanLocation.Application.Tests` ne contient aujourd'hui aucun test.
    - Couvrir les handlers (création, modification, transitions de statut, estimation multi-lignes) et les validators FluentValidation, avec repositories mockés.

## UI / UX

12. ✅ **Découper `Home.razor`**
    - Le composant fait ~400 lignes (état, chargement, sélection de plage, pilotage des modales).
    - Extraire l'état/orchestration (container ou service de page) et alléger le composant.

13. ✅ **Supprimer les styles inline**
    - Remplacer les `style="display:flex;gap:..."` dispersés (Home, Admin) par des classes CSS du thème.

14. ✅ **Accessibilité**
    - `aria-label` sur les boutons de navigation `<` / `>`.
    - Statuts non différenciés uniquement par la couleur : ajouter icône/texte (daltonisme).
    - Gestion du focus dans les modales (focus trap, fermeture clavier).

15. ⛔ **Transitions de statut « retour » (refuser/annuler/rétrograder)** — pas d'action pour le moment.

16. ❓ **Confidentialité des infos en mode public (H4)** — non tranché. À décider plus tard (RGPD / masquage nom locataire pour les anonymes).

### Nice-to-have (à rediscuter plus tard)

- Export iCal / abonnement calendrier
- Notifications email
- Historique/audit complet des modifications
- Récapitulatif annuel par propriétaire (nuitées, montants)
- Versionner le dossier `deployement/`

**Livrable** : Application durcie côté sécurité (rôles admin), intégrité (anti double-réservation), architecture (tarif au Domain, DTOs anglais, domain events, tests Application) et UI (découpage, styles, accessibilité).

## Statut d'avancement (Phase 9 livrée)

| # | Item | Statut |
|---|------|--------|
| 1 | Autorisation par rôle Admin | ✅ Fait |
| 2 | Comptes Christophe (Owner+Admin) + Sylvain (Admin) | ✅ Fait |
| 3 | Mots de passe par défaut | ⛔ Sans action (à revoir avant prod) |
| 4 | Contrôle de propriété des réservations | ⛔ Sans action |
| 5 | Anti double-réservation (transaction + index unique) | ✅ Fait |
| 6 | Tarif enfants -3 ans hors « Connaissance » | ⛔ Sans action |
| 7 | Réservation à cheval sur deux années | ⛔ Sans action (faire 2 réservations) |
| 8 | Tarification déplacée dans le Domain (`PricingGrid.CalculateAmount`) | ✅ Fait |
| 9 | DTOs renommés en anglais (`MonthlyPlanningDto`, `DailyOccupationDto`, `RangeOccupationDto`) | ✅ Fait |
| 10 | Domain events (INotification + intercepteur EF) | ✅ Fait |
| 11 | Tests de la couche Application (26 tests) | ✅ Fait |
| 12 | Découpage `Home.razor` (code-behind) | ✅ Fait |
| 13 | Styles inline → classes CSS | ✅ Fait |
| 14 | Accessibilité (ARIA, clavier, focus, `role=dialog`) | ✅ Fait |
| 15 | Transitions de statut « retour » | ⛔ Sans action |
| 16 | Confidentialité publique (H4) | ❓ Non tranché |

**Tests** : 94 tests verts (Domain 44, Application 26, Infrastructure 24). Build sans warning.

**Vérification E2E** : parcours navigateur complet (Claude Preview) de tous les cas d'usage — public, login propriétaire/admin, création/édition/accepter/confirmer/supprimer, overlap, grille tarifaire, responsive mobile.

**Correctifs issus des tests E2E** :
- Message de succès admin « Grille tarifaire enregistrée » qui ne s'affichait jamais → corrigé.
- Accessibilité clavier de la vue Mois (`.booking-span`) qui manquait → ajoutée.
- Page `/Account/AccessDenied` conviviale ajoutée (au lieu d'un écran blanc).

**Commits** : `d27ed9e` (sécurité/intégrité/archi), `7328895` (domain events), `9f8e86b` (UI), `8c18e60` (correctifs E2E).

**Reste ouvert** : item 16 (confidentialité H4) à trancher ; nice-to-have à planifier.
