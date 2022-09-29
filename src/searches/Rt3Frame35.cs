using System.Linq;
using System;
using System.Diagnostics;
using System.Collections.Generic;

using static SearchCommon;
using static RbyIGTChecker<Red>;

class Rt3Frame35
{
    const string State0 = "basesaves/red/manip/rt3f35t0.gqs";
    const string State1 = "basesaves/red/manip/rt3f35t1.gqs";
    const string State2 = "basesaves/red/manip/rt3f35t2.gqs";

    public static void Check()
    {
        RbyStrat pal;
        string state, path;

        // state = State2;
        // pal = RbyStrat.Pal; path = "DDDDDDDLLLLDDDDDDDDDALLALLALLLLLLLLLLLLLLLLLUUUUUUUU"; //57
        state = State0;
        pal = RbyStrat.NoPalAB; path = "RDDDDDDDDLLLLDDDDDDDDDDLLLLLLLLLLLLLLLLLLLLLLLLLRUUUUUUUUUUUUUUURR"; //57
        // pal = RbyStrat.NoPalAB; path = "RDDDDDDDDLLLLDDDDDDDDDDLLLLLLLLLLLLLLLLLLLLLLLLUUUUUUUUUUUUUUAURR"; //55
        // pal = RbyStrat.NoPalAB; path = "RDDDDDDDADLLLALDADDDDDDADDLLLLLLLLLLLLLLLLLLLLLUUUUUUUUUUALS_BAUUUU"; //56
        // pal = RbyStrat.PalHold; path = "RDDDDDDADDLLLLADDADDDDDDDALLLLLLLLLLLLALLLLLLLLLUUUUUUUUUUS_BLUUAUU"; //55
        // pal = RbyStrat.NoPalAB; path = "RDDDDADDDDLLLLDDDDDDDDDLLLLLLLLLLLLLLLLLLLLLUAUUUUUUS_BUULLUAUUAUU"; //57

        CheckIGT(state, new RbyIntroSequence(pal), path, "PARAS", 60);
    }

    public static void Search()
    {
        for(RbyStrat pal = RbyStrat.NoPal; pal <= RbyStrat.PalHold; ++pal)
            Search(new RbyIntroSequence(pal), 6, 6, 5, 100);
    }

    public static void Search(RbyIntroSequence intro, int numThreads, int numFrames, int success, int cost)
    {
        StartWatch();
        Trace.WriteLine(intro);

        Red[] gbs = MultiThread.MakeThreads<Red>(numThreads);
        Red gb = gbs[0];
        if(numThreads == 1) gb.Record("test");

        gb.LoadState(State0);
        IGTResults states = Red.IGTCheckParallel(gbs, intro, numFrames);

        RbyMap moon = gb.Maps[61];
        Action actions = Action.Right | Action.Down | Action.Up | Action.Left | Action.A | Action.StartB;
        RbyTile startTile = gb.Tile;
        RbyTile[] endTiles = { moon[10, 17] };
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, endTiles[0], actions, moon[34, 14], moon[37, 14], moon[33, 23], moon[34, 23], moon[35, 23], moon[36, 23]);

        var parameters = new DFParameters<Red, RbyMap, RbyTile>()
        {
            MaxCost = cost,
            SuccessSS = success,
            EndTiles = endTiles,
            MaxTurns = 8,
            EncounterCallback = gb => gb.EnemyMon.Species.Name == "PARAS" && gb.Yoloball() && gb.Tile.X <= 11 && gb.Tile.Y <= 18,
            LogStart = startTile.PokeworldLink + "/",
            FoundCallback = state =>
            {
                string failures = "";
                foreach(var i in state.IGT.IGTs)
                {
                    if(!i.Running && !i.Success)
                    {
                        gb.LoadState(i.State);
                        if(gb.EnemyMon.Species.Name != "PARAS") failures += " " + gb.EnemyMon.Species.Name;
                        else if(!gb.Yoloball()) failures += " yoloball";
                        else if(gb.Tile.X > 11 || gb.Tile.Y > 18) failures += " tile:" + gb.Tile.X + "/" + gb.Tile.Y;
                    }
                }
                Trace.WriteLine(state.Log + " Captured: " + state.IGT.TotalSuccesses + " Failed: " + (state.IGT.TotalFailures - state.IGT.TotalRunning) + " NoEnc: " + state.IGT.TotalRunning + " Cost: " + state.WastedFrames + failures);
            }
        };

        DepthFirstSearch.StartSearch(gbs, parameters, startTile, 0, states);
        Elapsed("search");
    }
}
