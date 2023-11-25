using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Botiki;
public class Config
{
    public List<ulong> admins_ID64 { get; set; } = new List<ulong>();
    public int bot_HP { get; set; }
    public int playerCount_botKick { get; set; }
    public string? add_bot_Mode { get; set; }
    public int bot_Count { get; set; }
}


[MinimumApiVersion(65)]
public class Botiki : BasePlugin
{
    public override string ModuleName => "Botiki";

    public override string ModuleVersion => "v1.6.0";

    public override string ModuleAuthor => "jackson tougher, VoCs007";
    public Config config = new Config();
    public override void Load(bool hotReload)
    {
        var configPath = Path.Join(ModuleDirectory, "Config.json");
        if (!File.Exists(configPath))
        {
            config.admins_ID64.Add(76561199414091272); config.admins_ID64.Add(76561199414091272);
            config.bot_HP = 100;
            config.playerCount_botKick = 10;
            config.add_bot_Mode = "balanced";
            config.bot_Count = 10;
            File.WriteAllText(configPath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
        }
        else config = JsonSerializer.Deserialize<Config>(File.ReadAllText(configPath));

        Console.WriteLine($"Plugin: {ModuleName} ver:{ModuleVersion} by {ModuleAuthor} has been loaded =)");
        SendConsoleCommand(BOT_KICK);
        SendConsoleCommand(BOT_QUOTA_1);
        SendConsoleCommand(BOT_MODE_FILL);
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
    public bool IsNeedKick = true;                                 //
    public bool isNeedMatchMode = true;                           //
    public bool isNeedFillMode = true;                           //
    public bool isNeedBalancedMode = true;                      //
    public bool isNeedAddBot = true;                           //

    public void SendConsoleCommand(string msg)
    {
        Server.ExecuteCommand(msg);
    }
    public void ChangePlayerTeamSide(List<CCSPlayerController> realPlayers, CsTeam teamName)
    {
        int teamToChange = teamName == CsTeam.Terrorist ? 3 : 2;
        realPlayers.Find(player => player.TeamNum == teamToChange)?.SwitchTeam(teamName);
    }

    public void OnConfigReload()
    {
        var configPath = Path.Join(ModuleDirectory, "Config.json");
        config = JsonSerializer.Deserialize<Config>(File.ReadAllText(configPath));
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
        playersList.ForEach(player =>
        {
            if (player.IsValid && player.IsBot && !player.IsHLTV)
            {
                if (config.bot_HP >= MIN_BOT_HP && config.bot_HP <= MAX_BOT_HP)
                    player.Pawn.Value.Health = config.bot_HP;
                else if (config.bot_HP < MIN_BOT_HP || config.bot_HP > MAX_BOT_HP)
                    player.Pawn.Value.Health = STANDART_BOT_HP;
            }
        });
    }
    // banaced mode <---
    public void AddBotsBalancedMode()
    {
        (int T, int CT, int SPEC, bool IsBotExists, int? botTeam, List<CCSPlayerController> realPlayers) = GetPlayersCount(Utilities.GetPlayers());

        if ((T + CT) % 2 != 0)
            SendConsoleCommand(T > CT ? BOT_ADD_CT : BOT_ADD_T);
    }
    public void KickBotsBalancedMode()
    {
        (int T, int CT, int SPEC, bool IsBotExists, int? botTeam, List<CCSPlayerController> realPlayers) = GetPlayersCount(Utilities.GetPlayers());

        if ((T + CT) % 2 == 0)
            SendConsoleCommand(BOT_KICK);
        else if (T + CT >= config.playerCount_botKick)
            SendConsoleCommand(BOT_KICK);
        else if (T + CT == 0 && SPEC >= 0)
            SendConsoleCommand(BOT_KICK);
    }
    public void BalancedMode()
    {
        (int T, int CT, int SPEC, bool IsBotExists, int? botTeam, List<CCSPlayerController> realPlayers) = GetPlayersCount(Utilities.GetPlayers());

        if (isNeedBalancedMode)
        {
            SendConsoleCommand(BOT_MODE_FILL);

            isNeedBalancedMode = false;
            isNeedFillMode = true;
            isNeedMatchMode = true;
        }

        if (T > 1 && CT == 0)
            ChangePlayerTeamSide(realPlayers, CsTeam.CounterTerrorist);
        if (CT > 1 && T == 0)
            ChangePlayerTeamSide(realPlayers, CsTeam.Terrorist);

        if (IsBotExists)
            KickBotsBalancedMode();
        else
            AddBotsBalancedMode();
    }
    // fill mode <---
    public void FillMode()
    {
        (int T, int CT, int SPEC, bool IsBotExists, int? botTeam, List<CCSPlayerController> realPlayers) = GetPlayersCount(Utilities.GetPlayers());
        (string kickbotT, string kickbotCT) = KickOneBot(Utilities.GetPlayers());

        if (isNeedFillMode)
        {
            SendConsoleCommand(BOT_MODE_FILL);
            SendConsoleCommand($"bot_quota {config.bot_Count}");
            SendConsoleCommand(BOT_JOIN_AFTER_PLAYER);
            SendConsoleCommand(BOT_ADD);

            isNeedFillMode = false;
            isNeedBalancedMode = true;
            isNeedMatchMode = true;
        }

        RegisterEventHandler<EventSwitchTeam>((@event, info) =>
        {
            SendConsoleCommand(T > CT ? kickbotT : kickbotCT );

            return HookResult.Continue;
        });

        RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
        {
            SendConsoleCommand(T > CT ? BOT_ADD_CT : BOT_ADD_T);

            return HookResult.Continue;
        });

        
    }
    // match mode <---
    public void MatchMode()
    {
        (int T, int CT, int SPEC, bool IsBotExists, int? botTeam, List<CCSPlayerController> realPlayers) = GetPlayersCount(Utilities.GetPlayers());

        if (isNeedMatchMode)
        {
            SendConsoleCommand(BOT_MODE_FILL);
            SendConsoleCommand($"bot_quota {config.bot_Count}");
            SendConsoleCommand(BOT_JOIN_AFTER_PLAYER);

            isNeedMatchMode = false;
            isNeedBalancedMode = true;
            isNeedFillMode = true;
        }

        
    }
    //
    [ConsoleCommand("css_btk_hp")]
    public void OnCommandSetBotHp(CCSPlayerController? controller, CommandInfo command)
    {
        if (controller == null) return;
        if (config.admins_ID64.Exists(adminID => adminID == controller.SteamID))
        {
            if (Regex.IsMatch(command.GetArg(1), @"^\d+$"))
            {
                if (int.Parse(command.GetArg(1)) == 0)
                {
                    controller.PrintToChat($" {ChatColors.Red}[ {ChatColors.Purple}Botiki {ChatColors.Red}] {ChatColors.Red}Bot HP can`t be zero!");
                }
                else
                {
                    config.bot_HP = int.Parse(command.GetArg(1));
                    controller.PrintToChat($" {ChatColors.Red}[ {ChatColors.Purple}Botiki {ChatColors.Red}] {ChatColors.Default}New Bot HP: {ChatColors.Green}{config.bot_HP}");
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
    [ConsoleCommand("css_btk_kick")]
    public void OnCommandBotikiKick(CCSPlayerController? controller, CommandInfo command)
    {
        if (controller == null) return;
        if (config.admins_ID64.Exists(adminID => adminID == controller.SteamID))
        {
            SendConsoleCommand(BOT_KICK);
            controller.PrintToChat($" {ChatColors.Red}[ {ChatColors.Purple}Botiki {ChatColors.Red}] {ChatColors.Olive}Bot`s was kicked... {ChatColors.Green}OK!");
        }
        else
            controller.PrintToChat($" {ChatColors.Red}You are not Admin!!!");
    }

    [ConsoleCommand("css_btk_reload")]
    public void OnBotikiConfigReload(CCSPlayerController? controller, CommandInfo command)
    {
        if (controller == null) return;
        if (config.admins_ID64.Exists(adminID => adminID == controller.SteamID))
        {
            OnConfigReload();
            controller.PrintToChat($" {ChatColors.Red}[ {ChatColors.Purple}Botiki {ChatColors.Red}] {ChatColors.Olive}...configuration was reloaded. {ChatColors.Green}OK!");
        }
        else
            controller.PrintToChat($" {ChatColors.Red}You are not Admin!!!");
    }
    [ConsoleCommand("css_btk_endround")]
    public void OnRoundEndCommand(CCSPlayerController? controller, CommandInfo command)
    {
        if (controller == null) return;
        if (config.admins_ID64.Exists(adminID => adminID == controller.SteamID))
        {
            SendConsoleCommand("sv_cheats true");
            SendConsoleCommand("endround");
            SendConsoleCommand("sv_cheats false");
            Server.PrintToChatAll($" {ChatColors.Gold}{controller.PlayerName} {ChatColors.Silver}End this round now!");
        }
        else
            controller.PrintToChat($" {ChatColors.Red}You are not Admin!!!");
    }


    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        SetBotHp(Utilities.GetPlayers());
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        (int T, int CT, int SPEC, bool IsBotExists, int? botTeam, List<CCSPlayerController> realPlayers) = GetPlayersCount(Utilities.GetPlayers());

        if (config.add_bot_Mode == null || config.add_bot_Mode == "" || config.add_bot_Mode != "fill" || config.add_bot_Mode != "match" || config.add_bot_Mode != "balanced" || config.add_bot_Mode != "off")
            Console.WriteLine("Error config setting");
        else
        {
            if (config.add_bot_Mode == "fill")
                FillMode();
            else if (config.add_bot_Mode == "match")
                MatchMode();
            else if (config.add_bot_Mode == "balanced")
                BalancedMode();
        }

        return HookResult.Continue;
    }
    [GameEventHandler]
    public HookResult OnPlayerChangeTeam(EventSwitchTeam @event, GameEventInfo info)
    {
        (int T, int CT, int SPEC, bool IsBotExists, int? botTeam, List<CCSPlayerController> realPlayers) = GetPlayersCount(Utilities.GetPlayers());

        if (IsNeedKick)
        {
            SendConsoleCommand(BOT_KICK);
            IsNeedKick = false;
        }
       
        if (((T == 0 && CT == 1) || (CT == 0 && T == 1)) && IsBotExists)
        {
            SendConsoleCommand(BOT_KICK);
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