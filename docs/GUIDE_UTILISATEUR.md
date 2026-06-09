# Guide utilisateur - Planning Location

## Présentation

Planning Location est une application web de gestion de planning pour une maison de vacances partagée entre 4 copropriétaires. Elle permet de visualiser les réservations, de gérer les disponibilités et de suivre les statuts de chaque réservation.

## Accès à l'application

### Mode public (lecture seule)

L'application est accessible sans compte. En mode public, vous pouvez :

- **Consulter le planning mensuel** : voir toutes les réservations par studio et par jour
- **Changer de mois** : utiliser les flèches `<` et `>` autour du nom du mois
- **Filtrer les réservations** : par studio, statut ou propriétaire via la barre latérale
- **Voir le détail d'une réservation** : cliquer sur une réservation dans le planning
- **Consulter l'occupation** : cliquer sur un jour pour voir les KPIs (places occupées, studios occupés)

### Mode propriétaire (édition)

Cliquer sur **Se connecter** et saisir vos identifiants (email + mot de passe).

En mode connecté, vous avez accès à toutes les fonctionnalités de lecture, plus :

- Créer des réservations
- Modifier des réservations existantes
- Changer le statut des réservations
- Supprimer des réservations
- Accéder à la page d'administration

## Vues du planning

### Vue Mois (par défaut)

Affiche un tableau avec les studios en lignes et les jours du mois en colonnes. Chaque réservation apparaît avec un code couleur selon son statut :

| Couleur | Statut | Signification |
|---------|--------|---------------|
| Orange | Demande | Réservation en attente de validation |
| Bleu | Acceptée | Validée par un propriétaire |
| Vert | Confirmée | Confirmation finale |

### Vue Semaine

Affiche le planning sur 7 jours avec plus d'espace par jour. Idéal pour voir le détail des réservations en cours. Naviguer entre les semaines avec les flèches.

### Vue Liste

Affiche toutes les réservations du mois sous forme de liste triée par date d'arrivée. Pratique pour avoir une vue d'ensemble rapide.

## Gérer les réservations

### Créer une réservation

1. Cliquer sur **+ Nouvelle réservation**
2. Remplir le formulaire :
   - **Studio** : choisir l'hébergement
   - **Dates** : sélectionner la date d'arrivée et de départ
   - **Locataire** : nom complet de la personne
   - **Adultes / Enfants < 3 ans** : nombre de personnes
   - **Type de client** : propriétaire, invité, connaissance, etc.
3. Le montant estimé se calcule automatiquement
4. La disponibilité est vérifiée en temps réel (alerte si chevauchement)
5. Cliquer sur **Enregistrer**

La réservation est créée avec le statut **Demande**.

### Modifier une réservation

1. Cliquer sur une réservation dans le planning
2. Dans la modale de détail, cliquer sur **Modifier**
3. Modifier les champs souhaités
4. Cliquer sur **Enregistrer**

### Changer le statut

Le workflow de statut est : **Demande → Acceptée → Confirmée**

1. Cliquer sur une réservation
2. Dans la modale de détail :
   - Si le statut est "Demande" : cliquer sur **Accepter**
   - Si le statut est "Acceptée" : cliquer sur **Confirmer**
3. Le changement est enregistré avec le nom du propriétaire et la date/heure

### Supprimer une réservation

1. Cliquer sur une réservation
2. Cliquer sur **Supprimer**
3. Confirmer la suppression dans la modale de confirmation

## Filtres et KPIs

### Barre latérale

- **Mois** : sélecteur de mois/année
- **Studio** : filtrer par hébergement
- **Statut** : filtrer par Demande / Acceptée / Confirmée
- **Propriétaire** : filtrer par propriétaire

### Indicateurs (KPIs)

Cliquer sur un jour dans le planning pour afficher :

- **Places occupées / total** : nombre de places prises vs capacité totale
- **Studios occupés** : nombre de studios ayant au moins une réservation (Acceptée ou Confirmée)

## Administration

Accessible depuis le bouton **Administration** dans l'en-tête (propriétaires connectés).

### Grille tarifaire

- Visualiser et modifier les tarifs par type de client pour chaque année
- Naviguer entre les années avec les flèches
- Créer une grille pour une nouvelle année si elle n'existe pas
- Les tarifs sont en euros par jour par personne

### Studios

- Visualiser le catalogue des studios (capacité, cuisine, louable seul)
- Le catalogue est figé dans la configuration initiale

## Règles métier

- Un studio est **libre ou occupé** (pas de location partielle)
- **Aucun chevauchement** de réservations sur un même studio
- Un studio **non louable seul** ne peut être réservé que conjointement avec un studio indépendant
- La capacité ne peut pas être dépassée (adultes + enfants ≤ capacité)
- Jour d'arrivée inclus, jour de départ exclu (logique nuitée)

## Hébergements disponibles

| Nom | Capacité | Cuisine | Louable seul |
|-----|----------|---------|-------------|
| Villa | 6 | Oui | Oui |
| Studio Est | 2 | Oui | Oui |
| Studio Ouest | 2 | Oui | Oui |
| Studio Centre | 2 | Non | Non |
| Mobil-home | 6 | Non | Non |
| Emplacement tente 1 | 4 | Non | Oui |
| Emplacement tente 2 | 4 | Non | Oui |

## Comptes propriétaires

Les 4 comptes propriétaires sont : Léa, Sarah, Jean, Christophe. Les identifiants sont fournis par l'administrateur système.
