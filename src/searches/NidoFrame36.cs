using System.Linq;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using static SearchCommon;
using static RbyIGTChecker<Red>;

class NidoFrame36
{
    public static void Check()
    {
        string nido = "LLLULLUAULALDLDLLDADDADLALLALUUAU";
        string interval = "UUU";
        // string backup = "ADDAUUADDUAUDDUAUDDUAUDADUUDADS_BS_BS_BS_BS_BS_BDDU"; // memeball mirror
        // string backup = "ALLUUUURRUULLL" + "RRRRRLLLLLRS_BRS_BRDDADDADDDADU"; // s2
        string backup = "ALLS_BLRRRDDDDDDDUUUUUUUDDDDS_BS_BDAUS_BS_BAU"; // 672
        // string backup = "ADUS_BDDDDDDDUUUUUUUDDDDDDDUS_BS_BAUUS_BS_BAU"; // 672b
        // string backup = "ADUS_BDDDDDDDUUUUUUUDDDS_BDDS_BDADUS_BS_BUAUU"; // 672c
        RbyIntroSequence intro = new RbyIntroSequence(RbyStrat.NoPal);
        CheckIGT("basesaves/red/manip/nido.gqs", intro, nido + interval + backup, "NIDORANM", 60, true, null, true, 36, 60);

        Red gb = new Red();
        gb.LoadState("basesaves/red/manip/nido.gqs");
        gb.HardReset();
        intro.ExecuteUntilIGT(gb);
        gb.CpuWrite("wPlayTimeFrames", 36);
        gb.Record("nido36");
        intro.ExecuteAfterIGT(gb);
        gb.Execute(SpacePath(nido));
        gb.Execute(SpacePath(interval + backup));
        gb.SelectBall();
        gb.ClearText();
        gb.Press(Joypad.A, Joypad.None, Joypad.A, Joypad.Start);
        gb.Execute("D");
        gb.Dispose();

        // gb.Record("nido36back");
        // intro.ExecuteAfterIGT(gb);
        // gb.Execute(SpacePath(nido));
        // // gb.Execute("UD");
        // gb.Execute(SpacePath("DRRRRUUURRRRRRRRRRD"));
        // gb.Press(Joypad.Start, Joypad.Up, Joypad.None, Joypad.Up, Joypad.None, Joypad.Up, Joypad.A);
        // gb.ClearText();
        // gb.AdvanceFrames(1);
        // gb.Press(Joypad.A);
        // gb.AdvanceFrames(32+105);
        // gb.HardReset();
        // intro.Execute(gb);
        // gb.Execute(SpacePath(nido));
        // gb.Yoloball();
        // gb.ClearText();
        // gb.Press(Joypad.A, Joypad.None, Joypad.A, Joypad.Start);
        // gb.Execute("D");
        // gb.Dispose();
    }

    public static void FixFile()
    {
        RbyIntroSequence intro = new RbyIntroSequence(RbyStrat.NoPal);
        string[] lines = File.ReadAllLines("nido36.txt");
        string nido = "LLLULLUAULALDLDLLDADDADLALLALUUAU";
        string interval = "UUUA";

        Red gb = new Red();
        gb.LoadState("basesaves/red/manip/nido.gqs");
        gb.HardReset();
        intro.ExecuteUntilIGT(gb);
        gb.CpuWrite("wPlayTimeFrames", 36);
        intro.ExecuteAfterIGT(gb);
        gb.Execute(SpacePath(nido + interval));
        byte[] state = gb.SaveState();
        string prevpath = null;
        foreach(string line in lines)
        {
            string path = Regex.Match(line, @"/([LRUDSA_B]+) ").Groups[1].Value;
            if(path[0] == 'S' || path == prevpath)
                continue;
            prevpath = path;
            gb.LoadState(state);
            gb.Execute(SpacePath(path));
            if(gb.Tile.Y < 11)
                path += "D";
            else if(gb.Tile.Y > 11)
                path += "U";
            else if(gb.Tile.X < 33)
                path += "R";
            else if(gb.Tile.X == 33 && gb.Tile.Y == 11)
                path += "U";
            else
                Console.WriteLine("error " + path + " " + gb.Tile);
            Trace.WriteLine(line.Substring(0,49) + path + line.Substring(49+path.Length-1));
        }
    }

