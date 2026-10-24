namespace FileLauncher.Core.Model;

/// <summary>既存の呼び出しを保つため既定OFF。左右の修飾キーは区別しない。</summary>
public sealed class DoubleTapSetting
{
    public bool Enabled { get; set; }
    public DoubleTapModifier Modifier { get; set; } = DoubleTapModifier.Ctrl;
    public int IntervalMs { get; set; } = 300;
}

public enum DoubleTapModifier { Ctrl, Alt, Shift, Meta }
