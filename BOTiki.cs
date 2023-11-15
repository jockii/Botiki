using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Utils;

namespace BOTiki;
public class BOTiki : BasePlugin, IPluginConfig<BOTikiConfig>
{
    public override string ModuleName => "BOTiki";

    public override string ModuleVersion => "v0.2";

    public override string ModuleAuthor => "jockii, VoCs";

    public BOTikiConfig Config { get; set; } = new();
    private string _BotMode = "";
    private int _bot_count = 0;
    private int _player_count_to_bot_kick = 0;

    public override void Load(bool hotReload)
    {
        Console.WriteLine("------------------------------------------------------------");
        Console.WriteLine($"Plugin: {ModuleName} {ModuleVersion} by {ModuleAuthor} has been loaded =)");
        Console.WriteLine("------------------------------------------------------------");
    }

    public void OnConfigParsed(BOTikiConfig config)
    {
        this.Config = config;
        _BotMode = config.BotMode;
        _bot_count = config.bot_count;
        _player_count_to_bot_kick = config.player_count_to_bot_kick;
    }

    const string BOT_ADD_CT = "bot_add_ct";
    const string BOT_ADD_T = "bot_add_t";
    const string BOT_KICK = "bot_kick";

    List<CCSPlayerController> players = Utilities.GetPlayers();

    public void ChangePlayerTeamSide(List<CCSPlayerController> realPlayers, CsTeam teamName)
    {
        int teamToChange = teamName == CsTeam.Terrorist ? 3 : 2;
        realPlayers.Find(player => player.TeamNum == teamToChange)?.ChangeTeam(teamName); //////////// line 51: null => ?
    }

    public void AddBotsByConfigMode(int T, int CT, bool isBotExists)
    {
        if (isBotExists && T + CT >= _player_count_to_bot_kick)
            Server.ExecuteCommand(BOT_KICK);
        else
        {
            switch (_BotMode)
            {
                case "normal":
                    Server.ExecuteCommand(BOT_KICK);
                    Server.ExecuteCommand("bot_quota_mode normal");
                    Server.ExecuteCommand($"bot_quota {_bot_count}");
                    break;
                case "balanced":
                    Server.ExecuteCommand(BOT_KICK);
                    if (T + CT == 1)
                    {
                        Server.ExecuteCommand("bot_quota_mode match");
                        Server.ExecuteCommand("bot_quota 1");
                        Server.ExecuteCommand("bot_join_after_player true");
                        Server.ExecuteCommand(T == 1 ? BOT_ADD_CT : BOT_ADD_T);
                    }
                    else if (T != CT)
                    {
                        Server.ExecuteCommand("bot_quota_mode normal");
                        Server.ExecuteCommand("bot_quota 1");
                        Server.ExecuteCommand("bot_join_after_player true");
                        Server.ExecuteCommand(T > CT ? BOT_ADD_CT : BOT_ADD_T);
                    }
                    break;
                default:
                    Console.WriteLine("------------------------------------------------(");
                    Console.WriteLine("'switch Plugin' not working =(");
                    Console.WriteLine("------------------------------------------------(");
                    break;
            }
        }
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

        AddBotsByConfigMode(T, CT, isBotExists);

        if (T > 1 && CT == 0)
            ChangePlayerTeamSide(realPlayers, CsTeam.Terrorist);
        if (CT > 1 && T == 0)
            ChangePlayerTeamSide(realPlayers, CsTeam.Terrorist);

    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        List<CCSPlayerController> players = Utilities.GetPlayers();
        Checker(players);

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        List<CCSPlayerController> players = Utilities.GetPlayers();
        Checker(players);

        return HookResult.Continue;
    }
}
