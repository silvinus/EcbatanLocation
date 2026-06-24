# Phase 2 : Couche Domain - Modèle métier

**Objectif** : Entités riches avec logique métier, value objects, interfaces de repository.

## Tâches

1. **Entités et Value Objects**
   - `Studio` (entité) : Id, Nom, Capacite, ACuisine, LouableSeul
   - `Proprietaire` (entité) : Id, Nom, UserId (lien Identity)
   - `Reservation` (aggregate root) : tous les champs du CDC
   - `DateRange` (value object) : DateDebut, DateFin, avec validation et méthode `Chevauche(DateRange other)`
   - `TypeClient` (enum) : Proprietaire, InviteAvecPresence, Connaissance, MobilHome, Tente
   - `StatutReservation` (enum) : Demande, Acceptee, Confirmee
   - `GrilleTarifaire` (entité) : Annee, liste de `LigneTarif`
   - `LigneTarif` (value object) : TypeClient, PrixParJourParPersonne

2. **Règles métier dans Reservation**
   - Méthode `Accepter(string parQui, DateTime quand)` : transition Demande → Acceptée
   - Méthode `Confirmer(string parQui, DateTime quand)` : transition Acceptée → Confirmée
   - Validation capacité dans le constructeur
   - Propriétés de traçabilité (AccepteePar, AccepteeLe)

3. **Interfaces de repository**
   - `IReservationRepository`
   - `IStudioRepository`
   - `IGrilleTarifaireRepository`
   - `IProprietaireRepository`

4. **Domain Services**
   - `ReservationDomainService` : vérification chevauchement, vérification règle "non louable seul"

5. **Tests unitaires Domain**
   - Tests des invariants de Reservation (chevauchement, capacité, transitions de statut)
   - Tests du DateRange (chevauchement, validité)
   - Tests du ReservationDomainService

**Livrable** : Modèle métier solide et testé, aucune dépendance infrastructure.
