using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

// Generates Revit-style ribbon icons (flat, grey body with blue accents).
// Every icon is produced at 16 and 32 px, in a light and a dark theme variant:
//   <Name>_Light_16.png, <Name>_Light_32.png, <Name>_Dark_16.png, <Name>_Dark_32.png
// "Light" is for the light theme background (dark strokes), "Dark" for the dark theme (light strokes).
// It also writes DxfToRevit.ico (16, 32, 48 and 256 px) for the installer and the uninstall entry.

string outDir = args.Length > 0 ? args[0] : "Resources";
Directory.CreateDirectory(outDir);

var themes = new[] { Palette.Light, Palette.Dark };

var thread = new Thread(() =>
{
    foreach (Palette p in themes)
    {
        foreach (int size in new[] { 16, 32 })
        {
            Save(outDir, $"ImportDxf_{p.Name}_{size}.png", size, dc => Icons.ImportDxf(dc, p, size));
            Save(outDir, $"Settings_{p.Name}_{size}.png", size, dc => Icons.Gear(dc, p, size));
            Save(outDir, $"About_{p.Name}_{size}.png", size, dc => Icons.About(dc, p, size));
        }
    }

    // Application icon: the 16 px design for 16 px, the 32 px design scaled for larger sizes.
    var icoFrames = new[] { 16, 32, 48, 256 }
        .Select(size => (size, Render(size, dc =>
        {
            if (size < 32)
            {
                Icons.ImportDxf(dc, Palette.Light, size);
                return;
            }

            dc.PushTransform(new ScaleTransform(size / 32.0, size / 32.0));
            Icons.ImportDxf(dc, Palette.Light, 32);
            dc.Pop();
        })))
        .ToList();
    SaveIco(Path.Combine(outDir, "DxfToRevit.ico"), icoFrames);
});
thread.SetApartmentState(ApartmentState.STA);
thread.Start();
thread.Join();
Console.WriteLine($"Icons written to {Path.GetFullPath(outDir)}");
return 0;

static void Save(string dir, string file, int size, Action<DrawingContext> draw) =>
    File.WriteAllBytes(Path.Combine(dir, file), Render(size, draw));

// Renders a square drawing and returns it as PNG bytes.
static byte[] Render(int size, Action<DrawingContext> draw)
{
    var visual = new DrawingVisual();
    using (DrawingContext dc = visual.RenderOpen())
        draw(dc);

    var bmp = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
    bmp.Render(visual);

    var enc = new PngBitmapEncoder();
    enc.Frames.Add(BitmapFrame.Create(bmp));
    using var ms = new MemoryStream();
    enc.Save(ms);
    return ms.ToArray();
}

// Writes a Windows .ico file whose images are stored as PNG (supported since Windows Vista).
static void SaveIco(string path, IReadOnlyList<(int Size, byte[] Png)> frames)
{
    using var fs = File.Create(path);
    using var w = new BinaryWriter(fs);

    // ICONDIR: reserved, type (1 = icon), image count.
    w.Write((ushort)0);
    w.Write((ushort)1);
    w.Write((ushort)frames.Count);

    // ICONDIRENTRY per image; image data follows the directory.
    int offset = 6 + 16 * frames.Count;
    foreach (var (size, png) in frames)
    {
        w.Write((byte)(size >= 256 ? 0 : size));   // width (0 means 256)
        w.Write((byte)(size >= 256 ? 0 : size));   // height
        w.Write((byte)0);                          // palette colour count
        w.Write((byte)0);                          // reserved
        w.Write((ushort)1);                        // colour planes
        w.Write((ushort)32);                       // bits per pixel
        w.Write(png.Length);                       // image size in bytes
        w.Write(offset);                           // image offset
        offset += png.Length;
    }

    foreach (var (_, png) in frames)
        w.Write(png);
}

