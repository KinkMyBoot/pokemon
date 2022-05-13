using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

class SearchCommon
{
    public delegate void FunctionToProfile();
    public static float Profile(string title, FunctionToProfile fn)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        fn();
        watch.Stop();
        float t = watch.ElapsedMilliseconds / 1000.0f;
        Console.WriteLine(title + ": " + t + "s");
        return t;
    }
    public static float Profile(FunctionToProfile fn)
    {
        return Profile("elapsed", fn);
    }
    static System.Diagnostics.Stopwatch Watch;
    static long LastMs;
    public static void StartWatch()
    {
        Watch = System.Diagnostics.Stopwatch.StartNew();
        LastMs = 0;
    }
    public static float Elapsed(string title = "elapsed", bool total = false)
    {
        float t = (Watch.ElapsedMilliseconds - (total ? 0 : LastMs)) / 1000.0f;
        LastMs = Watch.ElapsedMilliseconds;
        Console.WriteLine(title + ": " + t + "s");
        return t;
    }
    public static float ElapsedTotal(string title = "elapsed")
    {
        return Elapsed(title, true);
    }

    public class Path
    {
        public string P, I;
        public int SS, C, S, A, T;
        public Path(string path, int success = 0, int cost = 0, string info = "")
        {
            P = path;
            SS = success;
            C = cost;
            T = TurnCount(path);
            A = APressCount(path);
            S = StartCount(path);
            I = info;
        }
        public override string ToString()
        {
            string str = P;
            if(SS > 0)
                str += " " + SS;
            if(C > 0)
                str += " c:" + C;
            if(S > 0)
                str += " s:" + S;
            if(A > 0)
                str += " a:" + A;
            str += " t:" + T;
            if(I != "")
                str += " " + I;
            return str;
        }
    }
    public class Paths : List<Path>
    {
        public void PrintAll(string prefix = "")
        {
            foreach(Path p in this.OrderByDescending(p => p.SS).ThenBy(p => p.C).ThenBy(p => p.S).ThenBy(p => p.A).ThenBy(p => p.T))
                System.Diagnostics.Trace.WriteLine(prefix + p);
        }
        public void CleanPrintAll(string prefix = "")
        {
            if(Count == 0) return;
            System.Diagnostics.Trace.Listeners[1].Close();
            System.Diagnostics.Trace.Listeners[1] = new System.Diagnostics.TextWriterTraceListener(System.IO.File.CreateText("log.txt"));
            PrintAll(prefix);
        }
    }
    public static int TurnCount(string path)
    {
        path = Regex.Replace(path, "[^LRUD]", "");
        int turns = 0;
        for(int i = 1; i < path.Length; ++i)
            if(path[i] != path[i - 1])
                ++turns;
        return turns;
    }
    public static int APressCount(string path)
    {
        return path.Count(c => c == 'A');
    }
    public static int StartCount(string path)
    {
        return path.Count(c => c == 'S');
    }

    public static void Record<Gb>(string name, string state, RbyIntroSequence intro, params string[] paths) where Gb : Rby
    {
        Gb gb = (Gb) Activator.CreateInstance(typeof(Gb), args: new object[] { null, true });
        gb.LoadState(state);
        gb.Record("1");
        intro.Execute(gb);
        GridComponent g = new GridComponent(0, 0, 160, 144, 1);
        gb.Scene.AddComponent(g);
        foreach(string path in paths)
        {
            g.ChangePath(RbyIGTChecker<Red>.SpacePath(path));
            gb.Execute(RbyIGTChecker<Red>.SpacePath(path));
        }
        gb.RunUntil("DisableLCD");
        gb.Scene.RemoveComponent(g);
        gb.AdvanceFrames(250);
        gb.Dispose();
        FFMPEG.RunFFMPEGCommand("-i movies/1.mp4 -vf scale=800x720:flags=neighbor -y movies/" + name + ".mp4");
    }

    static Dictionary<int, (int X, int Y)> LocalToMap = new Dictionary<int, (int, int)>() {
        { 0, (50, 234) },
        { 1, (40, 162) },
        { 2, (40, 54) },
        { 3, (220, 36) },
        { 4, (320, 116) },
        { 5, (220, 180) },
        { 6, (150, 108) },
        { 7, (160, 270) },
        { 8, (50, 342) },
        { 9, (0, 8) },
        { 10, (220, 108) },
        { 12, (50, 198) },
        { 13, (50, 90) },
        { 14, (80, 62) },
        { 15, (130, 44) },
        { 16, (230, 72) },
        { 17, (230, 144) },
        { 18, (200, 116) },
        { 19, (260, 116) },
        { 20, (260, 44) },
        { 21, (320, 44) },
        { 22, (260, 188) },
        { 23, (320, 134) },
        { 24, (280, 242) },
        { 25, (260, 242) },
        { 26, (200, 278) },
        { 27, (110, 116) },
        { 28, (110, 134) },
        { 29, (110, 278) },
        { 30, (170, 306) },
        { 31, (70, 342) },
        { 32, (50, 252) },
        { 33, (0, 170) },
        { 34, (0, 26) },
        { 35, (230, 0) },
        { 36, (250, 0) },
    };
    public static string Link(RbyTile tile, string path, bool local = false)
    {
        Action[] actions = ActionFunctions.PathToActions(RbyIGTChecker<Red>.SpacePath(path));
        var warp = tile.WarpCheck();
        foreach(Action a in actions) {
            if(warp.TileToWarpTo != null && warp.ActionRequired == a)
                tile = warp.TileToWarpTo;
            else
                tile = tile.GetNeighbor(a);
            warp = tile.WarpCheck();
            if(warp.TileToWarpTo != null && warp.ActionRequired == Action.None)
            {
                tile = warp.TileToWarpTo;
                warp = tile.WarpCheck();
            }
        }
        if(!local && LocalToMap.ContainsKey(tile.Map.Id)) {
            var coord = LocalToMap[tile.Map.Id];
            return "https://gunnermaniac.com/pokeworld?map=1#" + (tile.X + coord.X) + "/" + (tile.Y + coord.Y) + "/";
        }
        return tile.PokeworldLink + "/";
    }
}

