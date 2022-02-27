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

    public static List<DFState<GscMap,GscTile>> Search(int numThreads = 15, int numFrames = 60, int success = -1)
    {
        StartWatch();
        GscIntroSequence intro = new GscIntroSequence();

        Gold[] gbs = MultiThread.MakeThreads<Gold>(numThreads);
        Gold gb = gbs[0];
        if(numThreads == 1) gb.Record("test");
        Elapsed("threads");

        IGTResults states = new IGTResults(numFrames);
        gb.LoadState("basesaves/goldtest.gqs");
        gb.HardReset();
        intro.ExecuteUntilIGT(gb);
        byte[] igtState = gb.SaveState();

        MultiThread.For(states.Length, gbs, (gb, f) =>
        {
            gb.LoadState(igtState);
            gb.CpuWrite("wGameTimeSeconds", (byte)(f / 60));
            gb.CpuWrite("wGameTimeFrames", (byte)(f % 60));
            intro.ExecuteAfterIGT(gb);

            states[f]=new IGTState(gb, false, f);
        });
        Elapsed("states");

        GscMap map = gb.Maps[6147];
        Action actions = Action.Right | Action.Down | Action.Up | Action.Left;
        GscTile startTile = gb.Tile;
        GscTile[] endTiles = { map[10, 6] };
        Pathfinding.GenerateEdges<GscMap,GscTile>(gb, 0, endTiles.First(), actions);

        var results = new List<DFState<GscMap,GscTile>>();
        var parameters = new DFParameters<Gold,GscMap,GscTile>()
        {
            MaxCost = 0,
            SuccessSS = success > 0 ? success : Math.Max(1, states.Length - 5),
            EndTiles = endTiles,
            EncounterCallback = gb =>
            {
                gb.RunUntil("CalcMonStats");
                return gb.EnemyMon.Species.Name == "PIDGEY" && gb.Tile == endTiles[0];
            },
            FoundCallback = state =>
            {
                results.Add(state);
                Trace.WriteLine("https://gunnermaniac.com/pokeworld2?local=" + startTile.Map.Id + "#" + startTile.X + "/" + startTile.Y + "/" + state.Log + " Success: " + state.IGT.TotalSuccesses + " Failed: " + (state.IGT.TotalFailures - state.IGT.TotalOverworld) + " NoEnc: " + state.IGT.TotalOverworld + " Cost: " + state.WastedFrames);
            }
        };

        DepthFirstSearch.StartSearch(gbs, parameters, startTile, 0, states);
        Elapsed("search");

        return results;
    }
}
