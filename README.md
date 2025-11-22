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
1. Install [CounterStrike Sharp](https://github.com/roflmuffin/CounterStrikeSharp) and [Metamod:Source](https://www.sourcemm.net/downloads.php/?branch=master)
2. Download the plugin from the [releases section](https://github.com/B3none/cs2-retakes/releases/latest):
   - **RetakesPlugin-[version].zip** - Includes pre-configured map spawns (recommended for new installations)
   - **RetakesPlugin-[version]-no-map-configs.zip** - Without map configurations (for custom setups)
3. Unzip the archive and upload it to the game server into your `addons/counterstrikesharp/` directory.
4. Start the server and wait for the config.json file to be generated in `addons/counterstrikesharp/configs/plugins/RetakesPlugin`.
5. Complete the configuration file with the parameters of your choice.

## Recommendations
I also recommend installing these plugins for an improved player experience
- Instadefuse: https://github.com/B3none/cs2-instadefuse
- Retakes Zones (prevent silly flanks / rotations): https://github.com/oscar-wos/Retakes-Zones
- Clutch Announce: https://github.com/B3none/cs2-clutch-announce
- Instaplant (if not using autoplant): https://github.com/B3none/cs2-instaplant

## Allocators
Although this plugin comes with it's own weapon allocation system, I would recommend using **one** of the following plugins for a better experience:
- Yoni's Allocator: https://github.com/yonilerner/cs2-retakes-allocator
- NokkviReyr's Allocator: https://github.com/nokkvireyr/kps-allocator
- Ravid's Allocator: https://github.com/Ravid-A/cs2-retakes-weapon-allocator

## Configuration
When the plugin is first loaded it will create a `retakes_config.json` file in the plugin directory. This file contains all of the configuration options for the plugin:

### GameSettings
| Config                    | Description                                                                                                                             | Default | Min   | Max   |
|---------------------------|-----------------------------------------------------------------------------------------------------------------------------------------|---------|-------|-------|
| MaxPlayers                | The maximum number of players allowed in the game at any time. (If you want to increase the max capability you need to add more spawns) | 9       | 2     | 10    |
| ShouldBreakBreakables     | Whether to break all breakable props on round start (People are noticing rare crashes when this is enabled).                            | false   | false | true  |
| ShouldOpenDoors           | Whether to open doors on round start (People are noticing rare crashes when this is enabled).                                           | false   | false | true  |
| EnableFallbackAllocation  | Whether to enable the fallback weapon allocation. You should set this value to false if you're using a standalone weapon allocator.     | true    | false | true  |

### QueueSettings
| Config                 | Description                                                                                                   | Default  | Min | Max |
|------------------------|---------------------------------------------------------------------------------------------------------------|----------|-----|-----|
| QueuePriorityFlag      | A list of priority flag configurations. Each entry contains DisplayName, Flag, and Priority. Players with higher priority can replace players with lower priority in the queue. | `[{"DisplayName": "VIP", "Flag": "@css/vip", "Priority": 0}]` | 0 | 100 |
| QueueImmunityFlag      | A list of immunity flag configurations. Each entry contains DisplayName, Flag, and Priority. Players with immunity priority cannot be replaced by players with equal or lower priority. | `[{"DisplayName": "VIP", "Flag": "@css/vip", "Priority": 0}]` | 0 | 100 |
| ShouldRemoveSpectators | When a player is moved to spectators, remove them from all retake queues. Ensures that AFK plugins work as expected. | true     | false | true |

**QueuePriorityFlag and QueueImmunityFlag Configuration:**
Each flag configuration object has the following properties:
- **DisplayName**: The display name shown in messages (e.g., "VIP", "VIP Plus")
- **Flag**: The CSS permission flag (e.g., "@css/vip", "@css/vipplus")
- **Priority**: The priority value (higher numbers = higher priority). Valid range: 0-100

**Example Configuration:**
```json
{
  "QueuePriorityFlag": [
    {"DisplayName": "VIP", "Flag": "@css/vip", "Priority": 0},
    {"DisplayName": "VIP Plus", "Flag": "@css/vipplus", "Priority": 100}
  ],
  "QueueImmunityFlag": [
    {"DisplayName": "VIP", "Flag": "@css/vip", "Priority": 0},
    {"DisplayName": "VIP Plus", "Flag": "@css/vipplus", "Priority": 100}
  ]
}
```

In this example:
- Players with `@css/vip` have priority 0
- Players with `@css/vipplus` have priority 100
- VIP Plus players can replace VIP players and regular players
- VIP players can replace regular players but not VIP Plus players
- Immunity works the same way: players with higher immunity priority cannot be replaced by players with equal or lower priority

**Example: Disabling Slot Priority and Immunity:**
To disable slot priority and immunity features, set both arrays to empty:
```json
{
  "QueuePriorityFlag": [],
  "QueueImmunityFlag": []
}
```

### TeamSettings
| Config                                            | Description                                                                                                                                     | Default | Min   | Max   |
|---------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------|---------|-------|-------|
| TerroristRatio                                    | The percentage of the total players that should be Terrorists.                                                                                  | 0.45    | 0     | 1     |
| RoundsToScramble                                  | The number of rounds won in a row before the teams are scrambled.                                                                               | 5       | -1    | 99999 |
| IsScrambleEnabled                                 | Whether to scramble the teams once the RoundsToScramble value is met.                                                                           | true    | false | true  |
| IsBalanceEnabled                                  | Whether to enable the default team balancing mechanic.                                                                                          | true    | false | true  |
| ShouldForceEvenTeamsWhenPlayerCountIsMultipleOf10 | Whether to force even teams when the active players is a multiple of 10 or not. (this means you will get 5v5 @ 10 players / 10v10 @ 20 players) | true    | false | true  |
| ShouldPreventTeamChangesMidRound                  | Whether or not to prevent players from switching teams at any point during the round.                                                           | true    | false | true  |

### MapConfigSettings
| Config                             | Description                                                                                     | Default | Min   | Max  |
|------------------------------------|-------------------------------------------------------------------------------------------------|---------|-------|------|
| EnableBombsiteAnnouncementVoices   | Whether to play the bombsite announcement voices.                                               | false   | false | true |
| EnableBombsiteAnnouncementCenter   | Whether to display the bombsite in the center announcement box.                                 | true    | false | true |
| EnableFallbackBombsiteAnnouncement | Whether to enable the fallback bombsite announcement.                                           | true    | false | true |

### BombSettings
| Config             | Description                                                         | Default | Min   | Max  |
|--------------------|---------------------------------------------------------------------|---------|-------|------|
| IsAutoPlantEnabled | Whether to enable auto bomb planting at the start of the round or not. | true    | false | true |

### DebugSettings
| Config      | Description                                                 | Default | Min   | Max  |
|-------------|-------------------------------------------------------------|---------|-------|------|
| IsDebugMode | Whether to enable debug output to the server console or not. | false   | false | true |

## Commands

### General Commands
| Command            | Arguments                         | Description                                                          | Permissions |
|--------------------|-----------------------------------|----------------------------------------------------------------------|-------------|
| !forcebombsite     | <A / B>                           | Force the retakes to occur from a single bombsite.                   | @css/root   |
| !forcebombsitestop |                                   | Clear the forced bombsite and return back to normal.                 | @css/root   |
| !scramble          |                                   | Scrambles the teams next round.                                      | @css/admin  |
| !scrambleteams     |                                   | Scrambles the teams next round (alias).                              | @css/admin  |
| !voices            |                                   | Toggles whether or not to hear the bombsite voice announcements.     |             |
| css_debugqueues    |                                   | **SERVER ONLY** Shows the current queue state in the server console. |             |

### Spawn Editor Commands
| Command            | Arguments                         | Description                                                          | Permissions |
|--------------------|-----------------------------------|----------------------------------------------------------------------|-------------|
| !showspawns        | <A / B>                           | Show the spawns for the specified bombsite.                          | @css/root   |
| !spawns            | <A / B>                           | Show the spawns for the specified bombsite (alias).                  | @css/root   |
| !edit              | <A / B>                           | Show the spawns for the specified bombsite (alias).                  | @css/root   |
| !addspawn          | <CT / T> <Y / N (can be planter)> | Adds a retakes spawn point for the bombsite spawns currently shown.  | @css/root   |
| !add               | <CT / T> <Y / N (can be planter)> | Adds a retakes spawn point (alias).                                  | @css/root   |
| !newspawn          | <CT / T> <Y / N (can be planter)> | Adds a retakes spawn point (alias).                                  | @css/root   |
| !new               | <CT / T> <Y / N (can be planter)> | Adds a retakes spawn point (alias).                                  | @css/root   |
| !removespawn       |                                   | Removes the nearest spawn point for the bombsite currently shown.    | @css/root   |
| !remove            |                                   | Removes the nearest spawn point (alias).                             | @css/root   |
| !deletespawn       |                                   | Removes the nearest spawn point (alias).                             | @css/root   |
| !delete            |                                   | Removes the nearest spawn point (alias).                             | @css/root   |
| !nearestspawn      |                                   | Teleports the player to the nearest spawn.                           | @css/root   |
| !nearest           |                                   | Teleports the player to the nearest spawn (alias).                   | @css/root   |
| !hidespawns        |                                   | Exits the spawn editing mode.                                        | @css/root   |
| !done              |                                   | Exits the spawn editing mode (alias).                                | @css/root   |
| !exitedit          |                                   | Exits the spawn editing mode (alias).                                | @css/root   |

### Map Config Commands
| Command            | Arguments          | Description                                    | Permissions |
|--------------------|--------------------|------------------------------------------------|-------------|
| !mapconfig         | <Config file name> | Forces a specific map config file to load.     | @css/root   |
| !setmapconfig      | <Config file name> | Forces a specific map config file to load (alias). | @css/root   |
| !loadmapconfig     | <Config file name> | Forces a specific map config file to load (alias). | @css/root   |
| !mapconfigs        |                    | Displays a list of available map configs.     | @css/root   |
| !viewmapconfigs    |                    | Displays a list of available map configs (alias). | @css/root   |
| !listmapconfigs    |                    | Displays a list of available map configs (alias). | @css/root   |

## Stay up to date
Subscribe to **release** notifications and stay up to date with the latest features and patches:

![image](https://github.com/B3none/cs2-retakes/assets/24966460/e288a882-0f1f-4e8c-b67f-e4c066af34ea)

## Credits
This was inspired by the [CS:GO Retakes project](https://github.com/splewis/csgo-retakes) written by [splewis](https://github.com/splewis).

## Server Hosting (Discounted)

Looking for reliable server hosting? [Dathost](https://dathost.net/r/b3none/cs2-server-hosting) offers top-tier performance, easy server management, and excellent support, with servers available in multiple regions across the globe. Whether you're in North America, Europe, Asia, or anywhere else, [Dathost](https://dathost.net/r/b3none/cs2-server-hosting) has you covered. Use [this link](https://dathost.net/r/b3none/cs2-server-hosting) to get **30% off your first month**. Click [here]( https://dathost.net/r/b3none/cs2-server-hosting) to get started with the discount!