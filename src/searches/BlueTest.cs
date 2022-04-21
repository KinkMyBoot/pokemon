using System.Linq;
using System;
using System.Diagnostics;
using System.Collections.Generic;

using static SearchCommon;

class BlueTest
{
    public static void Check()
    {
    }

    public static void Search(int numThreads = 16, int numFrames = 60, int success = 55)
    {
        StartWatch();
        RbyIntroSequence intro = new RbyIntroSequence(RbyStrat.NoPalAB);

        Blue[] gbs = MultiThread.MakeThreads<Blue>(numThreads);
        Blue gb = gbs[0];
        if(numThreads == 1) gb.Record("test");

        gb.LoadState("basesaves/blue/manip/bluetest.gqs");
        IGTResults states = Blue.IGTCheckParallel(gbs, intro, numFrames);

        RbyMap route2 = gb.Maps[13];
        Action actions = Action.Right | Action.Down | Action.Up | Action.Left | Action.A | Action.StartB;
        RbyTile startTile = gb.Tile;
        RbyTile[] endTiles = { route2[8, 48] };
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, endTiles[0], actions);

        var parameters = new DFParameters<Blue, RbyMap, RbyTile>()
        {
            MaxCost = 4,
            SuccessSS = success,
            EndTiles = endTiles,
            EncounterCallback = gb => gb.EnemyMon.Species.Name == "PIDGEY" && gb.Yoloball(),
            LogStart = startTile.PokeworldLink + "/",
            FoundCallback = state =>
            {
                Trace.WriteLine(state.Log + " Captured: " + state.IGT.TotalSuccesses + " Failed: " + (state.IGT.TotalFailures - state.IGT.TotalRunning) + " NoEnc: " + state.IGT.TotalRunning + " Cost: " + state.WastedFrames);
            }
        };

        DepthFirstSearch.StartSearch(gbs, parameters, startTile, 0, states);
        Elapsed("search");
    }
}
