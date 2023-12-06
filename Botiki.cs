using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Botiki;
public class Config
{
    public string? AdminFlag { get; set; }  // "@css/botiki"
    public int PluginMode { get; set; }     // 1 = fill; 2 = balanced;
    public int BotHealth { get; set; }      // 100
    public int BotCount { get; set; }       // 10
    public int PlayersCountForKickBots { get; set; }    // 10
    public int DebugMode { get; set; }  // 0 = off; 1 = console; 2 = gamechat; 3 = console/chat
}


[MinimumApiVersion(65)]
public class Botiki : BasePlugin
{
    public override string ModuleName => "Botiki";
    public override string ModuleVersion => "v1.7.0";
    public override string ModuleAuthor => "jackson tougher, VoCs";
    public Config config = new Config();
    public override async void Load(bool hotReload)
    {
        var configPath = Path.Join(ModuleDirectory, "Config.json");
        if (!File.Exists(configPath))
            CreateConfig();
        else
            LoadConfig();

        await Task.Delay(5000);
        StartupParams();

    }
        
    public const string BOT_ADD_CT = "bot_add_ct";
    public const string BOT_ADD_T = "bot_add_t";
    public const string BOT_KICK = "bot_kick";
    public const int MIN_BOT_HP = 1;
    public const int STANDART_BOT_HP = 100;
    public const int MAX_BOT_HP = 999999;
    public bool IsNeedKick = true;

    public void Debug(string logs)
    {
        if (config.DebugMode == 0) return;
        if (config.DebugMode == 1)
        {
            //log to console
            Console.WriteLine("BotikiDebug: " + logs);
        }
        if (config.DebugMode == 2)
        {
            Server.PrintToChatAll($" {ChatColors.LightRed}BotikiDebug: {ChatColors.LightPurple}{logs}");
        }
        if (config.DebugMode == 3)
        {
            Console.WriteLine("BotikiDebug: " + logs);
            Server.PrintToChatAll($" {ChatColors.LightRed}BotikiDebug: {ChatColors.LightPurple}{logs}");
        }

    }
    public void StartupParams()
    {
        if (config == null)
        {
            Console.WriteLine("No config file!!! Applied defaul settings:");
            try
            {
                var bot_qouta_mode = ConVar.Find("bot_quota_mode");
                var bot_qouta = ConVar.Find("bot_quota");
                bot_qouta_mode!.GetPrimitiveValue<string>() = "fill";
                bot_qouta!.GetPrimitiveValue<string>() = "10";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Catch by seted Cvars(bot_quota, bot_quota_mode)\n{ex}");
            }
        }
        if (config!.PluginMode == 1)
        {
            try
            {
                var bot_qouta_mode = ConVar.Find("bot_quota_mode");
                var bot_qouta = ConVar.Find("bot_quota");
                bot_qouta_mode!.GetPrimitiveValue<string>() = "fill";
                bot_qouta!.GetPrimitiveValue<string>() = config.BotCount.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Catch(1) by seted Cvars(bot_quota, bot_quota_mode)\n{ex}");
            }
        }
        if ( config!.PluginMode == 2)
        {
            try
            {
                var bot_qouta_mode = ConVar.Find("bot_quota_mode");
                var bot_qouta = ConVar.Find("bot_quota");
                bot_qouta_mode!.GetPrimitiveValue<string>() = "normal";
                bot_qouta!.GetPrimitiveValue<string>() = "0";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Catch(2) by seted Cvars(bot_quota, bot_quota_mode)\n{ex}");
            }
        }
    }
    public void SendConsoleCommand(string msg)
    {
        Server.ExecuteCommand(msg);
    }
    public void ChangePlayerTeamSide(List<CCSPlayerController> realPlayers, CsTeam teamName)
    {
        int teamToChange = teamName == CsTeam.Terrorist ? 3 : 2;
        realPlayers.Find(player => player.TeamNum == teamToChange)?.SwitchTeam(teamName);
    }

