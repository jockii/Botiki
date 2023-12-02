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
    public override string ModuleName => "Botiki Fill Mode";

    public override string ModuleVersion => "v1.0.0";

    public override string ModuleAuthor => "jockii";

    public BotikiConfig Config { get; set; } = new BotikiConfig();

    public void OnConfigParsed(BotikiConfig config)
    {
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        SendConsoleCommand(BOT_KICK);
        SendConsoleCommand(BOT_MODE_FILL);
        SendConsoleCommand($"bot_quota {Config.BotCount}");
        SendConsoleCommand(BOT_JOIN_AFTER_PLAYER);

        Console.WriteLine($"Plugin: {ModuleName} ver:{ModuleVersion} by {ModuleAuthor} has been loaded =)");
    }

    public const string BOT_JOIN_AFTER_PLAYER = "bot_join_after_player 1";  //
    public const string BOT_MODE_FILL = "bot_quota_mode fill";             //
    public const string BOT_ADD_CT = "bot_add_ct";                        //
    public const string BOT_ADD_T = "bot_add_t";                         //        
    public const string BOT_ADD = "bot_add";                            //
    public const string BOT_KICK = "bot_kick";                         //       <-- 
    public const int MIN_BOT_HP = 1;                                  //
    public const int STANDART_BOT_HP = 100;                          //
    public const int MAX_BOT_HP = 9999999;                          //

    public void SendConsoleCommand(string msg)
    {
        Server.ExecuteCommand(msg);
    }

    (int T, int CT) GetPlayersCount(List<CCSPlayerController> players)
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

        return (T, CT);
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

    public void FillMode(CCSPlayerController controller)
    {
        (int T, int CT) = GetPlayersCount(Utilities.GetPlayers());
        (string kickbotT, string kickbotCT) = KickOneBot(Utilities.GetPlayers());

        RegisterEventHandler<EventSwitchTeam>((@event, info) =>
        {
            if (controller.TeamNum == 1)
                SendConsoleCommand(T > CT ? BOT_ADD_CT : BOT_ADD_T);
            else
                SendConsoleCommand(T > CT ? kickbotT : kickbotCT);

            return HookResult.Continue;
        });

        RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
        {
            if (T + CT > Config.PlayersCountForKickBots)
                SendConsoleCommand(T > CT ? BOT_ADD_CT : BOT_ADD_T);

            return HookResult.Continue;
        });

        RegisterEventHandler<EventRoundEnd>((@event, info) =>
        {
            if (T + CT >= Config.PlayersCountForKickBots)
                SendConsoleCommand(BOT_KICK);

            return HookResult.Continue;
        });
    }

    [ConsoleCommand("bothp", "Setup bots Health")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY)]
    public void OnCommandBotHp(CCSPlayerController? controller, CommandInfo command)
    {
        if (controller == null) return;
        if (AdminManager.PlayerHasPermissions(controller, Config.AdminPermissionFlags))
        {
            if (Regex.IsMatch(command.GetArg(1), @"^\d+$"))
            {
                if (int.Parse(command.GetArg(1)) == 0)
                {
                    controller.PrintToChat($" {ChatColors.Red}[ {ChatColors.Purple}Botiki {ChatColors.Red}] {ChatColors.Red}Bot Health can`t be zero!");
                }
                else
                {
                    Config.BotsHealth = int.Parse(command.GetArg(1));
                    controller.PrintToChat($" {ChatColors.Red}[ {ChatColors.Purple}Botiki {ChatColors.Red}] {ChatColors.Default}New Bot Health: {ChatColors.Green}{Config.BotsHealth}");
                }
            }
            else
            {
                controller.PrintToChat($" {ChatColors.Red}Incorrect value! Please input correct number");
            }
        }
        else
            controller.PrintToChat($" {ChatColors.Red}You are not have permission on this command!");
    }

    [ConsoleCommand("botkick", "Just kick bots")]
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
}