using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Botiki;
public class BotikiConfig : BasePluginConfig
{
    [JsonPropertyName("AdminPermissionFlags")]
    public string AdminPermissionFlags { get; set; } = "css/example-flag";
    [JsonPropertyName("PluginMode")]
    public string PluginMode { get; set; } = "fill";
    [JsonPropertyName("BotJoinAfterPlayer")]
    public bool BotJoinAfterPlayer { get; set; } = true;
    [JsonPropertyName("BotsHealth")]
    public int BotsHealth { get; set; } = 100;
    [JsonPropertyName("PlayersCountForKickBots")]
    public int PlayersCountForKickBots { get; set; } = 10;
    [JsonPropertyName("BotCount")]
    public int BotCount { get; set; } = 10;
}


[MinimumApiVersion(86)]
public class Botiki : BasePlugin, IPluginConfig<BotikiConfig>
{
    public override string ModuleName => "Botiki";

    public override string ModuleVersion => "v1.8.0";

    public override string ModuleAuthor => "jockii, VoCs007";

    public BotikiConfig Config { get; set; } = new BotikiConfig();

    public void OnConfigParsed(BotikiConfig config)
    {
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        SendConsoleCommand(BOT_KICK);

        if (Config.PluginMode == "fill")
        {
            SendConsoleCommand(BOT_MODE_FILL);
            SendConsoleCommand($"bot_quota {Config.BotCount}");
            SendConsoleCommand(BOT_ADD);

            if (Config.BotJoinAfterPlayer)
                SendConsoleCommand(BOT_JOIN_AFTER_PLAYER);
            else
                SendConsoleCommand(BOT_NOT_JOIN_AFTER_PLAYER);
        }

        if (Config.PluginMode == "match")
        {
            SendConsoleCommand(BOT_MODE_MATCH);
            SendConsoleCommand($"bot_quota {Config.BotCount}");
            SendConsoleCommand(BOT_ADD);

            if (Config.BotJoinAfterPlayer)
                SendConsoleCommand(BOT_JOIN_AFTER_PLAYER);
            else
                SendConsoleCommand(BOT_NOT_JOIN_AFTER_PLAYER);
        }

        if (Config.PluginMode == "balanced")
        {
            SendConsoleCommand(BOT_MODE_FILL);
            SendConsoleCommand(BOT_QUOTA_1);
            SendConsoleCommand(BOT_ADD);

            if (Config.BotJoinAfterPlayer)
                SendConsoleCommand(BOT_JOIN_AFTER_PLAYER);
            else
                SendConsoleCommand(BOT_NOT_JOIN_AFTER_PLAYER);
        }


        Console.WriteLine($"====================");
        Console.WriteLine();
        Console.WriteLine($"Plugin: {ModuleName}\nVersion: {ModuleVersion}\nAuthor: {ModuleAuthor}\nInfo: https://github.com/jockii");
        Console.WriteLine();
        Console.WriteLine($"====================");
    }

    public const string BOT_JOIN_AFTER_PLAYER = "bot_join_after_player true";
    public const string BOT_NOT_JOIN_AFTER_PLAYER = "bot_join_after_player false";
    public const string BOT_AUTO_VACATE = "bot_auto_vacate 1";
    public const string BOT_MODE_FILL = "bot_quota_mode fill";
    public const string BOT_MODE_MATCH = "bot_quota_mode match";
    public const string BOT_QUOTA_1 = "bot_quota 1";
    public const string BOT_ADD_CT = "bot_add_ct";
    public const string BOT_ADD_T = "bot_add_t";
    public const string BOT_ADD = "bot_add";
    public const string BOT_KICK = "bot_kick";
    public const int MIN_BOT_HP = 1;
    public const int STANDART_BOT_HP = 100;
    public const int MAX_BOT_HP = 9999999;
    public bool isNeedKick = true;

    public void SendConsoleCommand(string msg)
    {
        Server.ExecuteCommand(msg);
    }
    public void ChangePlayerTeamSide(List<CCSPlayerController> realPlayers, CsTeam teamName)
    {
        int teamToChange = teamName == CsTeam.Terrorist ? 3 : 2;
        realPlayers.Find(player => player.TeamNum == teamToChange)?.SwitchTeam(teamName);
    }

