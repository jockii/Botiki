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
    public bool LastPlayerDisconnectKickBots { get; set; }  // true
    public int DebugMode { get; set; }  // 0 = off; 1 = console; 2 = gamechat; 3 = console/chat
}


[MinimumApiVersion(65)]
public class Botiki : BasePlugin
{
    public override string ModuleName => "Botiki";
    public override string ModuleVersion => "v1.7.0";
    public override string ModuleAuthor => "jackson tougher, VoCs";
    public Config config = new Config();
    public override void Load(bool hotReload)
    {
        var configPath = Path.Join(ModuleDirectory, "Config.json");
        if (!File.Exists(configPath))
            CreateConfig();
        else
            LoadConfig();

        StartupParams();

    }

    public const string BOT_ADD_CT = "bot_add_ct";
    public const string BOT_ADD_T = "bot_add_t";
    public const string BOT_KICK = "bot_kick";
    public const int MIN_BOT_HP = 1;
    public const int STANDART_BOT_HP = 100;
    public const int MAX_BOT_HP = 999999;
    public bool IsNeedKick = true;

    (int T, int Th, int Tb, int CT, int CTh, int CTb, int SPEC, bool isBotExists, int? botTeam) GetPlayersCount(List<CCSPlayerController> players)
    {
        List<CCSPlayerController> realPlayers = players.FindAll(player => player.IsValid && !player.IsBot);
        bool isBotExists = players.Exists(player => player.IsValid && player.IsBot && !player.IsHLTV);
        int CT = 0;
        int CTh = 0;
        int CTb = 0;
        int T = 0;
        int Th = 0;
        int Tb = 0;
        int SPEC = 0;
        int? botTeam = players.Find(player => player.IsValid && player.IsBot && !player.IsHLTV)?.TeamNum;

        players.ForEach(player =>
        {
            if (player == null ||  !player.IsValid) return;

            if (player.IsValid && !player.IsHLTV)
            {
                if (player.TeamNum == 1)
                {
                    SPEC++;
                }
                if (player.TeamNum == 2)
                {
                    T++;
                }
                if (player.TeamNum == 3)
                {
                    CT++;
                }
            }

            if (player.IsValid && !player.IsBot && !player.IsHLTV)
            {
                if (player.TeamNum == 2)
                {
                    Th++;
                }
                if (player.TeamNum == 3)
                {
                    CTh++;
                }
            }

            if (player.IsValid && player.IsBot && !player.IsHLTV)
            {
                if (player.TeamNum == 2)
                {
                    Tb++;
                }
                if (player.TeamNum == 3)
                {
                    CTb++;
                }
            }
        });

        return (T, Th, Tb, CT, CTh, CTb, SPEC, isBotExists, botTeam);
    }
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
    public async void StartupParams()
    {
        await Task.Delay(5000);
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
        if (config!.PluginMode == 2)
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
    public void CreateConfig()
    {
        var configPath = Path.Join(ModuleDirectory, "Config.json");
        if (!File.Exists(configPath))
        {
            config.AdminFlag = "@css/kick";
            config.PluginMode = 2;
            config.BotCount = 4;
            config.BotHealth = 100;
            config.PlayersCountForKickBots = 10;
            config.LastPlayerDisconnectKickBots = true;
            config.DebugMode = 0;
            File.WriteAllText(configPath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
    public void LoadConfig()
    {
        var configPath = Path.Join(ModuleDirectory, "Config.json");
        config = JsonSerializer.Deserialize<Config>(File.ReadAllText(configPath))!;

        string bc = $"bot_quota {config.BotCount}";
        if (config.PluginMode == 1)
        {
            SendConsoleCommand("bot_quota_mode fill");
            SendConsoleCommand(bc);
        }
        if (config.PluginMode == 2)
        {
            SendConsoleCommand("bot_quota_mode normal");
            SendConsoleCommand("bot_quota 1");
        }
    }
    public void ChangePlayerTeamSide(CsTeam teamName)
    {
        int teamToChange = teamName == CsTeam.Terrorist ? 3 : 2;
        Utilities.GetPlayers().Find(player => player.IsValid && !player.IsBot && !player.IsHLTV && player.TeamNum == teamToChange)?.SwitchTeam(teamName);
    }
    public void FillAdder(int Tb, int CTb)
    {
        //      need add bot
        if (Tb + CTb < config.BotCount)
        {
            if (Tb > CTb)
            {
                for (int i = Tb - CTb; i == 0; i--)
                {
                    SendConsoleCommand(BOT_ADD_CT);
                }
            }
            if (CTb > Tb)
            {
                for (int i = CTb - Tb; i == 0; i--)
                {
                    SendConsoleCommand(BOT_ADD_T);
                }
            }
        }
    }
    public void FillKicker(int T, int CT, int Tb, int Th, int CTb, int CTh, int? botTeam)
    {
        //      need kick bot
        if (Th + CTh >= config.PlayersCountForKickBots)
        {
            SendConsoleCommand(BOT_KICK);
        }

        if (T + CT > config.BotCount)
        {
            if (Tb > CTb)
            {
                // try $"bot_kick {.PlayerName}" 
                for (int i = Tb - CTb; i == 0; i--)
                {
                    SendConsoleCommand($"bot_kick {Utilities.GetPlayers().Find(pl => pl.IsValid && pl.IsBot && !pl.IsHLTV && pl.TeamNum == 2)!.PlayerName}");
                }
            }
            if (CTb > Tb)
            {
                for (int i = CTb - Tb; i == 0; i--)
                {
                    SendConsoleCommand($"bot_kick {Utilities.GetPlayers().Find(pl => pl.IsValid && pl.IsBot && !pl.IsHLTV && pl.TeamNum == 3)!.PlayerName}");
                }
            }
        }
    }
    public void FillMode(List<CCSPlayerController> players)
    {
        (int T, int Th, int Tb, int CT, int CTh, int CTb, int SPEC, bool isBotExists, int? botTeam) = GetPlayersCount(Utilities.GetPlayers());

        if (Th + CTh == 1 && Tb + CTb == config.BotCount)
        {
            if (botTeam == null) return;

            if (T > CT && botTeam == 2)
            {
                // try $"bot_kick {.PlayerName}" 
                string botName = Utilities.GetPlayers().First(pl => pl.IsValid && pl.IsBot && !pl.IsHLTV && pl.TeamNum == 2).PlayerName;
                SendConsoleCommand($"bot_kick {botName}");
                SendConsoleCommand(BOT_ADD_CT);
            }
            if (CT > T && botTeam == 3)
            {
                string botName = Utilities.GetPlayers().First(pl => pl.IsValid && pl.IsBot && !pl.IsHLTV && pl.TeamNum == 3).PlayerName;
                SendConsoleCommand($"bot_kick {botName}");
                SendConsoleCommand(BOT_ADD_T);
            }
        }
        else
        {
            FillKicker(T, CT, Tb, Th, CTb, CTh, botTeam);
            FillAdder(Tb, CTb);
        }
        
    }
    public void BalancerAdder(int T, int CT)
    {
        if ((T + CT) % 2 != 0)
        {
            SendConsoleCommand(BOT_KICK);
            SendConsoleCommand(T > CT ? BOT_ADD_CT : BOT_ADD_T);
        }
    }
    public void BalancedKicker(int T, int CT)
    {
        string kickBotNameT = $"bot_kick {Utilities.GetPlayers().Find(pl => pl.IsValid && pl.IsBot && !pl.IsHLTV && pl.TeamNum == 2)!.PlayerName}";
        string kickBotNameCT = $"bot_kick {Utilities.GetPlayers().Find(pl => pl.IsValid && pl.IsBot && !pl.IsHLTV && pl.TeamNum == 3)!.PlayerName}";

        if ((T + CT) % 2 == 0)
            SendConsoleCommand(BOT_KICK);

        if (T + CT >= config.PlayersCountForKickBots)
            SendConsoleCommand(BOT_KICK);
        
        if (config.LastPlayerDisconnectKickBots)
        {
            if (T + CT == 0)
                SendConsoleCommand(BOT_KICK);
        }

        if (T + CT == 1)
        {
            SendConsoleCommand(T > CT ? kickBotNameT : kickBotNameCT);
            SendConsoleCommand(T > CT ? BOT_ADD_T : BOT_ADD_CT);
        }
    }
    public void BalancedMode(List<CCSPlayerController> players)
    {
        (int T, int Th, int Tb, int CT, int CTh, int CTb, int SPEC, bool isBotExists, int? botTeam) = GetPlayersCount(Utilities.GetPlayers());


        if (T > 1 && CT == 0)
            ChangePlayerTeamSide(CsTeam.CounterTerrorist);
        if (CT > 1 && T == 0)
            ChangePlayerTeamSide(CsTeam.Terrorist);

        if (isBotExists)
            BalancedKicker(T, CT);
        else
            BalancerAdder(T, CT);
    }
    
    [ConsoleCommand("bothp", "Set bot Health")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnCommandBotHp(CCSPlayerController? controller, CommandInfo command)
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
    [ConsoleCommand("botkick", "Kick bots")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnCommandBotKick(CCSPlayerController? controller, CommandInfo command)
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

    [ConsoleCommand("botconfig", "Reload config")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnConfigReload(CCSPlayerController? controller, CommandInfo command)
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
        
    }

    [GameEventHandler(mode: HookMode.Post)]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        switch(config.PluginMode)
        {
            case 1:
                // fill
                FillMode(Utilities.GetPlayers());
                break;
            case 2:
                // balanced
                BalancedMode(Utilities.GetPlayers());
                break;
            default: 
                //  error message about config setting
                break; 
        }

        Utilities.GetPlayers().ForEach(player =>
        {
            if (player.IsValid && player.IsBot && !player.IsHLTV)
            {
                if (config.BotHealth >= MIN_BOT_HP && config.BotHealth <= MAX_BOT_HP)
                    player.Pawn.Value!.Health = config.BotHealth;
                else if (config.BotHealth < MIN_BOT_HP || config.BotHealth > MAX_BOT_HP)
                    player.Pawn.Value!.Health = STANDART_BOT_HP;
            }
        });

        return HookResult.Continue;
    }

    [GameEventHandler(mode: HookMode.Post)]
    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        

        return HookResult.Continue;
    }

    [GameEventHandler(mode: HookMode.Post)]
    public HookResult OnSwitchTeam(EventSwitchTeam @event, GameEventInfo info)
    {
        (int T, int Th, int Tb, int CT, int CTh, int CTb, int SPEC, bool isBotExists, int? botTeam) = GetPlayersCount(Utilities.GetPlayers());

        //if (IsNeedKick)  // working?
        //{
        //    SendConsoleCommand(BOT_KICK);
        //    IsNeedKick = false;
        //}

        switch (config.PluginMode)
        {
            case 1:
                // fill
                break;
            case 2:
                // balanced

                if (Th + CTh == 1 && isBotExists)
                {
                    SendConsoleCommand("sv_cheats true");
                    SendConsoleCommand("endround");
                    SendConsoleCommand("sv_cheats false");
                }

                if (Th + CTh == 1 && botTeam == 2) // isBotExists && T > CT && botTeam == 2
                    Utilities.GetPlayers().Find(player => player.IsValid && player.IsBot && !player.IsHLTV)!.ChangeTeam(CsTeam.CounterTerrorist);

                if (Th + CTh == 1 && botTeam == 3) // isBotExists && T > CT && botTeam == 3
                    Utilities.GetPlayers().Find(player => player.IsValid && player.IsBot && !player.IsHLTV)!.ChangeTeam(CsTeam.Terrorist);
                break;

            default:
                // error console message
                break;
        }

        return HookResult.Continue;
    }
}