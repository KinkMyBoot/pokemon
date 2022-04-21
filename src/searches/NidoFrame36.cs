using System.Linq;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using static SearchCommon;
using static RbyIGTChecker<Red>;

class NidoFrame36
{
    static RbyIntroSequence Intro = new RbyIntroSequence(RbyStrat.NoPal);
    const string Nido = "LLLULLUAULALDLDLLDADDADLALLALUUAU";
    const string State = "basesaves/red/manip/nido.gqs";
    const string StateSlot2 = "basesaves/red/manip/nidorace.gqs";

    public static void Check()
    {
        string interval = "UUU";
        // string backup = "ADDAUUADDUAUDDUAUDDUAUDADUUDADS_BS_BS_BS_BS_BS_BDDU"; // memeball mirror
        // string backup = "ALLUUUURRUULLL" + "RRRRRLLLLLRS_BRS_BRDDADDADDDADU"; // 689
        string backup = "ALLS_BLRRRDDDDDDDUUUUUUUDDDDS_BS_BDAUS_BS_BAU"; // 672
        // string backup = "ADUS_BDDDDDDDUUUUUUUDDDDDDDUS_BS_BAUUS_BS_BAU"; // 672b
        // string backup = "ADUS_BDDDDDDDUUUUUUUDDDS_BDDS_BDADUS_BS_BUAUU"; // 672c
        CheckIGT(State, Intro, Nido + interval + backup, "NIDORANM", 60, true, null, true, 36, 60);
    }

    public static void Record(string interval, string backup)
    {
        Red gb = new Red();
        gb.LoadState(StateSlot2);
        Intro.ExecuteUntilIGT(gb);
        gb.CpuWrite("wPlayTimeFrames", 36);
        gb.Record("nido36");
        Intro.ExecuteAfterIGT(gb);
        gb.Execute(SpacePath(Nido));
        gb.Execute(SpacePath(interval + backup));
        gb.Selectball(1);
        gb.ClearText();
        gb.Press(Joypad.A, Joypad.None, Joypad.A, Joypad.Start);
        gb.Execute("D");
        gb.Dispose();
    }

    public static void RecordGrid672()
    {
        Red gb = new Red();
        gb.LoadState(StateSlot2);
        Intro.ExecuteUntilIGT(gb);
        gb.CpuWrite("wPlayTimeFrames", 36);
        gb.Show();
        gb.SetSpeedupFlags(SpeedupFlags.None);
        Intro.ExecuteAfterIGT(gb);

        GridComponent g = new GridComponent(0, 0, 160, 144, 1, SpacePath("LLLULLUAULALDLDLLDADDADDLALLALUUAU" + "UUUALLS_BL"));
        gb.Scene.AddComponent(g);
        gb.Execute(SpacePath("LLLULLUAULALDLDLL"));
        gb.Scene.AddComponent(new RecordingComponent("nido36"));
        gb.Execute(SpacePath("DADDADLALLALUUAU" + "UUUALLS_BL"));
        g.ChangePath(SpacePath("RRRDDDDDDD"));
        gb.Execute(SpacePath("RRRDDDDDDD"));
        g.ChangePath(SpacePath("UUUUUUU"));
        gb.Execute(SpacePath("UUUUUUU"));
        g.ChangePath(SpacePath("DDDDS_BS_BD"));
        gb.Execute(SpacePath("DDDDS_BS_B"));
        g.ChangePath(SpacePath("DAUS_BS_B"));
        gb.Execute(SpacePath("DAUS_B"));
        g.ChangePath(SpacePath("AU"));
        gb.Execute(SpacePath("S_BAU"));
        gb.Scene.RemoveComponent(g);
        gb.Selectball(1);
        gb.ClearText(2);
        gb.Dispose();
    }

    public static void RecordGrid689()
    {
        Red gb = new Red();
        gb.LoadState(StateSlot2);
        Intro.ExecuteUntilIGT(gb);
        gb.CpuWrite("wPlayTimeFrames", 36);
        gb.Show();
        gb.SetSpeedupFlags(SpeedupFlags.None);
        Intro.ExecuteAfterIGT(gb);

        GridComponent g = new GridComponent(0, 0, 160, 144, 1, SpacePath("LLLULLUAULALDLDLLDADDADDLALLALUUAU" + "UUUALLUUUURRUULLL"));
        gb.Scene.AddComponent(g);
        gb.Execute(SpacePath("LLLULLUAULALDLDLL"));
        gb.Scene.AddComponent(new RecordingComponent("nido36"));
        gb.Execute(SpacePath("DADDADLALLALUUAU" + "UUUALLUUUURRUULLL"));
        g.ChangePath(SpacePath("RRRRR"));
        gb.Execute(SpacePath("RRRRR"));
        g.ChangePath(SpacePath("LLLLL"));
        gb.Execute(SpacePath("LLLLL"));
        g.ChangePath(SpacePath("RS_BRS_BRDDADDADDDDADL"));
        gb.Execute(SpacePath("RS_BRS_BRDDADDADDDADL"));
        gb.Scene.RemoveComponent(g);
        gb.Selectball(1);
        gb.ClearText(2);
        gb.Dispose();
    }

