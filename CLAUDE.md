# Conventions FootStats

Application Blazor Server (.NET 9) + EF Core/SQLite. Le fonctionnel est décrit dans [README.md](README.md) ;
ce fichier ne contient que ce qui ne se devine pas en lisant le code.

**La langue du projet est le français** : interface, libellés, messages d'erreur, commentaires et messages de
commit.

## Charte graphique

`wwwroot/theme.css` est la vraie feuille de style du projet. `wwwroot/app.css` est le résidu du template Blazor
par défaut : ne rien y ajouter.

Tout passe par les variables de `:root` — jamais de couleur en dur dans un composant :

| Rôle | Variables |
| --- | --- |
| Terrain | `--pitch-dark` `--pitch` `--pitch-light` `--pitch-lighter` |
| Accent | `--gold` `--gold-dark` |
| Neutres | `--ink` `--paper` `--paper-alt` `--line` `--muted` |
| Alerte | `--danger` |
| Système / ciel | `--system*` (pages admin), `--sky*` (invitations en attente) |

Typographie : `--font-display` (Bebas Neue) pour les titres et les chiffres, `--font-body` (Work Sans) pour le
texte. Classes réutilisables : `.container`, `.card`/`.card-pad`, `.stat-grid`/`.stat-tile`, `.pill`,
`.btn`/`.btn-primary`/`.btn-outline`/`.btn-danger`, `.score-row`, `.modal-backdrop`/`.modal-panel`, `.icon`.

Icônes : SVG inline en trait (`class="icon"`, `viewBox="0 0 24 24"`), jamais d'emoji.

Mobile : la bascule se fait à **640px** (feuilles ancrées en bas, boutons pleine largeur empilés) et à **900px**
(grilles resserrées). Toute nouvelle boîte doit être testée aux deux tailles.

## Render modes : le piège principal

`Components/Routes.razor` n'a **pas** de `@rendermode`. Conséquence : le routeur et les layouts sont rendus en
**statique (SSR)**, et chaque page est un **îlot interactif** isolé (`@rendermode InteractiveServer` en tête de
page). Deux implications qui ont déjà causé des bugs :

1. Un composant placé dans un layout est statique. S'il doit dialoguer avec les pages, il lui faut son propre
   `@rendermode="InteractiveServer"` (cf. `<ConfirmDialog @rendermode="InteractiveServer" />` dans `MainLayout`).
   Sans cela il vit dans une **autre portée d'injection** que les pages : les services *scoped* du rendu SSR et
   ceux du circuit sont des instances différentes, et rien ne communique.
2. Un `ErrorBoundary` placé dans un layout ne peut pas intercepter les exceptions des pages, pour la même raison.

`Login.razor` et `Error.razor` sont volontairement statiques (POST de formulaire vers `/account/login`).

## Confirmations

Ne jamais utiliser `JS.InvokeAsync<bool>("confirm", …)`. Injecter `ConfirmService` et appeler :

```csharp
if (!await Confirm.AskAsync(new ConfirmRequest
{
    Title = "Supprimer le joueur",
    Message = "Cette action est définitive. …",   // dire la CONSÉQUENCE, pas répéter l'action
    ConfirmLabel = "Supprimer",
    Tone = ConfirmTone.Danger,                     // Danger = bouton rouge, réservé à l'irréversible
    Icon = ConfirmIcon.Trash,
    SubjectName = $"{player.FirstName} {player.LastName}",
    SubjectMeta = player.Position,
    SubjectImagePath = player.PhotoPath            // photo/logo si disponible
})) return;
```

La boîte est rendue une seule fois par `Components/Shared/ConfirmDialog.razor`, monté dans `MainLayout`.

## Erreurs

Une exception non gérée **tue le circuit** Blazor Server : la page est définitivement figée, aucun composant ne
peut plus s'afficher. Le panneau d'erreur est donc du HTML/CSS/JS pur :

- markup : `#blazor-error-ui` dans `Components/App.razor` (hors du routeur, donc présent sur toutes les pages,
  quel que soit leur layout — ne pas le remettre dans un layout) ;
- style : `.errbox*` dans `theme.css` ;
- détails : `wwwroot/site.js` capture les dernières erreurs (`console.error`, `error`, `unhandledrejection`) et
  les injecte dans le panneau, car Blazor affiche le panneau sans jamais y écrire le texte de l'exception.

Le texte de l'exception .NET n'arrive au navigateur que si `DetailedErrors` est actif (voir README).

## Modèle : ce qui surprend

- **Une saison n'est pas recréée chaque année.** Elle est unique par couple (joueur, club) et se prolonge en
  modifiant ses dates et sa catégorie. `SeasonRenewalHelper` détecte les saisons à prolonger et propose les
  valeurs (dates +1 an, catégorie recalculée depuis l'âge). Une saison dont `EndDate` est dépassée bascule
  automatiquement en archivée au chargement de `Home.razor`.
- **Les séances d'entraînement n'existent pas en base.** Seule la règle récurrente (`Training`) est stockée ;
  `TrainingOccurrenceHelper` génère les dates à la volée. Une date précise ne porte une ligne que pour une
  exception : `TrainingOccurrenceOverride` (annulation/horaire/lieu) ou `TrainingAttendance` (présence).
- Le taux de présence ne compte que les séances **explicitement pointées** : une séance jamais pointée n'entre ni
  au numérateur ni au dénominateur, pour ne pas fausser le chiffre avec l'historique non saisi.

## Base de données

Migrations EF Core classiques, appliquées automatiquement au démarrage (`DbSeeder.SeedAsync`). Ne jamais éditer
une migration déjà appliquée en production.

```bash
cd FootStats.Web && dotnet ef migrations add <Nom>
```

## Pièges de l'environnement Windows

- **Le serveur de dev verrouille `bin/`.** Arrêter l'application avant `dotnet build` ou `dotnet ef`, sinon la
  compilation échoue sur la copie de `FootStats.Web.dll`/`.exe` (l'erreur ne parle pas du verrou, elle parle de
  droits d'accès). Pour compiler sans l'arrêter : `dotnet build -o <dossier temporaire>`.
- **`.gitignore` est insensible à la casse** (`core.ignorecase=true`). Une règle `data/` non ancrée exclurait
  aussi `FootStats.Web/Data/`, donc tout le code des services — d'où le `/data/` ancré à la racine. Vérifier avec
  `git check-ignore -v <fichier>` après toute modification du `.gitignore`.
- **Interpolations imbriquées interdites en Razor.** `$"… {(x ? $"…" : "")}"` dans un bloc `@code` provoque un
  `RZ1000: Unterminated string literal`. Calculer la valeur dans une variable locale avant.
- Ne jamais committer `FootStats.Web/publish/`, `wwwroot/uploads/` ni `Data/uploads/` : binaires et données
  personnelles (photos d'enfants).
