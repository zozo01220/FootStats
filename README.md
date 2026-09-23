# FootStats

Application de suivi des matchs et statistiques de football (tournois, buts, équipes, saisons) pour usage familial.

## Stack

- ASP.NET Core Blazor Server (.NET 9)
- Entity Framework Core + SQLite
- Authentification par cookie (nom d'utilisateur / mot de passe, hashé avec BCrypt)

## Modèle

- **Joueur** : nom, prénom, date de naissance, poste, numéro préféré, photo
- **Club** : nom, logo ; un joueur peut changer de club (historique avec dates de début/fin)
- **Saison** : ex. "2026-2027", rattachée à un joueur, avec catégorie (ex. U11) et club
- **Événement** (tournoi ou plateau) : nom (optionnel pour un plateau), ville, date, rattaché à une saison
- **Match** : équipe (1 à 5), adversaire, buts marqués par le joueur, score de l'équipe

Les statistiques (nombre de matchs, buts/match, répartition par équipe par match ou par événement, etc.) sont calculées à la volée par saison.

Les photos de joueurs et logos de clubs sont stockés dans le même volume persistant que la base de données (`/data/uploads`), donc ils survivent aux redéploiements du conteneur.

## Lancer en local (développement)

```bash
cd FootStats.Web
dotnet run
```

L'app écoute sur `http://localhost:5212`. Un utilisateur admin est créé automatiquement au premier lancement (voir console pour le mot de passe par défaut si `AdminUser:Password` n'est pas défini).

## Déploiement sur la VM Debian

1. Copier le dossier `FootStats` sur la VM (ex: via `git clone` ou `scp`)
2. Adapter le mot de passe admin dans `docker-compose.yml` (`AdminUser__Password`)
3. Lancer :
   ```bash
   docker compose up -d --build
   ```
   L'app écoute alors sur `http://127.0.0.1:8080` sur la VM.
4. Configurer nginx en reverse proxy (voir `nginx-footstats.conf.example`) pour exposer l'app sur `https://stats.familleprieur01.duckdns.org`
5. Activer HTTPS avec certbot :
   ```bash
   sudo certbot --nginx -d stats.familleprieur01.duckdns.org
   ```

Les données SQLite sont stockées dans un volume Docker nommé `footstats-data`, donc elles survivent aux mises à jour/redéploiements du conteneur.

## Sauvegardes

La base est un simple fichier SQLite. Pour sauvegarder :
```bash
docker cp footstats:/data/footstats.db ./backup-footstats-$(date +%F).db
```
