---
layout: default
title: Documentation fonctionnelle
nav_order: 4
---

# Documentation fonctionnelle
{: .fs-8 }

Modele metier, regles de gestion et tarification.
{: .fs-5 .fw-300 }

---

## Modele du domaine

### Reservation (Aggregate Root)

La reservation est l'entite centrale du systeme. Elle contient :

| Champ | Description |
|-------|-------------|
| Studio | Reference vers l'hebergement |
| DateDebut / DateFin | Periode de location (arrivee incluse, depart exclu) |
| Proprietaire | Le proprietaire qui a cree la reservation |
| Locataire | Nom et prenom de la personne qui occupe le studio |
| NbAdultes | Nombre d'adultes |
| NbEnfantsMoins3Ans | Nombre d'enfants de moins de 3 ans |
| TypeClient | Proprietaire, InviteAvecPresence, Connaissance, MobilHome, Tente |
| Statut | Demande, Acceptee, Confirmee |
| AccepteePar / AccepteeLe | Tracabilite de la validation |
| CreeeLe / ModifieeLe | Horodatage |

### Studio (Entite)

Catalogue initial des hebergements (modifiable par un admin : creation, suppression, et indicateur **Indisponible** pour retirer temporairement un studio du planning) :

| Nom | Capacite | Cuisine | Louable seul |
|-----|----------|---------|-------------|
| Villa | 6 pers. | Oui | Oui |
| Studio Est | 2 pers. | Oui | Oui |
| Studio Ouest | 2 pers. | Oui | Oui |
| Studio Centre | 2 pers. | Non | Non |
| Mobil-home | 6 pers. | Non | Non |
| Emplacement tente 1 | 4 pers. | Non | Oui |
| Emplacement tente 2 | 4 pers. | Non | Oui |

**Capacite totale** : 26 personnes

### Proprietaires

4 proprietaires fixes : **Lea**, **Sarah**, **Jean**, **Christophe**. Chacun dispose d'un compte ASP.NET Identity.

---

## Regles de gestion

### Disponibilite

- Un studio est **libre ou occupe** — pas de location partielle
- **Aucun chevauchement** de reservations sur un meme studio pour des dates qui se croisent
- La verification est effectuee en temps reel lors de la creation/modification

### Capacite

- Le nombre d'**adultes** ne peut pas depasser la capacite du studio
- Les **enfants de moins de 3 ans ne comptent pas** dans la capacite
- Controle : `Adultes <= Capacite`

### Studios dependants

Un studio marque **"Non louable seul"** (Studio Centre, Mobil-home) ne peut etre reserve qu'en etant **relie explicitement a une reservation parent** sur un studio independant :

- La reservation parent doit appartenir au **meme proprietaire** et porter sur un studio `RentableAlone = true`
- Les dates du parent doivent **englober entierement** celles de la reservation dependante (inclusion stricte, pas simple chevauchement)
- Le **statut du parent est propage** vers ses reservations dependantes
- Une reservation parent ne peut pas etre elle-meme dependante (pas de chainage)

### Dates

Logique de **nuitee** :
- Jour d'arrivee : **inclus**
- Jour de depart : **exclu**
- Exemple : du 3 au 10 = **7 nuits** facturees
- Le studio est considere comme **libre le jour du depart**

---

## Workflow de statut

Chaque reservation suit un cycle de vie en 3 etapes :

```
Demande (orange) → Acceptee (bleu) → Confirmee (vert)
```

| Statut | Couleur | Description |
|--------|---------|-------------|
| Demande | Orange (#ffb020) | Reservation en attente de validation |
| Acceptee | Bleu (#6ea8ff) | Validee par un proprietaire |
| Confirmee | Vert (#27c48b) | Confirmation finale |

Chaque transition enregistre :
- Le nom du proprietaire qui a effectue l'action
- La date et l'heure de la transition

---

## Tarification

### Grille tarifaire (2026)

Les tarifs sont exprimes en **euros par jour par personne** :

| Type de client | Tarif |
|---------------|-------|
| Proprietaire (et familles) | 3.50 EUR |
| Invite avec presence proprietaire | 7.00 EUR |
| Connaissance | 15.00 EUR |
| Connaissance enfant < 3 ans (50%) | 7.50 EUR |
| Mobil-home | 12.00 EUR |
| Tente | 7.00 EUR |

### Calcul du montant

```
Montant = Somme( Tarif x NbPersonnes x NbNuits ) pour chaque ligne de personnes
```

### Versionnement

La grille tarifaire est **versionnee par annee**. Un administrateur peut :
- Modifier les tarifs d'une annee existante
- Creer une nouvelle grille pour une annee future
- Les grilles passees sont conservees pour l'historique

### Cas particuliers

- **Mobil-home** : type client impose "Mobil-home", une seule ligne de personnes
- **Emplacements tente** : type client impose "Tente", une seule ligne de personnes
- **Enfants < 3 ans** : reduction automatique de 50 % pour les "Connaissances"

---

## Types de client

| Type | Description | Usage |
|------|-------------|-------|
| Proprietaire | Un des 4 coproprietaires et sa famille | Tarif le plus bas |
| InviteAvecPresence | Invite venant avec un proprietaire present | Tarif intermediaire |
| Connaissance | Personne sans lien avec un proprietaire | Tarif plein |
| MobilHome | Occupant du mobil-home | Tarif specifique, impose par le studio |
| Tente | Occupant d'un emplacement tente | Tarif specifique, impose par le studio |

---

## Indicateurs d'occupation (KPIs)

### Places occupees

Calcul : capacite maximale des studios ayant au moins une reservation **Acceptee ou Confirmee** ce jour-la.

- Les reservations au statut **Demande** ne sont pas comptees
- Un studio est compte en entier (sa capacite complete, pas le nombre reel de personnes)

### Taux d'occupation

Disponible sur une periode (selection de 2 dates) :

```
Taux = Moyenne( PlacesOccupees(jour) / CapaciteTotale ) pour chaque jour de la periode
```

---

## Roles et droits d'acces

| Role | Acces |
|------|-------|
| Public (anonyme) | Consultation du planning en lecture seule |
| Proprietaire | Creation, modification, suppression, changement de statut |
| Admin | Gestion de la grille tarifaire, des studios (CRUD) et des comptes proprietaires |

Le role Admin est attribue a Christophe et a un compte technique de maintenance.
