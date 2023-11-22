using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Botiki;
public class BotikiConfig : BasePluginConfig
{
    [JsonPropertyName("admin_ID64")] public UInt64 admin_ID64 { get; set; } = 76561199414091271; // need List !!!
    [JsonPropertyName("add_bot_Mode")] public string add_bot_Mode { get; set; } = "off";
    [JsonPropertyName("bot_Count")] public int bot_Count { get; set; } = 1;
    [JsonPropertyName("bot_HP")] public int bot_HP { get; set; } = 100;
    [JsonPropertyName("playerCount_botKick")] public int playerCount_botKick { get; set; } = 10;
}


[MinimumApiVersion(65)]
public class Botiki : BasePlugin, IPluginConfig<BotikiConfig>
{
    public override string ModuleName => "|Botiki|";

    public override string ModuleVersion => "|v1.0.0|";

    public override string ModuleAuthor => "|jackson tougher|";
    public BotikiConfig Config { get; set; }

    private UInt64 _admin_ID64;                             //
    private string? _add_bot_Mode;                         //
    private int _bot_Count;                               //      <-- variables
    private int _bot_HP;                                 //
    private int _playerCount_botKick;                   //
    public void OnConfigParsed(BotikiConfig config)
    {
        Config = config;
        _admin_ID64 = config.admin_ID64;
        _add_bot_Mode = config.add_bot_Mode;
        _bot_Count = config.bot_Count;
        _bot_HP = config.bot_HP;
        _playerCount_botKick = config.playerCount_botKick;
    }
    public override void Load(bool hotReload)
    {
        Console.WriteLine($"Plugin: {ModuleName} ver:{ModuleVersion} by {ModuleAuthor} has been loaded =)");
    }

    public const string BOT_ADD_CT = "bot_add_ct";              //
    public const string BOT_ADD_T = "bot_add_t";               //      <--   const 
    public const string BOT_KICK = "bot_kick";                //
    public const int MIN_BOT_HP = 1;                         //
    public const int STANDART_BOT_HP = 100;                 //
    public const int MAX_BOT_HP = 9999999;                 //

    public void ChangePlayerTeamSide(List<CCSPlayerController> realPlayers, CsTeam teamName)
    {
        int teamToChange = teamName == CsTeam.Terrorist ? 3 : 2;
        realPlayers.Find(player => player.TeamNum == teamToChange)?.ChangeTeam(teamName);
    }

    public void AddBotsByPlayersCount(int T, int CT)
    {
        if (T + CT == 1)
        {
            Server.ExecuteCommand("bot_quota_mode match");
            Server.ExecuteCommand("bot_join_after_player true");
            Server.ExecuteCommand(T == 1 ? BOT_ADD_CT : BOT_ADD_T);
        }
        else if (T + CT == 3)
        {
            Server.ExecuteCommand("bot_quota_mode fill");
            Server.ExecuteCommand("bot_join_after_player true");
            Server.ExecuteCommand(T > CT ? BOT_ADD_CT : BOT_ADD_T);
        }
    }

    public void KickBotsByPlayersCount(int T, int CT)
    {
        if (T + CT == 2 || T + CT > 3)
            Server.ExecuteCommand(BOT_KICK);
    }
    public void Checker(List<CCSPlayerController> players)
    {
        bool isBotExists = players.Exists(player => player.IsBot);
        List<CCSPlayerController> realPlayers = players.FindAll(player => !player.IsBot);

        int CT = 0;
        int T = 0;

        realPlayers.ForEach(player =>
        {
            if (player.TeamNum == 2)
                T++;
            else if (player.TeamNum == 3)
                CT++;
        });

        if (isBotExists)
            KickBotsByPlayersCount(T, CT);
        else
            AddBotsByPlayersCount(T, CT);

        if (T > 1 && CT == 0)
            ChangePlayerTeamSide(realPlayers, CsTeam.Terrorist);
        if (CT > 1 && T == 0)
            ChangePlayerTeamSide(realPlayers, CsTeam.Terrorist);
    }

    [ConsoleCommand("css_setbothp")]
    public void OnCommandSetBotHp(CCSPlayerController? controller, CommandInfo command, BotikiConfig config)
    {
        if (controller == null) return;
        if (controller.SteamID == _admin_ID64) // only jackson tougher. need List!!!
        {
            if (Regex.IsMatch(command.GetArg(1), @"^\d+$"))
            {
                config.bot_HP = int.Parse(command.GetArg(1));
                _bot_HP = config.bot_HP;
                controller.PrintToChat($" {ChatColors.Red}[Setbothp]{ChatColors.Olive}config reload... {ChatColors.Green}OK!");
                controller.PrintToChat($"New Bot HP: {ChatColors.Green}{_bot_HP}");
            }
            else
            {
                config.bot_HP = STANDART_BOT_HP;
                _bot_HP = config.bot_HP;
                controller.PrintToChat($" {ChatColors.Red}Incorrect value! Please input correct number");
            }
        }
        else
            controller.PrintToChat($" {ChatColors.Red}You are not Admin!!!");
    }
    public void SetBotHp(List<CCSPlayerController> playersList, int _bot_HP)
    {
        playersList.ForEach(player =>
        {
            if (player.IsBot)
            {
                if (_bot_HP >= MIN_BOT_HP && _bot_HP <= MAX_BOT_HP)
                    player.Pawn.Value.Health = _bot_HP;
                else if (_bot_HP < MIN_BOT_HP || _bot_HP > MAX_BOT_HP)
                    player.Pawn.Value.Health = STANDART_BOT_HP;
            }
        });
    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        Checker(Utilities.GetPlayers());
        SetBotHp(Utilities.GetPlayers(), _bot_HP);
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        Checker(Utilities.GetPlayers());

        return HookResult.Continue;
    }
}