    public void CreateConfig()
    {
        var configPath = Path.Join(ModuleDirectory, "Config.json");
        if (!File.Exists(configPath))
        {
            config.AdminFlag = "@css/botiki";
            config.PluginMode = 1;
            config.BotCount = 10;
            config.BotHealth = 100;
            config.PlayersCountForKickBots = 10;
            config.DebugMode = 0;
            File.WriteAllText(configPath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    public void LoadConfig()
    {
        var configPath = Path.Join(ModuleDirectory, "Config.json");
        config = JsonSerializer.Deserialize<Config>(File.ReadAllText(configPath))!;
    }
    public void AddBotsByPlayersCount(int T, int CT)
    {
        if ((T + CT) % 2 != 0)
        {
            SendConsoleCommand(BOT_KICK);
            SendConsoleCommand(T > CT ? BOT_ADD_CT : BOT_ADD_T);
        }
    }

    public void KickBotsByPlayersCount(int T, int CT, int SPEC)
    {
        if ((T + CT) % 2 == 0)
            SendConsoleCommand(BOT_KICK);
        else if (T + CT >= config.PlayersCountForKickBots)
            SendConsoleCommand(BOT_KICK);
        else if (T + CT == 0 && SPEC >= 0)
            SendConsoleCommand(BOT_KICK);
        else if (T + CT == 0)
            SendConsoleCommand(BOT_KICK);
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
    public void Checker(List<CCSPlayerController> players)
    {
        (int T, int CT, int SPEC, bool IsBotExists, int? botTeam, List<CCSPlayerController> realPlayers) = GetPlayersCount(players);

        if (T > 1 && CT == 0)
            ChangePlayerTeamSide(realPlayers, CsTeam.CounterTerrorist);
        if (CT > 1 && T == 0)
            ChangePlayerTeamSide(realPlayers, CsTeam.Terrorist);

        if (IsBotExists)
            KickBotsByPlayersCount(T, CT, SPEC);
        else
            AddBotsByPlayersCount(T, CT);
    }

    [ConsoleCommand("css_setbothp")]
    public void OnCommandSetBotHp(CCSPlayerController? controller, CommandInfo command)
    {
        if (controller == null) return;
        if (AdminManager.PlayerHasPermissions(controller, config.AdminFlag!))
        {
            if (Regex.IsMatch(command.GetArg(1), @"^\d+$"))
            {
                if (int.Parse(command.GetArg(1)) == 0)
                {
                    controller.PrintToChat($" {ChatColors.Red}[ {ChatColors.Purple}Botiki {ChatColors.Red}] {ChatColors.Red}Bot HP can`t be zero!");
                }
                else
                {
                    config.BotHealth = int.Parse(command.GetArg(1));
                    controller.PrintToChat($" {ChatColors.Red}[ {ChatColors.Purple}Botiki {ChatColors.Red}] {ChatColors.Olive}config reload... {ChatColors.Green}OK!");
                    controller.PrintToChat($" {ChatColors.Red}[ {ChatColors.Purple}Botiki {ChatColors.Red}] {ChatColors.Default}New Bot HP: {ChatColors.Green}{config.BotHealth}");
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
    [ConsoleCommand("css_botiki_kick")]
    public void OnCommandBotikiKick(CCSPlayerController? controller, CommandInfo command)
    {
        if (controller == null) return;
        if (AdminManager.PlayerHasPermissions(controller, config.AdminFlag!)) 
        {
            SendConsoleCommand(BOT_KICK);
            controller.PrintToChat($" {ChatColors.Red}[ {ChatColors.Purple}Botiki {ChatColors.Red}] {ChatColors.Olive}Bot`s was kicked... {ChatColors.Green}OK!");
        }
        else
            controller.PrintToChat($" {ChatColors.Red}You are not Admin!!!");
    }

    [ConsoleCommand("css_botiki_reload")]
    public void OnBotikiConfigReload(CCSPlayerController? controller, CommandInfo command)
    {
        if (controller == null) return;
        if (AdminManager.PlayerHasPermissions(controller, config.AdminFlag!))
        {
            LoadConfig();
            controller.PrintToChat($" {ChatColors.Red}[ {ChatColors.Purple}Botiki {ChatColors.Red}] {ChatColors.Olive}...configuration was reloaded. {ChatColors.Green}OK!");
        }
        else
            controller.PrintToChat($" {ChatColors.Red}You are not Admin!!!");
    }
    public void SetBotHp(List<CCSPlayerController> playersList)
    {
        playersList.ForEach(player =>
        {
            if (player.IsValid && player.IsBot && !player.IsHLTV)
            {
                if (config.BotHealth >= MIN_BOT_HP && config.BotHealth <= MAX_BOT_HP)
                    player.Pawn.Value!.Health = config.BotHealth;
                else if (config.BotHealth < MIN_BOT_HP || config.BotHealth > MAX_BOT_HP)
                    player.Pawn.Value!.Health = STANDART_BOT_HP;
            }
        });
    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        //Checker(Utilities.GetPlayers());
        SetBotHp(Utilities.GetPlayers());
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        (int T, int CT, int SPEC, bool IsBotExists, int? botTeam, List<CCSPlayerController> realPlayers) = GetPlayersCount(Utilities.GetPlayers());
        Checker(Utilities.GetPlayers());

        return HookResult.Continue;
    }
    [GameEventHandler]
    public HookResult OnPlayerChangeTeam(EventSwitchTeam @event, GameEventInfo info)
    {
        if (IsNeedKick)
        {
            SendConsoleCommand(BOT_KICK);
            IsNeedKick = false;
        }
        (int T, int CT, int SPEC, bool IsBotExists, int? botTeam, List<CCSPlayerController> realPlayers) = GetPlayersCount(Utilities.GetPlayers());
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
        {
            Utilities.GetPlayers().Find(player => player.IsValid && player.IsBot && !player.IsHLTV)!.ChangeTeam(CsTeam.CounterTerrorist);
        }
        else if (IsBotExists && CT > T && botTeam == 3)
        {
            Utilities.GetPlayers().Find(player => player.IsValid && player.IsBot && !player.IsHLTV)!.ChangeTeam(CsTeam.Terrorist);
        }
        return HookResult.Continue;
        
    }
}