    (int T, int CT, int SPEC, bool IsBotExists, int? botTeam, List<CCSPlayerController> realPlayers) GetPlayersCount(List<CCSPlayerController> players)
    {
        List<CCSPlayerController> realPlayers = players.FindAll(player => !player.IsBot);

        int CT = 0;
        int T = 0;
        int SPEC = 0;
        int? botTeam = players.Find(player => player.IsValid && player.IsBot && !player.IsHLTV)?.TeamNum;

        realPlayers.ForEach(player =>
        {
            if (player.TeamNum == 2)
                T++;
            else if (player.TeamNum == 3)
                CT++;
        });

        return (T, CT, SPEC, players.Exists(player => player.IsValid && player.IsBot && !player.IsHLTV), botTeam, realPlayers);
    }
    (string kickbotT, string kickbotCT) KickOneBot(List<CCSPlayerController> players)
    {
        List<CCSPlayerController> bots = players.FindAll(player => player.IsValid && player.IsBot && !player.IsHLTV);
        string kickbotT = "";
        string kickbotCT = "";
        int? botT_UserId = bots.Find(bot => bot.TeamNum == 2)?.UserId;
        int? botInTER = bots.Find(bot => bot.TeamNum == 2)?.TeamNum;
        int? botCT_UserId = bots.Find(bot => bot.TeamNum == 3)?.UserId;
        int? botInCT = bots.Find(bot => bot.TeamNum == 3)?.TeamNum;

        if (botInTER == 2)
            kickbotT = $"kickid {botT_UserId}";
        else if (botInCT == 3)
            kickbotCT = $"kickid {botCT_UserId}";

        return (kickbotT, kickbotCT);
    }
    public void SetBotHp(List<CCSPlayerController> players)
    {
        RegisterEventHandler<EventRoundStart>((@event, info) =>
        {
            players.ForEach(player =>
            {
                if (player.IsValid && player.IsBot && !player.IsHLTV)
                {
                    if (Config.BotsHealth >= MIN_BOT_HP && Config.BotsHealth <= MAX_BOT_HP)
                        player.Pawn.Value.Health = Config.BotsHealth;
                    else if (Config.BotsHealth < MIN_BOT_HP || Config.BotsHealth > MAX_BOT_HP)
                        player.Pawn.Value.Health = STANDART_BOT_HP;
                }
            });

            return HookResult.Continue;
        });
    }

    public void GlobalSpawner()
    {
        if (Config.PluginMode == "fill")
        {
            // addbot in fill mode
        }

        if (Config.PluginMode == "match")
        {
            // addbot in match mode
        }

        if (Config.PluginMode == "balanced")
        {
            // addbot in balanced mode
            if ((T + CT) % 2 != 0)
                SendConsoleCommand(T > CT ? BOT_ADD_CT : BOT_ADD_T);
        }
    }
    public void GlobalKicker()
    {
        if (Config.PluginMode == "fill")
        {
            // kick bot in fill mode
        }

        if (Config.PluginMode == "match")
        {
            // kick bot in match mode
        }

        if (Config.PluginMode == "balanced")
        {
            // kick func (balanced mode)
            if ((T + CT) % 2 == 0)
                SendConsoleCommand(BOT_KICK);

            if (T + CT >= Config.PlayersCountForKickBots)
                SendConsoleCommand(BOT_KICK);

            if (T + CT == 0)
                SendConsoleCommand(BOT_KICK);
        }
    }

    public void Checker()
    {
        (int T, int CT, int SPEC, bool IsBotExists, int? botTeam, List<CCSPlayerController> realPlayers) = GetPlayersCount(Utilities.GetPlayers());

        if (Config.PluginMode == "fill")
        {

        }

        if (Config.PluginMode == "match")
        {

        }

        if (Config.PluginMode == "balanced")
        {
            // switcher real players
            if (T > 1 && CT == 0)
                ChangePlayerTeamSide(realPlayers, CsTeam.CounterTerrorist);
            if (CT > 1 && T == 0)
                ChangePlayerTeamSide(realPlayers, CsTeam.Terrorist);
            // add or kick bot
            if (IsBotExists)
            {
                GlobalKicker();
            }
            else
            {
                GlobalSpawner();
            }
        }
    }

