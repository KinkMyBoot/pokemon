using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Linq;
using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

using static RbyIGTChecker<Red>;

class Program {

    static void AltNidos()
    {
        string nido = "LLLULLUAULALDLDLLDADDADLALLALUUAU"; //standard
        // string nido = "LLLULLUAULALDLDLLADDADDLALLALUUAU"; //turn-a
        // string nido = "LLLULLUAULALDLDLLADDDADLALLALUUAU"; //both
        // string nido = "LLLULLUAULALDLDLLDADDDLALLALUUAU"; //igt
        // string nido = "LDUALLULLLLAULLLLLADDADDLADLAUUAU"; //palette1
        // string nido = "LDUALLULLLLAULLLLDADDADLLADLAUUAU"; //palette2
        // string nido = "LLLULLLLAUDAULLLADLADDDADLALLUAUU"; //alt1
        // string nido = "LLLULLLAULADULLLADLLDADDADLALUAUU"; //alt2
        // string nido = "LLLULLLAULADULLLADLADDDADLALLUUAU"; //alt3
        // string nido = "LLLULULLLARLALLLADLDALDADLADLUUAU"; //alt4
        RbyIntroSequence intro = new RbyIntroSequence(RbyStrat.NoPal);
        RbyIGTChecker<Red>.CheckIGT("basesaves/red/manip/nido.gqs", intro, nido, "NIDORANM", true, true);
    }

    static void Poy()
    {
        string poy = "DDDDDDDDDDDARRRRRRRRRRRRRRRRD";
        poy+= "UUURRRRRDDRRRRRRRUURRRDDDDDDDDLLDDDDDDDDDLLLLLLLLLLLLLLLLLLLLLLLUUUALUUUUUUUUUUU";
        RbyIntroSequence intro = new RbyIntroSequence(RbyStrat.NoPal);
        RbyIGTChecker<Red>.CheckIGT("basesaves/red/manip/posthiker.gqs", intro, poy, "PARAS", true, false, null, true);
        // RbyIGTChecker<Red>.CheckIGT("basesaves/red/manip/posthiker_redbar.gqs", intro, poy, "PARAS", true, false, null, true);
    }

