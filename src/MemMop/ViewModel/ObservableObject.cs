using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MemMop.ViewModel;

/// <summary>
/// Minimal change notification, so the view models need no framework.
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the change notification for a property.
    /// </summary>
    /// <param name="propertyName">Filled in by the compiler.</param>
    protected void Raise([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Assigns a field and raises the notification when the value changed.
    /// </summary>
    /// <typeparam name="T">The field type.</typeparam>
    /// <param name="field">The field.</param>
    /// <param name="value">The new value.</param>
    /// <param name="propertyName">Filled in by the compiler.</param>
    /// <returns><c>true</c> when the value changed.</returns>
    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        Raise(propertyName);

        return true;
    }
}