sealed record Palette(
    string Name,
    Color Outline,
    Color Sheet,
    Color SheetFold,
    Color Accent,
    Color AccentText,
    Color WallFill,
    Color WallHatch,
    Color Gear,
    Color GearEdge)
{
    // Light theme: dark grey strokes on Revit's light grey ribbon, white paper, Autodesk blue.
    public static readonly Palette Light = new(
        "Light",
        Outline: Rgb(0x3F, 0x45, 0x4E),
        Sheet: Rgb(0xFF, 0xFF, 0xFF),
        SheetFold: Rgb(0xD5, 0xDB, 0xE3),
        Accent: Rgb(0x0B, 0x6C, 0xC4),
        AccentText: Rgb(0xFF, 0xFF, 0xFF),
        WallFill: Rgb(0xC3, 0xC9, 0xD1),
        WallHatch: Rgb(0x5E, 0x66, 0x71),
        Gear: Rgb(0x55, 0x5D, 0x69),
        GearEdge: Rgb(0x3A, 0x40, 0x48));

    // Dark theme: light grey strokes on the dark ribbon, dark paper, brighter blue.
    public static readonly Palette Dark = new(
        "Dark",
        Outline: Rgb(0xD8, 0xDC, 0xE2),
        Sheet: Rgb(0x4B, 0x54, 0x62),
        SheetFold: Rgb(0x6C, 0x76, 0x85),
        Accent: Rgb(0x3D, 0xA2, 0xF5),
        AccentText: Rgb(0x10, 0x1A, 0x26),
        WallFill: Rgb(0x6E, 0x78, 0x86),
        WallHatch: Rgb(0xD0, 0xD6, 0xDE),
        Gear: Rgb(0xCF, 0xD4, 0xDB),
        GearEdge: Rgb(0xE8, 0xEB, 0xEF));

    private static Color Rgb(byte r, byte g, byte b) => Color.FromRgb(r, g, b);
}

static class Icons
{
    private static Brush B(Color c)
    {
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }

    private static Pen P(Color c, double w)
    {
        var p = new Pen(B(c), w) { LineJoin = PenLineJoin.Round, StartLineCap = PenLineCap.Flat, EndLineCap = PenLineCap.Flat };
        p.Freeze();
        return p;
    }

    /// <summary>DXF sheet (folded corner + "DXF" label) → blue arrow → hatched wall block.</summary>
    public static void ImportDxf(DrawingContext dc, Palette p, int size)
    {
        if (size >= 32)
        {
            // Sheet
            var sheet = Geometry.Parse("M3.5,1.5 H14.5 L19.5,6.5 V26.5 H3.5 Z");
            dc.DrawGeometry(B(p.Sheet), P(p.Outline, 1), sheet);
            dc.DrawGeometry(B(p.SheetFold), P(p.Outline, 1), Geometry.Parse("M14.5,1.5 V6.5 H19.5 Z"));

            // Drawing lines on the sheet
            var lines = P(p.Outline, 1);
            dc.DrawLine(lines, new Point(6, 9.5), new Point(12, 9.5));
            dc.DrawLine(lines, new Point(6, 11.5), new Point(15, 11.5));
            dc.DrawLine(lines, new Point(6, 23.5), new Point(17, 23.5));

            // DXF etiketi
            dc.DrawRoundedRectangle(B(p.Accent), null, new Rect(1, 14, 17, 8), 1, 1);
            var text = new FormattedText(
                "DXF",
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                7.5,
                B(p.AccentText),
                1.0);
            dc.DrawText(text, new Point(1 + (17 - text.Width) / 2, 14 + (8 - text.Height) / 2));

            // Arrow (down)
            dc.DrawGeometry(B(p.Accent), null,
                Geometry.Parse("M23.5,3 H27.5 V12 H30.5 L25.5,18.5 L20.5,12 H23.5 Z"));

            // Wall block (hatched)
            var wallRect = new Rect(19.5, 20.5, 11, 10);
            dc.DrawRectangle(B(p.WallFill), null, wallRect);
            dc.PushClip(new RectangleGeometry(wallRect));
            var hatch = P(p.WallHatch, 1);
            for (double x = 12; x < 34; x += 3.5)
                dc.DrawLine(hatch, new Point(x, 31), new Point(x + 10, 21));
            dc.Pop();
            dc.DrawRectangle(null, P(p.Outline, 1), wallRect);
        }
        else
        {
            var sheet = Geometry.Parse("M1.5,0.5 H7.5 L10.5,3.5 V13.5 H1.5 Z");
            dc.DrawGeometry(B(p.Sheet), P(p.Outline, 1), sheet);
            dc.DrawGeometry(B(p.SheetFold), P(p.Outline, 1), Geometry.Parse("M7.5,0.5 V3.5 H10.5 Z"));
            dc.DrawRectangle(B(p.Accent), null, new Rect(0, 6, 9, 4));

            dc.DrawGeometry(B(p.Accent), null,
                Geometry.Parse("M11.5,1 H13.5 V6 H15.5 L12.5,9.5 L9.5,6 H11.5 Z"));

            var wallRect = new Rect(9.5, 10.5, 6, 5);
            dc.DrawRectangle(B(p.WallFill), P(p.Outline, 1), wallRect);
            dc.PushClip(new RectangleGeometry(wallRect));
            var hatch = P(p.WallHatch, 1);
            for (double x = 6; x < 18; x += 3)
                dc.DrawLine(hatch, new Point(x, 15.5), new Point(x + 5, 10.5));
            dc.Pop();
        }
    }

