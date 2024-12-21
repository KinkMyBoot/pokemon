using System.Linq;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using static SearchCommon;
using static RbyIGTChecker<Red>;

class NidoTest
{
    const string State = "basesaves/red/manip/nido_test.gqs";

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

        RbyMap route = gb.Maps[33];
        Action actions = Action.Right | Action.Down | Action.Up | Action.Left | Action.A;
        RbyTile startTile = gb.Tile;
        // RbyTile[] endTiles = { cave[36, 31], cave[37, 30], cave[37, 32] };

        List<RbyTile> blockedTiles = new List<RbyTile>(){};
        /*for(int y = 175; y <= 186; ++y) blockedTiles.Add(route[30, y]);
        for(int x = 30; x <= 51; ++x) blockedTiles.Add(route[x, 186]);
        for(int y = 175; y <= 186; ++y) blockedTiles.Add(route[51, y]);
        for(int x = 30; x <= 51; ++x) blockedTiles.Add(route[x, 175]);*/


        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, route[33, 11], actions, blockedTiles.ToArray());
        //gb.Maps[60][26, 3].AddEdge(0, new Edge<RbyMap, RbyTile>() { Action = Action.Down, NextTile = gb.Maps[60][26, 3], NextEdgeset = 0, Cost = 2 });
        Pathfinding.DebugDrawEdges<RbyMap, RbyTile>(gb, gb.Maps[1], 0);

        var parameters = new DFParameters<Red, RbyMap, RbyTile>()
        {
            MaxCost = 144,
            SuccessSS = success,
            EndTiles = new RbyTile[]{ gb.Maps[33][33, 11]},
            EncounterCallback = gb => gb.EnemyMon.Species.Name == "NIDORANM" &&
             /*gb.EnemyMon.DVs.Attack >= 8 && gb.EnemyMon.DVs.Defense >= 8 && gb.EnemyMon.DVs.Speed >= 8 && gb.EnemyMon.DVs.Special >= 8 && */
             gb.Yoloball() ,
            LogStart = startTile.PokeworldLink + "/",
            FoundCallback = state =>
            {
                
                Trace.WriteLine(state.Log + " " + CheckIGT(State, intro, state.Log, "NIDORANM", 60, false, false, Verbosity.Summary) + "/60 " + state.WastedFrames + " " + intro);
                
            }
        };

        DepthFirstSearch.StartSearch(gbs, parameters, startTile, 0, states);
        Elapsed("search");
    }
}
