using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Botiki;
public class BotikiConfig : BasePluginConfig
{
    public string AdminPermissionFlags { get; set; } = "css/example-flag";
    public int BotsHealth { get; set; } = 100;
    public int PlayersCountForKickBots { get; set; } = 10;
    public int BotCount { get; set; } = 10;
}


[MinimumApiVersion(65)]
public class Botiki : BasePlugin, IPluginConfig<BotikiConfig>
{
    public override string ModuleName => "Botiki";

    public override string ModuleVersion => "v1.7.5";

    public override string ModuleAuthor => "jockii, VoCs007";

    public BotikiConfig Config { get; set; } = new BotikiConfig();

    public void OnConfigParsed(BotikiConfig config)
    {
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        Console.WriteLine($"Plugin: {ModuleName} ver:{ModuleVersion} by {ModuleAuthor} has been loaded =)");

    }

    public const string BOT_JOIN_AFTER_PLAYER = "bot_join_after_player 1";     //
    public const string BOT_AUTO_VACATE = "bot_auto_vacate 1";                //
    public const string BOT_MODE_FILL = "bot_quota_mode fill";               //
    public const string BOT_MODE_MATCH = "bot_quota_mode match";            //
    public const string BOT_QUOTA_1 = "bot_quota 1";                       //
    public const string BOT_ADD_CT = "bot_add_ct";                        //
    public const string BOT_ADD_T = "bot_add_t";                         //        
    public const string BOT_ADD = "bot_add";                            //
    public const string BOT_KICK = "bot_kick";                         //       <-- 
    public const int MIN_BOT_HP = 1;                                  //
    public const int STANDART_BOT_HP = 100;                          //
    public const int MAX_BOT_HP = 9999999;                          //
    public bool isNeedKick = true;                           //

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
            if (player.TeamNum == 1)
                SPEC++;
            else if (player.TeamNum == 2)
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
    public void SetBotHp(List<CCSPlayerController> playersList)
    {
        RegisterEventHandler<EventRoundStart>((@event, info) =>
        {
            playersList.ForEach(player =>
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
    // banaced mode <---
    public void BalancedMode()
    {
        (int T, int CT, int SPEC, bool IsBotExists, int? botTeam, List<CCSPlayerController> realPlayers) = GetPlayersCount(Utilities.GetPlayers());

        if (T > 1 && CT == 0)
            ChangePlayerTeamSide(realPlayers, CsTeam.CounterTerrorist);
        if (CT > 1 && T == 0)
            ChangePlayerTeamSide(realPlayers, CsTeam.Terrorist);

        if (IsBotExists)
        {
            if ((T + CT) % 2 == 0)
                SendConsoleCommand(BOT_KICK);
            else if (T + CT >= Config.PlayersCountForKickBots)
                SendConsoleCommand(BOT_KICK);
            else if (T + CT == 0 && SPEC >= 0)
                SendConsoleCommand(BOT_KICK);
        }
        else
        {
            if ((T + CT) % 2 != 0)
                SendConsoleCommand(T > CT ? BOT_ADD_CT : BOT_ADD_T);
        }
    }
    // fill mode <---
    public void FillMode()
    {
        (int T, int CT, int SPEC, bool IsBotExists, int? botTeam, List<CCSPlayerController> realPlayers) = GetPlayersCount(Utilities.GetPlayers());
        (string kickbotT, string kickbotCT) = KickOneBot(Utilities.GetPlayers());

        SendConsoleCommand(BOT_MODE_FILL);
        SendConsoleCommand($"bot_quota {Config.BotCount}");
        SendConsoleCommand(BOT_JOIN_AFTER_PLAYER);
        SendConsoleCommand(BOT_ADD);

        RegisterEventHandler<EventSwitchTeam>((@event, info) =>
        {
            SendConsoleCommand(T > CT ? kickbotT : kickbotCT);

            return HookResult.Continue;
        });

        RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
        {
            SendConsoleCommand(T > CT ? BOT_ADD_CT : BOT_ADD_T);

            return HookResult.Continue;
        });

        if (T + CT >= Config.PlayersCountForKickBots)
            SendConsoleCommand(BOT_KICK);


    }
    // match mode <---
    public void MatchMode()
    {
        (int T, int CT, int SPEC, bool IsBotExists, int? botTeam, List<CCSPlayerController> realPlayers) = GetPlayersCount(Utilities.GetPlayers());
        (string kickbotT, string kickbotCT) = KickOneBot(Utilities.GetPlayers());

        SendConsoleCommand(BOT_MODE_MATCH);
        SendConsoleCommand($"bot_quota {Config.BotCount}");
        SendConsoleCommand(BOT_JOIN_AFTER_PLAYER);

        RegisterEventHandler<EventSwitchTeam>((@event, info) =>
        {
            SendConsoleCommand(T > CT ? kickbotT : kickbotCT);

            return HookResult.Continue;
        });

        RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
        {
            SendConsoleCommand(T > CT ? BOT_ADD_CT : BOT_ADD_T);

            return HookResult.Continue;
        });

        if (T + CT >= Config.PlayersCountForKickBots)
            SendConsoleCommand(BOT_KICK);


    }
    //
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


    //[ConsoleCommand("css_btk_endround")]
    //public void OnRoundEndCommand(CCSPlayerController? controller, CommandInfo command)
    //{
    //    if (controller == null) return;
    //    if (AdminManager.PlayerHasPermissions(controller, Config.AdminPermissinFlags))
    //    {
    //        SendConsoleCommand("sv_cheats true");
    //        SendConsoleCommand("endround");
    //        SendConsoleCommand("sv_cheats false");
    //        Server.PrintToChatAll($" {ChatColors.Gold}{controller.PlayerName} {ChatColors.Silver}End this round now!");
    //    }
    //    else
    //        controller.PrintToChat($" {ChatColors.Red}You are not Admin!!!");
    //}


    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {


        return HookResult.Continue;
    }


    [GameEventHandler]
    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        

        return HookResult.Continue;
    }


    [GameEventHandler]
    public HookResult OnPlayerChangeTeam(EventSwitchTeam @event, GameEventInfo info)
    {
        (int T, int CT, int SPEC, bool IsBotExists, int? botTeam, List<CCSPlayerController> realPlayers) = GetPlayersCount(Utilities.GetPlayers());

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
            Utilities.GetPlayers().Find(player => player.IsValid && player.IsBot && !player.IsHLTV).ChangeTeam(CsTeam.CounterTerrorist);
        else if (IsBotExists && CT > T && botTeam == 3)
            Utilities.GetPlayers().Find(player => player.IsValid && player.IsBot && !player.IsHLTV).ChangeTeam(CsTeam.Terrorist);
        return HookResult.Continue;

    }
}