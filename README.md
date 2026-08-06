# Metal Bayala Gestion - V1.0

Logiciel de gestion commerciale pour Metal Bayala (Mali).

## Prérequis

- Windows 10 ou 11 (64 bits)
- .NET 8 SDK (pour compilation uniquement)

## Installation / Compilation

1. Ouvrir une invite de commandes dans le dossier du projet
2. Exécuter : `dotnet restore`
3. Exécuter : `dotnet build`
4. Pour lancer : `dotnet run`

## Publication Windows (fichier unique)

Exécuter `publish.bat` ou la commande :

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish
```

L'exécutable `MetalBayalaGestion.exe` sera dans le dossier `publish/`.

## Base de données

La base SQLite est créée automatiquement au premier lancement dans :
`%LOCALAPPDATA%\MetalBayalaGestion\metalbayala.db`

## Données de démonstration

Pour charger les données de démo au premier lancement, lancer avec l'argument :

```bash
dotnet run -- --demo
```

## Identifiants par défaut

- Utilisateur : `admin`
- Mot de passe : `admin123`

## Fonctionnalités V1

- Tableau de bord avec KPI
- Gestion clients, fournisseurs, produits, catégories
- Devis avec export PDF A5
- Factures avec export PDF A5
- Paiements et suivi des créances
- Caisse et dépenses
- Sauvegarde / restauration de la base
- Paramètres de l'entreprise

## Technologies

- C# / .NET 8
- WPF / MVVM (CommunityToolkit.Mvvm)
- Entity Framework Core + SQLite
- MaterialDesignInXamlToolkit
- PdfSharp (PDF A5)
# Build trigger
