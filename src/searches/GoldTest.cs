using System.Linq;
using System;
using System.Diagnostics;
using System.Collections.Generic;

using static SearchCommon;

class GoldTest
{
    public static void Check()
    {
    }

    public static void Search(int numThreads = 16, int numFrames = 60, int success = 55)
    {
        StartWatch();
        GscIntroSequence intro = new GscIntroSequence();

        Gold[] gbs = MultiThread.MakeThreads<Gold>(numThreads);
        Gold gb = gbs[0];
        if(numThreads == 1) gb.Record("test");

        gb.LoadState("basesaves/goldtest.gqs");
        IGTResults states = Gold.IGTCheckParallel(gbs, 4600, intro, numFrames);

        GscMap map = gb.Maps[6147];
        Action actions = Action.Right | Action.Down | Action.Up | Action.Left;
        GscTile startTile = gb.Tile;
        GscTile[] endTiles = { map[10, 6] };
        Pathfinding.GenerateEdges<GscMap, GscTile>(gb, 0, endTiles[0], actions);

        var parameters = new DFParameters<Gold, GscMap, GscTile>()
        {
            MaxCost = 0,
            SuccessSS = success,
            EndTiles = endTiles,
            EncounterCallback = gb =>
            {
                gb.RunUntil("CalcMonStats");
                return gb.EnemyMon.Species.Name == "PIDGEY" && gb.Tile == endTiles[0];
            },
            LogStart = startTile.PokeworldLink + "/",
            FoundCallback = state =>
            {
                Trace.WriteLine(state.Log + " Success: " + state.IGT.TotalSuccesses + " Failed: " + (state.IGT.TotalFailures - state.IGT.TotalRunning) + " NoEnc: " + state.IGT.TotalRunning + " Cost: " + state.WastedFrames);
            }
        };

        DepthFirstSearch.StartSearch(gbs, parameters, startTile, 0, states);
        Elapsed("search");
    }
}
