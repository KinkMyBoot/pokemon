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

    public struct Display
    {
        public string Path;
        public int SS, C, T, A, S;
        public Display(string path, int success = 0, int cost = 0)
        {
            Path = path;
            SS = success;
            C = cost;
            T = TurnCount(path);
            A = APressCount(path);
            S = StartCount(path);
        }
        public override string ToString()
        {
            string str = Path;
            if(SS > 0)
                str += " " + SS;
            if(C > 0)
                str += " C:" + C;
            if(S > 0)
                str += " S:" + S;
            str += " T:" + T;
            str += " A:" + A;
            return str;
        }
        static public void PrintAll(List<Display> list, string prefix = "")
        {
            foreach(Display d in list.OrderByDescending((d) => d.SS).ThenBy((d) => d.C).ThenBy((d) => d.S).ThenBy((d) => d.A).ThenBy((d) => d.T))
                System.Diagnostics.Trace.WriteLine(prefix + d);
        }
    };
    public static int TurnCount(string path)
    {
        path = Regex.Replace(path, "[AS_B]", "");
        string a = String.Empty;
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
        handler.SetCallback(handler.gb.SYM["TryWalking"] + 25, (gb) =>
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
            (int, int) npc = (handler.gb.Map.Id, gb.CpuRead(0xffda) / 16);

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
