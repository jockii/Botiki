using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Utils;

namespace BOTiki;
public class BOTiki : BasePlugin, IPluginConfig<BOTikiConfig>
{
    public override string ModuleName => "BOTiki";

    public override string ModuleVersion => "0.2";

    public override string ModuleAuthor => "jockii, VoCs";

    public BOTikiConfig Config { get; set; } = new();
    private string _BotMode = "";
    private int _bot_count = 0;
    private int _max_player_to_bot_kick = 0;
    public override void Load(bool hotReload)
    {
        Console.WriteLine("----------------------------------------------------");
        Console.WriteLine($"Plugin: {ModuleName} ver:{ModuleVersion} by {ModuleAuthor} has been loaded =)");
        Console.WriteLine("---------------------------------------------");
        /* --------- create a json config -----------
        var configPath = Path.Join(ModuleDirectory, "Config.json");
        if (!File.Exists(configPath))
        {
            var data = new Config() { BotMode = "balanced", bot_count = 10, max_player_to_bot_kick = 10 };
            File.WriteAllText(configPath, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
            _config = data;
        }
        else _config = JsonSerializer.Deserialize<Config>(File.ReadAllText(configPath));
        */
        
        Server.ExecuteCommand("sv_cheats true");
        Server.ExecuteCommand("bot_join_after_player true");
        Server.ExecuteCommand("bot_quota 1");
        Server.ExecuteCommand("bot_quota_mode match");
        Server.ExecuteCommand("sv_cheats false");
    }

    public void OnConfigParsed(BOTikiConfig config)
    {
        this.Config = config;
        _BotMode = config.BotMode;
        _bot_count = config.bot_count;
        _max_player_to_bot_kick = config.max_player_to_bot_kick;
    }

    const string BOT_ADD_CT = "bot_add_ct";
    const string BOT_ADD_T = "bot_add_t";
    const string BOT_KICK = "bot_kick";

    public static T GetEntityFromIndex<T>(int index) where T : CEntityInstance
    {
        return (T)Activator.CreateInstance(typeof(T), NativeAPI.GetEntityFromIndex(index));
    }
    public static CCSPlayerController GetPlayerFromIndex(int index)
    {
        return GetEntityFromIndex<CCSPlayerController>(index);
    }

    public static List<CCSPlayerController> GetPlayers()
    {
        List<CCSPlayerController> list = new List<CCSPlayerController>();
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            CCSPlayerController playerFromIndex = GetPlayerFromIndex(i);
            if (playerFromIndex.IsValid && playerFromIndex.UserId != -1)
            {
                list.Add(playerFromIndex);
            }
        }

        return list;
    }

    public void ChangePlayerTeamSide(List<CCSPlayerController> realPlayers, CsTeam teamName)
    {
        int teamToChange = teamName == CsTeam.Terrorist ? 3 : 2;
        realPlayers.Find(player => player.TeamNum == teamToChange).ChangeTeam(teamName);
    }

    /*    ------ backup --------
         
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
    */

    public void AddBotsByConfigMode(int T, int CT, string _BotMode, int _bot_count)
    {
        switch (_BotMode)
        {
            case "fill":
                // log
                Server.PrintToChatAll("--- fill case ---");/////////////
                Console.WriteLine("--- fill case ---");/////////////////
                //
                Server.ExecuteCommand($"bot_quota {_bot_count}");
                Server.ExecuteCommand("bot_quota_mode fill"); break;
            case "match":
                // log
                Server.PrintToChatAll("--- match case ---");/////////////
                Console.WriteLine("--- match case ---");////////////////
                //
                Server.ExecuteCommand($"bot_quota 1");
                Server.ExecuteCommand("bot_quota_mode match"); break;
            case "balanced":
                // log
                Server.PrintToChatAll("--- balanced case ---");///////////////
                Console.WriteLine("--- balanced case ---");//////////////////
                //
                if (T + CT == 1)
                {
                    Server.ExecuteCommand("bot_quota_mode match");
                    Server.ExecuteCommand("bot_join_after_player true");
                    Server.ExecuteCommand(T == 1 ? BOT_ADD_CT : BOT_ADD_T);
                }
                else if (T + CT == 3 ) // (T + CT) % 2
                {
                    Server.ExecuteCommand("bot_quota_mode fill");
                    Server.ExecuteCommand("bot_join_after_player true");
                    Server.ExecuteCommand(T > CT ? BOT_ADD_CT : BOT_ADD_T);
                } break;
            default:
                Console.WriteLine("------------------------------------------------(");
                Console.WriteLine("'switch' not working =(");
                Console.WriteLine("'switch' not working =(");
                Console.WriteLine("'switch' not working =(");
                Console.WriteLine("'switch' not working =(");
                Console.WriteLine("------------------------------------------------(");
                break;
        }
    }

    public void KickBotsByPlayersCount(int T, int CT, int _max_player_to_bot_kick)
    {
        /*
         *  ----------- backup -----------
            if (T + CT == 2 || T + CT > 3) 
            Server.ExecuteCommand(BOT_KICK);
         */

        if (T == CT)
            Server.ExecuteCommand(BOT_KICK);
        else if (T + CT == _max_player_to_bot_kick)
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
            KickBotsByPlayersCount(T, CT, _max_player_to_bot_kick);
        else
            AddBotsByConfigMode(T, CT, _BotMode, _bot_count);

        if (T > 1 && CT == 0)
            ChangePlayerTeamSide(realPlayers, CsTeam.Terrorist);
        if (CT > 1 && T == 0)
            ChangePlayerTeamSide(realPlayers, CsTeam.Terrorist);

    }

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        Checker(GetPlayers());

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        Checker(GetPlayers());

        return HookResult.Continue;
    }
}
