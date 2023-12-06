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

    // [JsonPropertyName("add_bot_Mode")] public string add_bot_Mode { get; set; } = "off";
    // [JsonPropertyName("bot_Count")] public int bot_Count { get; set; } = 1;
}


[MinimumApiVersion(65)]
public class Botiki : BasePlugin
{
    public override string ModuleName => "Botiki";

    public override string ModuleVersion => "v1.5.0";

    public override string ModuleAuthor => "jackson tougher, VoCs";
    public Config config = new Config();
    public override void Load(bool hotReload)
    {
        var configPath = Path.Join(ModuleDirectory, "Config.json");
        if (!File.Exists(configPath))
        {
            config.admins_ID64.Add(76561199414091272); config.admins_ID64.Add(76561199414091272);
            config.bot_HP = 100;
            config.playerCount_botKick = 10;
            File.WriteAllText(configPath, JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
        }
        else config = JsonSerializer.Deserialize<Config>(File.ReadAllText(configPath));

        Console.WriteLine($"Plugin: {ModuleName} ver:{ModuleVersion} by {ModuleAuthor} has been loaded =)");
        SendConsoleCommand(BOT_KICK);
        SendConsoleCommand(BOT_QUOTA_1);
        SendConsoleCommand(BOT_QUOTA_MODE_F);
    }

    public const string BOT_QUOTA_MODE_F = "bot_quota_mode fill";            //
    public const string BOT_QUOTA_MODE_M = "bot_quota_mode match";          //
    public const string BOT_QUOTA_1 = "bot_quota 1";                       //
    public const string BOT_ADD_CT = "bot_add_ct";                        //
    public const string BOT_ADD_T = "bot_add_t";                         //      <--   const 
    public const string BOT_KICK = "bot_kick";                          //
    public const int MIN_BOT_HP = 1;                                   //
    public const int STANDART_BOT_HP = 100;                           //
    public const int MAX_BOT_HP = 9999999;                           //
    public bool IsNeedKick = true;                                  //

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
        else if (T + CT >= config.playerCount_botKick)
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
                    controller.PrintToChat($" {ChatColors.Red}[ {ChatColors.Purple}Botiki {ChatColors.Red}] {ChatColors.Olive}config reload... {ChatColors.Green}OK!");
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
    [ConsoleCommand("css_botiki_kick")]
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

    [ConsoleCommand("css_botiki_reload")]
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
        // Server.PrintToChatAll("=== OnRoundEnd ===");
        (int T, int CT, int SPEC, bool IsBotExists, int? botTeam, List<CCSPlayerController> realPlayers) = GetPlayersCount(Utilities.GetPlayers());
        //Server.PrintToChatAll($"T = {T}| CT = {CT}| SPEC = {SPEC}| IsBotExists = {IsBotExists}");
        // Server.PrintToChatAll("=== ==== ==== ===");
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
            Utilities.GetPlayers().Find(player => player.IsValid && player.IsBot && !player.IsHLTV).ChangeTeam(CsTeam.CounterTerrorist);
        }
        else if (IsBotExists && CT > T && botTeam == 3)
        {
            Utilities.GetPlayers().Find(player => player.IsValid && player.IsBot && !player.IsHLTV).ChangeTeam(CsTeam.Terrorist);
        }
        return HookResult.Continue;
        
    }
}
