# Phase 12 : Types client flexibles Mobil-home / Tente

**Objectif** : Permettre de choisir le type de client pour les studios Mobil-home et Tente, au lieu de forcer un type exclusif.

## Problème

Actuellement, sélectionner un studio Mobil-home force le type `MobileHome` et sélectionner un emplacement Tente force le type `Tent`. Le dropdown est désactivé, une seule ligne de personnes est autorisée. Cela empêche de facturer au tarif propriétaire ou invité quand c'est pertinent.

## Nouvelle règle

| Studio | Types autorisés |
|--------|----------------|
| Mobil-home | MobileHome, Owner, GuestWithPresence |
| Emplacement tente | Tent, Owner, GuestWithPresence |
| Autres studios | Owner, GuestWithPresence, Acquaintance |

- Les studios Mobil-home et Tente permettent désormais **plusieurs lignes de personnes** et le **mix de types**.
- Le dropdown est actif (plus désactivé).
- Les types `Acquaintance` (pour Mobil-home/Tente) et `MobileHome`/`Tent` (pour les studios classiques) restent interdits.

## Tâches

1. **`ReservationFormModal.razor`** — Refondre `UpdateExclusiveType()` :
   - Remplacer le flag booléen `_isExclusiveType` par une liste `_allowedClientTypes` calculée selon le studio.
   - Supprimer le forçage du type et la limitation à 1 ligne.
   - Filtrer le dropdown des types selon `_allowedClientTypes`.
   - Adapter `AddLine()` pour choisir le prochain type parmi les types autorisés.
   - Réactiver le bouton "+" et le bouton "×" pour Mobil-home/Tente.

## Impact

| Couche | Impact |
|--------|--------|
| Domain | Aucun |
| Application (validators) | Aucun (validation `.IsInEnum()` suffisante) |
| Infrastructure | Aucun |
| Tarification | Aucun (grille de prix existante couvre tous les types) |
| UI | Seul `ReservationFormModal.razor` est modifié |

**Livrable** : Types de client flexibles pour Mobil-home et Tente, multi-lignes autorisées.
