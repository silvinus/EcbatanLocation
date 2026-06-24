# Phase 6 : UI Blazor - Authentification & Édition propriétaire

**Objectif** : Les propriétaires peuvent se connecter et gérer les réservations.

## Tâches

1. **Pages d'authentification**
   - Page Login (`/login`) : formulaire email + mot de passe
   - Logout
   - Gestion de session Blazor Server (AuthenticationStateProvider)

2. **Adaptation du planning en mode connecté**
   - Bouton "+ Nouvelle réservation" visible uniquement pour les propriétaires
   - Actions sur les réservations existantes (modifier, changer statut)
   - Indicateur visuel du mode connecté

3. **Modal Création / Modification de réservation**
   - Formulaire complet : studio, dates, propriétaire, locataire, nb adultes, nb enfants, type client
   - Validation côté client (FluentValidation + affichage erreurs)
   - Contrôle chevauchement en temps réel (appel query)
   - Estimation du montant en temps réel
   - Boutons : Enregistrer / Valider

4. **Actions de changement de statut**
   - Boutons contextuels : "Accepter" (si Demande), "Confirmer" (si Acceptée)
   - Confirmation avant action
   - Traçabilité automatique (qui + quand)

5. **Gestion des autorisations Blazor**
   - `AuthorizeView` pour masquer les actions d'édition
   - Vérification côté serveur dans les handlers (double sécurité)

**Livrable** : Circuit complet de gestion des réservations par les propriétaires.
