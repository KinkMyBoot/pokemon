using System.Linq;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using static SearchCommon;
using static RbyIGTChecker<Blue>;

class CeaDugtrio
{
    const string State = "basesaves/blue/manip/dugtrio.gqs";

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
        path = "DDDDDDDDDADDDARRRADDADDARRRRADDDDADDADDARRARRRRARRARRRRADDARRARRRRRRRRRDS_BDARRAU"; pal = RbyStrat.Pal; // 57/60 c154
        CheckIGT(State, new RbyIntroSequence(pal), path, "DUGTRIO", 3600);
    }

    public static void Search()
    {
        for(RbyStrat pal = RbyStrat.PalAB; pal <= RbyStrat.PalRel; ++pal)
            for(RbyStrat gf = RbyStrat.GfSkip; gf <= RbyStrat.GfSkip; ++gf)
                for(RbyStrat hop = RbyStrat.Hop0; hop <= RbyStrat.Hop0; ++hop)
                    for(int backouts = 0; backouts <= 0; ++backouts)
                        Search(new RbyIntroSequence(pal, gf, hop, backouts), 8, 8, 8);
    }

    public static void Search(RbyIntroSequence intro, int numThreads = 16, int numFrames = 16, int success = 15)
    {
        StartWatch();

        Blue[] gbs = MultiThread.MakeThreads<Blue>(numThreads);
        Blue gb = gbs[0];
        if(numThreads == 1) gb.Record("test");

        gb.LoadState(State);
        IGTResults states = Blue.IGTCheckParallel(gbs, intro, numFrames);

        RbyMap cave = gb.Maps[197];
        Action actions = Action.Right | Action.Down | Action.Up | Action.Left | Action.A | Action.StartB;
        RbyTile startTile = gb.Tile;
        // RbyTile[] endTiles = { cave[36, 31], cave[37, 30], cave[37, 32] };

        List<RbyTile> blockedTiles = new List<RbyTile>(){ cave[5, 4] };
        for(int y = 4; y <= 17; ++y) blockedTiles.Add(cave[4, y]);
        for(int y = 14; y <= 16; ++y) blockedTiles.Add(cave[6, y]);
        for(int x = 7; x <= 8; ++x) blockedTiles.Add(cave[x, 16]);
        for(int y = 17; y <= 20; ++y) blockedTiles.Add(cave[9, y]);
        for(int x = 10; x <= 12; ++x) blockedTiles.Add(cave[x, 20]);
        for(int y = 21; y <= 28; ++y) blockedTiles.Add(cave[13, y]);
        for(int x = 14; x <= 24; ++x) blockedTiles.Add(cave[x, 28]);
        for(int y = 29; y <= 30; ++y) blockedTiles.Add(cave[25, y]);
        for(int x = 26; x <= 33; ++x) blockedTiles.Add(cave[x, 30]);

        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, cave[37, 31], actions, blockedTiles.ToArray());
        gb.Maps[13][12, 8].AddEdge(0, new Edge<RbyMap, RbyTile>() { Action = Action.Down, NextTile = gb.Maps[46][2, 7], NextEdgeset = 0, Cost = 0 });
        gb.Maps[13][13, 9].AddEdge(0, new Edge<RbyMap, RbyTile>() { Action = Action.Left, NextTile = gb.Maps[46][2, 7], NextEdgeset = 0, Cost = 0 });
        gb.Maps[13][12, 10].AddEdge(0, new Edge<RbyMap, RbyTile>() { Action = Action.Up, NextTile = gb.Maps[46][2, 7], NextEdgeset = 0, Cost = 0 });
        gb.Maps[13][13, 10].AddEdge(0, new Edge<RbyMap, RbyTile>() { Action = Action.Left, NextTile = gb.Maps[13][12, 10], NextEdgeset = 0, Cost = 0 });

        foreach(RbyTile t in cave.Tiles)
        {
            if(t.X < 34)
            {
                t.RemoveEdge(0, Action.Up);
                t.RemoveEdge(0, Action.Left);
            }
        }

        var parameters = new DFParameters<Blue, RbyMap, RbyTile>()
        {
            MaxCost = 170,
            SuccessSS = success,
            EndTiles = new RbyTile[]{ gb.Maps[85][4, 4] },
            EncounterCallback = gb => gb.Tile.X >= 34 && !(gb.Tile.X == 34 && gb.Tile.Y == 30) && gb.EnemyMon.Species.Name == "DUGTRIO" && gb.Yoloball() ,
            LogStart = startTile.PokeworldLink + "/",
            FoundCallback = state =>
            {
                Trace.WriteLine(state.Log + " " + CheckIGT(State, intro, state.Log, "DUGTRIO", 60, false, false, Verbosity.Summary) + "/60 " + state.WastedFrames + " " + intro);
            }
        };

        DepthFirstSearch.StartSearch(gbs, parameters, startTile, 0, states);
        Elapsed("search");
    }
}