    [ConsoleCommand("bothp")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnCommandSBotHp(CCSPlayerController? controller, CommandInfo command)
    {
        if (controller == null) return;
        if (AdminManager.PlayerHasPermissions(controller, Config.AdminPermissionFlags))
        {
            if (Regex.IsMatch(command.GetArg(1), @"^\d+$"))
            {
                if (int.Parse(command.GetArg(1)) == 0)
                {
                    controller.PrintToChat($" {ChatColors.Red}[ {ChatColors.Purple}Botiki {ChatColors.Red}] {ChatColors.Red}Bot HP can`t be zero!");
                }
                else
                {
                    Config.BotsHealth = int.Parse(command.GetArg(1));
                    controller.PrintToChat($" {ChatColors.Red}[ {ChatColors.Purple}Botiki {ChatColors.Red}] {ChatColors.Default}New Bot HP: {ChatColors.Green}{Config.BotsHealth}");
                }
            }
            else
            {
                controller.PrintToChat($" {ChatColors.Red}Incorrect value! Please input correct number");
            }
        }
        else
            controller.PrintToChat($" {ChatColors.Red}You are not Admin!!!");
    }

    [ConsoleCommand("botkick")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnCommandBotKick(CCSPlayerController? controller, CommandInfo command)
    {
        if (controller == null) return;
        if (AdminManager.PlayerHasPermissions(controller, Config.AdminPermissionFlags))
        {
            SendConsoleCommand(BOT_KICK);
            controller.PrintToChat($" {ChatColors.Red}[ {ChatColors.Purple}Botiki {ChatColors.Red}] {ChatColors.Olive}Bot`s was kicked... {ChatColors.Green}OK!");
        }
        else
            controller.PrintToChat($" {ChatColors.Red}You are not Admin!!!");
    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        if (Config.PluginMode == "fill")
        {

        }

        if (Config.PluginMode == "match")
        {

        }

        if (Config.PluginMode == "balanced")
        {

        }

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        if (Config.PluginMode == "fill")
        {

        }

        if (Config.PluginMode == "match")
        {

        }

        if (Config.PluginMode == "balanced")
        {

        }

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnSwitchTeam(EventSwitchTeam @event, GameEventInfo info)
    {
        if (Config.PluginMode == "fill")
        {

        }

        if (Config.PluginMode == "match")
        {

        }

        if (Config.PluginMode == "balanced")
        {
            if (isNeedKick)
            {
                SendConsoleCommand(BOT_KICK);
                isNeedKick = false;
            }

            if (((T == 0 && CT == 1) || (CT == 0 && T == 1)) && IsBotExists)
            {
                //SendConsoleCommand(BOT_KICK);
                SendConsoleCommand("sv_cheats true");
                SendConsoleCommand("endround");
                SendConsoleCommand("sv_cheats false");
            }

            if (T == 0 || CT == 0)
            {
                SendConsoleCommand("sv_cheats true");
                SendConsoleCommand("endround");
                SendConsoleCommand("sv_cheats false");
            }

            if (IsBotExists && T > CT && botTeam == 2)
                Utilities.GetPlayers()?.Find(player => player.IsValid && player.IsBot && !player.IsHLTV)?.ChangeTeam(CsTeam.CounterTerrorist);
            else if (IsBotExists && CT > T && botTeam == 3)
                Utilities.GetPlayers()?.Find(player => player.IsValid && player.IsBot && !player.IsHLTV)?.ChangeTeam(CsTeam.Terrorist);
        }

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        if (Config.PluginMode == "fill")
        {

        }

        if (Config.PluginMode == "match")
        {

        }

        if (Config.PluginMode == "balanced")
        {

        }

        return HookResult.Continue;
    }
}