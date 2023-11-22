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
    [JsonPropertyName("admin_ID64")] public ulong admin_ID64 { get; set; } = 76561199414091271; // need List !!!
   // [JsonPropertyName("add_bot_Mode")] public string add_bot_Mode { get; set; } = "off";
   // [JsonPropertyName("bot_Count")] public int bot_Count { get; set; } = 1;
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
    public void OnConfigParsed(BotikiConfig config)
    {
        Config = config;
        //_admin_ID64 = config.admin_ID64;
        //_add_bot_Mode = config.add_bot_Mode;
        //_bot_Count = config.bot_Count;
        //_bot_HP = config.bot_HP;
        //_playerCount_botKick = config.playerCount_botKick;
    }
    public override void Load(bool hotReload)
    {
        Console.WriteLine($"Plugin: {ModuleName} ver:{ModuleVersion} by {ModuleAuthor} has been loaded =)");
        SendConsoleCommand(BOT_KICK);
        SendConsoleCommand(BOT_QUOTA_1);
        SendConsoleCommand(BOT_QUOTA_MODE_F);
    }

    public const string BOT_QUOTA_MODE_F = "bot_quota_mode fill";           //
    public const string BOT_QUOTA_1 = "bot_quota 1";                       //
    public const string BOT_ADD_CT = "bot_add_ct";                        //
    public const string BOT_ADD_T = "bot_add_t";                         //      <--   const 
    public const string BOT_KICK = "bot_kick";                          //
    public const int MIN_BOT_HP = 1;                                   //
    public const int STANDART_BOT_HP = 100;                           //
    public const int MAX_BOT_HP = 9999999;                           //

    public void SendConsoleCommand(string msg)
    {
        Server.ExecuteCommand(msg);
    }
    public void ChangePlayerTeamSide(List<CCSPlayerController> realPlayers, CsTeam teamName)
    {
        int teamToChange = teamName == CsTeam.Terrorist ? 3 : 2;
        realPlayers.Find(player => player.TeamNum == teamToChange)?.ChangeTeam(teamName);
    }

    public void AddBotsByPlayersCount(int T, int CT)
    {
        if ((T + CT) % 2 != 0)
            SendConsoleCommand(T > CT ? BOT_ADD_CT : BOT_ADD_T);
    }

    public void KickBotsByPlayersCount(int T, int CT, int SPEC, bool IsBotExist)
    {
        if ( (T + CT) % 2 == 0 )
            SendConsoleCommand(BOT_KICK);
        else if ( T + CT >= Config.playerCount_botKick )
            SendConsoleCommand(BOT_KICK);
        else if ( (T + CT) % 2 == 0 && IsBotExist )
            SendConsoleCommand(BOT_KICK);
        else if ( T + CT == 0 && SPEC >= 0 )
            SendConsoleCommand(BOT_KICK);
        else if ( T - CT >= 2 || CT - T >= 2 && IsBotExist )
            SendConsoleCommand(BOT_KICK);
        else if ( T - CT >= 2 || CT - T >= 2 && !IsBotExist )
            SendConsoleCommand(BOT_KICK);
    }
    public void Checker(List<CCSPlayerController> players)
    {
        bool isBotExists = players.Exists(player => player.IsBot);
        List<CCSPlayerController> realPlayers = players.FindAll(player => !player.IsBot);

        int CT = 0;
        int T = 0;
        int SPEC = 0;

        realPlayers.ForEach(player =>
        {
            if (player.TeamNum == 1)
                SPEC++;
            else if (player.TeamNum == 2)
                T++;
            else if (player.TeamNum == 3)
                CT++;
        });

        if (isBotExists)
            KickBotsByPlayersCount(T, CT, SPEC, isBotExists);
        else
            AddBotsByPlayersCount(T, CT);

        if (T > 1 && CT == 0)
            ChangePlayerTeamSide(realPlayers, CsTeam.Terrorist);
        if (CT > 1 && T == 0)
            ChangePlayerTeamSide(realPlayers, CsTeam.Terrorist);

        //if (((T == 0 && CT == 1) || (CT == 1 && T == 0)) && player.ChangeTeam)
        //    SendConsoleCommand("endround");
    }

    [ConsoleCommand("css_setbothp")]
    public void OnCommandSetBotHp(CCSPlayerController? controller, CommandInfo command)  
    {
        if (controller == null) return;
        if (controller.SteamID == Config.admin_ID64) // only jackson tougher. need List!!!
        {
            if (Regex.IsMatch(command.GetArg(1), @"^\d+$"))
            {
                if (int.Parse(command.GetArg(1)) == 0)
                    controller.PrintToChat($" {ChatColors.Red}Bot HP can`t be zero!");
                else
                {
                    Config.bot_HP = int.Parse(command.GetArg(1));
                    controller.PrintToChat($" {ChatColors.Red}[Setbothp]{ChatColors.Olive}config reload... {ChatColors.Green}OK!");
                    controller.PrintToChat($"New Bot HP: {ChatColors.Green}{Config.bot_HP}");
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
    public void SetBotHp(List<CCSPlayerController> playersList)
    {
        playersList.ForEach(player =>
        {
            if (player.IsBot)
            {
                if (Config.bot_HP >= MIN_BOT_HP && Config.bot_HP <= MAX_BOT_HP)
                    player.Pawn.Value.Health = Config.bot_HP;
                else if (Config.bot_HP < MIN_BOT_HP || Config.bot_HP > MAX_BOT_HP)
                    player.Pawn.Value.Health = STANDART_BOT_HP;
            }
        });
    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        Checker(Utilities.GetPlayers());
        SetBotHp(Utilities.GetPlayers());
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        Checker(Utilities.GetPlayers());

        return HookResult.Continue;
    }
}