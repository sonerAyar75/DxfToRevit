namespace DxfToRevit.Core.Openings;

/// <summary>
/// Reads and writes the opening type map as a text file, one row per line:
/// <c>Category;Code;RevitType</c> (for example <c>Door;1;M_Single-Flush::0915 x 2134mm</c>).
/// </summary>
/// <remarks>
/// Fields are separated by a semicolon or a tab. Empty lines, lines starting with <c>#</c> and a
/// header line starting with <c>Category</c> are skipped. The category is <c>Door</c> or
/// <c>Window</c> (case-insensitive); the code <c>*</c> is the category default. The file is UTF-8.
/// </remarks>
public static class OpeningTypeMapFile
{
    /// <summary>Header line written by <see cref="Write"/>.</summary>
    public const string Header = "Category;Code;RevitType";

    /// <summary>Reads a map file.</summary>
    /// <param name="path">File path.</param>
    /// <returns>The rows read and the errors of unreadable lines.</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
    /// <exception cref="FileNotFoundException">The file does not exist.</exception>
    public static OpeningTypeMapReadResult Read(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path))
            throw new FileNotFoundException("Opening type map file was not found.", path);

        using var reader = new StreamReader(path, detectEncodingFromByteOrderMarks: true);
        return Parse(reader);
    }

    /// <summary>Parses map text.</summary>
    /// <param name="reader">Text source.</param>
    /// <returns>The rows read and the errors of unreadable lines.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="reader"/> is <c>null</c>.</exception>
    public static OpeningTypeMapReadResult Parse(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var entries = new List<OpeningTypeMapEntry>();
        var errors = new List<string>();
        int lineNumber = 0;
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            lineNumber++;
            string trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#') ||
                trimmed.StartsWith("Category", StringComparison.OrdinalIgnoreCase))
                continue;

            string[] fields = trimmed.Split(new[] { ';', '\t' });
            if (fields.Length != 3)
            {
                errors.Add($"Line {lineNumber}: expected 3 fields (Category;Code;RevitType), found {fields.Length}.");
                continue;
            }

            if (!TryParseCategory(fields[0], out OpeningCategory category))
            {
                errors.Add($"Line {lineNumber}: unknown category '{fields[0].Trim()}' (use Door or Window).");
                continue;
            }

            if (string.IsNullOrWhiteSpace(fields[1]) || string.IsNullOrWhiteSpace(fields[2]))
            {
                errors.Add($"Line {lineNumber}: code and Revit type cannot be empty.");
                continue;
            }

            entries.Add(new OpeningTypeMapEntry(category, fields[1].Trim(), fields[2].Trim()));
        }

        return new OpeningTypeMapReadResult(new OpeningTypeMap(entries), errors);
    }

    /// <summary>Writes a map file (UTF-8, with a comment and the header line).</summary>
    /// <param name="path">File path; missing directories are created.</param>
    /// <param name="map">Map to write.</param>
    /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="map"/> is <c>null</c>.</exception>
    public static void Write(string path, OpeningTypeMap map)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(map);

        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        var lines = new List<string>
        {
            "# DxfToRevit opening type map: TYPE attribute code -> Revit type (Family::Type).",
            "# Code * is the default of a category.",
            Header
        };
        lines.AddRange(map.Entries.Select(e => $"{e.Category};{e.Code};{e.TypeKey}"));
        File.WriteAllLines(path, lines, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    /// <summary>Door or window, case-insensitive; empty openings have no type.</summary>
    private static bool TryParseCategory(string text, out OpeningCategory category) =>
        Enum.TryParse(text.Trim(), ignoreCase: true, out category) &&
        category is OpeningCategory.Door or OpeningCategory.Window;
}
