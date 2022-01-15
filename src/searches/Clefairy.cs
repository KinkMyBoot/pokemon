using System.Linq;
using System;
using System.Diagnostics;
using System.Collections.Generic;

using static SearchCommon;

class Clefairy
{
    public static void Check()
    {
        string path = "S_BS_BDDDDDS_BUUUS_BUU"; //3299 119
        // string path = "S_BDDDDDUUS_BS_BUS_BUU"; //3299 119
        // string path = "DDS_BDDDUUS_BS_BUS_BUU"; //3299 119
        // string path = "S_BLLDDDUUS_BS_BUS_BRR"; //3299 119
        // string path = "DDS_BDDUUUS_BS_BUS_BLR"; //3299 119
        // string path = "S_BS_BDDDDUS_BUUUS_BLR"; //3239 119 (62 00f0)
        // string path = "S_BS_BLS_BLRRDDDUUS_BU"; //3239 119 (60 feee)
        // string path = "S_BS_BLS_BLRRDDUULS_BR"; //3239 119 (60 feee)
        RbyIGTChecker<Red>.CheckIGT("basesaves/red/manip/clefairybuf.gqs", new RbyIntroSequence(RbyStrat.NoPal), path, "CLEFAIRY", 3600, true, null, false, 0, 1, 16, false);

        // string path = "S_BS_BADDDDDDUAUS_BUAUUU"; //3360 60
        // string path = "S_BS_BADDDDDDUAUS_BAUUUU"; //3360 60
        // string path = "S_BS_BADDDDDDAUUS_BUAUUU"; //3360 60
        // string path = "S_BS_BADDDDDUUUS_BUAULAR"; //3360 60
        // string path = "S_BS_BADDDDDUUAUS_BUAULR"; //3360 60
        // string path = "S_BS_BADDDDDUUAUS_BAUULR"; //3360 60
        // string path = "DULLRRADDUS_BUALS_BS_BAR"; //3360 60
        // string path = "DULLRRADLUS_BRADS_BS_BAU"; //3360 60
        // string path = "DULLRRADLUS_BRALS_BS_BAR"; //3360 60
        // string path = "DULLRRADDDS_BUAUS_BS_BAU"; //3302 60 (59 geodude)
        // RbyIGTChecker<Red>.CheckIGT("basesaves/red/manip/clefairybuf.gqs", new RbyIntroSequence(RbyStrat.NoPalAB), path, "CLEFAIRY", 3600, true, null, true,  0, 1, 16, false);
    }

    public static void Search(int numThreads = 1, int numFrames = 1, int success = -1)
    {
        StartWatch();
        RbyIntroSequence intro = new RbyIntroSequence(RbyStrat.PalHold);

        Red[] gbs = MultiThread.MakeThreads<Red>(numThreads);
        Red gb = gbs[0];
        Elapsed("threads");

        IGTResults states = new IGTResults(numFrames);
        gb.LoadState("basesaves/red/manip/clefairy.gqs");
        gb.HardReset();
        intro.ExecuteUntilIGT(gb);
        byte[] igtState = gb.SaveState();

        MultiThread.For(states.Length, gbs, (gb, f) =>
        {
            gb.LoadState(igtState);
            gb.CpuWrite("wPlayTimeMinutes", 8);
            gb.CpuWrite("wPlayTimeSeconds", (byte)(f / 60));
            gb.CpuWrite("wPlayTimeFrames", (byte)(f % 60));
            intro.ExecuteAfterIGT(gb);

            states[f]=new IGTState(gb, false, f);
        });
        Elapsed("states");

        RbyMap moon = gb.Maps[61];
        Action actions = Action.Right | Action.Down | Action.Up | Action.Left | Action.A | Action.StartB;
        RbyTile startTile = gb.Tile;
        RbyTile[] endTiles = { moon[9, 21] };
        RbyTile[] blockedTiles = { moon[7, 20], moon[8, 20], moon[9, 20], moon[10, 21], moon[10, 22], moon[10, 23], moon[10, 24], moon[10, 25], moon[10, 26], moon[11, 26] };
        Pathfinding.GenerateEdges<RbyMap,RbyTile>(gb, 0, endTiles.First(), actions, blockedTiles);
        Pathfinding.DebugDrawEdges(gb, moon, 0);

        var parameters = new DFParameters<Red,RbyMap,RbyTile>()
        {
            MaxCost = 400,
            SuccessSS = success > 0 ? success : Math.Max(1, states.Length - 5),
            EncounterCallback = gb =>
            {
                return gb.EnemyMon.Species.Name == "CLEFAIRY"
                    && gb.EnemyMon.DVs.Attack >= 14 && gb.EnemyMon.DVs.Defense >= 14 && gb.EnemyMon.DVs.Speed >= 14 && gb.EnemyMon.DVs.Special >= 14;
            },
            // FoundCallback = state =>
            // {
            //     Trace.WriteLine(startTile.PokeworldLink + "/" + state.Log + " Captured: " + state.IGT.TotalSuccesses + " Failed: " + (state.IGT.TotalFailures - state.IGT.TotalOverworld) + " NoEnc: " + state.IGT.TotalOverworld + " Cost: " + state.WastedFrames);
            // },
            SingleCallback = (state,gb) =>
            {
                Trace.WriteLine(startTile.PokeworldLink + "/" + state.Log + "  " + gb.EnemyMon.Species.Name + " L" + gb.EnemyMon.Level + " DVs: " + gb.EnemyMon.DVs.ToString() + " Cost: " + state.WastedFrames);
            }
        };

        // DepthFirstSearch.StartSearch(gbs, parameters, startTile, 0, states);
        DepthFirstSearch.SingleSearch(gb, parameters, startTile, 0, states[0]);
        Elapsed("search");
    }
}
