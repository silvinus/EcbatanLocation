# Phase 5 : UI Blazor - Planning & Lecture publique

**Objectif** : L'écran principal du planning est fonctionnel et consultable publiquement.

## Tâches

1. **Layout principal**
   - `MainLayout.razor` : thème sombre (CSS basé sur les maquettes)
   - Barre de navigation : titre, bouton connexion, bouton nouvelle réservation
   - Layout responsive (grid 340px sidebar + 1fr main)

2. **Page Planning (`/`)**
   - Composant `PlanningMensuel.razor` : tableau studios × jours
   - Colonne sticky pour les noms de studios
   - Cellules avec réservations colorées par statut
   - Navigation mois précédent / suivant

3. **Sidebar Filtres & Synthèse**
   - Composant `FiltresSynthese.razor`
   - Filtres : mois, studio, statut, propriétaire
   - KPIs : places occupées/disponibles, studios occupés (pour le jour survolé ou sélectionné)
   - Légende des statuts

4. **Composant Réservation (cellule)**
   - `ReservationCell.razor` : affiche nom locataire, propriétaire, badge statut, nb personnes
   - Couleur de bordure selon statut
   - Clic → détail (modal ou panel)

5. **Modal Détail Réservation**
   - Lecture seule en mode public
   - Affiche toutes les infos + estimation montant

6. **CSS / Thème**
   - Variables CSS issues des maquettes (--bg, --panel, --brand, --ok, --warn, --danger)
   - Composants visuels : cards, chips, badges, boutons
   - Responsive breakpoint à 1020px

**Livrable** : Planning consultable publiquement, filtres, KPIs, thème sombre fidèle aux maquettes.
