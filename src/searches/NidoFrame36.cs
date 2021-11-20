using System.Linq;
using System;
using System.Diagnostics;
using System.Collections.Generic;

using static SearchCommon;
using static RbyIGTChecker<Red>;

class NidoFrame36
{
    // static void Check()
    // {
    // }

    public static List<DFState<RbyMap,RbyTile>> Search(int numThreads = 1, int numFrames = 1, int success = -1)
    {
        RbyIntroSequence intro = new RbyIntroSequence(RbyStrat.NoPal);
        Red[] gbs = {new Red()};
        Red gb = null;

        Profile("threads", () =>
        {
            // gbs = MultiThread.MakeThreads<Red>(numThreads);
            // gbs = { ; };
            gb = gbs[0];
            // if(numThreads == 1)
            //     gb.Record("test");
        });

        IGTResults states = new IGTResults(numFrames);

        Profile("states", () =>
        {
            gb.LoadState("basesaves/red/manip/nido.gqs");
            gb.HardReset();
            intro.ExecuteUntilIGT(gb);
            // byte[] igtState = gb.SaveState();

            // MultiThread.For(states.Length, gbs, (gb, f) =>
            // {
                // gb.LoadState(igtState);
                gb.CpuWrite("wPlayTimeMinutes", 5);
                gb.CpuWrite("wPlayTimeSeconds", 0);
                gb.CpuWrite("wPlayTimeFrames", 36);
                intro.ExecuteAfterIGT(gb);
                gb.Execute(SpacePath("LLLULLUAULALDLDLLDADDADLALLALUUAUUU"));

                // states[f]=new IGTState(gb, false, f);
                states[0]=new IGTState(gb, false, 36);
            // });
        });

        RbyMap route22 = gb.Maps[33];
        Action actions = Action.Right | Action.Down | Action.Up | Action.Left | Action.A | Action.StartB;
        RbyTile[] endTiles = { route22[33, 11] };
        Pathfinding.GenerateEdges<RbyMap,RbyTile>(gb, 0, endTiles.First(), actions);
        route22[30, 4].RemoveEdge(0, Action.Left);
        route22[30, 5].RemoveEdge(0, Action.Left);
        Pathfinding.DebugDrawEdges(gb, route22, 0);

        RbyTile tile = gb.Tile;
        var results = new List<DFState<RbyMap,RbyTile>>();

        var parameters = new DFParameters<Red,RbyMap,RbyTile>()
        {
            MaxCost = 1000,
            SuccessSS = success > 0 ? success : Math.Max(1, states.Length - 5),// amount of yoloball success for found
            EndTiles = endTiles,
            EncounterCallback = gb =>
            {
                int dv = gb.EnemyMon.DVs.HP + gb.EnemyMon.DVs.Attack + gb.EnemyMon.DVs.Defense + gb.EnemyMon.DVs.Speed + gb.EnemyMon.DVs.Special;
                return //gb.EnemyMon.Species.Name == "NIDORANM" && gb.EnemyMon.Level >= 3
                    /*&&*/ gb.EnemyMon.DVs.Attack == 15
                    && gb.EnemyMon.DVs.Defense >= 11
                    && gb.EnemyMon.DVs.Speed >= 14
                    && gb.EnemyMon.DVs.Special == 15
                    && gb.EnemyMon.DVs.HP >= 11;
                // return gb.EnemyMon.Species.Name == "NIDORANM" && gb.EnemyMon.Level== 4 && gb.Yoloball();
            },
            FoundCallback = state =>
            {
                results.Add(state);
                Trace.WriteLine(tile.PokeworldLink + "/" + state.Log + "  " + gb.EnemyMon.Species.Name + " L: " + gb.EnemyMon.Level + " DVs: " + gb.EnemyMon.DVs.ToString() + " Cost: " + state.WastedFrames);
            }
        };

        Profile("dfs", () =>
        {
            DepthFirstSearch.StartSearch(gbs, parameters, tile, 0, states, 0);
        });

        return new List<DFState<RbyMap,RbyTile>>(results.OrderByDescending((dfs) => dfs.IGT.TotalSuccesses).OrderBy((dfs) => APressCount(dfs.Log)).OrderBy((dfs) => TurnCount(dfs.Log)));
    }
}