    public static void CheckFile()
    {
        RbyIntroSequence intro = new RbyIntroSequence(RbyStrat.NoPal);
        string[] lines = File.ReadAllLines("nido36.txt");
        string nido = "LLLULLUAULALDLDLLDADDADLALLALUUAU";
        string interval = "UUUA"; //"UUUALLUUUURRUULLL";
        var display = new List<Display>();
        int lowest = 1000;
        foreach(string line in lines)
        {
            if(line.Contains("NIDORANM L4 DVs: 0xffef"))
            {
                int cost = int.Parse(Regex.Match(line, @"Cost: ([0-9]+)").Groups[1].Value);
                if(cost < lowest) lowest=cost;
            }
        }
        Trace.WriteLine("cost: " + lowest);
        foreach(string line in lines)
        {
            if(line.Contains("NIDORANM L4 DVs: 0xffef Cost: " + lowest))
            {
                string path = Regex.Match(line, @"/([LRUDSA_B]+) ").Groups[1].Value;
                display.Add(new Display(path));
                // Trace.WriteLine(path);
                CheckIGT("basesaves/red/manip/nido.gqs", intro, nido + interval + path, "NIDORANM", 1, true, null, false, 36, 60, 1);
                CheckIGT("basesaves/red/manip/nido.gqs", intro, nido + interval + path, "NIDORANM", 1, true, null, true, 36, 60, 1);
            }
        }
        Display.PrintAll(display, "https://gunnermaniac.com/pokeworld?local=33#33/8/");
    }

    public void Search(int numThreads = 10)
    {
        StartWatch();
        RbyIntroSequence intro = new RbyIntroSequence(RbyStrat.NoPal);
        Red gb = new Red();

        gb.LoadState("basesaves/red/manip/nido.gqs");
        gb.HardReset();
        intro.ExecuteUntilIGT(gb);

        gb.CpuWrite("wPlayTimeMinutes", 5);
        gb.CpuWrite("wPlayTimeSeconds", 0);
        gb.CpuWrite("wPlayTimeFrames", 36);
        intro.ExecuteAfterIGT(gb);
        gb.Execute(SpacePath("LLLULLUAULALDLDLLDADDADLALLALUUAU"+"UUU")); //UUUALLUUUURRUULLL

        IGTState state=new IGTState(gb, false, 36);
        Elapsed("states");

        RbyMap route22 = gb.Maps[33];
        Action actions = Action.Right | Action.Down | Action.Up | Action.Left | Action.A | Action.StartB;
        RbyTile startTile = gb.Tile;
        RbyTile[] endTiles = { route22[33, 11] };
        Pathfinding.GenerateEdges<RbyMap,RbyTile>(gb, 0, endTiles.First(), actions);
        route22[30, 4].RemoveEdge(0, Action.Left);
        route22[30, 5].RemoveEdge(0, Action.Left);

        var parameters = new DFParameters<Red,RbyMap,RbyTile>()
        {
            MaxCost = 675,
            EncounterCallback = gb =>
            {
                return //gb.EnemyMon.Species.Name == "NIDORANM" && gb.EnemyMon.Level >= 3 &&
                    gb.EnemyMon.DVs.Attack == 15 && gb.EnemyMon.DVs.Defense == 15 && gb.EnemyMon.DVs.Speed >= 14 && gb.EnemyMon.DVs.Special == 15;
            },
            SingleCallback = (state,gb) =>
            {
                Trace.WriteLine(startTile.PokeworldLink + "/" + state.Log + "  " + gb.EnemyMon.Species.Name + " L" + gb.EnemyMon.Level + " DVs: " + gb.EnemyMon.DVs.ToString() + " Cost: " + state.WastedFrames);
            }
        };

        DepthFirstSearch.SingleSearch(gb, parameters, startTile, 0, state, 2);
        Elapsed("search");
    }
}
