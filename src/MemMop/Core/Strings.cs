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

    /// <summary>Gets the tab header for the home view.</summary>
    public string Home => _english ? "Home" : "主页";

    /// <summary>Gets the tab header for the settings view.</summary>
    public string Settings => _english ? "Settings" : "设置";

    /// <summary>Gets the tab header for the about view.</summary>
    public string About => _english ? "About" : "关于";

    /// <summary>Gets the clean button label.</summary>
    public string Clean => _english ? "Clean" : "清理";

    /// <summary>Gets the process section title.</summary>
    public string ProcessUsage => _english ? "Process usage" : "进程占用";

    /// <summary>Gets the exclusion section title.</summary>
    public string Exclusions => _english ? "Excluded processes" : "排除的进程（不参与收缩程序驻留内存）";

    /// <summary>Gets the placeholder of the process search box.</summary>
    public string SearchProcess => _english ? "search processes by name" : "按名称搜索进程";

    /// <summary>Gets the caption of the language picker.</summary>
    public string Language => _english ? "Language" : "语言";

    /// <summary>Gets the caption of the close-to-tray switch.</summary>
    public string CloseToTray => _english ? "minimize to tray on close" : "关闭时最小化到托盘";

    /// <summary>Gets the about paragraph.</summary>
    public string AboutText => _english
        ? "A Windows memory management tool, portable, settings under the current user. Cleaning does not make a computer faster - standby memory already counts as available."
        : "一个 Windows 内存管理工具，绿色免安装，设置保存在当前用户下。清理不会让电脑变快——待机内存本来就计入可用。";

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
}
