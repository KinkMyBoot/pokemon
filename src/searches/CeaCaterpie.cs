using System.Linq;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using static SearchCommon;
using static RbyIGTChecker<Blue>;

class CeaCaterpie
{
    const string State = "basesaves/blue/manip/caterpie.gqs";

    public static void Check()
    {
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
        CheckIGT(State, intro, path, "CATERPIE", 60, false);
    }

    public static void CheckFile()
    {
        RbyIntroSequence intro = new RbyIntroSequence(RbyStrat.NoPal);
        Paths paths = new Paths();
        foreach(string line in System.IO.File.ReadAllLines("paths.txt"))
        {
            string path = Regex.Match(line, @"/([LRUDSA_B]+) ").Groups[1].Value;
            Trace.WriteLine(path);
            paths.Add(new Path(path, CheckIGT(State, intro, path, "CATERPIE", 60, false, false, Verbosity.Summary)));
        }
        paths.PrintAll("https://gunnermaniac.com/pokeworld?local=51#21/43/");
    }

    public static void Search(int numThreads = 16, int numFrames = 16, int success = 16)
    {
        StartWatch();
        RbyIntroSequence intro = new RbyIntroSequence(RbyStrat.NoPal);

        Blue[] gbs = MultiThread.MakeThreads<Blue>(numThreads);
        Blue gb = gbs[0];
        if(numThreads == 1) gb.Record("test");

        gb.LoadState(State);
        IGTResults states = Blue.IGTCheckParallel(gbs, intro, numFrames);

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
            TileCallbacks = new (Tile<RbyMap, RbyTile>, Action<Blue>)[] { (forest[25, 12], gb => gb.PickupItem()) },
            // EncounterCallback = gb => gb.Tile.X < 4 && gb.EnemyMon.Species.Name == "CATERPIE" && gb.Yoloball(),
            EncounterCallback = gb => gb.Tile == endTiles[0] && gb.EnemyMon.Species.Name == "CATERPIE" && gb.Yoloball(),
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
