using System.Collections.ObjectModel;
using System.Windows.Input;
using MemMop.Core;
using MemMop.Core.Reclaimers;

namespace MemMop.ViewModel;

/// <summary>
/// The home view model: the area list, the clean command and a one-line summary of the last run.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    private readonly CleaningEngine _engine;
    private readonly AutoCleanPolicy _policy = new();
    private readonly IMemoryReclaimer[] AllReclaimers;
    private bool _isBusy;
    private string? _summary;
    private string _physicalText = string.Empty;
    private string _segmentText = string.Empty;
    private string _commitText = string.Empty;
    private string _physicalSegments = string.Empty;
    private string _commitSegments = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    public MainViewModel()
    {
        var reclaimers = AllReclaimers = new IMemoryReclaimer[]
        {
            new StandbyListReclaimer(),
            new StandbyListReclaimer(true),
            new ModifiedPageListReclaimer(),
            new CombinedPageListReclaimer(),
            new SystemFileCacheReclaimer(),
            new RegistryCacheReclaimer(),
            new WorkingSetReclaimer(),
        };

        _engine = new CleaningEngine(reclaimers);

        Areas = new ObservableCollection<AreaItem>();

        RebuildAreas();

        Strings.Current.PropertyChanged += (_, _) => RebuildAreas();

        CleanCommand = new RelayCommand(Clean, () => !IsBusy && Areas.Any(a => a.IsSelected));
        AddExclusionCommand = new RelayCommand(() => AddExclusion(SelectedProcess), () => !string.IsNullOrEmpty(SelectedProcess));
        ResetSettingsCommand = new RelayCommand(ResetSettings);

        // one command instance serves every row: the row passes its own name in
        MoveUpCommand = new RelayCommand<string>(name => Move(name, -1), name => CanMove(name, -1));
        MoveDownCommand = new RelayCommand<string>(name => Move(name, 1), name => CanMove(name, 1));
        RemoveExclusionCommand = new RelayCommand<string>(name => RemoveExclusion(name), name => !string.IsNullOrEmpty(name));
        OptimizeProcessCommand = new RelayCommand<string>(OptimizeProcess, name => !string.IsNullOrEmpty(name));

        RefreshExcluded();
        RefreshAvailableProcesses();

        // the summary lines are built strings rather than bindings, so a language change has to re-raise them
        Strings.Current.PropertyChanged += (_, _) =>
        {
            Raise(nameof(Summary));
            Raise(nameof(LastCleanSummary));
            Raise(nameof(AutoCleanStatus));
            Raise(nameof(TitleText));
            Raise(nameof(NameText));
            Raise(nameof(VersionText));
            Raise(nameof(BuildText));
            Raise(nameof(ChartWindowLabel));
        };
    }

    /// <summary>Gets the window title, with the version so a screenshot identifies its build.</summary>
    public string TitleText =>
        Strings.Current.Name + " " + (System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0");

    /// <summary>Gets the program's name on its own, for the about card.</summary>
    public string NameText => Strings.Current.Name;

    /// <summary>Gets the application version, read from the assembly rather than typed twice.</summary>
    public string VersionText =>
        System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";

    /// <summary>
    /// Gets the build the executable came from. The build script stamps the commit into the product version,
    /// so a screenshot of the about page says exactly which source produced it.
    /// </summary>
    public string BuildText
    {
        get
        {
            try
            {
                var product = System.Diagnostics.FileVersionInfo
                    .GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location)
                    .ProductVersion;

                if (string.IsNullOrWhiteSpace(product))
                    return string.Empty;

                var plus = product!.IndexOf('+');

                if (plus < 0 || plus + 1 >= product.Length)
                    return string.Empty;

                var hash = product.Substring(plus + 1);

                return Strings.Current.Build(hash.Length > 7 ? hash.Substring(0, 7) : hash);
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }

    /// <summary>Gets or sets the physical memory headline.</summary>
    public string PhysicalText
    {
        get => _physicalText;
        set => Set(ref _physicalText, value);
    }

    /// <summary>Gets or sets the committed memory line.</summary>
    public string CommitText
    {
        get => _commitText;
        set => Set(ref _commitText, value);
    }

    /// <summary>Gets or sets the four-segment breakdown line.</summary>
    public string SegmentText
    {
        get => _segmentText;
        set => Set(ref _segmentText, value);
    }


    private bool _processUsageExpanded;
    private string _processSearchText = string.Empty;
    private string _selectedProcess = string.Empty;
    private bool _isPickerOpen;
    private string _selectedExclusion = string.Empty;
    private string _activeText = string.Empty;
    private string _modifiedText = string.Empty;
    private string _standbyText = string.Empty;
    private string _freeText = string.Empty;
    private string _usedPercentText = string.Empty;
    private string _usedText = string.Empty;
    private string _availableText = string.Empty;

    /// <summary>The interval stops, in minutes. 0 is off.</summary>
    private static readonly int[] IntervalStops = { 0, 15, 30, 60, 120 };

    /// <summary>How often a reading is taken, in seconds. The chart buckets these; it does not draw them.</summary>
    private const int SampleSeconds = 2;

    /// <summary>
    /// How far a bucket has to swing, in percentage points of total memory, before it reports its extreme
    /// instead of its mean. Below this the bucket is quiet and the mean is the honest answer.
    /// </summary>
    private const double SwingPercent = 1.5;

    /// <summary>The four windows, in seconds: one minute, five, fifteen, an hour.</summary>
    private static readonly int[] WindowSeconds = { 60, 300, 900, 3600 };

    /// <summary>
    /// How much time each point on the chart stands for. The windows are chosen so every one of them is drawn
    /// from thirty or sixty points: a minute at two seconds, the rest at five, fifteen and sixty.
    /// </summary>
    private static readonly int[] BucketSeconds = { 2, 5, 15, 60 };

    private readonly MemoryHistory _memory = new(1800);
    private readonly CleanHistory _cleans = new(100);
    private int _chartWindowIndex = 1;   // five minutes
    private string _chartValues = string.Empty;
    private string _chartMarkers = string.Empty;
    private double _chartMaximum;
    private double _chartSpanSeconds;
    private string? _lastCleanSummary;

    /// <summary>
    /// The usage stops, as a ceiling on memory in use. Usage is total minus available, so standby memory
    /// does not count against the user; the range is high because a healthy machine already sits well above
    /// half, and a 70% ceiling would fire on a machine with ten gigabytes still free.
    /// </summary>
    private static readonly int[] ThresholdStops = { 0, 75, 80, 85, 90 };

    /// <summary>Gets the process rows, largest first.</summary>
    public ObservableCollection<ProcessUsage> ProcessUsageItems { get; } = new();

    /// <summary>Gets what each area released on the last run.</summary>
    public ObservableCollection<AreaResultRow> LastCleanAreas { get; } = new();

    /// <summary>Gets the runs the statistics page lists, newest first.</summary>
    public ObservableCollection<CleanRecordRow> CleanRecords { get; } = new();

    /// <summary>Gets a value indicating whether any run has been recorded.</summary>
    public bool HasCleanRecords => CleanRecords.Count > 0;

    /// <summary>Gets the one-line description of the last run.</summary>
    public string LastCleanSummary
    {
        get => _lastCleanSummary ?? Strings.Current.NoCleanYet;
        private set => Set(ref _lastCleanSummary, value);
    }

    /// <summary>Gets the readings the chart draws, comma-separated, oldest first.</summary>
    public string ChartValues
    {
        get => _chartValues;
        private set => Set(ref _chartValues, value);
    }

    /// <summary>Gets the indices to mark as cleans, comma-separated.</summary>
    public string ChartMarkers
    {
        get => _chartMarkers;
        private set => Set(ref _chartMarkers, value);
    }

    /// <summary>Gets the value the top of the chart represents: the machine's total memory.</summary>
    public double ChartMaximum
    {
        get => _chartMaximum;
        private set => Set(ref _chartMaximum, value);
    }

    /// <summary>Gets how far back the chart reaches, in seconds, for the horizontal axis labels.</summary>
    public double ChartWindowSeconds => ChartWindowSecondsFor(_chartWindowIndex);

    /// <summary>Gets how much time the readings actually cover, in seconds; less than the window at first.</summary>
    public double ChartSpanSeconds
    {
        get => _chartSpanSeconds;
        private set => Set(ref _chartSpanSeconds, value);
    }

    private static double ChartWindowSecondsFor(int index)
        => WindowSeconds[Math.Max(0, Math.Min(index, WindowSeconds.Length - 1))];

    /// <summary>Gets the newest reading as used over total, so a percentage has something to be a percentage of.</summary>
    public string ChartUsedLabel
    {
        get
        {
            if (_memory.Count == 0)
                return string.Empty;

            var last = _memory[_memory.Count - 1];
            var total = last.Used + last.Standby + last.Free;

            return total <= 0
                ? string.Empty
                : string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "{0:0.#} / {1:0.#} GB",
                    last.Used / (1024d * 1024 * 1024),
                    total / (1024d * 1024 * 1024));
        }
    }

    /// <summary>Gets the label under the home page sparkline: which window it covers.</summary>
    public string ChartWindowLabel => _chartWindowIndex switch
    {
        0 => Strings.Current.LastMinute,
        1 => Strings.Current.Last5Minutes,
        2 => Strings.Current.Last15Minutes,
        _ => Strings.Current.LastHour,
    };

    /// <summary>Gets the usage percentage at which automatic cleaning fires; 0 when it is off.</summary>
    public double ChartThreshold => Settings.LowMemoryPercent;

    /// <summary>Gets the newest reading as a percentage, so the card can say where the line ends.</summary>
    public string ChartCurrentLabel
    {
        get
        {
            if (_memory.Count == 0)
                return string.Empty;

            var last = _memory[_memory.Count - 1];
            var total = last.Used + last.Standby + last.Free;

            return total <= 0
                ? string.Empty
                : (last.Used * 100.0 / total).ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%";
        }
    }

    /// <summary>Gets a value indicating whether there are enough readings to draw a line.</summary>
    public bool HasChartData => _memory.Count >= 2;

    /// <summary>Gets or sets which window the chart covers: 5 minutes, 15 minutes, or the whole hour.</summary>
    public int ChartWindowIndex
    {
        get => _chartWindowIndex;
        set
        {
            if (Set(ref _chartWindowIndex, value))
            {
                Raise(nameof(ChartWindowSeconds));
                Raise(nameof(ChartWindowLabel));
                RefreshChart();
            }
        }
    }

    /// <summary>Gets the names that can be excluded.</summary>
    public ObservableCollection<string> AvailableProcesses { get; } = new();

    /// <summary>Gets a value indicating whether any name is excluded, for the empty state.</summary>
    public bool HasExclusions => ExcludedProcesses.Count > 0;

    /// <summary>Gets the excluded names.</summary>
    public ObservableCollection<string> ExcludedProcesses { get; } = new();

    /// <summary>Gets or sets the total the shown rows hold.</summary>
    public string ProcessUsageTotal { get; private set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the process section is open.</summary>
    public bool ProcessUsageExpanded
    {
        get => _processUsageExpanded;
        set
        {
            if (Set(ref _processUsageExpanded, value) && value)
                RefreshProcessUsage();
        }
    }

    /// <summary>Gets or sets the filter applied to the process picker.</summary>
    public string ProcessSearchText
    {
        get => _processSearchText;
        set
        {
            if (Set(ref _processSearchText, value))
                RefreshAvailableProcesses();
        }
    }

    /// <summary>Gets a value indicating whether any candidate is available.</summary>
    public bool HasAvailableProcesses => AvailableProcesses.Count > 0;

    /// <summary>
    /// Gets or sets a value indicating whether the candidate list is showing. It floats over the whitelist
    /// rather than pushing it down, so the page does not move while the user is searching.
    /// </summary>
    public bool IsPickerOpen
    {
        get => _isPickerOpen;
        set => Set(ref _isPickerOpen, value);
    }

    /// <summary>Gets or sets the selected name in the picker.</summary>
    public string SelectedProcess
    {
        get => _selectedProcess;
        set
        {
            if (Set(ref _selectedProcess, value))
                ((RelayCommand)AddExclusionCommand).RaiseCanExecuteChanged();
        }
    }

    /// <summary>Gets the command that adds the selected name to the exclusion list.</summary>
    public ICommand AddExclusionCommand { get; }

    /// <summary>Gets the command that trims one process; the row passes its own name in.</summary>
    public ICommand OptimizeProcessCommand { get; }

    /// <summary>Gets the command that moves the selected name one place up.</summary>
    public ICommand MoveUpCommand { get; private set; } = null!;

    /// <summary>Gets the command that moves the selected name one place down.</summary>
    public ICommand MoveDownCommand { get; private set; } = null!;

    /// <summary>Gets the command that removes a name from the exclusion list.</summary>
    public ICommand RemoveExclusionCommand { get; }

    private void ResetSettings()
    {
        Settings.ShowCommitted = true;
        Settings.DetailedMemory = true;
        Settings.FontSizeIndex = 1;
        Settings.CloseToTray = true;
        Settings.StartMinimized = true;
        Settings.AutoCleanSafeOnly = true;
        Settings.AutoCleanMinutes = 0;
        Settings.LowMemoryPercent = 0;
        Settings.ExcludedProcesses.Clear();
        Settings.Save();

        foreach (var name in new[] { nameof(ShowCommitted), nameof(DetailedMemory), nameof(FontSizeIndex), nameof(FontSize), nameof(StartMinimized) })
            Raise(name);

        RefreshExcluded();
        RefreshAvailableProcesses();
    }

    /// <summary>
    /// Rebuilds the area rows from the catalog, keeping the ticks the user set.
    /// </summary>
    private void RebuildAreas()
    {
        var chosen = Areas.Where(a => a.IsSelected).Select(a => a.Reclaimer.Name).ToHashSet(StringComparer.Ordinal);

        Areas.Clear();

        var texts = Strings.Current.Areas;

        for (var i = 0; i < texts.Count; i++)
        {
            var item = new AreaItem(AllReclaimers[i], texts[i].Title, texts[i].Risk);

            if (chosen.Count > 0)
                item.IsSelected = chosen.Contains(item.Reclaimer.Name);

            Areas.Add(item);
        }

        (CleanCommand as RelayCommand)?.RaiseCanExecuteChanged();
    }

    /// <summary>
    /// Rebuilds the process table. Twelve rows is what the two-column home page shows without scrolling; the list is
    /// the one thing on that page that would otherwise push the clean button off the bottom.
    /// </summary>
    public void RefreshProcessUsage()
    {
        ProcessUsageItems.Clear();

        foreach (var row in ProcessUsageReader.Read(12))
            ProcessUsageItems.Add(row);

        var total = ProcessUsageItems.Sum(r => r.PrivateWorkingSet);

        ProcessUsageTotal = Strings.Current.ProcessTotal(total);

        Raise(nameof(ProcessUsageTotal));
    }

    /// <summary>
    /// Trims one process and refreshes the table, so the row's size reflects what is left.
    /// </summary>
    /// <param name="name">The process name the row carries.</param>
    public void OptimizeProcess(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        var result = ProcessOptimizer.Trim(name!);

        RefreshProcessUsage();

        Summary = result.Succeeded
            ? Strings.Current.Trimmed(name!, Math.Max(0, result.FreedBytes))
            : Strings.Current.TrimFailed(name!, result.Message ?? Strings.Current.UnknownReason);

        Raise(nameof(Summary));
    }

    /// <summary>
    /// Records a reading for the chart. Called on a slower beat than the display refresh: one point every few
    /// seconds is enough to draw a line, and the per-second readings would only thicken it.
    /// </summary>
    /// <param name="snapshot">The reading.</param>
    public void SampleMemory(MemorySnapshot snapshot)
        => SampleMemory(snapshot.Total - snapshot.Available, snapshot.Standby, snapshot.Free);

    /// <summary>
    /// Records a reading from raw numbers, so a caller that already has the counters does not have to build a
    /// snapshot first.
    /// </summary>
    /// <param name="used">Memory in use, in bytes.</param>
    /// <param name="standby">Standby memory, in bytes.</param>
    /// <param name="free">Free memory, in bytes.</param>
    public void SampleMemory(long used, long standby, long free)
        => SampleMemory(used, standby, free, DateTime.Now);

    /// <summary>
    /// Records a reading taken at a known time, so a caller replaying history does not have to pretend it all
    /// happened just now.
    /// </summary>
    /// <param name="used">Memory in use, in bytes.</param>
    /// <param name="standby">Standby memory, in bytes.</param>
    /// <param name="free">Free memory, in bytes.</param>
    /// <param name="at">When the reading was taken.</param>
    public void SampleMemory(long used, long standby, long free, DateTime at)
    {
        _memory.Add(new MemorySample(at, used, standby, free));

        RefreshChart();
    }

    /// <summary>
    /// Rebuilds the chart's inputs from the readings held for the selected window.
    /// </summary>
    public void RefreshChart()
    {
        var window = WindowSeconds[Math.Max(0, Math.Min(_chartWindowIndex, WindowSeconds.Length - 1))];
        var bucket = BucketSeconds[Math.Max(0, Math.Min(_chartWindowIndex, BucketSeconds.Length - 1))];
        var buckets = window / bucket;

        var now = DateTime.Now;
        var windowStart = now.AddSeconds(-window);

        // Each bucket is the time-weighted mean of the readings that fall inside it. A five-second bucket
        // holds two whole two-second readings and half of a third, so the halves are weighted rather than
        // rounded away - otherwise the line would step at every bucket boundary.
        var sums = new double[buckets];
        var weights = new double[buckets];
        var lowest = new double[buckets];
        var highest = new double[buckets];
        var opening = new double[buckets];
        var closing = new double[buckets];
        var seen = new bool[buckets];

        for (var i = 0; i < _memory.Count; i++)
        {
            var sample = _memory[i];
            var start = sample.At;
            var end = start.AddSeconds(SampleSeconds);

            if (end <= windowStart || start >= now)
                continue;

            var first = Math.Max(0, (int)Math.Floor((start - windowStart).TotalSeconds / bucket));
            var last = Math.Min(buckets - 1, (int)Math.Floor((end.AddTicks(-1) - windowStart).TotalSeconds / bucket));

            for (var b = first; b <= last; b++)
            {
                var bucketStart = windowStart.AddSeconds(b * bucket);
                var bucketEnd = bucketStart.AddSeconds(bucket);
                var overlap = (Earlier(end, bucketEnd) - Later(start, bucketStart)).TotalSeconds;

                if (overlap <= 0)
                    continue;

                sums[b] += sample.Used * overlap;
                weights[b] += overlap;

                if (!seen[b])
                {
                    lowest[b] = highest[b] = opening[b] = sample.Used;
                    seen[b] = true;
                }
                else
                {
                    lowest[b] = Math.Min(lowest[b], sample.Used);
                    highest[b] = Math.Max(highest[b], sample.Used);
                }

                closing[b] = sample.Used;
            }
        }

        var firstUsed = -1;

        for (var b = 0; b < buckets; b++)
        {
            if (weights[b] <= 0)
                continue;

            firstUsed = b;

            break;
        }

        if (firstUsed < 0 || buckets - firstUsed < 2)
        {
            ChartValues = string.Empty;
            ChartMarkers = string.Empty;
            ChartMaximum = 0;
            ChartSpanSeconds = 0;

            Raise(nameof(ChartValues));
            Raise(nameof(ChartMarkers));
            Raise(nameof(ChartMaximum));
            Raise(nameof(ChartSpanSeconds));
            Raise(nameof(HasChartData));
            Raise(nameof(ChartCurrentLabel));
            Raise(nameof(ChartUsedLabel));

            return;
        }

        var newest = _memory[_memory.Count - 1];
        var maximum = newest.Used + newest.Standby + newest.Free;

        var values = new List<double>(buckets - firstUsed);

        for (var b = firstUsed; b < buckets; b++)
        {
            // a bucket with no readings keeps the previous value rather than punching a hole in the line
            if (!seen[b])
            {
                values.Add(values.Count > 0 ? values[values.Count - 1] : 0);

                continue;
            }

            var mean = sums[b] / weights[b];
            var swing = maximum > 0 ? (highest[b] - lowest[b]) / maximum * 100 : 0;

            // A quiet bucket is its mean. A bucket with a real swing is not: averaging a clean away is how a
            // six-percent drop becomes a three-percent one, so the bucket reports the end of the swing that
            // made it interesting - the trough for a fall, the peak for a rise.
            if (swing < SwingPercent)
            {
                values.Add(mean);

                continue;
            }

            // The direction decides first: a bucket that ends lower than it started is a fall, and the fall is
            // the thing worth keeping. Only when it ends where it started - a spike or a dip that came back -
            // does the extreme furthest from the mean win.
            if (closing[b] < opening[b])
            {
                values.Add(lowest[b]);

                continue;
            }

            if (closing[b] > opening[b])
            {
                values.Add(highest[b]);

                continue;
            }

            values.Add(highest[b] - mean > mean - lowest[b] ? highest[b] : lowest[b]);
        }

        ChartMaximum = maximum;
        ChartValues = string.Join(",", values.Select(v => (long)v));
        ChartSpanSeconds = (buckets - firstUsed) * bucket;

        var markers = new List<int>();

        foreach (var record in _cleans.Records)
        {
            var b = (int)Math.Floor((record.At - windowStart).TotalSeconds / bucket);

            if (b < firstUsed || b >= buckets)
                continue;

            markers.Add(b - firstUsed);
        }

        ChartMarkers = string.Join(",", markers);

        Raise(nameof(ChartValues));
        Raise(nameof(ChartMarkers));
        Raise(nameof(ChartMaximum));
        Raise(nameof(ChartSpanSeconds));
        Raise(nameof(HasChartData));
        Raise(nameof(ChartCurrentLabel));
    }

    private string TitleOf(string name)
        => Areas.FirstOrDefault(a => a.Reclaimer.Name == name)?.Title ?? name;

    private static DateTime Later(DateTime a, DateTime b) => a > b ? a : b;

    private static DateTime Earlier(DateTime a, DateTime b) => a < b ? a : b;

    /// <summary>
    /// Rebuilds the two lists the statistics page shows: the last run's areas, and the runs themselves.
    /// </summary>
    public void RefreshStatistics()
    {
        LastCleanAreas.Clear();
        CleanRecords.Clear();

        var latest = _cleans.Latest;

        if (latest != null)
        {
            foreach (var (area, freed) in latest.Areas)
                LastCleanAreas.Add(new AreaResultRow(TitleOf(area), freed));

            LastCleanSummary = Strings.Current.LastCleanSummary(latest.At, latest.Automatic, latest.Duration.TotalSeconds);
        }
        else
        {
            LastCleanSummary = Strings.Current.NoCleanYet;
        }

        foreach (var record in _cleans.Records)
            CleanRecords.Add(new CleanRecordRow(record.At, record.Automatic, record.TotalFreed));

        Raise(nameof(LastCleanSummary));
        Raise(nameof(HasCleanRecords));
    }

    /// <summary>
    /// Records a run that has already happened. The window calls this after a clean; the screenshot tool calls
    /// it to render the page with something in it.
    /// </summary>
    /// <param name="automatic">Whether the policy started the run.</param>
    /// <param name="duration">How long it took.</param>
    /// <param name="areas">What each area released, in bytes.</param>
    /// <param name="at">When the run finished; the current time when omitted.</param>
    public void RecordClean(bool automatic, TimeSpan duration, IReadOnlyList<(string Area, long Freed)> areas, DateTime? at = null)
    {
        _cleans.Add(new CleanRecord(at ?? DateTime.Now, automatic, duration, areas));

        RefreshStatistics();
        RefreshChart();
    }

    private void RecordResults(bool automatic, TimeSpan duration, IReadOnlyList<(string Name, ReclaimResult Result)> results)
    {
        // the history lives in memory and dies with the process; the log is what survives, so a report of
        // "cleaning did nothing" has something to look at
        var released = results.Where(r => r.Result.Succeeded).Sum(r => Math.Max(0, r.Result.FreedBytes));
        var failed = results.Count(r => !r.Result.Succeeded);

        Log.Info(string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "clean {0}: {1} areas, {2} bytes, {3} failed, {4:0.0}s",
            automatic ? "automatic" : "manual",
            results.Count,
            released,
            failed,
            duration.TotalSeconds));

        foreach (var (name, result) in results.Where(r => !r.Result.Succeeded))
            Log.Error("clean area " + name + " failed: " + (result.Message ?? "unknown"));

        RecordClean(
            automatic,
            duration,
            results
                // the stable name is stored, not the title: a record made in one language has to read
                // correctly in the other, and the title is a translation
                .Select(r => (Area: r.Name, Freed: r.Result.FreedBytes))
                .ToList());
    }

    /// <summary>
    /// Builds the history as CSV, one row per area per run. Splitting by area is the point: what the user
    /// wants to know after exporting is which areas actually release anything.
    /// </summary>
    /// <returns>The file's contents.</returns>
    public string BuildHistoryCsv()
    {
        var builder = new System.Text.StringBuilder();

        builder.AppendLine(Strings.Current.CsvHeader);

        foreach (var record in _cleans.Records)
        {
            foreach (var (area, freed) in record.Areas)
            {
                var bytes = Math.Max(0, freed);

                builder
                    .Append(record.At.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture)).Append(',')
                    .Append(record.Automatic ? Strings.Current.Automatic : Strings.Current.Manual).Append(',')
                    .Append('"').Append(TitleOf(area).Replace("\"", "\"\"")).Append('"').Append(',')
                    .Append(bytes).Append(',')
                    .Append((bytes / (1024d * 1024 * 1024)).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture))
                    .AppendLine();
            }
        }

        return builder.ToString();
    }

    private void RefreshAvailableProcesses()
    {
        var names = System.Diagnostics.Process.GetProcesses()
            .Select(p => p.ProcessName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(n => !Settings.ExcludedProcesses.Contains(n))
            .Where(n => string.IsNullOrEmpty(_processSearchText)
                        || n.IndexOf(_processSearchText, StringComparison.OrdinalIgnoreCase) >= 0)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase);

        AvailableProcesses.Clear();

        foreach (var name in names)
            AvailableProcesses.Add(name);

        Raise(nameof(HasAvailableProcesses));

        if (!AvailableProcesses.Contains(_selectedProcess))
            SelectedProcess = AvailableProcesses.FirstOrDefault() ?? string.Empty;
    }

    private void RefreshExcluded()
    {
        ExcludedProcesses.Clear();

        foreach (var name in Settings.ExcludedProcesses.OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
            ExcludedProcesses.Add(name);

        Raise(nameof(HasExclusions));

        // the first row cannot move up and the last cannot move down, so every row re-queries
        (MoveUpCommand as RelayCommand<string>)?.RaiseCanExecuteChanged();
        (MoveDownCommand as RelayCommand<string>)?.RaiseCanExecuteChanged();
    }

    private static int NearestStop(int[] stops, int value)
    {
        var best = 0;

        for (var i = 1; i < stops.Length; i++)
        {
            if (Math.Abs(stops[i] - value) < Math.Abs(stops[best] - value))
                best = i;
        }

        return best;
    }

    private void AddExclusion(string? name)
    {
        if (string.IsNullOrWhiteSpace(name) || Settings.ExcludedProcesses.Contains(name!))
            return;

        Settings.ExcludedProcesses.Add(name!);

        Settings.Save();
        RefreshExcluded();
        RefreshAvailableProcesses();

        // the picker closes itself once the name is in, so the whitelist is visible again
        IsPickerOpen = false;
    }

    private bool CanMove(string? name, int delta)
    {
        var index = ExcludedProcesses.IndexOf(name ?? string.Empty);

        return index >= 0 && index + delta >= 0 && index + delta < ExcludedProcesses.Count;
    }

    private void Move(string? name, int delta)
    {
        var index = ExcludedProcesses.IndexOf(name ?? string.Empty);

        if (index < 0 || index + delta < 0 || index + delta >= ExcludedProcesses.Count)
            return;

        ExcludedProcesses.Move(index, index + delta);

        Settings.ExcludedProcesses.Clear();
        Settings.ExcludedProcesses.AddRange(ExcludedProcesses);

        Settings.Save();
        SelectedExclusion = name!;

        // the first row cannot move up and the last cannot move down, so both rows re-query
        (MoveUpCommand as RelayCommand<string>)?.RaiseCanExecuteChanged();
        (MoveDownCommand as RelayCommand<string>)?.RaiseCanExecuteChanged();
    }

    private void RemoveExclusion(string? name)
    {
        if (string.IsNullOrWhiteSpace(name) || !Settings.ExcludedProcesses.Remove(name!))
            return;

        Settings.Save();
        RefreshExcluded();
        RefreshAvailableProcesses();
    }

    /// <summary>Raised after a manual run with the one-line summary.</summary>
    public event Action<string>? CleanCompleted;

    /// <summary>
    /// Gets or sets a value indicating whether the program starts at logon. Failures roll the switch back and
    /// raise <see cref="CleanCompleted"/> with the reason, because a check that lies is worse than no check.
    /// </summary>
    public bool RunOnStartup
    {
        get => Settings.RunOnStartup;
        set
        {
            if (!AutoStart.Set(value))
            {
                CleanCompleted?.Invoke(value ? Strings.Current.StartupEnableFailed : Strings.Current.StartupDisableFailed);
                Raise();

                return;
            }

            Settings.RunOnStartup = value;
            Settings.Save();
            Raise();
        }
    }

    /// <summary>Gets or sets a value indicating whether the compact view is shown.</summary>
    public bool CompactMode
    {
        get => Settings.CompactMode;
        set
        {
            Settings.CompactMode = value;
            Settings.Save();
            Raise();
        }
    }

    /// <summary>Gets or sets a value indicating whether the window stays above others.</summary>
    public bool AlwaysOnTop
    {
        get => Settings.AlwaysOnTop;
        set
        {
            Settings.AlwaysOnTop = value;
            Settings.Save();
            Raise();
        }
    }

    /// <summary>Gets or sets the active segment line.</summary>
    public string ActiveText
    {
        get => _activeText;
        set => Set(ref _activeText, value);
    }

    /// <summary>Gets or sets the modified segment line.</summary>
    public string ModifiedText
    {
        get => _modifiedText;
        set => Set(ref _modifiedText, value);
    }

    /// <summary>Gets or sets the standby segment line.</summary>
    public string StandbyText
    {
        get => _standbyText;
        set => Set(ref _standbyText, value);
    }

    /// <summary>Gets or sets the free segment line.</summary>
    public string FreeText
    {
        get => _freeText;
        set => Set(ref _freeText, value);
    }

    /// <summary>Gets the used percentage shown in the compact view.</summary>
    public string UsedPercentText
    {
        get => _usedPercentText;
        set => Set(ref _usedPercentText, value);
    }

    /// <summary>Gets the used / total line shown in the compact view.</summary>
    public string UsedText
    {
        get => _usedText;
        set => Set(ref _usedText, value);
    }

    /// <summary>
    /// Gets the available / total line shown under the ring. Available is free plus standby, which is the
    /// number that answers "can I open another program" - and the one the whole product argues about.
    /// </summary>
    public string AvailableText
    {
        get => _availableText;
        set => Set(ref _availableText, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the clean hotkey is active. Registration failures are reported
    /// through <see cref="CleanCompleted"/> and the switch rolls back.
    /// </summary>
    public bool HotkeyEnabled
    {
        get => Settings.HotkeyEnabled;
        set
        {
            Settings.HotkeyEnabled = value;
            Settings.Save();
            Raise();

            HotkeyChanged?.Invoke();
        }
    }

    /// <summary>Raised whenever the hotkey settings changed and the window must re-register.</summary>
    public event Action? HotkeyChanged;

    /// <summary>Gets or sets the hotkey modifier flags.</summary>
    public int HotkeyModifiers
    {
        get => Settings.HotkeyModifiers;
        set
        {
            Settings.HotkeyModifiers = value;
            Settings.Save();
            Raise();
            HotkeyChanged?.Invoke();
        }
    }

    /// <summary>Gets or sets the hotkey virtual-key code.</summary>
    public int HotkeyKey
    {
        get => Settings.HotkeyKey;
        set
        {
            Settings.HotkeyKey = value;
            Settings.Save();
            Raise();
            HotkeyChanged?.Invoke();
        }
    }

    /// <summary>Reports that the hotkey could not be registered.</summary>
    /// <param name="reason">Why.</param>
    public void ReportHotkeyFailure(string reason) => CleanCompleted?.Invoke(reason);

    /// <summary>Gets when the last successful clean finished.</summary>
    public DateTime? LastCleanAt { get; private set; }

    /// <summary>Gets a short description of when the last clean ran.</summary>
    public string LastCleanText
    {
        get
        {
            if (LastCleanAt == null)
                return Strings.Current.NothingCleanedYet;

            var minutes = (int)(DateTime.Now - LastCleanAt.Value).TotalMinutes;

            return minutes < 1 ? Strings.Current.CleanedJustNow : Strings.Current.CleanedMinutesAgo(minutes);
        }
    }

    /// <summary>Gets or sets the row the whitelist has highlighted. The row buttons act on their own row, not on this.</summary>
    public string SelectedExclusion
    {
        get => _selectedExclusion;
        set => Set(ref _selectedExclusion, value);
    }

    /// <summary>Gets or sets a value indicating whether closing hides the window instead of quitting.</summary>
    public bool CloseToTray
    {
        get => Settings.CloseToTray;
        set
        {
            Settings.CloseToTray = value;
            Settings.Save();
            Raise();
        }
    }

    /// <summary>Gets or sets a value indicating whether the window starts hidden in the tray.</summary>
    public bool StartMinimized
    {
        get => Settings.StartMinimized;
        set
        {
            Settings.StartMinimized = value;
            Settings.Save();
            Raise();
        }
    }

    /// <summary>Gets or sets a value indicating whether automatic cleaning stays on the conservative subset.</summary>
    public bool AutoCleanSafeOnly
    {
        get => Settings.AutoCleanSafeOnly;
        set
        {
            Settings.AutoCleanSafeOnly = value;
            Settings.Save();
            Raise();
        }
    }

    /// <summary>
    /// Gets or sets the interval stop. The slider moves between stops, not minutes: "every 37 minutes"
    /// is not a decision anyone makes, and a 121-position track cannot be hit accurately with a mouse.
    /// </summary>
    public int AutoCleanIntervalIndex
    {
        get => NearestStop(IntervalStops, Settings.AutoCleanMinutes);
        set
        {
            Settings.AutoCleanMinutes = IntervalStops[Math.Max(0, Math.Min(value, IntervalStops.Length - 1))];
            Settings.Save();
            Raise();
            Raise(nameof(AutoCleanStatus));
        }
    }

    /// <summary>Gets or sets the low-memory stop, in the same terms as the interval.</summary>
    public int AutoCleanThresholdIndex
    {
        get => NearestStop(ThresholdStops, Settings.LowMemoryPercent);
        set
        {
            Settings.LowMemoryPercent = ThresholdStops[Math.Max(0, Math.Min(value, ThresholdStops.Length - 1))];
            Settings.Save();
            Raise();
            Raise(nameof(AutoCleanStatus));
        }
    }

    /// <summary>
    /// Gets the one-line description the home page shows while automatic cleaning is on. It is the only
    /// place the user learns why memory changed on its own, so it names both triggers.
    /// </summary>
    public string AutoCleanStatus
        => Strings.Current.AutoCleanStatus(Settings.AutoCleanMinutes, Settings.LowMemoryPercent);

    /// <summary>Gets or sets a value indicating whether cleaning raises a tray notification.</summary>
    public bool ShowNotifications
    {
        get => Settings.ShowNotifications;
        set
        {
            Settings.ShowNotifications = value;
            Settings.Save();
            Raise();
        }
    }

    /// <summary>Gets or sets a value indicating whether the committed line is shown.</summary>
    public bool ShowCommitted
    {
        get => Settings.ShowCommitted;
        set
        {
            Settings.ShowCommitted = value;
            Settings.Save();
            Raise();
        }
    }

    /// <summary>Gets or sets a value indicating whether the four-part breakdown is shown.</summary>
    public bool DetailedMemory
    {
        get => Settings.DetailedMemory;
        set
        {
            Settings.DetailedMemory = value;
            Settings.Save();
            Raise();
        }
    }

    /// <summary>Gets or sets the font size index: 0 small, 1 normal, 2 large.</summary>
    public int FontSizeIndex
    {
        get => Settings.FontSizeIndex;
        set
        {
            Settings.FontSizeIndex = value;
            Settings.Save();
            Raise();
            Raise(nameof(FontSize));
        }
    }

    /// <summary>Gets the window font size for the current index.</summary>
    public double FontSize => Settings.FontSizeIndex switch
    {
        0 => 12,
        2 => 18,
        _ => 14,
    };

    /// <summary>Gets the command that restores the defaults.</summary>
    public System.Windows.Input.ICommand ResetSettingsCommand { get; private set; } = null!;

    /// <summary>Gets or sets the four-segment bar values.</summary>
    public string PhysicalSegments
    {
        get => _physicalSegments;
        set => Set(ref _physicalSegments, value);
    }

    /// <summary>Gets or sets the four-segment bar colours.</summary>
    public string PhysicalPalette => "#E2523B,#9E9E9E,#A5D66A,#43B047";

    /// <summary>Gets or sets the committed bar values.</summary>
    public string CommitSegments
    {
        get => _commitSegments;
        set => Set(ref _commitSegments, value);
    }

    /// <summary>Gets or sets the committed bar colours.</summary>
    public string CommitPalette => "#E2523B,#43B047";

    /// <summary>Gets the areas shown on the home tab.</summary>
    public ObservableCollection<AreaItem> Areas { get; }

    /// <summary>Gets the clean command.</summary>
    public ICommand CleanCommand { get; }

    /// <summary>Gets or sets a value indicating whether a run is in progress.</summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (Set(ref _isBusy, value))
                (CleanCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    /// <summary>Gets or sets the summary of the last run.</summary>
    public string Summary
    {
        get => _summary ?? Strings.Current.NothingCleanedYet;
        private set => Set(ref _summary, value);
    }

    /// <summary>
    /// Runs the automatic policy: interval or low-memory, then the narrowed area set.
    /// </summary>
    /// <param name="now">The current time.</param>
    /// <returns>What happened, for the caller to report.</returns>
    public async Task<string?> TryAutoCleanAsync(DateTime now)
    {
        var snapshot = MemorySnapshot.Capture();
        var byInterval = _policy.IsIntervalDue(now, Settings.AutoCleanMinutes);
        var byMemory = AutoCleanPolicy.IsMemoryDue(snapshot, Settings.LowMemoryPercent);

        if (!byInterval && !byMemory)
            return null;

        var areas = AutoCleanPolicy.SelectForRun(
            Areas.Where(a => a.IsSelected).Select(a => a.Reclaimer.Name),
            automatic: true,
            safeOnly: Settings.AutoCleanSafeOnly);

        if (areas.Count == 0)
            return null;

        _policy.MarkIntervalRun(now);

        var started = DateTime.Now;
        var results = await Task.Run(() => _engine.Run(areas));

        foreach (var (name, result) in results)
            Areas.FirstOrDefault(a => a.Reclaimer.Name == name)?.Apply(result);

        RecordResults(automatic: true, DateTime.Now - started, results);

        LastCleanAt = DateTime.Now;
        Raise(nameof(LastCleanText));

        var failed = results.Count(r => !r.Result.Succeeded);
        var released = results.Where(r => r.Result.Succeeded).Sum(r => Math.Max(0, r.Result.FreedBytes));

        return failed > 0
            ? Strings.Current.AutoCleanPartial(results.Count - failed, failed)
            : Strings.Current.AutoCleanFreed(released);
    }

    private async void Clean()
    {
        var selected = Areas.Where(a => a.IsSelected).ToList();

        if (selected.Count == 0)
            return;

        IsBusy = true;
        Summary = Strings.Current.Cleaning;

        try
        {
            var started = DateTime.Now;
            var results = await Task.Run(() => _engine.Run(selected.Select(a => a.Reclaimer.Name)));

            foreach (var (name, result) in results)
            {
                var row = Areas.FirstOrDefault(a => a.Reclaimer.Name == name);

                row?.Apply(result);
            }

            RecordResults(automatic: false, DateTime.Now - started, results);

            var released = results.Where(r => r.Result.Succeeded).Sum(r => Math.Max(0, r.Result.FreedBytes));
            var failed = results.Count(r => !r.Result.Succeeded);

            LastCleanAt = DateTime.Now;
            Raise(nameof(LastCleanText));

            CleanCompleted?.Invoke(Summary);

            Summary = failed > 0
                ? Strings.Current.PartialRun(selected.Count - failed, failed)
                : released > 0
                    ? Strings.Current.FreedThisRun(released)
                    : Strings.Current.NothingMeasured;
        }
        catch (Exception exception)
        {
            // an async void method that throws takes the whole process down, and a cleaner that crashes
            // while cleaning is worse than one that reports it could not
            Log.Error("clean failed", exception);

            Summary = Strings.Current.CleanFailed(exception.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