class CallbackHandler<Gb> where Gb : GameBoy
{
    int Address;
    Action<Gb> Callback = null;
    public Gb gb;
    public CallbackHandler(Gb gb)
    {
        this.gb = gb;
    }
    public void SetCallback(int address, Action<Gb> callback)
    {
        Address = address;
        Callback = callback;
    }
    public int Hold(Func<Joypad, int[], int> hold, Joypad joypad, params int[] addrs)
    {
        if(Callback != null)
            addrs = addrs.Append(Address).ToArray();
        int ret;
        while((ret = hold(joypad, addrs)) == Address)
        {
            Callback(gb);
            gb.RunFor(1);
        }
        return ret;
    }
}

class NpcTracker<Gb> where Gb : Rby
{
    public Dictionary<(int, int), string> NpcMovement = new Dictionary<(int, int), string>();
    public NpcTracker(CallbackHandler<Gb> handler)
    {
        handler.SetCallback(handler.gb.SYM["TryWalking"] + 0x19, (gb) =>
        {
            string movement;
            switch((RbySpriteMovement) gb.B)
            {
                case RbySpriteMovement.MovingRight: movement = "r"; break;
                case RbySpriteMovement.MovingLeft: movement = "l"; break;
                case RbySpriteMovement.MovingDown: movement = "d"; break;
                case RbySpriteMovement.MovingUp: movement = "u"; break;
                default: movement = ""; break;
            }
            if((gb.F & 0x10) == 0)
                movement = movement.ToUpper();
            (int, int) npc = (handler.gb.Map.Id, gb.CpuRead("hCurrentSpriteOffset") / 16);

            string log = NpcMovement.GetValueOrDefault(npc);
            if(log == null || log.Last().ToString().ToLower() != movement)
                NpcMovement[npc] = log + movement;
        });
    }
    public string GetMovement(params (int map, int id)[] npc)
    {
        string str = NpcMovement.GetValueOrDefault(npc[0]);
        for(int i = 1; i < npc.Length; ++i)
        {
            str += "," + NpcMovement.GetValueOrDefault(npc[i]);
        }
        return str;
    }
}

class BlueCb : Blue
{
    public CallbackHandler<BlueCb> CallbackHandler;
    public BlueCb(string savFile = null, bool speedup = true) : base(savFile, speedup) { CallbackHandler = new CallbackHandler<BlueCb>(this); }
    public unsafe override int Hold(Joypad joypad, params int[] addrs) { return CallbackHandler.Hold(base.Hold, joypad, addrs); }
}

class RedCb : Red
{
    public CallbackHandler<RedCb> CallbackHandler;
    public RedCb(string savFile = null, bool speedup = true) : base(savFile, speedup) { CallbackHandler = new CallbackHandler<RedCb>(this); }
    public unsafe override int Hold(Joypad joypad, params int[] addrs) { return CallbackHandler.Hold(base.Hold, joypad, addrs); }
}
