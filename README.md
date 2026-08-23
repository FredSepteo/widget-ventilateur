# FanWidget — Contrôle des ventilateurs PC

Widget Windows compact pour régler la vitesse de **Sys Fan 1** et **Sys Fan 2** via PWM (carte mère).

## Prérequis

- Windows 10/11 (64 bits)
- [.NET 9 SDK](https://dotnet.microsoft.com/download) pour compiler
- **[PawnIO](https://pawnio.eu/)** — pilote kernel requis pour accéder au SuperIO (NCT6687D sur votre MSI B760)
- **Droits administrateur** (UAC au démarrage)

### Installation PawnIO

```powershell
# Automatique
.\install-pawnio.bat

# Ou via winget
winget install --id namazso.PawnIO --exact --silent --accept-package-agreements --accept-source-agreements
```

## Lancement

```powershell
.\lancer.bat
```

Le script installe PawnIO si absent, compile si nécessaire, puis lance le widget en administrateur.

## Utilisation

1. Le widget s’affiche **toujours au premier plan** — glissez la barre de titre pour le déplacer.
2. Utilisez les **curseurs** pour fixer la vitesse PWM (0–100 %).
3. Cliquez **Auto** pour rendre le contrôle à la carte mère (courbe BIOS).
4. Ouvrez **Paramètres** (icône engrenage) si Sys Fan 1/2 ne sont pas détectés automatiquement.

## Détection automatique

Le widget tente de reconnaître les ventilateurs boîtier par nom (`Sys Fan`, `System Fan`, `CHA_FAN`, `Fan #3`, etc.) et exclut CPU / pompe AIO.

La correspondance est enregistrée dans :

`%LOCALAPPDATA%\FanWidget\mapping.json`

## Limitations

- Le contrôle PWM dépend du chipset SuperIO de votre carte mère ; toutes les cartes ne sont pas supportées.
- Fermez les autres logiciels qui contrôlent les ventilateurs (FanControl, BIOS tools, etc.) pour éviter les conflits.
- À la fermeture, les ventilateurs repassent en mode **Auto** (contrôle carte mère).

## Structure

```
FanWidget/
├── Services/FanControlService.cs   # Accès matériel via LibreHardwareMonitor
├── Models/                         # Modèles ventilateur
├── Views/SettingsWindow.xaml       # Mapping manuel Sys Fan 1 / 2
└── MainWindow.xaml                 # Interface widget
```
