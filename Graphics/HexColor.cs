using System.Drawing;
using WlxMirror.Numerics;

namespace WlxMirror.Graphics;

public class HexColor
{
    public static Vector3 FromRgb(string str)
    {
        var c = ColorTranslator.FromHtml(str);
        return new Vector3(c.R, c.G, c.B) / byte.MaxValue;
    }
}