    public static void RecordBack()
    {
        Red gb = new Red();
        gb.LoadState(StateSlot2);
        Intro.ExecuteUntilIGT(gb);
        gb.CpuWrite("wPlayTimeFrames", 36);
        gb.Record("nido36back");
        Intro.ExecuteAfterIGT(gb);
        gb.Execute(SpacePath(Nido));
        gb.AdvanceFrames(30);
        // gb.Execute("U D");
        gb.Execute(SpacePath("DRRRRUUURRRRRRRRRRD"));
        gb.Press(Joypad.Start, Joypad.Up, Joypad.None, Joypad.Up, Joypad.None, Joypad.Up, Joypad.A);
        gb.ClearText();
        // gb.AdvanceFrames(1);
        gb.Press(Joypad.A);
        gb.AdvanceFrames(5);
        Intro.Execute(gb);
        gb.Execute(SpacePath(Nido));
        gb.Yoloball(1);
        gb.ClearText();
        gb.Press(Joypad.A, Joypad.None, Joypad.A, Joypad.Start);
        gb.Execute("D");
        gb.Dispose();
    }

    public static void CheckFile()
    {
        string[] lines = System.IO.File.ReadAllLines("nido36.txt");
        string interval = "UUUA"; // UUUALLUUUURRUULLL
        Paths paths = new Paths();
        int lowest = 1000;
        foreach(string line in lines)
        {
            if(line.Contains("NIDORANM L4 dvs: 0xffef"))
            {
                int cost = int.Parse(Regex.Match(line, @"cost: ([0-9]+)").Groups[1].Value);
                if(cost < lowest) lowest = cost;
            }
        }
        Trace.WriteLine("cost: " + lowest);
        foreach(string line in lines)
        {
            if(line.Contains("NIDORANM L4 dvs: 0xffef cost: " + lowest))
            {
                string path = Regex.Match(line, @"/([LRUDSA_B]+) ").Groups[1].Value;
                paths.Add(new Path(path));
                // Trace.WriteLine(path);
                CheckIGT(State, Intro, Nido + interval + path, "NIDORANM", 1, true, null, false, 36, 60, 1);
                CheckIGT(State, Intro, Nido + interval + path, "NIDORANM", 1, true, null, true, 36, 60, 1);
            }
        }
        paths.PrintAll("https://gunnermaniac.com/pokeworld?local=33#33/8/");
    }

    public static void Search(int numThreads = 16)
    {
        StartWatch();

        Red[] gbs = MultiThread.MakeThreads<Red>(numThreads);
        Red gb = gbs[0];

        gb.LoadState(State);
        IGTState state = gb.IGTCheck(Intro, 1, () => gb.Execute(SpacePath(Nido + "UUU")) != 0, 0, 36)[0];

        RbyMap route22 = gb.Maps[33];
        Action actions = Action.Right | Action.Down | Action.Up | Action.Left | Action.A | Action.StartB;
        RbyTile startTile = gb.Tile;
        RbyTile[] endTiles = { route22[33, 11] };
        Pathfinding.GenerateEdges<RbyMap, RbyTile>(gb, 0, endTiles[0], actions);
        route22[30, 4].RemoveEdge(0, Action.Left);
        route22[30, 5].RemoveEdge(0, Action.Left);

        var parameters = new SFParameters<Red, RbyMap, RbyTile>()
        {
            MaxCost = 675,
            EncounterCallback = gb => //gb.EnemyMon.Species.Name == "NIDORANM" && gb.EnemyMon.Level >= 3 &&
                    gb.EnemyMon.DVs.Attack == 15 && gb.EnemyMon.DVs.Defense == 15 && gb.EnemyMon.DVs.Speed >= 14 && gb.EnemyMon.DVs.Special == 15,
            LogStart = startTile.PokeworldLink + "/",
            FoundCallback = (state, gb) =>
            {
                Trace.WriteLine(state.Log + " " + gb.EnemyMon.Species.Name + " L" + gb.EnemyMon.Level + " dvs: " + gb.EnemyMon.DVs.ToString() + " cost: " + state.WastedFrames);
            }
        };

        SingleFrameSearch.StartSearch(gbs, parameters, startTile, 0, state, 2);
        Elapsed("search");
    }
}
