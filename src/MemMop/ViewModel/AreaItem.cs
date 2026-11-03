using MemMop.Core;

namespace MemMop.ViewModel;

/// <summary>
/// One row of the areas list: whether it is ticked, what it does, what it costs, and what it did last time.
/// </summary>
public sealed class AreaItem : ObservableObject
{
    private bool _isSelected = true;
    private string _lastResult = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="AreaItem"/> class.
    /// </summary>
    /// <param name="reclaimer">The reclaimer this row drives.</param>
    /// <param name="title">The name shown to the user.</param>
    /// <param name="risk">What the operation costs.</param>
    public AreaItem(IMemoryReclaimer reclaimer, string title, string risk)
    {
        Reclaimer = reclaimer;
        Title = title;
        Risk = risk;
    }

    /// <summary>Gets the underlying operation.</summary>
    public IMemoryReclaimer Reclaimer { get; }

    /// <summary>Gets the name shown to the user.</summary>
    public string Title { get; }

    /// <summary>Gets what the operation costs, in one line.</summary>
    public string Risk { get; }

    /// <summary>Gets or sets a value indicating whether the row is ticked.</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => Set(ref _isSelected, value);
    }

    /// <summary>
    /// Gets or sets what the last run produced for this row.
    /// </summary>
    public string LastResult
    {
        get => _lastResult;
        set => Set(ref _lastResult, value);
    }

    /// <summary>
    /// Records a result. A release of zero or less says so rather than showing a number.
    /// </summary>
    /// <param name="result">The measured result.</param>
    public void Apply(ReclaimResult result)
    {
        if (!result.Succeeded)
        {
            LastResult = Strings.Current.Failed + (result.Message ?? Strings.Current.UnknownReason);

            return;
        }

        LastResult = result.ReleasedAnything
            ? Strings.Current.LastFreed(result.FreedBytes)
            : Strings.Current.NoChange;
    }
}
