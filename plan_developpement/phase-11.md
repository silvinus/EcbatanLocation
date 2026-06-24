# Phase 11 : Gestion des utilisateurs (Admin)

**Objectif** : Permettre à un administrateur de gérer les comptes utilisateurs (propriétaires et administrateurs) depuis l'interface d'administration : ajouter, modifier, supprimer, et régénérer un mot de passe aléatoire.

## Analyse d'impact

| Couche | Fichiers impactés | Nature de l'impact |
|--------|-------------------|--------------------|
| **Domain** | `Owner.cs`, `IOwnerRepository.cs` | `Owner.Update()` pour renommer. Ajout de `AddAsync`, `UpdateAsync`, `DeleteAsync` au repository. |
| **Application** | Nouveaux dossiers Commands + Queries | 5 nouvelles opérations CQRS (voir ci-dessous). Nouveau DTO `UserDto`. |
| **Infrastructure** | `OwnerRepository.cs`, `DbInitializer.cs` | Implémentation des nouvelles méthodes du repository. Interaction `UserManager` pour Identity. |
| **Web** | `Admin.razor` | Nouvel onglet « Utilisateurs » dans la page admin existante. |
| **Tests** | Domain.Tests, Application.Tests | Tests unitaires des nouvelles rules + handlers. |

## Règles métier & contraintes

- Un utilisateur peut avoir le rôle **Owner**, **Admin**, ou les deux (comme Christophe).
- Un **Owner** a une entrée dans la table `Owners` (liée à `AspNetUsers` via `UserId`). Un Admin pur n'en a pas.
- **Suppression** : un propriétaire ne peut être supprimé que s'il n'a **aucune réservation** existante (intégrité référentielle). Si des réservations existent, l'admin doit d'abord les réaffecter ou supprimer.
- **Mot de passe régénéré** : mot de passe aléatoire de 12 caractères (majuscules, minuscules, chiffres, caractère spécial). Affiché **une seule fois** à l'admin dans une modale de confirmation (pas d'envoi email — pas de serveur SMTP configuré).
- L'admin ne peut pas se supprimer lui-même.
- Il doit toujours rester au moins un compte Admin.

## Tâches

### 1. Domain

- **`Owner.cs`** — Ajouter méthode `Update(string name)` pour permettre le renommage.
- **`IOwnerRepository.cs`** — Ajouter : `Task AddAsync(Owner owner, CancellationToken ct)`, `Task UpdateAsync(Owner owner, CancellationToken ct)`, `Task DeleteAsync(Owner owner, CancellationToken ct)`.

### 2. Application — Commands

1. **`CreateUserCommand`** (`IRequireAdmin`)
   - Input : `DisplayName`, `Email`, `Roles[]` (Owner/Admin)
   - Handler : crée `ApplicationUser` via `UserManager`, assigne les rôles, crée l'entrée `Owner` si rôle Owner, génère un mot de passe aléatoire.
   - Retour : `CreatedUserResult` contenant le mot de passe généré (affiché une fois).

2. **`UpdateUserCommand`** (`IRequireAdmin`)
   - Input : `UserId`, `DisplayName`, `Email`, `Roles[]`
   - Handler : met à jour `ApplicationUser`, ajuste rôles, met à jour ou crée/supprime l'entrée `Owner` selon le changement de rôle.

3. **`DeleteUserCommand`** (`IRequireAdmin`)
   - Input : `UserId`
   - Handler : vérifie aucune réservation liée, vérifie que ce n'est pas le dernier admin, supprime `Owner` + `ApplicationUser`.

4. **`ResetPasswordCommand`** (`IRequireAdmin`)
   - Input : `UserId`
   - Handler : génère un mot de passe aléatoire via `UserManager.ResetPasswordAsync`, retourne le nouveau mot de passe.

### 3. Application — Queries

5. **`GetUsersQuery`** (`IRequireAdmin`)
   - Retour : `IReadOnlyList<UserDto>` — Id, DisplayName, Email, Roles[], IsOwner, HasReservations (pour savoir si supprimable).

### 4. Application — DTOs & Validators

- **`UserDto`** : `UserId`, `DisplayName`, `Email`, `Roles`, `IsOwner`, `HasReservations`
- **`CreatedUserResult`** : `UserId`, `GeneratedPassword`
- **Validators** : email valide et unique, nom non vide, au moins un rôle, mot de passe conforme aux règles Identity.

### 5. Infrastructure

- **`OwnerRepository.cs`** — Implémenter `AddAsync`, `UpdateAsync`, `DeleteAsync`.
- **Handlers** — Les handlers `CreateUser`, `UpdateUser`, `DeleteUser`, `ResetPassword` utilisent directement `UserManager<ApplicationUser>` (injecté). Pas besoin d'abstraction supplémentaire — c'est de la logique d'infrastructure/Identity, pas du domaine pur.

### 6. Web — UI

- **`Admin.razor`** — Ajouter un 3e onglet « Utilisateurs » dans les tabs existants :
  - Tableau listant tous les utilisateurs : nom, email, rôles (badges), actions.
  - Bouton « + Nouvel utilisateur » ouvrant une modale de création.
  - Actions par ligne : Modifier (modale), Régénérer mot de passe (confirmation + affichage), Supprimer (confirmation).
  - Modale création/modification : champs DisplayName, Email, checkboxes Propriétaire/Administrateur.
  - Modale mot de passe généré : affiche le mot de passe en clair une seule fois avec bouton copier.
  - Le bouton supprimer est désactivé si l'utilisateur a des réservations (tooltip explicatif).

### 7. Tests

- **Domain.Tests** : `Owner.Update()` renomme correctement.
- **Application.Tests** :
  - `CreateUserCommandHandler` : crée user + owner si rôle Owner, refuse email dupliqué.
  - `DeleteUserCommandHandler` : refuse si réservations existantes, refuse si dernier admin.
  - `ResetPasswordCommandHandler` : retourne un mot de passe valide.
  - `GetUsersQueryHandler` : retourne la liste avec rôles et flag `HasReservations`.

## Points d'attention

- **Pas de migration EF Core nécessaire** : `Owner` et `AspNetUsers` existent déjà. Les nouvelles opérations sont des CRUD sur les tables existantes.
- **`UserManager`** gère déjà la complexité Identity (hashing, validation mot de passe, rôles). On s'appuie dessus sans réinventer.
- **Accès** : toutes les opérations marquées `IRequireAdmin` → sécurisées via `AdminAuthorizationBehavior` existant.

**Livrable** : Page de gestion des utilisateurs dans l'admin, CRUD complet propriétaires/admins, régénération de mot de passe aléatoire.
