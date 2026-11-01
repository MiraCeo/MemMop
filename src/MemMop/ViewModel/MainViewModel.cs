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
    private bool _isBusy;
    private string _summary = string.Empty;
    private string _physicalText = string.Empty;
    private string _segmentText = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    public MainViewModel()
    {
        var reclaimers = new IMemoryReclaimer[]
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

        // the texts mirror SPEC §2.1: a name that says what happens, and a cost that is not hidden
        Areas = new ObservableCollection<AreaItem>
        {
            new(new StandbyListReclaimer(), "清理待机缓存", "清空文件缓存，再次读取需重新读盘"),
            new(new StandbyListReclaimer(true), "仅清理低优先级待机缓存", "只清理最冷的待机页，与上一项互斥"),
            new(new ModifiedPageListReclaimer(), "写回已修改内存", "等待未写盘的数据落盘，这一步最慢"),
            new(new CombinedPageListReclaimer(), "合并重复内存页", "合并内容相同的物理页，不删除数据"),
            new(new SystemFileCacheReclaimer(), "收缩系统缓存", "降低文件缓存上限，之后读写可能变慢"),
            new(new RegistryCacheReclaimer(), "注册表缓存整理", "整理注册表的内存缓存，收益不确定"),
            new(new WorkingSetReclaimer(), "收缩程序驻留内存", "移出各程序驻留的内存，重用时可能卡顿"),
        };

        CleanCommand = new RelayCommand(Clean, () => !IsBusy && Areas.Any(a => a.IsSelected));
        AddExclusionCommand = new RelayCommand(() => AddExclusion(SelectedProcess), () => !string.IsNullOrEmpty(SelectedProcess));
        RemoveExclusionCommand = new RelayCommand(() => { }, () => true);

        RefreshExcluded();
        RefreshAvailableProcesses();
    }

    /// <summary>Gets or sets the physical memory headline.</summary>
    public string PhysicalText
    {
        get => _physicalText;
        set => Set(ref _physicalText, value);
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

    /// <summary>Gets the process rows, largest first.</summary>
    public ObservableCollection<ProcessUsage> ProcessUsageItems { get; } = new();

    /// <summary>Gets the names that can be excluded.</summary>
    public ObservableCollection<string> AvailableProcesses { get; } = new();

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

    /// <summary>Gets the command that removes a name from the exclusion list.</summary>
    public ICommand RemoveExclusionCommand { get; }

    /// <summary>
    /// Rebuilds the process table.
    /// </summary>
    public void RefreshProcessUsage()
    {
        ProcessUsageItems.Clear();

        foreach (var row in ProcessUsageReader.Read(10))
            ProcessUsageItems.Add(row);

        var total = ProcessUsageItems.Sum(r => r.PrivateWorkingSet);

        ProcessUsageTotal = string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "合计（专用工作集，不含共享页） {0:0.#} GB",
            total / (1024d * 1024 * 1024));

        Raise(nameof(ProcessUsageTotal));
    }

    private void RefreshAvailableProcesses()
    {
        var names = System.Diagnostics.Process.GetProcesses()
            .Select(p => p.ProcessName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(n => !Settings.ExcludedProcesses.Contains(n))
            .Where(n => string.IsNullOrEmpty(_processSearchText)
                        || n.Contains(_processSearchText, StringComparison.OrdinalIgnoreCase))
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase);

        AvailableProcesses.Clear();

        foreach (var name in names)
            AvailableProcesses.Add(name);

        if (!AvailableProcesses.Contains(_selectedProcess))
            SelectedProcess = AvailableProcesses.FirstOrDefault() ?? string.Empty;
    }

    private void RefreshExcluded()
    {
        ExcludedProcesses.Clear();

        foreach (var name in Settings.ExcludedProcesses.OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
            ExcludedProcesses.Add(name);
    }

    private void AddExclusion(string? name)
    {
        if (string.IsNullOrWhiteSpace(name) || !Settings.ExcludedProcesses.Add(name))
            return;

        Settings.Save();
        RefreshExcluded();
        RefreshAvailableProcesses();
        ((RelayCommand)RemoveExclusionCommand).RaiseCanExecuteChanged();
    }

    private void RemoveExclusion(string? name)
    {
        if (string.IsNullOrWhiteSpace(name) || !Settings.ExcludedProcesses.Remove(name))
            return;

        Settings.Save();
        RefreshExcluded();
        RefreshAvailableProcesses();
    }

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
                ((RelayCommand)CleanCommand).RaiseCanExecuteChanged();
        }
    }

    /// <summary>Gets or sets the summary of the last run.</summary>
    public string Summary
    {
        get => _summary;
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

        var results = await Task.Run(() => _engine.Run(areas));

        foreach (var (name, result) in results)
            Areas.FirstOrDefault(a => a.Reclaimer.Name == name)?.Apply(result);

        var failed = results.Count(r => !r.Result.Succeeded);
        var released = results.Where(r => r.Result.Succeeded).Sum(r => Math.Max(0, r.Result.FreedBytes));

        return failed > 0
            ? $"自动清理：{results.Count - failed} 项完成，{failed} 项失败"
            : string.Format(System.Globalization.CultureInfo.InvariantCulture, "自动清理：释放 {0:0.#} GB", released / (1024d * 1024 * 1024));
    }

    private async void Clean()
    {
        var selected = Areas.Where(a => a.IsSelected).ToList();

        if (selected.Count == 0)
            return;

        IsBusy = true;
        Summary = "清理中…";

        try
        {
            var results = await Task.Run(() => _engine.Run(selected.Select(a => a.Reclaimer.Name)));

            foreach (var (name, result) in results)
            {
                var row = Areas.FirstOrDefault(a => a.Reclaimer.Name == name);

                row?.Apply(result);
            }

            var released = results.Where(r => r.Result.Succeeded).Sum(r => Math.Max(0, r.Result.FreedBytes));
            var failed = results.Count(r => !r.Result.Succeeded);

            Summary = failed > 0
                ? $"{selected.Count - failed} 项完成，{failed} 项失败"
                : released > 0
                    ? string.Format(System.Globalization.CultureInfo.InvariantCulture, "本次释放 {0:0.#} GB", released / (1024d * 1024 * 1024))
                    : "本次无可测量的释放";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
