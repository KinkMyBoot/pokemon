using System.Linq;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using static SearchCommon;
using static RbyIGTChecker<Blue>;

class CeaCaterpie
{
    public static void Check()
    {
        string state = "basesaves/blue/manip/caterpie.gqs";
        // string path = "UURRRRUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUULLLLLLLLDDDDDDDLLLLLAUUUUUUUUUUUAUUALLLLDDADDDDDDDDDDDDDDLLAD"; // current 3419
        // string path = "RRRRUUUUUUUUUUUUUUUUUUUUAUUUUUUUUUUUUUULLLLLLLLDDDDDDDLLLLUUUUUUUUUUUUULLLLLLADDDDDDDDDDDDDDDDDDDALLLLLLU"; // 3419²
        string path = "RARRRUUUUUUUUUUUUUAUUUUUUUUUUUUUUUUUUUUULLLLLLLLDDDDDDDLLLLUUUUUUUUUUUUULLLLLLDDDDDDDDDDDDDDDDDLDDLLLLLUUU"; // final 3421-3480
        // string path = "RARRRUUUUUUUUUUUUUAUUUUUUUUUUUUUUUUUUUUULLLLLLLLDDDDDDDLLLLUUUUUUUUUUUUULLLLLLDDDDDDDDDDDDDLDDDDDDLLLLLUUU"; // 21-80
        // string path = "RARRRUUUUUUUUUUUUUAUUUUUUUUUUUUUUUUUUUUULLLLLLLLDDDDDDDLLLLUUUUUUUUUUUUULLLLLDDLDDDDDDDDDDDDDDDLDDLLLLLUUU"; // 21-80
        // string path = "RARRRUUUUUUUUUUUUUAUUUUUUUUUUUUUUUUUUUUULLLLLLLLDDDDDDDLLLLUUUUUUUUUUUUULLLLLDDLDDDDDDDDDDDDDDDLDDLLLLULUU"; // 20-79
        // string path = "RRUUARRUAUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUULLLLLLLLDDDDDDDLLLLUUUUUUUUUUUULLULLLLDDDDDDDDDDDDDDDDLDDDLLLLULUU"; // 17-76
        // string path = "RRUARRAUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUULLLLLLLLDDDDDDDLLLLUUUUUUUUUUUULLULLLLDDDDDDDDDDDDDDDDLDDDLLLLULUU"; // 17-76
        // string path = "RARRRUUUUUUUUUUUUUAUUUUUUUUUUUUUUUUUUUUULLLLLLLLDDDDDDDLLLLUUUUUUUUUUULUULLLLDDLDDDDDDDDDDDDDDDLDDLLLLULUU"; // 20-79
        // string path = "RARRRUUUUUUUUUUUUAUUUUUUUUUUUUUUUUUUUUUULLLLLLLLDDDDDDDLLLLUUUUUUUUUUULUULLLLDDLDDDDDDDDDDDDDDDLDDLLLLULUU"; // 20-79
        // string path = "RUURRRUAUUAUUUUUUUUUUUUUUUUUUUUUUUUUUUUULLLLLLLLDDDDDDDLLLLUUUUUUUUUUUUULLLLLLDDDDDDDDDDDDDDLDDDDADLLLLLUUU"; // 03-62
        // string path = "RRRRUUUAUUAUUUUUUUUUUUUUUUUUUUUUUAUUUUUUULLLLLLLLDDDDDDDLLLLUUUUUUUUUUUUULLLLLLDDDDDDDDDDDDDDDDDDDLLLLLLUUU"; // 3A 3420
        // string path = "URRRURUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUUULLLLLLLLDDDDDDDLALLLUUUUUUUUUUULUULLLLDLDDDDDDDDDDDDDDLDDDDLLLLULUU"; // 1A 3410
        RbyIntroSequence intro = new RbyIntroSequence(RbyStrat.NoPal);
        var antidote = new List<(int, byte, byte)> { (51, 25, 12) };
        CheckIGT(state, intro, path, "CATERPIE", 60, false, antidote);
    }

    public static void CheckFile()
    {
        string state = "basesaves/blue/manip/caterpie.gqs";
        RbyIntroSequence intro = new RbyIntroSequence(RbyStrat.NoPal);
        var antidote = new List<(int, byte, byte)> { (51, 25, 12) };
        string[] lines = File.ReadAllLines("paths.txt");
        List<Display> display = new List<Display>();
        foreach(string line in lines)
        {
            string path = Regex.Match(line, @"/([LRUDSA_B]+) ").Groups[1].Value;
            Trace.WriteLine(path);
            int success = CheckIGT(state, intro, path, "CATERPIE", 60, false, antidote, false, 0, 1, 16, Verbosity.Summary);
            display.Add(new Display(path, success));
        }
        Display.PrintAll(display, "https://gunnermaniac.com/pokeworld?local=51#21/43/");
    }

    public static void Search(int numThreads = 16, int numFrames = 16, int success = 16)
    {
        StartWatch();
        RbyIntroSequence intro = new RbyIntroSequence(RbyStrat.NoPal);

        Blue[] gbs = MultiThread.MakeThreads<Blue>(numThreads);
        Blue gb = gbs[0];
        if(numThreads == 1) gb.Record("test");
        Elapsed("threads");

        IGTResults states = new IGTResults(numFrames);
        gb.LoadState("basesaves/blue/manip/caterpie.gqs");
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

        RbyMap forest = gb.Maps[51];
        forest.Sprites.Remove(25, 11);
        Action actions = Action.Right | Action.Down | Action.Up | Action.Left | Action.A | Action.StartB;
        RbyTile startTile = gb.Tile;
        RbyTile[] endTiles = { forest[1, 19] };
        RbyTile[] blockedTiles = {
            forest[26, 12],
            forest[16, 10], forest[18, 10],
            forest[16, 15], forest[18, 15],
            forest[11, 15], forest[12, 15],
            forest[11, 6], forest[12, 6],
            forest[6, 6], forest[8, 6],
            forest[6, 15], forest[8, 15],
            forest[2, 19], forest[1, 18]
        };
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, endTiles[0], actions, blockedTiles);
        forest[25, 12].RemoveEdge(0, Action.A);
        forest[25, 13].RemoveEdge(0, Action.A);

        var parameters = new DFParameters<Blue, RbyMap, RbyTile>()
        {
            MaxCost = 6,
            SuccessSS = success,
            EndTiles = endTiles,
            TileCallback = (forest[25, 12], gb =>
                gb.PickupItem()
            ),
            EncounterCallback = gb =>
            {
                // return gb.Tile.X < 4 && gb.EnemyMon.Species.Name == "CATERPIE" && gb.Yoloball();
                return gb.Tile == endTiles[0] && gb.EnemyMon.Species.Name == "CATERPIE" && gb.Yoloball();
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
