using System.Linq;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using static SearchCommon;
using static RbyIGTChecker<Red>;
using System.Text;
using System.Threading.Tasks;

class nuntuple
{
    static SortedSet<int> IgnoredFrames = new SortedSet<int> { 11, 12, 27, 33, 34, 35, 36, 37 };
    static bool CheckEncounter(int address, Red gb, string pokename, IGTResult res)
    {
        if (address != gb.WildEncounterAddress)
            return false;

        res.Mon = gb.EnemyMon;
        if (res.Mon.Species.Name != pokename)
            return false;

        res.Yoloball = gb.Yoloball(0, Joypad.B);
        return res.Yoloball;
    }

    static bool CheckNoEncounter(int address, Red gb, IGTResult res)
    {
        if (address != gb.WildEncounterAddress)
            return true;

        res.Mon = gb.EnemyMon;
        return false;
    }
    const string State = "basesaves/red/manip/nido.gqs";
    public static void BuildStates(List<byte> stats = null)
    {
        if (System.IO.File.Exists("basesaves/red/manip/nun/nuntuple_0_0.gqs"))
            return;

        System.IO.Directory.CreateDirectory("basesaves/red/manip/nun");
        const int numThreads = 12;
        RbyIntroSequence intro = new RbyIntroSequence(RbyStrat.NoPal);
        Red[] gbs = MultiThread.MakeThreads<Red>(numThreads);
        Red gb = gbs[0];
        if (numThreads == 1) gb.Record("test");

        gb.LoadState("basesaves/red/manip/nido.gqs");
        gb.HardReset();
        intro.ExecuteUntilIGT(gb);

        byte[] igtState = gb.SaveState();

        const int numFrames = 3600;
        MultiThread.For(numFrames, gbs, (gb, f) =>
        {
            if ((f + 1) * 100 / numFrames > f * 100 / numFrames) Console.WriteLine("%");

            gb.LoadState(igtState);
            byte sec = (byte)(f / 60);
            byte frame = (byte)(f % 60);


            gb.CpuWrite("wPlayTimeMinutes", 7);
            gb.CpuWrite("wPlayTimeSeconds", sec);
            gb.CpuWrite("wPlayTimeFrames", frame);
            intro.ExecuteAfterIGT(gb);
            if (stats != null && stats.Count == 5)
            {
                gb.CpuWriteBE<ushort>("wPartyMon1HP", stats[0]);
                gb.CpuWriteBE<ushort>("wPartyMon1MaxHP", stats[0]);
                gb.CpuWriteBE<ushort>("wPartyMon1Attack", stats[1]);
                gb.CpuWriteBE<ushort>("wPartyMon1Defense", stats[2]);
                gb.CpuWriteBE<ushort>("wPartyMon1Speed", stats[3]);
                gb.CpuWriteBE<ushort>("wPartyMon1Special", stats[4]);
            }
            string nidopath = "LLLULLUAULALDLDLLDADDADLALLALUUAU";
            int ret;
            ret = gb.Execute(SpacePath(nidopath));

            if (!CheckEncounter(ret, gb, "NIDORANM", new IGTResult()))
                return;

            gb.ClearText(Joypad.B);
            gb.Press(Joypad.A);
            gb.RunUntil("_Joypad");
            gb.AdvanceFrame();
            //gb.SaveState("basesaves/red/manip/nun/nuntuple" + sec + "_" + frame + ".gqs");
            gb.AdvanceFrames(2); // chooses path 2
            gb.Press(Joypad.A);
            gb.Press(Joypad.Start);

            int ret2 = gb.Execute(SpacePath("DRRUUURRRRRRRRRRRRRRRRRRRRRURUUUUUURUUAUULUUUAUUUUUUUUUUUUUUAUULLLUUUUUUURRRRUAUUU"));

            if (!CheckEncounter(ret2, gb, "PIDGEY", new IGTResult()))
            {
                return;
            }

            gb.ClearText(Joypad.A);
            gb.Press(Joypad.B);

            gb.SaveState("basesaves/red/manip/nun/nuntuple_" + sec + "_" + frame + ".gqs");

        });
    }

