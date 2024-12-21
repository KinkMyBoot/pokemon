using System.Linq;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using static SearchCommon;
using static RbyIGTChecker<Red>;

class ParasBackupTest
{
    const string State = "basesaves/red/manip/postnerd_paras_redbar.gqs";

    public static void Check()
    {
        string path;
        RbyStrat pal;
        // path = "DDDADDADDDDDDDRRARRDADDRDRRDADDDRDDRRRRRRRRRARRRDDDDARRRRRRRRRRRR"; pal = RbyStrat.PalHold;
        // path = "DDDDDDDADDADDDRRARRDDDDRRRRDDDADDDDDARRRRRRARRRRRRDDARRRRRRRRRRRR"; pal = RbyStrat.PalAB;
        // path = "DDDDDDDADDADDDRARRDDDDRRRRDDDDADDDDRARRARRRRRRRRRDDRARRRRRRRRRRRR"; pal = RbyStrat.PalAB;

        // path = "DDDDDDDDDADDDARRRADDDADRRRRDDDDADDDDRRRRRRARRRRRRDRRRARRRS_BRRRRDDDRRUUU"; pal = RbyStrat.Pal; // current 56/60 c168
        // path = "DUUURRDDDDDDDDDDDDRRRRDDDDRRRRDDDDDDRRARRRRARRRARRADDADRARRARRRRRRRR"; pal = RbyStrat.NoPalAB;
        // path = "DUUURRDDDDDDDDDDDDRRRRDDDDRRRARDADDADDDDDRARRARRRRRRRARDRARRRRRRRRRR"; pal = RbyStrat.NoPalAB;
        path = "LLLLLLDLALLDLDDDRRRR"; pal = RbyStrat.NoPalAB; // 57/60 c154
        CheckIGT(State, new RbyIntroSequence(pal), path, "PARAS", 3600);
    }

    public static void Search()
    {
        for(RbyStrat pal = RbyStrat.NoPal; pal <= RbyStrat.PalRel; pal++)
            for(RbyStrat gf = RbyStrat.GfSkip; gf <= RbyStrat.GfSkip; ++gf)
                for(RbyStrat hop = RbyStrat.Hop0; hop <= RbyStrat.Hop0; ++hop)
                    for(int backouts = 0; backouts <= 0; ++backouts)
                        Search(new RbyIntroSequence(pal, gf, hop, backouts), 12, 16, 14);
    }

    public static void Search(RbyIntroSequence intro, int numThreads = 12, int numFrames = 16, int success = 15)
    {
        StartWatch();

        Red[] gbs = MultiThread.MakeThreads<Red>(numThreads);
        Red gb = gbs[0];
        if(numThreads == 1) gb.Record("test");

        gb.LoadState(State);
        IGTResults states = Red.IGTCheckParallel(gbs, intro, numFrames);

        RbyMap cave = gb.Maps[61];
        Action actions = Action.Right | Action.Down | Action.Left | Action.A;
        RbyTile startTile = gb.Tile;
        // RbyTile[] endTiles = { cave[36, 31], cave[37, 30], cave[37, 32] };

        List<RbyTile> blockedTiles = new List<RbyTile>(){ cave[5, 6] };
        /*for(int y = 2; y <= 4; ++y) blockedTiles.Add(cave[14, y]);
        for(int x = 3; x <= 13; ++x) blockedTiles.Add(cave[x, 1]);
        for (int x = 4; x <= 12; ++x) blockedTiles.Add(cave[x, 5]);
        for (int y = 2; y <= 7; ++y) blockedTiles.Add(cave[2, y]);
        for(int x = 3; x <= 5; ++x) blockedTiles.Add(cave[x, 8]);
        for (int x = 5; x <= 5; ++x) blockedTiles.Add(cave[x, 6]);*/


        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, gb.Maps[60][26, 3], actions, blockedTiles.ToArray());
        //gb.Maps[60][26, 3].AddEdge(0, new Edge<RbyMap, RbyTile>() { Action = Action.Down, NextTile = gb.Maps[60][26, 3], NextEdgeset = 0, Cost = 2 });
        Pathfinding.DebugDrawEdges<RbyMap, RbyTile>(gb, gb.Maps[60], 0);

        var parameters = new DFParameters<Red, RbyMap, RbyTile>()
        {
            MaxCost = 6,
            SuccessSS = success,
            EndTiles = new RbyTile[]{ gb.Maps[60][26, 3]},
            EncounterCallback = gb => gb.EnemyMon.Species.Name == "PARAS" && gb.Map.Id == 60 && gb.RedbarYoloball() ,
            LogStart = startTile.PokeworldLink + "/",
            FoundCallback = state =>
            {
                success = CheckIGT(State, intro, state.Log, "PARAS", 60, false, false, Verbosity.Nothing);
                if(success>55){
                    Trace.WriteLine(state.Log + " " + CheckIGT(State, intro, state.Log, "PARAS", 60, false, false, Verbosity.Summary) + "/60 " + state.WastedFrames + " " + intro);
                }
            }
        };

        DepthFirstSearch.StartSearch(gbs, parameters, startTile, 0, states);
        Elapsed("search");
    }
}
