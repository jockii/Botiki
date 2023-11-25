![Static Badge](https://img.shields.io/badge/Plugin-v1.5.0-darkgreen)
![Static Badge](https://img.shields.io/badge/CSSharpAPI-Min:v65-blue)
![Static Badge](https://img.shields.io/badge/Status-dev-darkred)

![image](https://github.com/jackson-tougher/cs2_BOTiki/assets/119735356/c7ab2b4a-1c24-4364-a2e8-346c3d94aa4b)

## About
The plugin adds bots and currently works as a `balance mode` that is, if `T = 2` and `CT = 1`, then a bot will be added to the CT team.

## Functions
1. Only admin can manage settings/commands.
To do this, you need to enter your steam ID in the format steam ID64
2. The plugin automatically adds a bot for a team with fewer players, if the players are equal, the bots will be removed.
You can specify the value of players in the config, after which the bots will be deleted automatically
3. Set the bot's health in the configuration to permanently accept this value, even when the server is restarted, or a command in the game chat that will change the bot's health until the server is restarted
4. Kick the bots if it is not necessary, or if there is a bug with the bot, they will be added in the next round
5. Change the config values and download the changes immediately without rebooting the server

## To do list
I want to implement full management of bots in one place =D
- [x] Balance mode ( there are still bugs :) )
- [ ] Fill mode ( similarly bot_quota_mode fill ) 
- [ ] Match mods ( similarly bot_quota_mode match )
- [ ] Off/On bots ( turn bots ON or OFF with one click )

## Commands
* `!botiki_kick` - will remove bots immediately
* `!botiki_reload` - reload the config if you need to accept any changes
* `!setbothp N` - will set N value of the bot's HP (1 .... 9999999)

![image](https://github.com/jackson-tougher/cs2_BOTiki/assets/119735356/06ca556a-d83d-40ba-8646-440eeb67a50c)
![image](https://github.com/jackson-tougher/cs2_BOTiki/assets/119735356/8943fb30-533d-4382-93e3-bf891634c57e)
![image](https://github.com/jackson-tougher/cs2_BOTiki/assets/119735356/76ab8f02-fad4-4121-ac42-288b40f4ba5b)
## Config
The config `Config.json` is generated automatically in the folder where the plugin is located. In order for your changes to take effect (you made yourself an admin), 
you need to restart the server the first time, after changing the data, you can restart the plugin with the command `!botiki_restart`
* `admins_ID64` - list of steam_id`s who will be an admin in the steam_id64 format (example: 76561199424391272)
* `bot_HP` - value of the bot's HP (example: 300 | value = 1 .... 9999999)
* `playerCount_botKick` - number of players value after which bots will be deleted (example: 10)

![image](https://github.com/jackson-tougher/cs2_BOTiki/assets/119735356/e0f259b9-50bd-4630-8e36-e8310694bdde)

## Note
* `game_mode 0 game_type 0` => `casual`  |  in `competitive` maybe not correct working!
* You maybe need to remove `bot_quota` and `bot_quota_mode` from your server CFG, but not sure :/
## Attention
* Maybe the plugin will have some bugs, pls send me ERRORS                     
## Developers
![Static Badge](https://img.shields.io/badge/Author-jackson%20tougher-orange)
![Static Badge](https://img.shields.io/badge/Collaborator-VoCs-purple)