    public static List<DFState<RbyMap, RbyTile>> PruneForest(string path = null, int numThreads = 12, int numFrames = 54, int success = -1, int maxcost = 10, List<byte> stats = null, int r1damage = 0, int minClusterSize = 3)
    {
        BuildStates(stats);
        StartWatch();

        Red[] gbs = MultiThread.MakeThreads<Red>(numThreads);
        Red gb = gbs[0];
        if (numThreads == 1)
            gb.Record("test");
        Elapsed("threads");

        IGTResults states = new IGTResults(numFrames);
        MultiThread.For(states.Length, gbs, (gb, i) =>
        {
            int f = i;
            for (int s = 0; s < 60; ++s)
                foreach (int skip in IgnoredFrames)
                    if (f >= skip + 60 * s)
                        ++f;

            gb.LoadState("basesaves/red/manip/pext/preFpidgey_" + (f / 60) + "_" + (f % 60) + ".gqs");
            if (path != null)
            {
                int ret = gb.Execute(SpacePath(path));
            }

            states[i] = new IGTState(gb, false, f);
        });
        Elapsed("states");

        RbyMap viridian = gb.Maps[1];
        RbyMap route2 = gb.Maps[13];
        RbyMap gate = gb.Maps[50];
        RbyMap forest = gb.Maps[51];
        forest.Sprites.Remove(25, 11);
        Action actions = Action.Right | Action.Left | Action.Up | Action.Down | Action.A | Action.StartB;
        //viridian.Sprites.Remove(18, 9);
        //viridian.Sprites.Remove(17, 5);
        RbyTile startTile = gb.Tile;
        RbyTile[] endTiles = { gate[5, 1] };



        RbyTile[] blockedTiles = {
            forest[26, 12],
            forest[2, 19]};
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, endTiles[0], actions, blockedTiles);
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, gate[5, 1], actions, blockedTiles);
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, route2[3, 44], actions, blockedTiles);

        forest[1, 19].RemoveEdge(0, Action.A);
        forest[1, 20].RemoveEdge(0, Action.A);
        forest[25, 12].RemoveEdge(0, Action.A);
        forest[25, 13].RemoveEdge(0, Action.A);
        route2[3, 44].AddEdge(0, new Edge<RbyMap, RbyTile>() { Action = Action.Up, NextTile = gate[4, 7], NextEdgeset = 0, Cost = 0 });
        gate[5, 1].AddEdge(0, new Edge<RbyMap, RbyTile>() { Action = Action.Up, NextTile = forest[17, 47], NextEdgeset = 0, Cost = 0 });

        //Pathfinding.DebugDrawEdges(gb,forest, 0);
        //Pathfinding.DebugDrawEdges(gb, gate, 0);
        //Pathfinding.DebugDrawEdges(gb, forest, 0);
        var results = new List<DFState<RbyMap, RbyTile>>();
        var parameters = new DFParameters<Red, RbyMap, RbyTile>()
        {
            MaxCost = maxcost,
            SuccessSS = success >= 0 ? success : Math.Max(1, states.Length - 3),// amount of yoloball success for found
            EndTiles = endTiles,
            TileCallbacks = new (Tile<RbyMap, RbyTile>, Action<Red>)[] { (forest[25, 12], gb => gb.PickupItem()), (forest[1, 19], gb => gb.PickupItem()) },
            FoundCallback = state =>
            {
                if (state.Log.Length == 19)
                {
                    Trace.WriteLine(state.Log);
                }

                //Elapsed("checking");
                /*var fightResults = FullCheck(path+state.Log,stats:stats ,r1damage:r1damage, minClusterSize:minClusterSize, numThreads:12);
                //Trace.WriteLine(startTile.PokeworldLink + "/" + state.Log);
                int count =0;
                StringBuilder trace = new StringBuilder();
                trace.AppendLine(startTile.PokeworldLink + "/" + state.Log);
                for(int i = 0;i<60;i++){
                    if(IgnoredFrames.Contains(i)){continue;}
                    int successcount = 0;
                    int totalDmg=0;
                    foreach(var res in fightResults.Where(res => res.IGTFrame == i)){
                        successcount++;
                        totalDmg+=res.dmgTaken.Sum();
                    }     
                    float avgDamage = (float)totalDmg / ((r1damage+1)*minClusterSize);
                    if(successcount>0){
                        count++;                        
                        trace.AppendLine("Frame: " + i +" Success: " + successcount + "/1 IGT seconds. Average Damage: " + avgDamage);
                    }          
                    }
                if (count >= 1){                    
                    Trace.WriteLine(trace.ToString());
                }
                
                //Trace.WriteLine(startTile.PokeworldLink + "/" + state.Log);*/
            }
        };

        DepthFirstSearch.StartSearch(gbs, parameters, startTile, 0, states, 0);
        Elapsed("search");

        return results;
    }


    public static List<DFState<RbyMap, RbyTile>> SearchForest(string path = null, int numThreads = 12, int numFrames = 1,
        int success = -1, int maxcost = 10, List<byte> stats = null, int r1damage = 0 , int minClusterSize = 3, int igtFrameCluster = 5, int offset60fps = 0, int targetFrame = 7)
    {
        BuildStates(stats);
        StartWatch();
        Red[] gbs = MultiThread.MakeThreads<Red>(numThreads);
        Red gb = gbs[0];
        if (numThreads == 1)
            //gb.Record("test");
        Elapsed("threads");
        IGTResults states = new IGTResults(numFrames);

        MultiThread.For(states.Length, gbs, (gb, i) =>
        {
            int f = i;
            for (int s = 0; s < 60; ++s)
                foreach (int skip in IgnoredFrames)
                    if (f >= skip + 60 * s)
                        ++f;

            gb.LoadState("basesaves/red/manip/nun/nuntuple" + (f + 7 / 60) + "_" + (f + 7 % 60) + ".gqs");
            if (path != null)
            {
                int ret = gb.Execute(SpacePath(path));
            }

            states[i] = new IGTState(gb, false, f);
        });




        Elapsed("states");

        RbyMap viridian = gb.Maps[1];
        RbyMap route2 = gb.Maps[13];
        RbyMap gate = gb.Maps[50];
        RbyMap forest = gb.Maps[51];
        //forest.Sprites.Remove(25, 11);
        Action actions = Action.Right | Action.Left | Action.Up | Action.Down | Action.A | Action.StartB;
        //viridian.Sprites.Remove(18, 9);
        //viridian.Sprites.Remove(17, 5);
        RbyTile startTile = gb.Tile;
        RbyTile[] endTiles = { forest[2, 19] };



        RbyTile[] blockedTiles = {
            forest[18, 12],
            forest[16, 13],
            forest[12, 10],
            forest[11, 8],
            forest[11, 9],
            //forest[8, 8],
            //forest[6, 19]
            };
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, endTiles[0], actions, blockedTiles);
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, gate[5, 1], actions, blockedTiles);
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, route2[3, 44], actions, blockedTiles);

        forest[1, 19].RemoveEdge(0, Action.A);
        forest[1, 20].RemoveEdge(0, Action.A);
        forest[25, 12].RemoveEdge(0, Action.A);
        forest[25, 13].RemoveEdge(0, Action.A);
        route2[3, 44].AddEdge(0, new Edge<RbyMap, RbyTile>() { Action = Action.Up, NextTile = gate[4, 7], NextEdgeset = 0, Cost = 0 });
        gate[5, 1].AddEdge(0, new Edge<RbyMap, RbyTile>() { Action = Action.Up, NextTile = forest[17, 47], NextEdgeset = 0, Cost = 0 });
        bool gotThroughGrass = false;
        Pathfinding.DebugDrawEdges(gb, forest, 0);

        ManualResetEventSlim pauseSearch = new ManualResetEventSlim(true);

        //Pathfinding.DebugDrawEdges(gb, gate, 0);
        //Pathfinding.DebugDrawEdges(gb, forest, 0);
        var results = new List<DFState<RbyMap, RbyTile>>();
        var parameters = new DFParameters<Red, RbyMap, RbyTile>()
        {
            MaxCost = maxcost,
            SuccessSS = success >= 0 ? success : Math.Max(1, states.Length - 3),// amount of yoloball success for found
            EndTiles = endTiles,
            PauseSearch = pauseSearch,
            TileCallbacks = new (Tile<RbyMap, RbyTile>, Action<Red>)[] { },
            FoundCallback = state =>
            {
                //Console.WriteLine("1");
                //Elapsed("checking: ");
                //gotThroughGrass = true;
                //pauseSearch.Reset();
                
                CheckClusterFight(
                path + state.Log,
                stats: stats,
                r1damage: r1damage,
                minClusterSize: minClusterSize,
                numThreads: 5,
                offset60fps: offset60fps,
                targetFrame: targetFrame);
                
                
            }
        };

        //DepthFirstSearch.StartSearch(gbs, parameters, startTile, 0, states, 0);

        foreach (var gbInstance in gbs)
        {
            gbInstance.Dispose();
        }

        if (!gotThroughGrass)
        {
            //Trace.WriteLine("Bad prune: " + path);
        }

        //Elapsed("search");
        return results;
    }

    public static void Check()
    {
        string path;
        RbyStrat pal;

        //path = "UUUUUUUUUUUAUUUULLLUUUUURUUUURRR"; pal = RbyStrat.Pal;
        //CheckIGT(State, new RbyIntroSequence(pal), path, "PIDGEY", 3600, verbose:Verbosity.Summary);
        //path = "UUUUUUUUUUUAUUUULLULUUUURUUUURRR"; pal = RbyStrat.Pal;
        //CheckIGT(State, new RbyIntroSequence(pal), path, "PIDGEY", 3600, verbose:Verbosity.Summary);
        //path = "UUUUUUUUAUUUUUUULLULUUUURUUUURRR"; pal = RbyStrat.Pal; // 57/60 
        //string forest = "UUUULLLLLUUUUUUURUUUUURRRRRRRRUUUUUUAUUUUUUUUUUUUUUUUUUUUUUUUUUUULLALLALLLLDDDDDDDLLLLUUUUUUUUUUUUULLLLLLDDDDDDDDDDDDDDDDDDDALLLLLLUUU";
        string forest = "UUULALLLLUUUUUUUURUUUUURRRURURRRRUUUUUUUUUUUUUUUUUUUUUUUUUUURUUUUULLLLLLLLLDDDDDDDLLLLUUUUULULUUUUUUULLLDDDDDLDDDDDDDDDDDDDDLLLLLUUU";
        //BuildStates();
        //RedCb[] gbs = MultiThread.MakeThreads<RedCb>(1);
        //gbs[0].Record("test");
        var stat = new List<byte> { 22, 12, 12, 10, 11 };
        var r1dmg = new List<int> { 0, 2, 3, 4, 5, 6, 9 };
        BuildStates(stat);

        int igtFrameCluster = 5;
        int r1damage = 0;
        int offset = 1;

        var fightResults = FullCheck(forest, stats: stat, r1damage: r1damage, minClusterSize: 2, numThreads: 12, offset60fps: offset);
        //Trace.WriteLine(startTile.PokeworldLink + "/" + state.Log);

        StringBuilder trace = new StringBuilder();
        trace.AppendLine("https://gunnermaniac.com/pokeworld?local=51#21/59/" + forest);
        List<int> goodFrames = new List<int>();

        for (int i = 0; i < 60; i++)
        {
            if (IgnoredFrames.Contains(i)) { continue; }
            //int successcount = 0;
            //int totalDmg=0;
            foreach (var res in fightResults.Where(res => res.IGTFrame == i))
            {
                //successcount++;
                goodFrames.Add(i);
                //totalDmg+=res.dmgTaken.Sum();
            }
            //float avgDamage = (float)totalDmg / ((r1damage+1)*minClusterSize);
            //if(successcount>0){
            //count++;
            //trace.AppendLine("Frame: " + i +" Success: " + successcount + "/1 IGT seconds. Average Damage: ");
            //}          
        }
        if (goodFrames.Count > 0)
        {
            int count = 1;
            List<(int, int)> intPairs = new List<(int, int)>();
            for (int i = 1; i < goodFrames.Count; i++)
            {
                if (goodFrames[i] == goodFrames[i - 1] + 1)
                {
                    count++; // Increase count if consecutive     
                    if (count == goodFrames.Count && count >= igtFrameCluster)
                    {
                        intPairs.Add((count, goodFrames[i] + 1));
                    }
                }
                else if (goodFrames[i] != goodFrames[i - 1])
                {
                    if (count >= igtFrameCluster)
                    {
                        intPairs.Add((count, goodFrames[i - 1] + 1));
                    }
                    count = 1; // Reset count if non-consecutive
                }
            }
            List<int> targetCluster = new List<int>();
            foreach ((int, int) rawcluster in intPairs)
            {
                for (int i = rawcluster.Item2 - rawcluster.Item1; i < rawcluster.Item2; i++)
                {
                    targetCluster.Add(i);
                }
            }
            if (targetCluster.Count >= igtFrameCluster)
            {
                var igtSecResults = FullCheck(forest, stats: stat, r1damage: r1damage, minClusterSize: 2, numThreads: 12, targetFrames: targetCluster, numFrames: 3600, offset60fps: offset);
                foreach (int i in targetCluster)
                {
                    if (IgnoredFrames.Contains(i)) { continue; }
                    int successcount = 0;
                    int totalDmg = 0;
                    foreach (var res in igtSecResults.Where(res => res.IGTFrame == i))
                    {
                        successcount++;
                        totalDmg += res.dmgTaken.Sum();
                    }
                    float avgDamage = (float)totalDmg / (3 * successcount);
                    if (successcount >= 0)
                    {
                        count++;
                        trace.AppendLine("Frame: " + i + " Success: " + successcount + "/60 IGT seconds. Average Damage: " + avgDamage);
                    }
                }

                Trace.WriteLine(trace.ToString());

                //Trace.WriteLine(startTile.PokeworldLink + "/" + state.Log);
            }
        }

    }

    public static void Search()
    {
        var stat = new List<byte> { 22, 12, 12, 10, 11 };
        var r1dmg = 0; // 0, 2, 3, 4, 5, 6, 9;
        string[] lines = File.ReadAllLines("catch2gatenun.txt");

        foreach (string line in lines)
        {
            if (!line.StartsWith("#"))
            {
                
                SearchForest(
                    stats: stat,
                    r1damage: r1dmg,
                    minClusterSize: 2,
                    path: line,
                    maxcost: 0,
                    igtFrameCluster: 5,
                    offset60fps: 1,
                    targetFrame: 7,
                    numThreads: 12
                );
                Trace.WriteLine($"Done with prune: {line}");
                    
                // Wait for all r1dmg searches for this line
            }
        }
    }
    static public void CheckClusterFight(
    string path,
    List<byte> stats,
    int r1damage,
    int minClusterSize,
    int numThreads,
    int offset60fps,
    int targetFrame)
    {
        // Build the 5-frame cluster, clamped to 0–59 and skipping ignored frames
        List<int> cluster = new List<int>();
        for (int i = targetFrame - 2; i <= targetFrame + 2; i++)
        {
            int frame = ((i % 60) + 60) % 60; // wrap around
            if (!IgnoredFrames.Contains(frame) && !cluster.Contains(frame))
                cluster.Add(frame);
        }

        // First pass: check 60 frames, only cluster
        var quickResults = FullCheck(
            path,
            stats: stats,
            r1damage: r1damage,
            minClusterSize: minClusterSize,
            numThreads: numThreads,
            targetFrames: cluster,
            numFrames: 60,
            offset60fps: offset60fps
        );

        // If all cluster frames have at least one success, do the full check
        bool allGood = cluster.All(f => quickResults.Any(res => res.IGTFrame == f && res.Success));
        if (!allGood)
            return;

        // Second pass: full data for cluster over 3600 frames
        var fullResults = FullCheck(
            path,
            stats: stats,
            r1damage: r1damage,
            minClusterSize: minClusterSize,
            numThreads: numThreads,
            targetFrames: cluster,
            numFrames: 3600,
            offset60fps: offset60fps
        );

        StringBuilder trace = new StringBuilder();
        
        trace.AppendLine("https://gunnermaniac.com/pokeworld?local=51#21/59/" + path);
        trace.AppendLine("Cluster fight results for r1damage: " + r1damage.ToString());
        foreach (int i in cluster)
        {
            int successcount = 0;
            int totalDmg = 0;
            foreach (var res in fullResults.Where(res => res.IGTFrame == i))
            {
                successcount++;
                totalDmg += res.dmgTaken.Sum();
            }
            float avgDamage = successcount > 0 ? (float)totalDmg / (minClusterSize * successcount) : 0;
            trace.AppendLine("Frame: " + i + " Success: " + successcount + "/60 IGT seconds. Average Damage: " + avgDamage);
        }

        Trace.WriteLine(trace.ToString());
    }
    public static List<IGTResult> FullCheck(string path, int numFrames = 60,
                                 bool verbose = true, int numThreads = 12, int minClusterSize = 3,
                                 List<byte> stats = null, int r1damage = 9, List<int> targetFrames = null, List<int> targetSecs = null, int offset60fps = 0)
    {
        RedCb[] gbs = MultiThread.MakeThreads<RedCb>(numThreads);
        if (numThreads == 1)
        {
            gbs[0].Record("test");
        }
        List<IGTResult> results = new List<IGTResult>();

        //Console.WriteLine( "poison: "+(int)gbs[0].SYM["PoisonEffect.inflictPoison"]);
        //Console.WriteLine( "miss: "+(int)gbs[0].SYM["MoveHitTest.moveMissed"]);
        //Console.WriteLine( "crit: "+ (int)(gbs[0].SYM["CriticalHitTest.SkipHighCritical"]+ 0xB));
        //Console.WriteLine( "menuinput: "+(int)gbs[0].SYM["HandleMenuInput"]);
        //Console.WriteLine( "enemyfaint: "+(int)gbs[0].SYM["HandleEnemyMonFainted"]);
        //Console.WriteLine( "playerfaint: "+(int)gbs[0].SYM["HandlePlayerMonFainted"]);

        MultiThread.For(numFrames, gbs, (gb, f) =>
        {
            if (verbose && numFrames >= 100 && (f + 1) * 100 / numFrames > f * 100 / numFrames) Console.WriteLine("%");



            if (IgnoredFrames.Contains(f % 60)) { return; }
            if (targetFrames != null && !targetFrames.Contains(f % 60)) { return; }
            if (targetSecs != null && !targetSecs.Contains(f / 60)) { return; }
            IGTResult res = new IGTResult();
            res.Success = false;
            res.IGTSec = (byte)(f / 60);
            res.IGTFrame = (byte)(f % 60);
            res.dmgTaken = new List<int>();
            string state = "basesaves/red/manip/nun/nuntuple_" + res.IGTSec + "_" + res.IGTFrame + ".gqs";
            for (byte dmgTaken = (byte)r1damage; dmgTaken <= r1damage; dmgTaken++)
            {


                if (System.IO.File.Exists(state)) { gb.LoadState(state); }
                else { return; }

                //var npcTracker = new NpcTracker<RedCb>(gb.CallbackHandler);
                var newhp = stats[0] - dmgTaken;
                gb.CpuWriteBE<ushort>("wPartyMon1HP", (byte)newhp);

                int address = gb.Execute(SpacePath(path));
                //res.Info = npcTracker.GetMovement((50, 2), (51, 1), (51, 8));
                res.Tile = gb.Tile;
                res.Map = gb.Map;

                if (!CheckNoEncounter(address, gb, res))
                {
                    //Console.WriteLine("encounter");
                    return; //encounter anywhere
                }
                gb.Press(Joypad.A);
                var ret = gb.RunUntil("WaitForTextScrollButtonPress");

                gb.RunUntil("Joypad");
                byte[] initialState = gb.SaveState();

                int clusterDamage = 0;
                for (byte i = 0; i < minClusterSize; i++)
                {
                    gb.LoadState(initialState);
                    gb.AdvanceFrames(i + offset60fps);
                    gb.Press(Joypad.B);
                    gb.Hold(Joypad.A, "WaitForTextScrollButtonPress");
                    gb.Press(Joypad.B);
                    gb.ClearText(Joypad.B);
                    //turn 1
                    //gb.RunUntil(gb.SYM["HandleMenuInput"]);
                    //gb.MenuPress(Joypad.A);
                    //gb.MenuPress(Joypad.Up);
                    //gb.MenuPress(Joypad.A);

                    gb.BattleMenu(0, 0);
                    gb.ChooseMenuItem(1);

                    gb.RunUntil(gb.SYM["SelectEnemyMove.done"] + 0x3);

                    if (gb.CpuRead("wEnemySelectedMove") == 40) //PS
                    {
                        var ret0 = gb.Hold(Joypad.A,
                        gb.SYM["PoisonEffect.inflictPoison"],
                        gb.SYM["MoveHitTest.moveMissed"],
                        gb.SYM["WaitForTextScrollButtonPress"],
                        gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB,
                        gb.SYM["HandlePlayerMonFainted"]
                        );

                        if (ret0 != (int)gb.SYM["WaitForTextScrollButtonPress"])
                        {
                            //Console.WriteLine("turn1 PS "+ ret0);
                            return;
                        } //PS miss crit poison, or dead, or tw miss                     
                    }
                    else
                    {
                        gb.Hold(Joypad.A, gb.SYM["WaitForTextScrollButtonPress"]);
                        gb.Press(Joypad.B);
                        var ret2 = gb.Hold(Joypad.A, gb.SYM["MoveHitTest.moveMissed"], gb.SYM["WaitForTextScrollButtonPress"]);
                        if (ret2 == (int)gb.SYM["MoveHitTest.moveMissed"])
                        {
                            //Console.WriteLine("turn1 SS "+ ret2);
                            return;
                        } //TW miss

                    }
                    gb.Press(Joypad.B);

                    //turn 2
                    gb.MenuPress(Joypad.A);
                    gb.MenuPress(Joypad.Select);
                    gb.MenuPress(Joypad.A);
                    gb.RunUntil(gb.SYM["SelectEnemyMove.done"] + 0x3);

                    if (gb.CpuRead("wEnemySelectedMove") == 40)
                    {
                        var ret0 = gb.Hold(Joypad.A, gb.SYM["PoisonEffect.inflictPoison"], gb.SYM["MoveHitTest.moveMissed"], gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB, gb.SYM["WaitForTextScrollButtonPress"], gb.SYM["HandlePlayerMonFainted"]);
                        if (ret0 != (int)gb.SYM["WaitForTextScrollButtonPress"])
                        {
                            //Console.WriteLine("turn2 PS "+ ret0);
                            return;
                        } //PS miss crit poison, or dead, or TW miss
                    }
                    else
                    {
                        gb.Hold(Joypad.A, gb.SYM["WaitForTextScrollButtonPress"]);
                        gb.Press(Joypad.B);
                        var ret2 = gb.Hold(Joypad.A, gb.SYM["MoveHitTest.moveMissed"], gb.SYM["WaitForTextScrollButtonPress"]);
                        if (ret2 == (int)gb.SYM["MoveHitTest.moveMissed"])
                        {
                            //Console.WriteLine("turn2 SS "+ ret2);
                            return;
                        } // TW miss
                    }
                    gb.Press(Joypad.B);

                    //turn 3
                    gb.MenuPress(Joypad.A);
                    gb.MenuPress(Joypad.Up);
                    gb.MenuPress(Joypad.A);
                    gb.RunUntil(gb.SYM["SelectEnemyMove.done"] + 0x3);

                    if (gb.CpuRead("wEnemySelectedMove") == 40)
                    {
                        var ret0 = gb.Hold(Joypad.B, gb.SYM["PoisonEffect.inflictPoison"], gb.SYM["MoveHitTest.moveMissed"], gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB, gb.SYM["HandleMenuInput"], gb.SYM["HandleEnemyMonFainted"], gb.SYM["HandlePlayerMonFainted"]);
                        if (ret0 == (int)gb.SYM["HandleEnemyMonFainted"])
                        {
                            clusterDamage += newhp - gb.CpuReadBE<ushort>(gb.SYM["wBattleMonHP"]);
                            continue; //killed him
                        }
                        if (ret0 != (int)gb.SYM["HandleMenuInput"])
                        {
                            //Console.WriteLine("turn3 PS "+ ret0);
                            return;
                        } //PS miss crit poison, or dead, or TW miss
                        if (gb.CpuReadBE<ushort>(gb.SYM["wBattleMonHP"]) <= 4)
                        {
                            //Console.WriteLine("redbar turn 3");
                            return; // redbar. Can't manip
                        }
                    }
                    else
                    {
                        gb.Hold(Joypad.A, gb.SYM["WaitForTextScrollButtonPress"]);
                        gb.Press(Joypad.B);
                        var ret2 = gb.Hold(Joypad.B, gb.SYM["MoveHitTest.moveMissed"], gb.SYM["HandleMenuInput"], gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB, gb.SYM["HandleEnemyMonFainted"]);
                        if (ret2 == (int)gb.SYM["HandleEnemyMonFainted"])
                        {
                            clusterDamage += newhp - gb.CpuReadBE<ushort>(gb.SYM["wBattleMonHP"]);
                            continue; //killed him
                        }
                        if (ret2 != (int)gb.SYM["HandleMenuInput"])
                        {
                            //Console.WriteLine("turn3 SS "+ ret2);
                            return;
                        }
                        // tackle crit or miss
                    }

                    //turn 4
                    gb.MenuPress(Joypad.A);
                    gb.MenuPress(Joypad.Select);
                    gb.MenuPress(Joypad.A);
                    gb.RunUntil(gb.SYM["SelectEnemyMove.done"] + 0x3);

                    if (gb.CpuRead("wEnemySelectedMove") == 40)
                    {
                        var ret0 = gb.Hold(Joypad.B, gb.SYM["PoisonEffect.inflictPoison"], gb.SYM["MoveHitTest.moveMissed"], gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB, gb.SYM["HandleMenuInput"], gb.SYM["HandleEnemyMonFainted"], gb.SYM["HandlePlayerMonFainted"]);
                        if (ret0 == (int)gb.SYM["HandleEnemyMonFainted"])
                        {
                            clusterDamage += newhp - gb.CpuReadBE<ushort>(gb.SYM["wBattleMonHP"]);
                            continue; //killed him
                        }
                        if (ret0 != (int)gb.SYM["HandleMenuInput"])
                        {
                            //Console.WriteLine("turn4 PS "+ ret0);
                            return;
                        } //PS miss crit poison, or dead, or TW miss
                        if (gb.CpuReadBE<ushort>(gb.SYM["wBattleMonHP"]) <= 4)
                        {
                            //Console.WriteLine("redbar turn4");
                            return; // redbar. Can't manip
                        }
                    }
                    else
                    {
                        gb.Hold(Joypad.A, gb.SYM["WaitForTextScrollButtonPress"]);
                        gb.Press(Joypad.B);
                        var ret2 = gb.Hold(Joypad.B, gb.SYM["MoveHitTest.moveMissed"], gb.SYM["HandleMenuInput"], gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB, gb.SYM["HandleEnemyMonFainted"]);
                        if (ret2 == (int)gb.SYM["HandleEnemyMonFainted"])
                        {
                            clusterDamage += newhp - gb.CpuReadBE<ushort>(gb.SYM["wBattleMonHP"]);
                            continue; //killed him
                        }
                        if (ret2 != (int)gb.SYM["HandleMenuInput"])
                        {
                            //Console.WriteLine("turn4 SS "+ ret2);
                            return;
                        } // tackle crit or miss
                    }

                    //turn 5
                    gb.MenuPress(Joypad.A);
                    gb.MenuPress(Joypad.Select);
                    gb.MenuPress(Joypad.A);
                    gb.RunUntil(gb.SYM["SelectEnemyMove.done"] + 0x3);
                    if (gb.CpuRead("wEnemySelectedMove") == 40)
                    {
                        var ret0 = gb.Hold(Joypad.B, gb.SYM["PoisonEffect.inflictPoison"], gb.SYM["MoveHitTest.moveMissed"], gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB, gb.SYM["HandleMenuInput"], gb.SYM["HandleEnemyMonFainted"], gb.SYM["HandlePlayerMonFainted"]);
                        if (ret0 == (int)gb.SYM["HandleEnemyMonFainted"])
                        {
                            clusterDamage += newhp - gb.CpuReadBE<ushort>(gb.SYM["wBattleMonHP"]);
                            continue; //killed him
                        }
                        if (ret0 != (int)gb.SYM["HandleMenuInput"])
                        {

                            //Console.WriteLine("turn5 PS "+ ret0);
                            return;
                        } //PS miss crit poison, or dead, or TW miss
                        if (gb.CpuReadBE<ushort>(gb.SYM["wBattleMonHP"]) <= 4)
                        {
                            //Console.WriteLine("redbar turn 5");
                            return; // redbar. Can't manip
                        }
                    }
                    else
                    {
                        gb.Hold(Joypad.A, gb.SYM["WaitForTextScrollButtonPress"]);
                        gb.Press(Joypad.B);
                        var ret2 = gb.Hold(Joypad.B, gb.SYM["MoveHitTest.moveMissed"], gb.SYM["HandleMenuInput"], gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB, gb.SYM["HandleEnemyMonFainted"]);
                        if (ret2 == (int)gb.SYM["HandleEnemyMonFainted"])
                        {
                            clusterDamage += newhp - gb.CpuReadBE<ushort>(gb.SYM["wBattleMonHP"]);
                            continue; //killed him
                        }
                        else if (ret2 != (int)gb.SYM["HandleMenuInput"])
                        {

                            //Console.WriteLine("turn5 SS "+ ret2);
                            return;
                        } // tackle crit or miss
                    }

                    //turn 6
                    gb.MenuPress(Joypad.A);
                    gb.MenuPress(Joypad.Select);
                    gb.MenuPress(Joypad.A);
                    gb.RunUntil(gb.SYM["SelectEnemyMove.done"] + 0x3);
                    if (gb.CpuRead("wEnemySelectedMove") == 40)
                    {
                        var ret0 = gb.Hold(Joypad.B, gb.SYM["PoisonEffect.inflictPoison"], gb.SYM["MoveHitTest.moveMissed"], gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB, gb.SYM["HandleMenuInput"], gb.SYM["HandleEnemyMonFainted"], gb.SYM["HandlePlayerMonFainted"]);
                        if (ret0 == (int)gb.SYM["HandleEnemyMonFainted"])
                        {
                            clusterDamage += newhp - gb.CpuReadBE<ushort>(gb.SYM["wBattleMonHP"]);

                            //Console.WriteLine("killed him PS");
                            continue; //killed him
                        }
                        if (ret0 != (int)gb.SYM["HandleMenuInput"])
                        {

                            //Console.WriteLine("turn 6 PS "+ ret0);
                            return;
                        } //PS miss crit poison, or dead, or TW miss
                    }
                    else
                    {
                        gb.Hold(Joypad.A, gb.SYM["WaitForTextScrollButtonPress"]);
                        gb.Press(Joypad.B);
                        var ret2 = gb.Hold(Joypad.B, gb.SYM["MoveHitTest.moveMissed"], gb.SYM["HandleMenuInput"], gb.SYM["CriticalHitTest.SkipHighCritical"] + 0xB, gb.SYM["HandleEnemyMonFainted"]);
                        if (ret2 == (int)gb.SYM["HandleEnemyMonFainted"])
                        {
                            clusterDamage += newhp - gb.CpuReadBE<ushort>(gb.SYM["wBattleMonHP"]);

                            //Console.WriteLine("killed him SS");
                            continue; //killed him
                        }
                        if (ret2 != (int)gb.SYM["HandleMenuInput"])
                        {

                            //Console.WriteLine("turn6 SS " + ret2);

                            return;
                        } // tackle crit or miss
                    }

                    //Console.WriteLine("miss range");
                    return; //didnt kill it in 6 turns, lowrolls
                }
                res.Success = true;
                res.dmgTaken.Add(clusterDamage);

            }
            lock (results)
                results.Add(res);


        });
        return results;
    }
    public static void Search(RbyIntroSequence intro, int numThreads = 12, int numFrames = 16, int success = 15)
    {
        StartWatch();

        Red[] gbs = MultiThread.MakeThreads<Red>(numThreads);
        Red gb = gbs[0];
        if (numThreads == 1) gb.Record("test");

        gb.LoadState(State);
        IGTResults states = Red.IGTCheckParallel(gbs, intro, numFrames);

        RbyMap cave = gb.Maps[61];
        Action actions = Action.Right | Action.Down | Action.Up | Action.Left | Action.A | Action.StartB;
        RbyTile startTile = gb.Tile;
        // RbyTile[] endTiles = { cave[36, 31], cave[37, 30], cave[37, 32] };

        List<RbyTile> blockedTiles = new List<RbyTile>() { };



        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, gb.Maps[13][8, 48], actions, blockedTiles.ToArray());
        //gb.Maps[60][26, 3].AddEdge(0, new Edge<RbyMap, RbyTile>() { Action = Action.Down, NextTile = gb.Maps[60][26, 3], NextEdgeset = 0, Cost = 2 });
        //Pathfinding.DebugDrawEdges<RbyMap, RbyTile>(gb, gb.Maps[13], 0);
        // map 13 is route 2 map 1 is viridian
        var parameters = new DFParameters<Red, RbyMap, RbyTile>()
        {
            MaxCost = 4,
            SuccessSS = success,
            EndTiles = new RbyTile[] { gb.Maps[13][8, 48] },
            EncounterCallback = gb => gb.EnemyMon.Species.Name == "PIDGEY" && (gb.Tile == gb.Maps[13][8, 48] || gb.Tile == gb.Maps[13][7, 48] || gb.Tile == gb.Maps[13][6, 48] || gb.Tile == gb.Maps[13][8, 49] || gb.Tile == gb.Maps[13][7, 49] || gb.Tile == gb.Maps[13][8, 50])
            && gb.Yoloball() && gb.EnemyMon.DVs.HP <= 9 && gb.EnemyMon.Level == 3,
            //LogStart = startTile.PokeworldLink + "/",
            FoundCallback = state =>
            {
                success = CheckIGT(State, intro, state.Log, "PIDGEY", 60, false, false, Verbosity.Nothing);
                if (success > 57)
                {
                    Trace.WriteLine(state.Log + " " + CheckIGT(State, intro, state.Log, "PIDGEY", 60, false, false, Verbosity.Summary) + "/60 " + state.WastedFrames + " " + intro);
                }
            }
        };
        Trace.WriteLine(startTile.PokeworldLink + "/");
        /*DepthFirstSearch.StartSearch(gbs, parameters, startTile, 0, states);
        Elapsed("search");*/
    }
}
