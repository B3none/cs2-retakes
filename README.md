[![GitHub Downloads](https://img.shields.io/github/downloads/b3none/cs2-retakes/total.svg?style=flat-square&label=Downloads)](https://github.com/b3none/cs2-retakes/releases/latest)
![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/b3none/cs2-retakes/plugin-build.yml?branch=master&style=flat-square&label=Latest%20Build)

# CS2 Retakes
CS2 implementation of retakes written in C# for CounterStrikeSharp. Based on the version for CS:GO by Splewis.

## Share the love
If you appreciate the project then please take the time to star the repository 🙏

![Star us](https://github.com/b3none/gdprconsent/raw/development/.github/README_ASSETS/star_us.png)

## Features / Roadmap
- [x] Bombsite selection
- [x] Per map configurations
- [x] Ability to add spawns
- [x] Spawn system
- [x] Temporary weapon allocation (hard coded)
- [x] Temporary grenade allocation (hard coded)
- [x] Equipment allocation
- [x] Queue manager (Queue system)
- [x] Team manager (with team switch calculations)
- [x] Retakes config file
- [x] Add translations
- [x] Improve bombsite announcement
- [x] Queue priority for VIPs
- [x] Add autoplant
- [x] Add a command to view the spawns for the current bombsite
- [x] Add a command to delete the nearest spawn
- [x] Implement better spawn management system
- [x] Add a release zip file without spawns too

## Installation
- Download the zip file from the [latest release](https://github.com/B3none/cs2-retakes/releases), and extract the contents into your `counterstrikesharp/plugins` directory.
- Download the [shared plugin zip file](https://github.com/B3none/cs2-retakes/releases/download/2.0.0/cs2-retakes-plugin-shared-2.0.0.zip) and put it into your `counterstrikesharp/shared` directory.

## Recommendations
I also recommend installing these plugins for an improved player experience
- Instadefuse: https://github.com/B3none/cs2-instadefuse
- Clutch Announce: https://github.com/B3none/cs2-clutch-announce
- Instaplant (if not using autoplant): https://github.com/B3none/cs2-instaplant

## Allocators
Although this plugin comes with it's own weapon allocation system, I would recommend using **one** of the following plugins for a better experience:
- Yoni's Allocator: https://github.com/yonilerner/cs2-retakes-allocator
- NokkviReyr's Allocator: https://github.com/nokkvireyr/kps-allocator
- Ravid's Allocator: https://github.com/Ravid-A/cs2-retakes-weapon-allocator

## Configuration
When the plugin is first loaded it will create a `retakes_config.json` file in the plugin directory. This file contains all of the configuration options for the plugin:

| Config                                            | Description                                                                                                                                     | Default    | Min        | Max        |
|---------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------|------------|------------|------------|
| Version                                           | The retakes config version. This is used to automatically migrate the retakes config file for you. **ignore this value**.                       | **IGNORE** | **IGNORE** | **IGNORE** |
| MaxPlayers                                        | The maximum number of players allowed in the game at any time. (If you want to increase the max capability you need to add more spawns)         | 9          | 2          | 10         |
| TerroristRatio                                    | The percentage of the total players that should be Terrorists.                                                                                  | 0.45       | 0          | 1          |
| RoundsToScramble                                  | The number of rounds won in a row before the teams are scrambled.                                                                               | 5          | -1         | 99999      |
| IsScrambleEnabled                                 | Whether to scramble the teams once the RoundsToScramble value is met.                                                                           | true       | false      | true       |
| EnableFallbackAllocation                          | Whether to enable the fallback weapon allocation. You should set this value to false if you're using a standalone weapon allocator.             | true       | false      | true       |
| EnableBombsiteAnnouncementVoices                  | Whether to play the bombsite announcement voices. The volume for these values is client sided `snd_toolvolume`.                                 | true       | false      | true       |
| EnableBombsiteAnnouncementCenter                  | Whether to display the bombsite in the center announcement box.                                                                                 | true       | false      | true       |
| ShouldBreakBreakables                             | Whether to break all breakable props on round start (People are noticing rare crashes when this is enabled).                                    | false      | false      | true       |
| ShouldOpenDoors                                   | Whether to open doors on round start (People are noticing rare crashes when this is enabled).                                                   | false      | false      | true       |
| IsAutoPlantEnabled                                | Whether to enable auto bomb planting at the start of the round or not.                                                                          | true       | false      | true       |
| IsDebugMode                                       | Whether to enable debug output to the server console or not.                                                                                    | false      | false      | true       |
| ShouldForceEvenTeamsWhenPlayerCountIsMultipleOf10 | Whether to force even teams when the active players is a multiple of 10 or not. (this means you will get 5v5 @ 10 players / 10v10 @ 20 players) | true       | false      | true       |
| EnableFallbackBombsiteAnnouncement                | Whether to enable the fallback bombsite announcement.                                                                                           | true       | false      | true       |
| ShouldRemoveSpectators                            | When a player is moved to spectators, remove them from all retake queues. Ensures that AFK plugins work as expected.                            | false      |    false      | true       |

## Commands
| Command         | Arguments                         | Description                                                          | Permissions |
|-----------------|-----------------------------------|----------------------------------------------------------------------|-------------|
| !showspawns     | <A / B>                           | Show the spawns for the specified bombsite.                          | @css/root   |
| !addspawn       | <CT / T> <Y / N (can be planter)> | Adds a retakes spawn point for the bombsite spawns currently shown.  | @css/root   |
| !removespawn    |                                   | Removes the nearest spawn point for the bombsite currently shown.    | @css/root   |
| !nearestspawn   |                                   | Teleports the player to the nearest spawn.                           | @css/root   |
| !hidespawns     |                                   | Exits the spawn editing mode.                                        | @css/root   |
| !scramble       |                                   | Scrambles the teams next round.                                      | @css/admin  |
| !voices         |                                   | Toggles whether or not to hear the bombsite voice announcements.     |             |
| css_debugqueues |                                   | **SERVER ONLY** Shows the current queue state in the server console. |             |

## Stay up to date
Subscribe to **release** notifications and stay up to date with the latest features and patches:

![image](https://github.com/B3none/cs2-retakes/assets/24966460/e288a882-0f1f-4e8c-b67f-e4c066af34ea)

## Credits
This was inspired by the [CS:GO Retakes project](https://github.com/splewis/csgo-retakes) written by [splewis](https://github.com/splewis).
