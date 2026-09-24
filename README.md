# FootStats

Application de suivi sportif familial : saisons, matchs, tournois, entraînements et statistiques, pour un ou
plusieurs enfants suivis par un compte administrateur.

## Stack

- ASP.NET Core Blazor Server (.NET 9)
- Entity Framework Core + SQLite
- Authentification par cookie (nom d'utilisateur / mot de passe, hashé avec BCrypt)

## Modèle

- **Joueur** : nom, prénom, date de naissance, poste, numéro préféré, photo, actif/inactif
- **Club** : nom, acronyme, logo, couleur de maillot ; l'historique des clubs d'un joueur est conservé
  (`PlayerClub`, avec dates de début/fin)
- **Saison** : rattachée à **un** joueur et **un** club — un joueur ne peut avoir qu'une seule saison par club.
  Elle porte un libellé (ex. « 2026/2027 »), une catégorie (ex. U11), un sport et des dates. Elle n'est pas
  recréée chaque année : on prolonge la même saison (dates + catégorie) d'une année sur l'autre.
- **Sport** : Football, Basketball ou Équitation ; la fiche saison s'adapte (buts/points, ou concours et épreuves)
- **Événement** (tournoi ou plateau) : nom (optionnel pour un plateau), ville, date, rattaché à une saison
- **Match** : équipe (1 à 5), adversaire, buts/points et passes du joueur, score de l'équipe
- **Concours équestre** : ville, date, et ses épreuves (discipline + classement)
- **Entraînement** : une règle *récurrente* par saison (jours de la semaine, horaires, lieu, période). Les séances
  ne sont pas stockées une par une : elles sont générées à la volée. Deux exceptions se greffent sur une date
  précise — une **annulation** (motif : annulé / trêve-vacances / joueur malade) et une **présence**
  (présent / excusé / absent), qui alimente le taux de présence affiché sur la fiche saison.
- **Utilisateurs** : rôles SuperAdmin, Admin, User et Consultant (lecture seule). Un admin invite des proches par
  email, ou partage l'accès à certains joueurs via un lien à usage unique.

Les statistiques (nombre de matchs, buts/match, répartition par équipe, taux de présence, etc.) sont calculées à
la volée, jamais stockées.

Les photos de joueurs et logos de clubs sont stockés dans le même volume persistant que la base de données
(`/data/uploads`), donc ils survivent aux redéploiements du conteneur. Ils ne sont jamais versionnés dans Git.

## Lancer en local (développement)

```bash
cd FootStats.Web
dotnet run
```

L'app écoute sur `http://localhost:5212`. Les migrations EF Core sont appliquées automatiquement au démarrage.
Un utilisateur super-administrateur est créé au premier lancement (voir la console pour le mot de passe par
défaut si `AdminUser:Password` n'est pas défini).

## Déploiement sur la VM Debian

1. Copier le dossier `FootStats` sur la VM (ex: via `git clone` ou `scp`)
2. Adapter le mot de passe admin dans `docker-compose.yml` (`AdminUser__Password`)
3. Lancer :
   ```bash
   docker compose up -d --build
   ```
   L'app écoute alors sur `http://127.0.0.1:8080` sur la VM.
4. Configurer nginx en reverse proxy (voir `nginx-footstats.conf.example`) pour exposer l'app sur
   `https://stats.familleprieur01.duckdns.org`
5. Activer HTTPS avec certbot :
   ```bash
   sudo certbot --nginx -d stats.familleprieur01.duckdns.org
   ```

Les données SQLite sont stockées dans un volume Docker nommé `footstats-data`, donc elles survivent aux
mises à jour/redéploiements du conteneur.

### Variables d'environnement utiles

| Variable | Effet |
| --- | --- |
| `AdminUser__Username` / `AdminUser__Password` | Identifiants du super-administrateur créé au premier lancement |
| `DetailedErrors` | `true` affiche la pile d'appels .NET dans le panneau d'erreur côté navigateur. Actif d'office en développement ; à n'activer en production que si vous acceptez d'exposer les traces aux utilisateurs connectés. |

Les identifiants OAuth Google/Microsoft se configurent sous `Authentication:Google` et `Authentication:Microsoft`
(vides par défaut : la connexion externe est alors masquée).

## Sauvegardes

La base est un simple fichier SQLite. Pour sauvegarder :
```bash
docker cp footstats:/data/footstats.db ./backup-footstats-$(date +%F).db
```

## Contribuer

Les conventions du projet (charte graphique, render modes Blazor, boîtes de confirmation, gestion des erreurs,
pièges connus) sont décrites dans [CLAUDE.md](CLAUDE.md).
