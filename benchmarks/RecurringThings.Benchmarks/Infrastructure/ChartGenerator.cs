namespace RecurringThings.Benchmarks.Infrastructure;

using ScottPlot;

/// <summary>
/// Generates line charts from benchmark CSV results using ScottPlot.
/// Creates one chart per palette/theme combination in separate subfolders.
/// </summary>
internal static class ChartGenerator
{
    /// <summary>
    /// Generates performance charts from benchmark CSV results.
    /// </summary>
    /// <param name="csvPath">Path to the BenchmarkDotNet CSV report.</param>
    /// <param name="outputDir">Base directory for chart output.</param>
    public static void GenerateCharts(string csvPath, string outputDir)
    {
        var (data, unit) = ParseCsv(csvPath);
        if (data.Count == 0)
        {
            Console.WriteLine("  No data found in CSV");
            return;
        }

        // Group by (Provider, DataVolume) - each becomes a line
        var series = data
            .GroupBy(d => (d.Provider, d.DataVolume))
            .OrderBy(g => g.Key.Provider)
            .ThenBy(g => g.Key.DataVolume)
            .ToList();

        // Define palette/theme combinations
        var themes = GetThemes();

        foreach (var theme in themes)
        {
            var themePath = Path.Combine(outputDir, theme.Name);
            Directory.CreateDirectory(themePath);

            var plt = new Plot();

            // Apply theme colors
            plt.FigureBackground.Color = theme.FigureBackground;
            plt.DataBackground.Color = theme.DataBackground;
            plt.Grid.MajorLineColor = theme.GridColor;
            plt.Axes.Color(theme.AxisColor);

            // Set palette for data colors
            plt.Add.Palette = theme.Palette;

            // Add each series as a line
            foreach (var group in series)
            {
                var points = group.OrderBy(d => d.ConcurrentRequests).ToList();
                var xValues = points.Select(p => (double)p.ConcurrentRequests).ToArray();
                var yValues = points.Select(p => p.MeanValue).ToArray();

                var scatter = plt.Add.Scatter(xValues, yValues);
                scatter.Label = $"{group.Key.Provider} - Database Volume: {group.Key.DataVolume:N0}";
                scatter.LineWidth = 2;
                scatter.MarkerSize = 8;
            }

            // Configure axes - use unit from CSV
            plt.XLabel("Concurrent Requests");
            plt.YLabel($"Response Time ({unit})");
            plt.Title("Read Performance: Response Time vs Concurrency");

            // Configure legend
            plt.ShowLegend(Alignment.UpperLeft);

            // Save chart
            var outputPath = Path.Combine(themePath, "ReadPerformanceChart.png");
            plt.SavePng(outputPath, 1920, 1080);
            Console.WriteLine($"  Saved: {theme.Name}/ReadPerformanceChart.png");
        }
    }

