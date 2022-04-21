public class Ao : RedBlue {

    public Ao(string savFile = null, bool speedup = true) : base("roms/pokeao.gbc", savFile, speedup) { }

    public override byte[][] BGPalette() {
        return new byte[][] {
                    new byte[] { 248, 248, 248 },
                    new byte[] { 113, 182, 208 },
                    new byte[] { 15, 62, 170 },
                    new byte[] { 0, 0, 0 }};
    }

    public override byte[][] ObjPalette() {
        return new byte[][] {
                    new byte[] { 127, 56, 72 },
                    new byte[] { 248, 248, 248 },
                    new byte[] { 225, 128, 150 },
                    new byte[] { 0, 0, 0 }};
    }

    public override void Inject(Joypad joypad) {
        Hold(joypad, SYM["Joypad"] + 0x13);
    }

    public override bool Yoloball(int ballSlot = 0, Joypad hold = Joypad.None) {
        ClearText(hold);
        Press(Joypad.Right, Joypad.A | Joypad.Right, Joypad.A | (ballSlot == 0 ? Joypad.Up : Joypad.Down));
        return Hold(Joypad.A, "ItemUseBall.captured", "ItemUseBall.failedToCapture") == SYM["ItemUseBall.captured"];
    }

    public override bool Selectball(int ballSlot = 0, Joypad hold = Joypad.None) {
        ClearText(hold);
        Press(Joypad.Right, Joypad.A | Joypad.Right, Joypad.Select | Joypad.Right, Joypad.A | (ballSlot == 0 ? Joypad.Up : Joypad.Down));
        return Hold(Joypad.A, "ItemUseBall.captured", "ItemUseBall.failedToCapture") == SYM["ItemUseBall.captured"];
    }

    public static void Init() {
        var b = new Blue();
        b.LoadState("basesaves/blue/manip/bluetest.gqs");
        Rby.ParsedROMs[56374] = Rby.ParsedROMs[40202];
    }
}

class AoCb : Ao {
    public CallbackHandler<AoCb> CallbackHandler;
    public AoCb(string savFile = null, bool speedup = true) : base(savFile, speedup) { CallbackHandler = new CallbackHandler<AoCb>(this); }
    public unsafe override int Hold(Joypad joypad, params int[] addrs) { return CallbackHandler.Hold(base.Hold, joypad, addrs); }
}
