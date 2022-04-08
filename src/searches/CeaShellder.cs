using System.Linq;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using static SearchCommon;
using static RbyIGTChecker<Blue>;

class CeaShellder
{
    const string State = "basesaves/blue/manip/shellderbike.gqs";
    const string TargetPoke = "PSYDUCK";

    public static void Check()
    {
        // string path = "UUL", intro = "nopal_gfskip_hop0_title0_cont_cont"; // SHELLDER
        // string path = "LUUUUUUUU" + "RRRDS_BDD", intro = "nopal_gfskip_hop0_title0_cont_cont"; // SEEL
        // string path = "LUUUUUUUUDAD", intro = "pal_gfskip_hop0_title0_cont_cont"; // PSYDUCK
        string path = "LUS_BUUUAUUUAU", intro = "nopal(ab)_gfskip_hop0_title0_cont_cont"; // PSYDUCK
        // string path = "LUAUUUUUAUU", intro = "nopal(ab)_gfskip_hop0_title0_cont_backout_cont_cont"; // KRABBY
        // string path = "LUAUUUU", intro = "nopal_gfskip_hop0_title0_cont_backout_cont_cont"; // STARYU
        // string path = "LUAUUUUAUU", intro = "pal_gfskip_hop2_title0_cont_backout_cont_cont"; // KINGLER
        // string path = "LUUUAUUUAU", intro = "pal_gfskip_hop2_title0_cont_backout_cont_cont"; // DEWGONG
        CheckIGT(new CheckIGTParameters() { StatePath = State, Intro = new RbyIntroSequence(intro), Path = path, TargetPoke = TargetPoke, MemeBall = gb => true, NumFrames = 60 });
        // Record<Blue>(TargetPoke.ToLower(), State, new RbyIntroSequence(intro), path);
        Record<Blue>(TargetPoke.ToLower(), State, new RbyIntroSequence(intro), path.Substring(0, 1), path.Substring(1));
    }

    public static void Search()
    {
        for(RbyStrat pal = RbyStrat.NoPal; pal <= RbyStrat.PalHold; ++pal)
            for(RbyStrat gf = RbyStrat.GfSkip; gf <= RbyStrat.GfSkip; ++gf)
                for(RbyStrat hop = RbyStrat.Hop0; hop <= RbyStrat.Hop2; ++hop)
                    for(int backouts = 0; backouts <= 2; ++backouts)
                        Search(new RbyIntroSequence(pal, gf, hop, backouts), 60, 60, 56);
    }

    public static void Search(RbyIntroSequence intro, int numThreads = 16, int numFrames = 16, int success = 15)
    {
        StartWatch();

        Blue[] gbs = MultiThread.MakeThreads<Blue>(numThreads);
        Blue gb = gbs[0];
        if(numThreads == 1) gb.Record("test");

        IGTResults states = new IGTResults(numFrames);
        gb.LoadState(State);
        gb.HardReset();
        intro.ExecuteUntilIGT(gb);
        byte[] igtState = gb.SaveState();

        // int[] framesToSearch = {22, 36, 37};
        // int[] framesToSearch = {22, 23, 36};

        MultiThread.For(states.Length, gbs, (gb, it) =>
        {
            int f = it;
            // f = framesToSearch[it];
            gb.LoadState(igtState);
            gb.CpuWrite("wPlayTimeSeconds", (byte) (f / 60));
            gb.CpuWrite("wPlayTimeFrames", (byte) (f % 60));
            intro.ExecuteAfterIGT(gb);

            // gb.Execute(SpacePath("LUUUUUSUUU"));

            states[it] = new IGTState(gb, false, f);
        });

        int baseCost = (int) (gb.EmulatedSamples / GameBoy.SamplesPerFrame - 902);

        RbyMap b2f = gb.Maps[160];
        RbyMap b3f = gb.Maps[161];
        Action actions = Action.Right | Action.Down | Action.Up | Action.Left | Action.A | Action.StartB;
        RbyTile startTile = gb.Tile;
        RbyTile[] endTiles = { b3f[25, 6] };
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, endTiles[0], actions);
        b2f[26, 14].RemoveEdge(0, Action.Up);
        b3f[25, 14].RemoveEdge(0, Action.StartB);
        b3f[25, 14].RemoveEdge(0, Action.A);
        b3f[25, 14].RemoveEdge(0, Action.Left);

        var results = new Dictionary<string, int>();
        var parameters = new DFParameters<Blue, RbyMap, RbyTile>()
        {
            MaxCost = 10,
            SuccessSS = success,
            EncounterCallback = gb =>
            {
                return gb.EnemyMon.Species.Name == TargetPoke;// && gb.EnemyMon.Level > 32;
            },
            FoundCallback = state =>
            {
                foreach((string path, int success) in results)
                    if(state.Log.StartsWith(path) && state.IGT.TotalSuccesses == success)
                        return;
                results.Add(state.Log, state.IGT.TotalSuccesses);
                Trace.WriteLine("https://gunnermaniac.com/pokeworld?local=161#26/14/" + state.Log + " " + state.IGT.TotalSuccesses + "/" + numFrames + " " + state.WastedFrames + " " + intro.ToString() + " " + baseCost);
            }
        };

        DepthFirstSearch.StartSearch(gbs, parameters, startTile, 0, states);
        Elapsed("search");
    }
}