    /// <summary>Gear wheel (8 teeth, hole in the middle).</summary>
    public static void Gear(DrawingContext dc, Palette p, int size)
    {
        double c = size / 2.0;
        double outer = size >= 32 ? 14.5 : 7.5;
        double root = size >= 32 ? 11.0 : 5.6;
        double hole = size >= 32 ? 4.6 : 2.3;
        const int teeth = 8;
        const double tipHalf = 10.0;   // degrees, half angle of a tooth tip
        const double baseHalf = 14.0;  // degrees, half angle of a tooth base

        var fig = new PathFigure { IsClosed = true, IsFilled = true };
        for (int i = 0; i < teeth; i++)
        {
            double a = i * 360.0 / teeth - 90;
            Point rootStart = Polar(c, root, a - baseHalf);
            Point tipStart = Polar(c, outer, a - tipHalf);
            Point tipEnd = Polar(c, outer, a + tipHalf);
            Point rootEnd = Polar(c, root, a + baseHalf);

            if (i == 0)
                fig.StartPoint = rootStart;
            else
                fig.Segments.Add(new ArcSegment(rootStart, new Size(root, root), 0, false, SweepDirection.Clockwise, true));

            fig.Segments.Add(new LineSegment(tipStart, true));
            fig.Segments.Add(new ArcSegment(tipEnd, new Size(outer, outer), 0, false, SweepDirection.Clockwise, true));
            fig.Segments.Add(new LineSegment(rootEnd, true));
        }
        // Root arc from the last tooth back to the first
        fig.Segments.Add(new ArcSegment(Polar(c, root, -90 - baseHalf), new Size(root, root), 0, false, SweepDirection.Clockwise, true));

        var gear = new PathGeometry { FillRule = FillRule.EvenOdd };
        gear.Figures.Add(fig);
        gear.AddGeometry(new EllipseGeometry(new Point(c, c), hole, hole));

        dc.DrawGeometry(B(p.Gear), size >= 32 ? P(p.GearEdge, 0.8) : null, gear);

        // Blue hub ring (the accent used on Revit settings icons)
        if (size >= 32)
            dc.DrawEllipse(null, P(p.Accent, 1.6), new Point(c, c), hole + 2.3, hole + 2.3);
    }

    /// <summary>"i" inside a blue circle.</summary>
    public static void About(DrawingContext dc, Palette p, int size)
    {
        double s = size / 32.0;
        dc.DrawEllipse(B(p.Accent), null, new Point(16 * s, 16 * s), 14.5 * s, 14.5 * s);
        Brush fg = B(p.AccentText);
        dc.DrawEllipse(fg, null, new Point(16 * s, 9 * s), 2.4 * s, 2.4 * s);
        dc.DrawRoundedRectangle(fg, null, new Rect(13.8 * s, 13 * s, 4.4 * s, 12 * s), 1 * s, 1 * s);
    }

    private static Point Polar(double c, double r, double deg)
    {
        double rad = deg * Math.PI / 180.0;
        return new Point(c + r * Math.Cos(rad), c + r * Math.Sin(rad));
    }
}
