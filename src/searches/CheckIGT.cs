using System;
using System.Collections.Generic;
using System.Diagnostics;

using static RbyIGTChecker<Red>;

class CheckIGT {

    public static void AltNidos()
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

    public static void Poy()
    {
        string poy = "DDDDDDDDDDDARRRRRRRRRRRRRRRRD";
        poy+= "UUURRRRRDDRRRRRRRUURRRDDDDDDDDLLDDDDDDDDDLLLLLLLLLLLLLLLLLLLLLLLUUUALUUUUUUUUUUU";
        RbyIntroSequence intro = new RbyIntroSequence(RbyStrat.NoPal);
        RbyIGTChecker<Red>.CheckIGT("basesaves/red/manip/posthiker.gqs", intro, poy, "PARAS", true, false, null, true);
        // RbyIGTChecker<Red>.CheckIGT("basesaves/red/manip/posthiker_redbar.gqs", intro, poy, "PARAS", true, false, null, true);
    }

    public static void Rt3Moon()
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

    public static void EntrMoon()
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

    public static void NidoFrame33Backup()
    {
        RbyIntroSequence intro = new RbyIntroSequence(RbyStrat.NoPal);
        Red gb = new Red();
        const Joypad PLD=Joypad.B, PLD_ball=Joypad.A;

        gb.LoadState("basesaves/red/manip/nido.gqs");
        gb.HardReset();
        intro.ExecuteUntilIGT(gb);
        byte[] igtState = gb.SaveState();
        // for (byte maxhp = 21; maxhp <= 23; ++maxhp)
        // for (byte hp = 10; hp <= maxhp; ++hp)
        // for (byte atk = 10; atk <= 12; ++atk)
        // for (byte def = 12; def <= 14; ++def)
        // for (byte spd = 10; spd <= 12; ++spd)
        // for (byte spc = 11; spc <= 12; ++spc)
        for (byte s = 0; s < 60; ++s)
        {
            gb.LoadState(igtState);

            gb.CpuWrite("wPlayTimeMinutes", 5);
            gb.CpuWrite("wPlayTimeSeconds", s);
            gb.CpuWrite("wPlayTimeFrames", 33);
            intro.ExecuteAfterIGT(gb);

            // gb.CpuWriteBE<ushort>("wPartyMon1HP",      hp );
            // gb.CpuWriteBE<ushort>("wPartyMon1MaxHP",   maxhp );
            // gb.CpuWriteBE<ushort>("wPartyMon1Attack",  atk );
            // gb.CpuWriteBE<ushort>("wPartyMon1Defense", def );
            // gb.CpuWriteBE<ushort>("wPartyMon1Speed",   spd );
            // gb.CpuWriteBE<ushort>("wPartyMon1Special", spc );
            // Trace.Write(gb.CpuReadBE<ushort>("wPartyMon1HP")+"/"+gb.CpuReadBE<ushort>("wPartyMon1MaxHP")+" "+gb.CpuReadBE<ushort>("wPartyMon1Attack")+" "+gb.CpuReadBE<ushort>("wPartyMon1Defense")+" "+gb.CpuReadBE<ushort>("wPartyMon1Speed")+" "+gb.CpuReadBE<ushort>("wPartyMon1Special"));

            const string nidopath = "LLLULLUAULALDLDLLDADDADLALLALUUAU";
            int addr;
            addr = gb.Execute(SpacePath(nidopath));

            Trace.Write($"{s,2} 33,  ");

            if (addr != gb.SYM["CalcStats"])
            {
                Trace.WriteLine("No enc");
                continue;
            }

            if (gb.EnemyMon.Species.Name != "NIDORANM")
            {
                Trace.WriteLine(gb.EnemyMon.Species.Name);
                continue;
            }

            bool yoloball;

            gb.Hold(PLD, gb.SYM["ManualTextScroll"]); // nido appeared
            gb.Press(Joypad.A);

            gb.Hold(PLD, gb.SYM["PlayCry"]);
            gb.Press(Joypad.Down | Joypad.A, Joypad.A | Joypad.Left); // yoloball 1
            yoloball = gb.Hold(PLD_ball, gb.SYM["ItemUseBall.captured"], gb.SYM["ItemUseBall.failedToCapture"]) == gb.SYM["ItemUseBall.captured"];
            Trace.Write("Yoloball1: " + yoloball);
            if(yoloball)
            {
                Trace.WriteLine("");
                continue;
            }

            gb.Hold(PLD, gb.SYM["ManualTextScroll"]); // missed
            gb.Press(Joypad.A);

            addr=gb.RunUntil(gb.SYM["MoveMissed"], gb.SYM["ManualTextScroll"], gb.SYM["HandleMenuInput_"]); // get move info
            byte move=gb.CpuRead(gb.SYM["wEnemySelectedMove"]);
            if(move > 0) {
                Trace.Write(", Move: " + gb.Moves[move].Name);
            }
            if(addr == gb.SYM["MoveMissed"]) {
                Trace.Write(" Miss");
                addr=gb.RunUntil(gb.SYM["ManualTextScroll"], gb.SYM["HandleMenuInput_"]);
            }
            if(gb.CpuRead(gb.SYM["wCriticalHitOrOHKO"]) > 0) {
                Trace.Write(" Crit");
            }

            if(addr==gb.SYM["ManualTextScroll"]) { // crit / status
                gb.AdvanceFrames(4);
                gb.Press(Joypad.B);
            }

            gb.Press(Joypad.A, Joypad.A | Joypad.Right); // yoloball 2
            // gb.Press(Joypad.A, Joypad.Select, Joypad.A); // select
            yoloball = gb.Hold(PLD_ball, gb.SYM["ItemUseBall.captured"], gb.SYM["ItemUseBall.failedToCapture"]) == gb.SYM["ItemUseBall.captured"];
            Trace.WriteLine(", Yoloball2: " + yoloball);
        }
    }
}
