using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
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
    public string AdminPermissionFlags { get; set; } = "@css/kick";
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
            SetCVAR("bot_quota", Config.BotCount);
            //SendConsoleCommand(BOT_ADD);

            if (Config.BotJoinAfterPlayer)
                SendConsoleCommand(BOT_JOIN_AFTER_PLAYER);
            else
                SendConsoleCommand(BOT_NOT_JOIN_AFTER_PLAYER);
        }

        if (Config.PluginMode == "match")
        {
            SendConsoleCommand(BOT_MODE_MATCH);
            SetCVAR("bot_quota", Config.BotCount);
            //SendConsoleCommand(BOT_ADD);

            if (Config.BotJoinAfterPlayer)
                SendConsoleCommand(BOT_JOIN_AFTER_PLAYER);
            else
                SendConsoleCommand(BOT_NOT_JOIN_AFTER_PLAYER);
        }

        if (Config.PluginMode == "balanced")
        {
            SendConsoleCommand(BOT_MODE_FILL);
            SetCVAR("bot_quota", 1);
            //SendConsoleCommand(BOT_ADD);

            if (Config.BotJoinAfterPlayer)
                SendConsoleCommand(BOT_JOIN_AFTER_PLAYER);
            else
                SendConsoleCommand(BOT_NOT_JOIN_AFTER_PLAYER);
        }

        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        RegisterEventHandler<EventSwitchTeam>(OnSwitchTeam);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);

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

    public void SetCVAR(string convar, int value)
    {
        var CVar = ConVar.Find(convar);
        CVar?.SetValue(value);

    }
    public void ChangePlayerTeamSide(List<CCSPlayerController> players, CsTeam teamName)
    {
        int teamToChange = teamName == CsTeam.Terrorist ? 3 : 2;
        players.Find(player => player.TeamNum == teamToChange)?.SwitchTeam(teamName);
    }

    (int T, int Tb, int Th, int CT, int CTb, int CTh, int SPEC, bool IsBotExists, int? botTeam, string kickbotT, string kickbotCT) PlayersData()
    {
        List<CCSPlayerController> players = Utilities.GetPlayers();
        List<CCSPlayerController> realPlayers = players.FindAll(player => !player.IsBot);
        List<CCSPlayerController> bots = players.FindAll(player => player.IsValid && player.IsBot && !player.IsHLTV);

        int CT = 0;
        int CTh = 0;
        int CTb = 0;
        int T = 0;
        int Th = 0;
        int Tb = 0;
        int SPEC = 0;
        int? botTeam = players.Find(player => player.IsValid && player.IsBot && !player.IsHLTV)?.TeamNum;
        bool isBotExists = players.Exists(player => player.IsValid && player.IsBot && !player.IsHLTV);

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

        players.ForEach(player =>
        {
            if (player == null) return;

            if (player.IsValid && !player.IsHLTV)
            {
                if (player.TeamNum == 2)
                    T++;
                else if (player.TeamNum == 3)
                    CT++;
            }

            if (player.IsValid && !player.IsBot && !player.IsHLTV)
            {
                if (player.TeamNum == 2)
                    Th++;
                else if (player.TeamNum == 3)
                    CTh++;
            }

            if (player.IsValid && player.IsBot && !player.IsHLTV)
            {
                if (player.TeamNum == 2)
                    Tb++;
                else if (player.TeamNum == 3)
                    CTb++;
            }
        });

        return (T, Tb, Th, CT, CTb, CTh, SPEC, isBotExists, botTeam, kickbotT, kickbotCT);

        //return (T, CT, SPEC, players.Exists(player => player.IsValid && player.IsBot && !player.IsHLTV), botTeam, kickbotT, kickbotCT);
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
        (int T, int Tb, int Th, int CT, int CTb, int CTh, int SPEC, bool IsBotExists, int? botTeam, string kickbotT, string kickbotCT) = PlayersData();

        CCSPlayerController controller = Utilities.GetPlayers().Find(pl => pl.IsValid && !pl.IsBot && !pl.IsHLTV)!;

        //set bot hp
        Utilities.GetPlayers().ForEach(player =>
        {
            if (player.IsValid && player.IsBot && !player.IsHLTV)
            {
                if (Config.BotsHealth >= MIN_BOT_HP && Config.BotsHealth <= MAX_BOT_HP)
                    player.Pawn.Value!.Health = Config.BotsHealth;
                else if (Config.BotsHealth < MIN_BOT_HP || Config.BotsHealth > MAX_BOT_HP)
                    player.Pawn.Value!.Health = STANDART_BOT_HP;
            }
        });

        switch (Config.PluginMode)
        {
            case "fill":

                if (Th + CTh >= Config.PlayersCountForKickBots)
                    SendConsoleCommand(BOT_KICK);

                if (T + CT < Config.BotCount)
                {
                    if (T > CT)
                        SendConsoleCommand(BOT_ADD_CT);
                    if (CT > T)
                        SendConsoleCommand(BOT_ADD_T);
                }

                if ( T > CT && T + CT <= Config.BotCount)
                {
                    SendConsoleCommand(kickbotT);
                    SendConsoleCommand(BOT_ADD_CT);
                }

                if (CT > T && T + CT <= Config.BotCount)
                {
                    SendConsoleCommand(kickbotCT);
                    SendConsoleCommand(BOT_ADD_T);
                }

                if (!IsBotExists && (T + CT) < Config.PlayersCountForKickBots)
                {
                    for (int i = 0; (T + CT) < Config.BotCount; i++)
                    {
                        SendConsoleCommand(T > CT ? BOT_ADD_CT : BOT_ADD_T);
                    }
                }

                break;

            case "match":
                //code
                break;

            case "balanced":
                // add or kick bot
                if (IsBotExists)
                {
                    // kick func (balanced mode)
                    if ((T + CT) % 2 == 0)
                        SendConsoleCommand(BOT_KICK);

                    if (T + CT >= Config.PlayersCountForKickBots)
                        SendConsoleCommand(BOT_KICK);

                    if (T + CT == 0)
                        SendConsoleCommand(BOT_KICK);
                }
                else
                {
                    // addbot in balanced mode
                    if ((T + CT) % 2 != 0)
                        SendConsoleCommand(T > CT ? BOT_ADD_CT : BOT_ADD_T);
                }
                break;


            default:
                Console.WriteLine("Error confign mode");
                break;
        }

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        (int T, int Tb, int Th, int CT, int CTb, int CTh, int SPEC, bool IsBotExists, int? botTeam, string kickbotT, string kickbotCT) = PlayersData();

        CCSPlayerController controller = Utilities.GetPlayers().Find(pl => pl.IsValid && !pl.IsBot && !pl.IsHLTV)!;

        switch (Config.PluginMode)
        {
            case "fill":

                //if (!IsBotExists && (T + CT) < Config.PlayersCountForKickBots)
                //{
                //    for (int i = 0; (T + CT) < Config.BotCount; i++)
                //    {
                //        SendConsoleCommand(T > CT ? BOT_ADD_CT : BOT_ADD_T);
                //    }
                //}

                break;

            case "match":
                //code
                break;

            case "balanced":
                //code
                break;


            default:
                Console.WriteLine("Error confign mode");
                break;
        }

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnSwitchTeam(EventSwitchTeam @event, GameEventInfo info)
    {
        (int T, int Tb, int Th, int CT, int CTb, int CTh, int SPEC, bool IsBotExists, int? botTeam, string kickbotT, string kickbotCT) = PlayersData();

        CCSPlayerController controller = Utilities.GetPlayers().Find(pl => pl.IsValid && !pl.IsBot && !pl.IsHLTV)!;

        switch (Config.PluginMode)
        {
            case "fill":

                if (controller.TeamChanged)
                {
                    if (controller.TeamNum == 2 && controller.InSwitchTeam)
                        SendConsoleCommand(kickbotT);
                    else if (controller.TeamNum == 3)
                        SendConsoleCommand(kickbotCT);
                }


                break;

            case "match":
                //code
                break;

            case "balanced":
                if (isNeedKick)
                {
                    SendConsoleCommand(BOT_KICK);
                    isNeedKick = false;
                }

                // switcher real players
                if (T > 1 && CT == 0)
                    ChangePlayerTeamSide(Utilities.GetPlayers(), CsTeam.CounterTerrorist);
                if (CT > 1 && T == 0)
                    ChangePlayerTeamSide(Utilities.GetPlayers(), CsTeam.Terrorist);

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
                break;


            default:
                Console.WriteLine("Error confign mode");
                break;
        }

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        (int T, int Tb, int Th, int CT, int CTb, int CTh, int SPEC, bool IsBotExists, int? botTeam, string kickbotT, string kickbotCT) = PlayersData();

        CCSPlayerController controller = Utilities.GetPlayers().Find(pl => pl.IsValid && !pl.IsBot && !pl.IsHLTV)!;


        switch (Config.PluginMode)
        {
            case "fill":

                if (T + CT < Config.BotCount)
                    SendConsoleCommand(T > CT ? BOT_ADD_CT : BOT_ADD_T);

                break;

            case "match":
                //code
                break;

            case "balanced":
                //code
                break;


            default:
                Console.WriteLine("Error confign mode");
                break;
        }

        return HookResult.Continue;
    }
}