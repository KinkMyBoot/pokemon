using System.Linq;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using static SearchCommon;
using static RbyIGTChecker<Blue>;

class CeaPidgeotto
{
    const string State = "basesaves/blue/manip/pidgeotto.gqs";
    const string TargetPoke = "RATTATA";

    public static void Check()
    {
        string path = "RR", intro = "nopal(ab)_gfskip_hop2_title0_cont_cont"; // RATTATA
        // string path = "R", intro = "nopal_gfwait_hop0_title0_cont_backout_cont_backout_cont_cont"; // RATICATE
        CheckIGT(new CheckIGTParameters() { StatePath = State, Intro = new RbyIntroSequence(intro), Path = path, TargetPoke = TargetPoke, MemeBall = gb => true, NumFrames = 60 });
        Record<Blue>(TargetPoke.ToLower(), State, new RbyIntroSequence(intro), path);
    }

    public static void Search()
    {
        for(RbyStrat pal = RbyStrat.NoPal; pal <= RbyStrat.PalHold; ++pal)
            for(RbyStrat gf = RbyStrat.GfSkip; gf <= RbyStrat.GfWait; ++gf)
                for(RbyStrat hop = RbyStrat.Hop0; hop <= RbyStrat.Hop2; ++hop)
                    for(int backouts = 0; backouts <= 2; ++backouts)
                        Search(new RbyIntroSequence(pal, gf, hop, backouts), 60, 60, 57);
    }

    public static void Search(RbyIntroSequence intro, int numThreads = 16, int numFrames = 16, int success = 15)
    {
        StartWatch();

        Blue[] gbs = MultiThread.MakeThreads<Blue>(numThreads);
        Blue gb = gbs[0];
        if(numThreads == 1) gb.Record("test");

        gb.LoadState(State);
        IGTResults states = Blue.IGTCheckParallel(gbs, intro, numFrames);

        RbyMap map = gb.Maps[32];
        Action actions = Action.Right | Action.Down | Action.Up | Action.Left | Action.A | Action.StartB;
        RbyTile startTile = map[10, 4];
        RbyTile[] endTiles = { map[13, 4] };
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, endTiles[0], actions);

        var results = new Dictionary<string, int>();
        var parameters = new DFParameters<Blue, RbyMap, RbyTile>()
        {
            MaxCost = 60,
            SuccessSS = success,
            EncounterCallback = gb => gb.EnemyMon.Species.Name == TargetPoke,
            LogStart = "https://gunnermaniac.com/pokeworld?local=32#10/4/",
            FoundCallback = state =>
            {
                foreach((string path, int success) in results)
                    if(state.Log.StartsWith(path) && state.IGT.TotalSuccesses == success)
                        return;
                results.Add(state.Log, state.IGT.TotalSuccesses);
                Trace.WriteLine(state.Log + " " + state.IGT.TotalSuccesses + "/" + numFrames + " " + state.WastedFrames + " " + intro);
            }
        };

        DepthFirstSearch.StartSearch(gbs, parameters, startTile, 0, states);
        Elapsed("search");
    }
}
