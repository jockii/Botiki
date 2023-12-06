using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Botiki;
public class BotikiConfig : BasePluginConfig
{
    [JsonPropertyName("AdminPermissionFlags")]
    public string AdminPermissionFlags { get; set; } = "@css/kick";
    [JsonPropertyName("PluginMode")]
    public int PluginMode { get; set; } = 1;  // 1 = fill; 2 = match; 3 = balanced;
    [JsonPropertyName("BotJoinAfterPlayer")]
    public bool BotJoinAfterPlayer { get; set; } = true;
    [JsonPropertyName("BotsHealth")]
    public int BotsHealth { get; set; } = 100;
    [JsonPropertyName("PlayersCountForKickBots")]
    public int PlayersCountForKickBots { get; set; } = 10;
    [JsonPropertyName("BotCount")]
    public int BotCount { get; set; } = 10;

    [JsonPropertyName("DebugMode")]
    public bool DebugMode { get; set; } = true;
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

        if (Config.PluginMode == 1)
        {
            //SendConsoleCommand(BOT_MODE_FILL);
            try
            {
                string _bot_count = Config.BotCount.ToString();
                SendConsoleCommand($"bot_quota {_bot_count}");

            }
            catch (Exception ex)
            {
                Console.WriteLine("##############################################################");
                Console.WriteLine(ex.Message);
                Console.WriteLine("##############################################################");
            }



            if (Config.BotJoinAfterPlayer)
                SetCVAR("bot_join_after_player", true);
            else
                SetCVAR("bot_join_after_player", false);
        }

