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

    public override string ModuleVersion => "0.0.1";

    public override string ModuleAuthor => "jockii, VoCs";

    public override void Load(bool hotReload)
    {
        Console.WriteLine("----------------------------------------------------");
        Console.WriteLine($"Plugin: {ModuleName} ver:{ModuleVersion} by {ModuleAuthor} has been loaded =)");
        Console.WriteLine("---------------------------------------------");

        Server.ExecuteCommand("sv_cheats true");
        Server.ExecuteCommand("bot_quota 1");
        Server.ExecuteCommand("bot_quota_mode match");
        Server.ExecuteCommand("sv_cheats false");
    }

    const string BOT_ADD_CT = "bot_add_ct";
    const string BOT_ADD_T = "bot_add_t";
    const string BOT_KICK = "bot_kick";

    public void ChangePlayerTeamSide(List<CCSPlayerController> realPlayers, CsTeam teamName)
    {
        int teamToChange = teamName == CsTeam.Terrorist ? 3 : 2;
        realPlayers.Find(player => player.TeamNum == teamToChange).ChangeTeam(teamName);
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

    [GameEventHandler]
    public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        Checker(Utilities.GetPlayers());

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        Checker(Utilities.GetPlayers());

        return HookResult.Continue;
    }
}
