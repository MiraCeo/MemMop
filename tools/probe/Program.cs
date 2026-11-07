using System.Reflection;
using MemMop.Core;
using MemMop.ViewModel;

// A headless pass over the parts that can be checked without elevation and without a window: the counters
// add up, the catalog is complete in both languages, and the log actually writes. Anything that needs
// administrator rights - the reclaimers themselves - is out of reach here and is not pretended otherwise.

var failures = 0;

void Check(string name, bool ok, string detail)
{
    Console.WriteLine((ok ? "  ok   " : "  FAIL ") + name + (detail.Length > 0 ? "  " + detail : string.Empty));

    if (!ok)
        failures++;
}

// ---- the counters ------------------------------------------------------------------------------------

Console.WriteLine("memory snapshot");

var snapshot = MemorySnapshot.Capture();

Check("total is positive", snapshot.Total > 0, $"{snapshot.Total / (1024d * 1024 * 1024):0.#} GB");
Check("available is within total", snapshot.Available > 0 && snapshot.Available <= snapshot.Total, string.Empty);

if (snapshot.HasSegments)
{
    var sum = snapshot.Active + snapshot.Modified + snapshot.Standby + snapshot.Free;

    // the four parts are the whole of physical memory; a mismatch means an offset is wrong
    Check("segments add up to total", Math.Abs(sum - snapshot.Total) < snapshot.Total / 100, $"{sum} vs {snapshot.Total}");
    Check("available is standby plus free", Math.Abs(snapshot.Available - (snapshot.Standby + snapshot.Free)) < snapshot.Total / 100, string.Empty);
}
else
{
    Console.WriteLine("  skip  segments: not readable on this system");
}

Check("commit limit is above commit used", snapshot.CommitLimit == 0 || snapshot.CommitUsed <= snapshot.CommitLimit, string.Empty);

// ---- the catalog -------------------------------------------------------------------------------------

Console.WriteLine("string catalog");

var catalog = Strings.Current;
var properties = typeof(Strings)
    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
    .Where(p => p.PropertyType == typeof(string) && p.GetIndexParameters().Length == 0)
    .ToList();

foreach (var english in new[] { false, true })
{
    catalog.SetLanguage(english);

    var empty = properties
        .Where(p => string.IsNullOrWhiteSpace((string?)p.GetValue(catalog)))
        .Select(p => p.Name)
        .ToList();

    Check((english ? "english" : "chinese") + " has no empty entries", empty.Count == 0, string.Join(", ", empty));
}

// ---- the areas ---------------------------------------------------------------------------------------

Console.WriteLine("cleaning areas");

foreach (var english in new[] { false, true })
{
    catalog.SetLanguage(english);

    var areas = catalog.Areas;

    Check((english ? "english" : "chinese") + " lists seven areas", areas.Count == 7, areas.Count.ToString());
    Check(
        (english ? "english" : "chinese") + " titles are filled",
        areas.All(a => !string.IsNullOrWhiteSpace(a.Title) && !string.IsNullOrWhiteSpace(a.Risk)),
        string.Empty);
}

catalog.SetLanguage(false);

// ---- the log -----------------------------------------------------------------------------------------

Console.WriteLine("log");

// written directly rather than through Log, so a failure can say why instead of being swallowed
try
{
    Directory.CreateDirectory(Log.Folder);
    File.AppendAllText(Log.Path, "probe" + Environment.NewLine);

    Check("writes a file", File.Exists(Log.Path), Log.Path);
}
catch (Exception exception)
{
    Check("writes a file", false, exception.GetType().Name + ": " + exception.Message);
}

// ---- the view model ----------------------------------------------------------------------------------

Console.WriteLine("view model");

var vm = new MainViewModel();

Check("seven areas are offered", vm.Areas.Count == 7, vm.Areas.Count.ToString());
// MOD_ALT | MOD_CONTROL, and the letter M
Check("hotkey defaults to control+alt+M", vm.HotkeyModifiers == 0x3 && vm.HotkeyKey == 0x4D, $"0x{vm.HotkeyModifiers:X} 0x{vm.HotkeyKey:X}");
Check("summary is never blank", !string.IsNullOrWhiteSpace(vm.Summary), vm.Summary);

Console.WriteLine();
Console.WriteLine(failures == 0 ? "all checks passed" : failures + " check(s) failed");

return failures == 0 ? 0 : 1;
