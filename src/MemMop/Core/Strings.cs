using System.ComponentModel;

namespace MemMop.Core;

/// <summary>
/// The user-visible strings, in both languages. One instance, exposed to XAML as a resource, so a language
/// change is a property-changed event rather than a restart.
/// </summary>
public sealed class Strings : INotifyPropertyChanged
{
    private static readonly Strings Instance = new();

    private bool _english;

    private Strings()
    {
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets the catalog.</summary>
    public static Strings Current => Instance;

    // ---- window chrome -------------------------------------------------------------------------------

    /// <summary>Gets the tooltip of the always-on-top button.</summary>
    public string AlwaysOnTop => _english ? "Always on top" : "窗口置顶";

    /// <summary>Gets the tooltip of the compact-view button.</summary>
    public string CompactView => _english ? "Compact view" : "紧凑视图";

    /// <summary>Gets the tooltip of the minimize button.</summary>
    public string Minimize => _english ? "Minimize" : "最小化";

    /// <summary>Gets the tooltip of the close button.</summary>
    public string Close => _english ? "Close" : "关闭";

    // ---- tabs ----------------------------------------------------------------------------------------

    /// <summary>Gets the tab header for the home view.</summary>
    public string Home => _english ? "Home" : "主页";

    /// <summary>Gets the tab header for the statistics view.</summary>
    public string Statistics => _english ? "Statistics" : "统计";

    /// <summary>Gets the tab header for the settings view.</summary>
    public string Settings => _english ? "Settings" : "设置";

    /// <summary>
    /// Gets the tab header for the whitelist view. English says "Exemptions" rather than "Whitelist":
    /// the list means "never cleaned", and whitelist/blacklist wording is being retired across software.
    /// </summary>
    public string Whitelist => _english ? "Exemptions" : "白名单";

    /// <summary>Gets the tab header for the about view.</summary>
    public string About => _english ? "About" : "关于";

    // ---- home ----------------------------------------------------------------------------------------

    /// <summary>Gets the clean button label.</summary>
    public string Clean => _english ? "Clean" : "清理";

    /// <summary>Gets the process section title.</summary>
    public string ProcessUsage => _english ? "Process usage" : "进程占用";

    /// <summary>Gets the tooltip of a row's trim button.</summary>
    public string OptimizeProcess => _english ? "Trim this process" : "优化这个进程";

    // ---- statistics ----------------------------------------------------------------------------------

    /// <summary>Gets the chart card's title.</summary>
    public string MemoryUsage => _english ? "Memory in use" : "内存占用";

    /// <summary>Gets the line shown while the chart has too few readings.</summary>
    public string Sampling => _english ? "sampling…" : "正在采样…";

    /// <summary>Gets the one-minute window label.</summary>
    public string LastMinute => _english ? "last minute" : "最近 1 分钟";

    /// <summary>Gets the five-minute window label.</summary>
    public string Last5Minutes => _english ? "last 5 minutes" : "最近 5 分钟";

    /// <summary>Gets the fifteen-minute window label.</summary>
    public string Last15Minutes => _english ? "last 15 minutes" : "最近 15 分钟";

    /// <summary>Gets the one-hour window label.</summary>
    public string LastHour => _english ? "last hour" : "最近 1 小时";

    /// <summary>Gets the last-clean card's title.</summary>
    public string LastClean => _english ? "Last clean" : "上次清理";

    /// <summary>Gets the history card's title.</summary>
    public string CleanHistory => _english ? "Clean history" : "清理记录";

    /// <summary>Gets the export button label.</summary>
    public string Export => _english ? "Export" : "导出";

    /// <summary>Gets the line shown when no run has been recorded.</summary>
    public string NoRecords => _english ? "nothing recorded yet" : "还没有记录";

    /// <summary>Gets the line shown when nothing has been cleaned yet.</summary>
    public string NoCleanYet => _english ? "nothing cleaned yet" : "还没有清理过";

    /// <summary>Gets the chart's "now" label.</summary>
    public string Now => _english ? "now" : "现在";

    /// <summary>Gets the chart's threshold label.</summary>
    /// <param name="percent">The threshold.</param>
    /// <returns>The label.</returns>
    public string AutoCleanThreshold(int percent) => _english
        ? $"auto clean {percent}%"
        : $"自动清理 {percent}%";

    /// <summary>Formats a distance in the past in seconds only, for an axis that keeps one unit.</summary>
    /// <param name="seconds">How long ago.</param>
    /// <returns>The label.</returns>
    public string SecondsAgo(int seconds) => _english ? $"{seconds}s ago" : $"{seconds} 秒前";

    /// <summary>Formats a distance in the past, in seconds, minutes or hours.</summary>
    /// <param name="seconds">How long ago.</param>
    /// <returns>The label.</returns>
    public string Ago(double seconds)
    {
        var whole = (int)Math.Round(seconds);

        if (whole < 60)
            return _english ? $"{whole}s ago" : $"{whole} 秒前";

        if (whole >= 3600)
        {
            var hours = whole / 3600;
            var rest = whole % 3600 / 60;

            if (rest == 0)
                return _english ? $"{hours}h ago" : $"{hours} 小时前";

            return _english ? $"{hours}h {rest}m ago" : $"{hours} 小时 {rest} 分前";
        }

        var minutes = whole / 60;
        var remainder = whole % 60;

        if (remainder == 0)
            return _english ? $"{minutes}m ago" : $"{minutes} 分钟前";

        return _english ? $"{minutes}m {remainder}s ago" : $"{minutes} 分 {remainder} 秒前";
    }

    // ---- settings ------------------------------------------------------------------------------------

    /// <summary>Gets the general settings card's title.</summary>
    public string GeneralSettings => _english ? "General" : "通用设置";

    /// <summary>Gets the caption of the language picker.</summary>
    public string Language => _english ? "Language" : "语言";

    /// <summary>Gets the Chinese language option.</summary>
    public string Chinese => _english ? "Chinese (Simplified)" : "简体中文";

    /// <summary>Gets the caption of the text-size picker.</summary>
    public string TextSize => _english ? "Text size" : "字号";

    /// <summary>Gets the small text-size option.</summary>
    public string SizeSmall => _english ? "small" : "小";

    /// <summary>Gets the normal text-size option.</summary>
    public string SizeNormal => _english ? "normal" : "标准";

    /// <summary>Gets the large text-size option.</summary>
    public string SizeLarge => _english ? "large" : "大";

    /// <summary>Gets the start-minimized switch.</summary>
    public string StartMinimized => _english ? "start minimized" : "静默启动";

    /// <summary>Gets the run-at-startup switch.</summary>
    public string RunOnStartup => _english ? "run at startup" : "开机自启";

    /// <summary>Gets the notification switch.</summary>
    public string ShowNotifications => _english ? "show clean notifications" : "显示清理通知";

    /// <summary>Gets the committed-memory switch.</summary>
    public string ShowCommitted => _english ? "show committed" : "显示已提交";

    /// <summary>Gets the detailed-memory switch.</summary>
    public string DetailedMemory => _english ? "detailed memory" : "详细内存信息";

    /// <summary>Gets the hotkey switch.</summary>
    public string EnableHotkey => _english ? "clean hotkey (Ctrl+Alt+M)" : "启用清理热键（Ctrl+Alt+M）";

    /// <summary>Gets the areas card's title.</summary>
    public string CleanAreas => _english ? "Areas to clean" : "清理区域";

    /// <summary>Gets the areas card's hint.</summary>
    public string CleanAreasHint => _english
        ? "Tick what to clean. The line under each is what it costs."
        : "勾选要清理的项目。每项下面写的是它的代价。";

    /// <summary>Gets the automatic-cleaning card's title.</summary>
    public string AutoClean => _english ? "Automatic cleaning" : "自动清理";

    /// <summary>Gets the interval caption.</summary>
    public string Interval => _english ? "Every" : "间隔";

    /// <summary>Gets the threshold caption.</summary>
    public string Threshold => _english ? "When in use exceeds" : "占用超过";

    /// <summary>Gets the "off" stop.</summary>
    public string Off => _english ? "off" : "关闭";

    /// <summary>Gets the fifteen-minute stop.</summary>
    public string Minutes15 => _english ? "15m" : "15 分";

    /// <summary>Gets the thirty-minute stop.</summary>
    public string Minutes30 => _english ? "30m" : "30 分";

    /// <summary>Gets the one-hour stop.</summary>
    public string Hour1 => _english ? "1h" : "1 小时";

    /// <summary>Gets the two-hour stop.</summary>
    public string Hour2 => _english ? "2h" : "2 小时";

    /// <summary>Gets the conservative-only switch.</summary>
    public string SafeOnly => _english
        ? "conservative only (no working sets, no system cache)"
        : "只做保守操作（不收缩工作集、不压系统缓存）";

    /// <summary>Gets the reset warning.</summary>
    public string ResetHint => _english
        ? "resetting clears every saved setting"
        : "重置会清除所有已保存的设置";

    /// <summary>Gets the reset button label.</summary>
    public string Reset => _english ? "Reset" : "重置";

    // ---- whitelist -----------------------------------------------------------------------------------

    /// <summary>Gets the exclusion section title.</summary>
    public string Exclusions => _english ? "Excluded processes" : "排除的进程（不参与收缩程序驻留内存）";

    /// <summary>Gets the whitelist card's title.</summary>
    public string WhitelistTitle => _english ? "Exemptions (never cleaned)" : "白名单（不参与清理）";

    /// <summary>Gets the add-process caption.</summary>
    public string SearchAndAdd => _english ? "Search and add a process" : "搜索并添加进程";

    /// <summary>Gets the move-up tooltip.</summary>
    public string MoveUp => _english ? "Move up" : "上移";

    /// <summary>Gets the move-down tooltip.</summary>
    public string MoveDown => _english ? "Move down" : "下移";

    /// <summary>Gets the remove tooltip.</summary>
    public string Remove => _english ? "Remove" : "删除";

    /// <summary>Gets the line shown when the search matches nothing.</summary>
    public string NoMatches => _english ? "no matching process" : "无匹配的进程";

    // ---- about ---------------------------------------------------------------------------------------

    /// <summary>
    /// Gets the program's name. The Chinese half is a pun that does not survive translation, so English
    /// uses the name alone.
    /// </summary>
    public string Name => _english ? "MemMop" : "忆干二净 MemMop";

    /// <summary>Gets the one-line description under the name.</summary>
    public string Tagline => _english
        ? "See what is holding memory, and release it when you mean to."
        : "看清内存被什么占着，并在需要时精确释放。";

    /// <summary>Gets the "what it is" card's title.</summary>
    public string WhatItIsTitle => _english ? "What it is" : "它是什么";

    /// <summary>Gets the opening position: the category has a bad name, and this is the other road.</summary>
    public string WhatItIs1 => _english
        ? "Memory tools have a bad name. What a \"one-click booster\" promises and what it does are rarely the same thing."
        : "内存工具这个品类名声不好。「一键加速」承诺的和实际做的，往往不是一回事。";

    /// <summary>Gets the description of what the program actually does.</summary>
    public string WhatItIs2 => _english
        ? "MemMop takes the other road: it lays the makeup of memory out in front of you - active, modified, standby, free - lists what is holding it, and lets you decide which part to touch."
        : "MemMop 走另一条路：把内存的构成摊开给你看——活动、已修改、待机、空闲——列出谁在占，然后让你决定动哪一块。";

    /// <summary>Gets the promise about costs and measurement.</summary>
    public string WhatItIs3 => _english
        ? "Every area says what it costs. What a run freed is measured before and after; when nothing can be measured, it says so."
        : "每一项清理都写明代价。释放量是清理前后实测的差值，测不出来就写「无变化」。";

    /// <summary>Gets the "how to check it" card's title.</summary>
    public string HowToCheckTitle => _english ? "How to check it" : "怎么验证";

    /// <summary>Gets the first verification step.</summary>
    public string HowToCheck1 => _english
        ? "You do not have to take my word for it. Open Resource Monitor (resmon.exe), Memory tab, and watch the blue Standby band - that is the cache left behind by programs you closed."
        : "不用信我。打开资源监视器（resmon.exe）的「内存」标签页，看蓝色的「待机」——那是已关闭程序的缓存。";

    /// <summary>Gets the second verification step.</summary>
    public string HowToCheck2 => _english
        ? "Open a few large programs, close them, and watch it grow. Then tick only \"Clear standby cache\" in MemMop and press Clean. However far the blue falls, the light green Available rises by the same amount."
        : "打开几个大程序再关掉，看它涨；然后在 MemMop 里只勾「清理待机缓存」，点清理。蓝色掉多少，浅绿的「可用」就涨多少。";

    /// <summary>Gets the technical card's title.</summary>
    public string Technical => _english ? "Technical" : "技术信息";

    /// <summary>Gets the runtime caption.</summary>
    public string Runtime => _english ? "Runtime" : "运行时";

    /// <summary>Gets the shape caption.</summary>
    public string Shape => _english ? "Shape" : "形态";

    /// <summary>Gets the single-file description.</summary>
    public string SingleFile => _english ? "one file, no dependencies" : "单文件，无外部依赖";

    /// <summary>Gets the settings caption.</summary>
    public string SettingsLocation => _english ? "Settings" : "设置";

    /// <summary>Gets the open-settings button label.</summary>
    public string OpenSettings => _english ? "Open settings location" : "打开设置位置";

    /// <summary>Gets the predecessor card's title.</summary>
    public string Predecessor => _english ? "Predecessor" : "前身";

    /// <summary>Gets the predecessor note.</summary>
    public string PredecessorText => _english
        ? "A new implementation, written from scratch. Its predecessor is MemMop-Archived (a modified WinMemoryCleaner, GPL v3); the two share no code."
        : "从零开始的新实现。前身是 MemMop-Archived（基于 WinMemoryCleaner 修改，GPL v3），两者的代码不共享。";

    // ---- cleaning ------------------------------------------------------------------------------------

    /// <summary>Gets the seven area titles.</summary>
    public IReadOnlyList<(string Title, string Risk)> Areas => _english
        ? new[]
        {
            ("Clear standby cache", "cached files must be read again"),
            ("Low priority standby only", "coldest pages only; exclusive with the entry above"),
            ("Write back modified memory", "waits for pending data to reach the disk"),
            ("Combine duplicate pages", "identical physical pages merge; nothing deleted"),
            ("Shrink system cache", "file access may slow down"),
            ("Reconcile registry cache", "reconciles the registry cache; the benefit varies"),
            ("Shrink process working sets", "resident pages move out; programs may stutter"),
        }
        : new[]
        {
            ("清理待机缓存", "清空文件缓存，再次读取需重新读盘"),
            ("仅清理低优先级待机缓存", "只清理最冷的待机页，与上一项互斥"),
            ("写回已修改内存", "等待未写盘的数据落盘，这一步最慢"),
            ("合并重复内存页", "合并内容相同的物理页，不删除数据"),
            ("收缩系统缓存", "降低文件缓存上限，之后读写可能变慢"),
            ("注册表缓存整理", "整理注册表的内存缓存，收益不确定"),
            ("收缩程序驻留内存", "移出各程序驻留的内存，重用时可能卡顿"),
        };

    /// <summary>Gets the prefix of a failed area.</summary>
    public string Failed => _english ? "failed: " : "失败：";

    /// <summary>Gets the fallback when a failure carries no message.</summary>
    public string UnknownReason => _english ? "unknown" : "未知原因";

    /// <summary>Gets the line shown when an area released nothing measurable.</summary>
    public string NoChange => _english ? "no change" : "无变化";

    /// <summary>Formats what an area released.</summary>
    /// <param name="bytes">Bytes released.</param>
    /// <returns>The line.</returns>
    public string LastFreed(long bytes) => _english
        ? $"last freed {Size(bytes)}"
        : $"上次释放 {Size(bytes)}";

    /// <summary>Gets the summary before anything has run.</summary>
    public string NothingCleanedYet => _english ? "nothing cleaned yet" : "本次尚未清理";

    /// <summary>Gets the summary while a run is in progress.</summary>
    public string Cleaning => _english ? "cleaning…" : "清理中…";

    /// <summary>Formats the result of a run.</summary>
    /// <param name="bytes">Bytes released.</param>
    /// <returns>The line.</returns>
    public string FreedThisRun(long bytes) => _english
        ? $"freed {Size(bytes)}"
        : $"本次释放 {Size(bytes)}";

    /// <summary>Formats a run where some areas failed.</summary>
    /// <param name="done">How many finished.</param>
    /// <param name="failed">How many failed.</param>
    /// <returns>The line.</returns>
    public string PartialRun(int done, int failed) => _english
        ? $"{done} done, {failed} failed"
        : $"{done} 项完成，{failed} 项失败";

    /// <summary>Gets the line shown when a run released nothing measurable.</summary>
    public string NothingMeasured => _english ? "nothing measurable released" : "本次无可测量的释放";

    /// <summary>Formats the result of trimming one process.</summary>
    /// <param name="name">The process.</param>
    /// <param name="bytes">Bytes released.</param>
    /// <returns>The line.</returns>
    public string Trimmed(string name, long bytes) => _english
        ? $"trimmed {name}, freed {Size(bytes)}"
        : $"已优化 {name}，释放 {Size(bytes)}";

    /// <summary>Formats a failed trim.</summary>
    /// <param name="name">The process.</param>
    /// <param name="reason">Why it failed.</param>
    /// <returns>The line.</returns>
    public string TrimFailed(string name, string reason) => _english
        ? $"could not trim {name}: {reason}"
        : $"优化 {name} 失败：{reason}";

    /// <summary>Formats the total line under the process table.</summary>
    /// <param name="bytes">Bytes held by the listed processes.</param>
    /// <returns>The line.</returns>
    public string ProcessTotal(long bytes) => _english
        ? $"total (private working set, shared pages excluded)  {Gb(bytes)}"
        : $"合计（专用工作集，不含共享页）  {Gb(bytes)}";

    /// <summary>Formats the last-clean card's subtitle.</summary>
    /// <param name="at">When it ran.</param>
    /// <param name="automatic">Whether the policy started it.</param>
    /// <param name="seconds">How long it took.</param>
    /// <returns>The line.</returns>
    public string LastCleanSummary(DateTime at, bool automatic, double seconds) => _english
        ? $"{at:MM-dd HH:mm} · {(automatic ? "automatic" : "manual")} · {seconds:0}s"
        : $"{at:MM-dd HH:mm} · {(automatic ? "自动" : "手动")} · 耗时 {seconds:0} 秒";

    /// <summary>Gets the automatic trigger label.</summary>
    public string Automatic => _english ? "automatic" : "自动";

    /// <summary>Gets the manual trigger label.</summary>
    public string Manual => _english ? "manual" : "手动";

    /// <summary>Gets the status line shown when automatic cleaning is off.</summary>
    public string AutoCleanOff => _english ? "automatic: off" : "自动清理：关闭";

    /// <summary>Formats the automatic-cleaning status line.</summary>
    /// <param name="intervalMinutes">The interval, or 0.</param>
    /// <param name="percent">The threshold, or 0.</param>
    /// <returns>The line.</returns>
    public string AutoCleanStatus(int intervalMinutes, int percent)
    {
        var parts = new List<string>();

        if (intervalMinutes > 0)
            parts.Add(_english ? $"every {DescribeMinutes(intervalMinutes)}" : "每 " + DescribeMinutes(intervalMinutes));

        if (percent > 0)
            parts.Add(_english ? $"above {percent}%" : $"占用超过 {percent}%");

        if (parts.Count == 0)
            return AutoCleanOff;

        return (_english ? "automatic: " : "自动清理：") + string.Join(" · ", parts);
    }

    /// <summary>Formats the result of an automatic run.</summary>
    /// <param name="bytes">Bytes released.</param>
    /// <returns>The line.</returns>
    public string AutoCleanFreed(long bytes) => _english
        ? $"automatic: freed {Size(bytes)}"
        : $"自动清理：释放 {Size(bytes)}";

    /// <summary>Formats an automatic run where some areas failed.</summary>
    /// <param name="done">How many finished.</param>
    /// <param name="failed">How many failed.</param>
    /// <returns>The line.</returns>
    public string AutoCleanPartial(int done, int failed) => _english
        ? $"automatic: {done} done, {failed} failed"
        : $"自动清理：{done} 项完成，{failed} 项失败";

    /// <summary>Gets the line shown when the last run was a moment ago.</summary>
    public string CleanedJustNow => _english ? "cleaned just now" : "刚刚清理";

    /// <summary>Formats how long ago the last run was.</summary>
    /// <param name="minutes">Minutes ago.</param>
    /// <returns>The line.</returns>
    public string CleanedMinutesAgo(int minutes) => _english
        ? $"last cleaned {minutes} min ago"
        : $"上次清理 {minutes} 分钟前";

    /// <summary>Gets the failure reported when the startup entry cannot be written.</summary>
    public string StartupEnableFailed => _english ? "could not enable run at startup" : "开机自启设置失败";

    /// <summary>Gets the failure reported when the startup entry cannot be removed.</summary>
    public string StartupDisableFailed => _english ? "could not disable run at startup" : "关闭开机自启失败";

    /// <summary>Gets the failure reported when the hotkey window does not exist yet.</summary>
    public string HotkeyNoWindow => _english ? "the window does not exist yet" : "窗口尚未创建";

    /// <summary>Gets the failure reported when another program owns the hotkey.</summary>
    public string HotkeyTaken => _english ? "another program already owns this hotkey" : "快捷键已被其他程序占用";

    /// <summary>Gets the tray menu's settings item.</summary>
    public string TraySettings => _english ? "Settings…" : "设置…";

    /// <summary>Gets the tray menu's exit item.</summary>
    public string TrayExit => _english ? "Exit" : "退出";

    /// <summary>Gets the CSV header.</summary>
    public string CsvHeader => _english
        ? "time,trigger,area,bytes,gb"
        : "时间,触发,区域,释放字节,释放GB";

    /// <summary>
    /// Switches the language and notifies everything bound to the catalog.
    /// </summary>
    /// <param name="english">Whether to use English.</param>
    public void SetLanguage(bool english)
    {
        if (_english == english)
            return;

        _english = english;

        foreach (var property in typeof(Strings).GetProperties().Where(p => p.PropertyType != typeof(IReadOnlyList<(string, string)>)))
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property.Name));

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Areas)));
    }

    /// <summary>Formats the physical memory headline.</summary>
    /// <param name="used">Used bytes.</param>
    /// <param name="total">Total bytes.</param>
    /// <returns>The line.</returns>
    public string Physical(long used, long total) => _english
        ? $"physical  {Gb(used)} / {Gb(total)}"
        : $"物理内存  {Gb(used)} / {Gb(total)}";

    /// <summary>Formats the four-segment line.</summary>
    /// <param name="active">Active bytes.</param>
    /// <param name="modified">Modified bytes.</param>
    /// <param name="standby">Standby bytes.</param>
    /// <param name="free">Free bytes.</param>
    /// <returns>The line.</returns>
    public string Segments(long active, long modified, long standby, long free) => _english
        ? $"active {Gb(active)} · modified {Gb(modified)} · standby {Gb(standby)} · free {Gb(free)}"
        : $"活动 {Gb(active)} · 已修改 {Gb(modified)} · 待机 {Gb(standby)} · 空闲 {Gb(free)}";

    /// <summary>Formats the committed memory line.</summary>
    /// <param name="used">Committed bytes.</param>
    /// <param name="limit">Commit limit in bytes.</param>
    /// <returns>The line.</returns>
    public string Commit(long used, long limit) => _english
        ? $"committed  {Gb(used)} / {Gb(limit)}"
        : $"已提交  {Gb(used)} / {Gb(limit)}";

    /// <summary>Gets the line shown when the kernel page lists cannot be read.</summary>
    public string SegmentsUnavailable => _english
        ? "four-part breakdown: not readable on this system"
        : "四段构成：当前系统版本无法读取";

    /// <summary>Formats the available-memory line under the ring.</summary>
    /// <param name="available">Available bytes.</param>
    /// <param name="total">Total bytes.</param>
    /// <returns>The line.</returns>
    public string Available(long available, long total) => _english
        ? $"available {Gb(available)} / {Gb(total)}"
        : $"可用 {Gb(available)} / {Gb(total)}";

    /// <summary>Formats the active-memory legend line.</summary>
    /// <param name="bytes">Active bytes.</param>
    /// <returns>The line.</returns>
    public string Active(long bytes) => (_english ? "active " : "活动 ") + Gb(bytes);

    /// <summary>Formats the modified-memory legend line.</summary>
    /// <param name="bytes">Modified bytes.</param>
    /// <returns>The line.</returns>
    public string Modified(long bytes) => (_english ? "modified " : "已修改 ") + Gb(bytes);

    /// <summary>Formats the standby-memory legend line.</summary>
    /// <param name="bytes">Standby bytes.</param>
    /// <returns>The line.</returns>
    public string Standby(long bytes) => (_english ? "standby " : "待机 ") + Gb(bytes);

    /// <summary>Formats the free-memory legend line.</summary>
    /// <param name="bytes">Free bytes.</param>
    /// <returns>The line.</returns>
    public string Free(long bytes) => (_english ? "free " : "空闲 ") + Gb(bytes);

    /// <summary>Gets the tray balloon shown when the window is hidden.</summary>
    public string MinimizedToTray => _english ? "minimized to tray" : "已最小化到托盘";

    /// <summary>Gets the export dialog's file-name prefix.</summary>
    public string ExportFileName => _english ? "MemMop-history-" : "MemMop-记录-";

    /// <summary>Gets the export dialog's filter.</summary>
    public string ExportFilter => _english
        ? "CSV (*.csv)|*.csv|all files (*.*)|*.*"
        : "CSV (*.csv)|*.csv|所有文件 (*.*)|*.*";

    /// <summary>Formats a failed export.</summary>
    /// <param name="reason">Why it failed.</param>
    /// <returns>The message.</returns>
    public string ExportFailed(string reason) => _english
        ? "export failed: " + reason
        : "导出失败：" + reason;

    /// <summary>Formats a failure to open the registry editor.</summary>
    /// <param name="reason">Why it failed.</param>
    /// <returns>The message.</returns>
    public string OpenSettingsFailed(string reason) => _english
        ? "could not open the registry editor: " + reason
        : "打不开注册表编辑器：" + reason;

    /// <summary>Formats a failure to register the hotkey.</summary>
    /// <param name="reason">Why it failed.</param>
    /// <returns>The message.</returns>
    public string HotkeyFailed(string reason) => _english
        ? "clean hotkey could not be registered: " + reason
        : "清理热键注册失败：" + reason;

    /// <summary>Formats the used-over-total line beside the percentage.</summary>
    /// <param name="used">Used bytes.</param>
    /// <param name="total">Total bytes.</param>
    /// <returns>The line.</returns>
    public string UsedOverTotal(long used, long total) => $"{Gb(used)} / {Gb(total)}";

    /// <summary>Formats the build stamp on the about page.</summary>
    /// <param name="hash">The commit hash.</param>
    /// <returns>The line.</returns>
    public string Build(string hash) => _english ? "build " + hash : "构建 " + hash;

    private string DescribeMinutes(int minutes)
    {
        if (minutes % 60 != 0)
            return _english ? $"{minutes} min" : $"{minutes} 分钟";

        var hours = minutes / 60;

        return _english ? $"{hours}h" : $"{hours} 小时";
    }

    /// <summary>
    /// Formats a size. Anything under a gigabyte is shown in megabytes, because "0 GB" for twelve megabytes
    /// is a number that is not true.
    /// </summary>
    /// <param name="bytes">The size.</param>
    /// <returns>The size, formatted.</returns>
    public static string Size(long bytes)
    {
        const double gigabyte = 1024d * 1024 * 1024;

        return bytes >= gigabyte
            ? string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.#} GB", bytes / gigabyte)
            : string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0} MB", bytes / (1024d * 1024));
    }

    private static string Gb(long bytes) => string.Format(
        System.Globalization.CultureInfo.InvariantCulture,
        "{0:0.#} GB",
        bytes / (1024d * 1024 * 1024));
}