    private static (List<BenchmarkDataPoint> Data, string Unit) ParseCsv(string csvPath)
    {
        var results = new List<BenchmarkDataPoint>();
        var unit = "ms"; // Default unit
        var lines = File.ReadAllLines(csvPath);

        if (lines.Length < 2)
            return (results, unit);

        // Parse header to find column indices
        var header = ParseCsvLine(lines[0]);
        var providerIdx = FindColumnIndex(header, "Provider");
        var volumeIdx = FindColumnIndex(header, "DataVolume");
        var concurrencyIdx = FindColumnIndex(header, "ConcurrentRequests");
        var meanIdx = FindColumnIndex(header, "Mean");

        if (providerIdx < 0 || volumeIdx < 0 || concurrencyIdx < 0 || meanIdx < 0)
        {
            Console.WriteLine("  Warning: Could not find required columns in CSV");
            return (results, unit);
        }

        // Parse data rows
        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                var columns = ParseCsvLine(line);
                if (columns.Length <= Math.Max(Math.Max(providerIdx, volumeIdx), Math.Max(concurrencyIdx, meanIdx)))
                    continue;

                var provider = columns[providerIdx];
                var volume = int.Parse(columns[volumeIdx]);
                var concurrency = int.Parse(columns[concurrencyIdx]);
                var (meanValue, parsedUnit) = ParseMeanValueWithUnit(columns[meanIdx]);

                // Use the unit from the first valid row
                if (results.Count == 0)
                    unit = parsedUnit;

                results.Add(new BenchmarkDataPoint(provider, volume, concurrency, meanValue));
            }
            catch
            {
                // Skip malformed rows
            }
        }

        return (results, unit);
    }

    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var inQuotes = false;
        var current = "";

        foreach (var c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.Trim());
                current = "";
            }
            else
            {
                current += c;
            }
        }

        result.Add(current.Trim());
        return result.ToArray();
    }

    private static int FindColumnIndex(string[] header, string columnName)
    {
        for (var i = 0; i < header.Length; i++)
        {
            if (header[i].Equals(columnName, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    private static (double Value, string Unit) ParseMeanValueWithUnit(string value)
    {
        // BenchmarkDotNet includes units like "1,234.56 ns" or "29,935.6 ms"
        // Extract the numeric part and unit separately
        var trimmed = value.Trim();

        // Find where the unit starts (first letter after digits)
        var unitStart = -1;
        for (var i = 0; i < trimmed.Length; i++)
        {
            if (char.IsLetter(trimmed[i]) || trimmed[i] == 'μ')
            {
                unitStart = i;
                break;
            }
        }

        string unit;
        string numericPart;

        if (unitStart > 0)
        {
            unit = trimmed[unitStart..].Trim();
            numericPart = trimmed[..unitStart].Trim();
        }
        else
        {
            unit = "ms"; // Default
            numericPart = trimmed;
        }

        // Remove commas and parse
        var cleaned = numericPart.Replace(",", "");
        var numericValue = double.Parse(cleaned);

        return (numericValue, unit);
    }

    private static List<ChartTheme> GetThemes()
    {
        return
        [
            // Light themes
            new ChartTheme(
                "Light-Category10",
                Color.FromHex("#FFFFFF"),
                Color.FromHex("#F5F5F5"),
                Color.FromHex("#E0E0E0"),
                Color.FromHex("#333333"),
                new ScottPlot.Palettes.Category10()),

            new ChartTheme(
                "Light-ColorblindFriendly",
                Color.FromHex("#FFFFFF"),
                Color.FromHex("#F5F5F5"),
                Color.FromHex("#E0E0E0"),
                Color.FromHex("#333333"),
                new ScottPlot.Palettes.ColorblindFriendly()),

            new ChartTheme(
                "Light-Nord",
                Color.FromHex("#ECEFF4"),
                Color.FromHex("#E5E9F0"),
                Color.FromHex("#D8DEE9"),
                Color.FromHex("#2E3440"),
                new ScottPlot.Palettes.Nord()),

            // Dark themes
            new ChartTheme(
                "Dark-Category10",
                Color.FromHex("#1E1E1E"),
                Color.FromHex("#252526"),
                Color.FromHex("#3C3C3C"),
                Color.FromHex("#D4D4D4"),
                new ScottPlot.Palettes.Category10()),

            new ChartTheme(
                "Dark-OneHalfDark",
                Color.FromHex("#282C34"),
                Color.FromHex("#21252B"),
                Color.FromHex("#3E4451"),
                Color.FromHex("#ABB2BF"),
                new ScottPlot.Palettes.OneHalfDark()),

            new ChartTheme(
                "Dark-Nord",
                Color.FromHex("#2E3440"),
                Color.FromHex("#3B4252"),
                Color.FromHex("#434C5E"),
                Color.FromHex("#ECEFF4"),
                new ScottPlot.Palettes.Nord()),

            new ChartTheme(
                "Dark-Penumbra",
                Color.FromHex("#303030"),
                Color.FromHex("#3A3A3A"),
                Color.FromHex("#4A4A4A"),
                Color.FromHex("#BEBEBE"),
                new ScottPlot.Palettes.Penumbra())
        ];
    }

    private record BenchmarkDataPoint(string Provider, int DataVolume, int ConcurrentRequests, double MeanValue);

    private record ChartTheme(
        string Name,
        Color FigureBackground,
        Color DataBackground,
        Color GridColor,
        Color AxisColor,
        IPalette Palette);
}
