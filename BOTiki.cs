using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;

namespace BOTiki;
public class BOTiki : BasePlugin
{
    public override string ModuleName => "BOTiki";

    public override string ModuleVersion => "dev 0.0.1";

    public override string ModuleAuthor => "jockii";

    public override string ModuleDescription => "when player count up two, post CVAR 'bot_kick', bla..bla...bla... ";

    public override void Load(bool hotReload)
    {
        Console.WriteLine("----------------------------------------------------");
        Console.WriteLine($"Plugin: {ModuleName} ver:{ModuleVersion} by {ModuleAuthor} has been loaded =)");
        Console.WriteLine($"Description: {ModuleDescription} .");
        Console.WriteLine("---------------------------------------------");

        Server.ExecuteCommand("sv_cheats true");
        Server.ExecuteCommand("bot_quota 1");
        Server.ExecuteCommand("bot_quota_mode fill");
        Server.ExecuteCommand("sv_cheats false");
    }

    

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
            if (playerFromIndex.IsValid && playerFromIndex.UserId != -1)  //&& !playerFromIndex.IsBot
            {
                list.Add(playerFromIndex);
            }
        }

        return list;
    }

    public void Checker(List<CCSPlayerController> players)
    {
        int playersCount = players.Count;
        
        int CT = 0;
        int T = 0;
        bool isBotExists = false;

        for (int i = 0; i < playersCount; i++)
        {
            var player = players[i];

            if (!player.IsBot)
            {
                if (player.TeamNum == 2)
                    T++;
                else if (player.TeamNum == 3)
                    CT++;
            }
            else
                isBotExists = true;
        }

        if (T + CT == 1 && !isBotExists)
        {
            if (T > CT)
             Server.ExecuteCommand("bot_add_ct");
            else
             Server.ExecuteCommand("bot_add_t");
            
        }
        else if ((T + CT == 2  || T + CT > 3) && isBotExists)
        {
            Server.ExecuteCommand("bot_kick");
            Server.PrintToChatAll("command: 'bot_kick' was executed");
        }
        else if (T + CT == 3 && !isBotExists)
        {
            if (T > CT)
                Server.ExecuteCommand("bot_add_ct");
            else
                Server.ExecuteCommand("bot_add_t");
        }

        if (T > 1 && CT == 0)
        {
            var Tswap = players.Find(player => player.TeamNum == 2 && !player.IsBot);
            Tswap.ChangeTeam(CsTeam.CounterTerrorist);
        }
          
            
        if (CT > 1 && T == 0)
        {
            var CTswap = players.Find(player => player.TeamNum == 3 && !player.IsBot);
            CTswap.ChangeTeam(CsTeam.Terrorist);
        }

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