    static void Rt3Moon()
    {
        var items = new List<(int, byte, byte)> {
            (59, 34, 31), // candy
            (59, 35, 23), // rope
            (59, 3, 2), // moon stone
            (59, 5, 31), // wg
            (61, 28, 5), // mp
        };
        string rt3Moon = "RRRRRRRRURRUUUUUARRRRRRRRRRRRDDDDDRRRRRRRARUURRUUUUUUUUUURRRRUUUUUUUUUURRRRRU";
        rt3Moon += "UUUUUULLLLLALLLLDD";
        rt3Moon += "RRRRUURRRARRUUUUUUURRRRRRRAUUUUUUURRRDRDDDDDDDADDDDDDDDADRRRRRURRRR";
        rt3Moon += "UUUUUUUUR";
        rt3Moon += "ULUUUUUAUUUUUULLLUUUUUUUULLLLLLDDLALLLLLLLDDDDDD";
        rt3Moon += "LALLALLALLALDD";
        rt3Moon += "RRRUUULAUR";
        rt3Moon += "DDADLALLAD";
        // rt3Moon += "DDADDALLAL"; //alt post mp
        rt3Moon += "RARRARRARRARUU";
        rt3Moon += "DDLDDDDLLLLLLLULUUUUULUUUUUUUULLLUL";
        rt3Moon += "DADDRAR";
        rt3Moon += "DRRDDDDDDDDDDRRRARRRRRRRRRRDR";
        // rt3Moon += "DRRDDDDDDDDDDRRRARRRRRRRRRRRD"; //slayer
        // rt3Moon += "DRRDDDDDDDDDADRRRRRRRRRRRRRDR"; //4 early
        // rt3Moon += "DRRDDDDDDDDDDARRRRRRRRRRRRRDR"; //3 early
        // rt3Moon += "DRRDDDDDDDDDDRARRRRRRRRRRRRDR"; //2 early
        // rt3Moon += "DRRDDDDDDDDDDRRARRRRRRRRRRRDR"; //1 early
        // rt3Moon += "DRRDDDDDDDDDDRRRRARRRRRRRRRDR"; //1 late
        // rt3Moon += "DRRDDDDDDDDDDRRRRRARRRRRRRRDR"; //2 late
        // rt3Moon += "DRRDDDDDDDDDDRRRRRRARRRRRRRDR"; //3 late
        // rt3Moon += "DRRDDDDDDDDDDRRRRRRRARRRRRRDR"; //4 late
        rt3Moon += "RRUUURARRRDDRRRRRUARURARRDDDDDDDDALLLLDDDDDDDADDLLLALLLLLLLLLLLLALLLLLLUUUUAUUALUUUUUUUU";
        // rt3Moon += "RRUUURARRRDDRRRRRUAURRARRDDDDDDDDALLLLDDDDDDDADDLLLALLLLLLLLLLLLALLLLLLUUUUAUUALUUUUUUUU"; //alt b2f
        // rt3Moon += "RRUUURARRRDDRRRRRUAURRARRDDDDDDDDALLLLDDDDDDADDDLLLALLLLLLLLLLLLALLLLLLUUUUAUUALUUUUUUUU"; //alt b2f v2
        // rt3Moon += "RRUUURARRRDDRRRRRUARURARRDDDDDDDDALLLLDDDDDDDADDLLLALLLLLLLLLLLLLALLLLLUUUUAUUALUUUUUUUU";//7 1 late
        // rt3Moon += "RRUUURARRRDDRRRRRUARURARRDDDDDDDDALLLLDDDDDDDADDLLLALLLLLLLLLLLLLLALLLLUUUUAUUALUUUUUUUU";//7 2 late
        // rt3Moon += "RRUUURARRRDDRRRRRUARURARRDDDDDDDDALLLLDDDDDDDADDLLLALLLLLLLLLLLLLLLALLLUUUUAUUALUUUUUUUU";//7 3 late
        // rt3Moon += "RRUUURARRRDDRRRRRUARURARRDDDDDDDDALLLLDDDDDDDADDLLLALLLLLLLLLLLALLLLLLLUUUUAUUALUUUUUUUU";//7 1 early
        // rt3Moon += "RRUUURARRRDDRRRRRUARURARRDDDDDDDDALLLLDDDDDDADDDLLLALLLLLLLLLLLLALLLLLLUUUUAUUALUUUUUUUU";//5 1 early
        // rt3Moon += "RRUUURARRRDDRRRRRUARURARRDDDDDDDDALLLLDDDDDDDDADLLLALLLLLLLLLLLLALLLLLLUUUUAUUALUUUUUUUU";//5 1 late
        // rt3Moon += "RRUUURARRRDDRRRRRUARURARRDDDDDDDDALLLLDDDDDDDADDLLALLLLLLLLLLLLLALLLLLLUUUUAUUALUUUUUUUU";//6 1 early
        // rt3Moon += "RRUUURARRRDDRRRRRUARURARRDDDDDDDDALLLLDDDDDDDADDLLLLALLLLLLLLLLLALLLLLLUUUUAUUALUUUUUUUU";//6 1 late
        RbyIntroSequence rt3MoonIntro = new RbyIntroSequence(RbyStrat.PalHold);
        RbyIGTChecker<Red>.CheckIGT("basesaves/red/manip/rt3moon.gqs", rt3MoonIntro, rt3Moon, "PARAS", true, false, items);
    }

    static void EntrMoon()
    {
        var items = new List<(int, byte, byte)> {
            (59, 34, 31), // candy
            (59, 35, 23), // rope
            (59, 3, 2), // moon stone
            (59, 5, 31), // wg
            (61, 28, 5), // mp
        };
        string entrMoon = "UAUUUUULLLLLLLLALDD";
        entrMoon += "RUUUUURRRRURUURURRRRRRRRUUUUUUURRRRDDRDDDDDADDDDDDDDDDRRRRRRURRR";
        // entrMoon += "RUUUUURRRRURUURURRRRRRRRUUUUUUURRRRDRDDDDDDADDDDDDDDDDRRRRRRURRR"; //early ladder turn
        entrMoon += "UUUUUUUURUUUUUUUUUUULUUUUULLLUUUULLDDLLLLLALLLLLLLDDDDDD";
        entrMoon += "LLALLDADLALLAL";
        entrMoon += "RRRUUULUR";
        entrMoon += "DDDDLLL";
        entrMoon += "URARRARRARRARU";
        entrMoon += "DDDADDALDLLLLALUULALUUUUUUUULLUAUULUULLL";
        entrMoon += "DADRARD";
        entrMoon += "DADDRRDDDDDDDDDRRRRRRRRRRRRRR";
        entrMoon += "RRUUURARRRDDRRRRRRUURRARDDDDDDDDLLLLDDDDDDDDDLLLLLLLLLLLLLLLLLLLLLLUUUUUUUUUUUAUUU";
        RbyIntroSequence entrMoonIntro = new RbyIntroSequence(RbyStrat.NoPalAB);
        RbyIGTChecker<Red>.CheckIGT("basesaves/red/manip/entrmoon.gqs", entrMoonIntro, entrMoon, "PARAS", false, false, items, true);
    }

