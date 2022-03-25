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
        Elapsed("threads");

        IGTResults states = new IGTResults(numFrames);
        gb.LoadState("basesaves/bluetest.gqs");
        gb.HardReset();
        intro.ExecuteUntilIGT(gb);
        byte[] igtState = gb.SaveState();

        MultiThread.For(states.Length, gbs, (gb, f) =>
        {
            gb.LoadState(igtState);
            gb.CpuWrite("wPlayTimeSeconds", (byte) (f / 60));
            gb.CpuWrite("wPlayTimeFrames", (byte) (f % 60));
            intro.ExecuteAfterIGT(gb);

            states[f] = new IGTState(gb, false, f);
        });
        Elapsed("states");

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
            EncounterCallback = gb =>
            {
                return gb.EnemyMon.Species.Name == "PIDGEY" && gb.Yoloball();
            },
            FoundCallback = state =>
            {
                Trace.WriteLine(startTile.PokeworldLink + "/" + state.Log + " Captured: " + state.IGT.TotalSuccesses + " Failed: " + (state.IGT.TotalFailures - state.IGT.TotalOverworld) + " NoEnc: " + state.IGT.TotalOverworld + " Cost: " + state.WastedFrames);
            }
        };

        DepthFirstSearch.StartSearch(gbs, parameters, startTile, 0, states);
        Elapsed("search");
    }
}