        if (Config.PluginMode == 2)
        {
            //SendConsoleCommand(BOT_MODE_MATCH);
            try
            {
                SendConsoleCommand($"bot_quota {Config.BotCount.ToString()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("##############################################################");
                Console.WriteLine(ex.Message);
                Console.WriteLine("##############################################################");
            }


            if (Config.BotJoinAfterPlayer)
                SetCVAR("bot_join_after_player", true);
            else
                SetCVAR("bot_join_after_player", false);
        }

        if (Config.PluginMode == 3)
        {
            //SendConsoleCommand(BOT_MODE_FILL);
            try
            {
                SendConsoleCommand($"bot_quota 1");
            }
            catch (Exception ex)
            {
                Console.WriteLine("##############################################################");
                Console.WriteLine(ex.Message);
                Console.WriteLine("##############################################################");
            }

            if (Config.BotJoinAfterPlayer)
                SetCVAR("bot_join_after_player", true);
            else
                SetCVAR("bot_join_after_player", false);
        }

        //RegisterEventHandler<EventRoundStart>(OnRoundStart);
        //RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        //RegisterEventHandler<EventSwitchTeam>(OnSwitchTeam);
        //RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);

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
    public bool isFirstPlayer = true;

    public int quota; /// ??????????????????????????????

    public void SetBotCount()
    {
        int quota = Config.BotCount;
        Server.ExecuteCommand($"bot_quota {quota}");
    }
    public void log(string error)
    {
        if (Config.DebugMode)
        {
            Server.PrintToChatAll($" {ChatColors.LightRed}{error}");
            //Console.WriteLine("##########################  Botiki debug block start  ##########################");
            Console.WriteLine(error);
            //Console.WriteLine("##########################  Botiki debug block end  ############################");
        }
        else
            return;
    }

    public void SendConsoleCommand(string msg)
    {
        Server.ExecuteCommand(msg);
    }

    public void SetCVAR(string convar, bool value)
    {
        try
        {
            var CVar = ConVar.Find(convar);
            if (CVar == null) return;
            CVar!.GetPrimitiveValue<bool>() = value;
        }
        catch (Exception ex)
        {
            Console.WriteLine("##############################################################");
            Console.WriteLine(ex.ToString());
            Console.WriteLine("##############################################################");
        }

    }
    public void ChangePlayerTeamSide(List<CCSPlayerController> players, CsTeam teamName)
    {
        int teamToChange = teamName == CsTeam.Terrorist ? 3 : 2;
        players.Find(player => player.TeamNum == teamToChange)?.SwitchTeam(teamName);
    }

    (int T, int Tb, int Th, int CT, int CTb, int CTh, int SPEC, bool IsBotExists, int? botTeam, string kickbotT, string kickbotCT) PlayersData(List<CCSPlayerController> players)
    {
        List<CCSPlayerController> bots = players.FindAll(player => player.IsValid && player.IsBot && !player.IsHLTV);

        int? botT_UserId = bots.Find(bot => bot.TeamNum == 2)?.UserId;
        int? botInTER = bots.Find(bot => bot.TeamNum == 2)?.TeamNum;
        int? botCT_UserId = bots.Find(bot => bot.TeamNum == 3)?.UserId;
        int? botInCT = bots.Find(bot => bot.TeamNum == 3)?.TeamNum;

        string kickbotT = $"";
        string kickbotCT = $"";

        if (botInTER == 2)
            kickbotT = $"kickid {botT_UserId}";
        else if (botInCT == 3)
            kickbotCT = $"kickid {botCT_UserId}";

        int CT = 0;
        int CTh = 0;
        int CTb = 0;
        int T = 0;
        int Th = 0;
        int Tb = 0;
        int SPEC = 0;
        int? botTeam = players.Find(player => player.IsValid && player.IsBot && !player.IsHLTV)?.TeamNum;
        bool isBotExists = players.Exists(player => player.IsValid && player.IsBot && !player.IsHLTV);

        players.ForEach(player =>
        {
            if (player == null || player.IsHLTV || !player.IsValid) return;

            if (player.TeamNum == 2)
                T++;
            else if (player.TeamNum == 3)
                CT++;

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

        // return (T, CT, SPEC, players.Exists(player => player.IsValid && player.IsBot && !player.IsHLTV), botTeam, kickbotT, kickbotCT);
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

    [GameEventHandler(mode: HookMode.Post)]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        var players = Utilities.GetPlayers();
        (int T, int Tb, int Th, int CT, int CTb, int CTh, int SPEC, bool IsBotExists, int? botTeam, string kickbotT, string kickbotCT) = PlayersData(players);

        CCSPlayerController controller = Utilities.GetPlayers().Find(pl => pl.IsValid && !pl.IsBot && !pl.IsHLTV)!;
        

        if (Quota > Config.BotCount)
            Quota = Config.BotCount;

        if (T + CT > Config.BotCount)
        {
            string _bot_count = Config.BotCount.ToString();
            SendConsoleCommand($"bot_quota {_bot_count}");
            Quota = Config.BotCount;
        }

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

        log("------ @event Round Start");

        switch (Config.PluginMode)
        {
            case 1:

                log("------ case 1(fill)");

                if (Th + CTh >= Config.PlayersCountForKickBots)
                {
                    SendConsoleCommand(BOT_KICK);
                    log("------  if #1");
                }

                //      need kick bot
                if (Tb + CTb > Quota)
                {
                    log("------ if #2");
                    if (Tb > CTb)
                    {
                        log("------ if #2.1");
                        // try $"bot_kick {.PlayerName}" 
                        for (int i = Tb - CTb; i == 0; i--)
                        {
                            string botName = Utilities.GetPlayers().First(pl => pl.IsValid && pl.IsBot && !pl.IsHLTV && pl.TeamNum == 2).UserId.ToString()!;
                            SendConsoleCommand($"bot_kick {botName}");
                            log("-----  if #2.1 loop");
                        }
                    }
                    if (CTb > Tb)
                    {
                        log("------ if #2.2");
                        for (int i = CTb - Tb; i == 0 ; i--)
                        {
                            string botName = Utilities.GetPlayers().First(pl => pl.IsValid && pl.IsBot && !pl.IsHLTV && pl.TeamNum == 3).UserId.ToString()!;
                            SendConsoleCommand($"bot_kick {botName}");
                            log("------ if 2.2 loof");
                        }
                    }
                }

                //      need add bot
                if (Tb + CTb < Quota)
                {
                    log("------ if #3");
                    if (Tb > CTb)
                    {
                        log("------ if #3.1");
                        for (int i = Tb - CTb;i == 0 ; i--)
                        {
                            SendConsoleCommand(BOT_ADD_CT);
                            log("------ if #3.1 loop");
                        }
                    }
                    if (CTb > Tb)
                    {
                        log("------ if #3.2");
                        for (int i = CTb - Tb; i == 0; i--)
                        {
                            SendConsoleCommand(BOT_ADD_T);
                            log("------ if #3.2 loop");
                        }
                    }
                }
                if (Th + CTh == 1 && Tb + CTb == Quota)
                {
                    log("------ if #4");

                    if (T > CT)
                    {
                        // try $"bot_kick {.PlayerName}" 
                        string botName = Utilities.GetPlayers().First(pl => pl.IsValid && pl.IsBot && !pl.IsHLTV && pl.TeamNum == 2).UserId.ToString()!;
                        SendConsoleCommand($"kickid {botName}");
                        SendConsoleCommand(BOT_ADD_CT);
                        log("------ if #4.1");
                    }
                    if (CT > T)
                    {
                        string botName = Utilities.GetPlayers().First(pl => pl.IsValid && pl.IsBot && !pl.IsHLTV && pl.TeamNum == 3).UserId.ToString()!;
                        SendConsoleCommand($"kickid {botName}");
                        SendConsoleCommand(BOT_ADD_T);
                        log("------ if #4.2");
                    }
                }

                log("------ case 1: break");
                break;

            case 2:
                //code
                break;

            case 3:
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
                Console.WriteLine("Error config mode");
                break;
        }

        return HookResult.Continue;
    }

    [GameEventHandler(mode: HookMode.Post)]
    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        var players = Utilities.GetPlayers();
        (int T, int Tb, int Th, int CT, int CTb, int CTh, int SPEC, bool IsBotExists, int? botTeam, string kickbotT, string kickbotCT) = PlayersData(players);

        CCSPlayerController controller = Utilities.GetPlayers().Find(pl => pl.IsValid && !pl.IsBot && !pl.IsHLTV)!;
        
        switch (Config.PluginMode)
        {
            case 1:

                //if (!IsBotExists && (T + CT) < Config.PlayersCountForKickBots)
                //{
                //    for (int i = 0; (T + CT) < Config.BotCount; i++)
                //    {
                //        SendConsoleCommand(T > CT ? BOT_ADD_CT : BOT_ADD_T);
                //    }
                //}

                break;

            case 2:
                //code
                break;

            case 3:
                //code
                break;


            default:
                Console.WriteLine("Error confign mode");
                break;
        }

        return HookResult.Continue;
    }

    [GameEventHandler(mode: HookMode.Post)]
    public HookResult OnSwitchTeam(EventSwitchTeam @event, GameEventInfo info)
    {
        var players = Utilities.GetPlayers();
        (int T, int Tb, int Th, int CT, int CTb, int CTh, int SPEC, bool IsBotExists, int? botTeam, string kickbotT, string kickbotCT) = PlayersData(players);

        CCSPlayerController controller = Utilities.GetPlayers().Find(pl => pl.IsValid && !pl.IsBot && !pl.IsHLTV)!;

        Quota++;

        log("------ @event Switch Team");

        switch (Config.PluginMode)
        {
            case 1:

                log("------ case 1(fill)");

                //if (controller == null) return HookResult.Continue;

                if (controller!.TeamChanged && controller.TeamNum == 1)
                {
                    Quota++;
                    log("------ if #1 go to SPEC");
                }

                //if (controller.TeamChanged && controller.TeamNum == 2)
                //{
                //    log("------ перша іф");
                //    ushort botID = (ushort)Utilities.GetPlayers().First(pl => pl.IsValid && pl.IsBot && !pl.IsHLTV && pl.TeamNum == 2).UserId!;
                //    SendConsoleCommand($"kickid {botID}");
                //}
                //if (controller.TeamChanged && controller.TeamNum == 3)
                //{
                //    log("------ друга іф");
                //    ushort botID = (ushort)Utilities.GetPlayers().First(pl => pl.IsValid && pl.IsBot && !pl.IsHLTV && pl.TeamNum == 3).UserId!;
                //    SendConsoleCommand($"kickid {botID}");
                //}

                //log("------ кейс breack");
                break;

            case 2:
                //code
                break;

            case 3:
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

    [GameEventHandler(mode: HookMode.Post)]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var players = Utilities.GetPlayers();
        (int T, int Tb, int Th, int CT, int CTb, int CTh, int SPEC, bool IsBotExists, int? botTeam, string kickbotT, string kickbotCT) = PlayersData(players);

        CCSPlayerController controller = Utilities.GetPlayers().Find(pl => pl.IsValid && !pl.IsBot && !pl.IsHLTV)!;

        Quota++;

        switch (Config.PluginMode)
        {
            case 1:

                //if (T + CT < Config.BotCount)
                //    SendConsoleCommand(T > CT ? BOT_ADD_CT : BOT_ADD_T);

                break;

            case 2:
                //code
                break;

            case 3:
                //code
                break;


            default:
                Console.WriteLine("Error confign mode");
                break;
        }

        return HookResult.Continue;
    }

    [GameEventHandler(mode: HookMode.Post)]
    public HookResult OnPlayerConnect(EventPlayerConnect @event, GameEventInfo info)
    {
        var players = Utilities.GetPlayers();
        (int T, int Tb, int Th, int CT, int CTb, int CTh, int SPEC, bool IsBotExists, int? botTeam, string kickbotT, string kickbotCT) = PlayersData(players);

        CCSPlayerController controller = Utilities.GetPlayers().Find(pl => pl.IsValid && !pl.IsBot && !pl.IsHLTV)!;

        Quota--;

        if (isFirstPlayer)
        {
            int quota = Config.BotCount;
            SendConsoleCommand("bot_quota_mode fill");
            SendConsoleCommand("bot_quota " + quota.ToString());

            isFirstPlayer = false;
        }

        switch (Config.PluginMode)
        {
            case 1:


                break;

            case 2:
                //code
                break;

            case 3:
                //code
                break;


            default:
                Console.WriteLine("Error confign mode");
                break;
        }

        return HookResult.Continue;
    }
}