    static void NidoFrame33Backup()
    {
        RbyIntroSequence intro = new RbyIntroSequence(RbyStrat.NoPal, RbyStrat.GfSkip, RbyStrat.Hop0, RbyStrat.Title0, RbyStrat.Continue, RbyStrat.Continue);
        Red gb = new Red();

        gb.LoadState("basesaves/red/manip/nido.gqs");
        gb.HardReset();
        // gb.Record("test");
        intro.ExecuteUntilIGT(gb);
        byte[] igtState = gb.SaveState();
        for (byte s = 0; s < 60; ++s)
        {
            gb.LoadState(igtState);

            gb.CpuWrite("wPlayTimeMinutes", 5);
            gb.CpuWrite("wPlayTimeSeconds", s);
            gb.CpuWrite("wPlayTimeFrames", 33);
            intro.ExecuteAfterIGT(gb);
            const string nidopath = "LLLULLUAULALDLDLLDADDADLALLALUUAU";
            int addr;
            addr = gb.Execute(SpacePath(nidopath));

            Console.Write("" + s + " 33,  ");

            if (addr != gb.SYM["CalcStats"])
            {
                Console.WriteLine("No enc");
                continue;
            }

            if (gb.EnemyMon.Species.Name != "NIDORANM")
            {
                Console.WriteLine(gb.EnemyMon.Species.Name);
                continue;
            }

            bool yoloball;

            gb.Hold(Joypad.B, gb.SYM["ManualTextScroll"]); // nido appeared
            gb.Press(Joypad.A);

            gb.Hold(Joypad.B, gb.SYM["PlayCry"]);
            gb.Press(Joypad.Down | Joypad.A, Joypad.A | Joypad.Left); // yoloball 1
            yoloball = gb.Hold(Joypad.A, gb.SYM["ItemUseBall.captured"], gb.SYM["ItemUseBall.failedToCapture"]) == gb.SYM["ItemUseBall.captured"];
            Console.Write("Yoloball1: " + yoloball);
            if(yoloball)
            {
                Console.WriteLine();
                continue;
            }

            gb.Hold(Joypad.B, gb.SYM["ManualTextScroll"]); // missed
            gb.Press(Joypad.A);

            addr=gb.RunUntil(gb.SYM["MoveMissed"], gb.SYM["ManualTextScroll"], gb.SYM["HandleMenuInput_"]); // get move info
            byte move=gb.CpuRead(gb.SYM["wEnemySelectedMove"]);
            if(move > 0) {
                Console.Write(", Move: " + gb.Moves[move].Name);
            }
            if(addr == gb.SYM["MoveMissed"]) {
                Console.Write(" Miss");
                addr=gb.RunUntil(gb.SYM["ManualTextScroll"], gb.SYM["HandleMenuInput_"]);
            }
            if(gb.CpuRead(gb.SYM["wCriticalHitOrOHKO"]) > 0) {
                Console.Write(" Crit");
            }

            if(addr==gb.SYM["ManualTextScroll"]) { // crit / status
                gb.Press(Joypad.B);
            }

            gb.Press(Joypad.Down | Joypad.A, Joypad.A | Joypad.Left); // yoloball 2
            yoloball = gb.Hold(Joypad.A, gb.SYM["ItemUseBall.captured"], gb.SYM["ItemUseBall.failedToCapture"]) == gb.SYM["ItemUseBall.captured"];
            Console.WriteLine(", Yoloball2: " + yoloball);
        }
    }

    static void Main(string[] args) {
        // Tests.RunAllTests();

        PidgeyBackup.Search(15, 60, 57);
        // PidgeyBackup.Search(10, 10, 7);
        // PidgeyBackup.Check();
    }
}
