using System.Windows.Data;
using Binding = System.Windows.Data.Binding;
using System.Windows.Markup;

namespace MemMop.Converters;

/// <summary>
/// Binds one entry of the string catalog: <c>Text="{c:Text Home}"</c>. It returns a binding rather than a
/// value, so switching the language reaches the element without rebuilding it.
/// </summary>
/// <remarks>
/// No <c>MarkupExtensionReturnType</c> here: declaring the result as a string makes the parser try to
/// convert the binding into one instead of applying it.
/// </remarks>
public sealed class TextExtension : MarkupExtension
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TextExtension"/> class.
    /// </summary>
    public TextExtension()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TextExtension"/> class.
    /// </summary>
    /// <param name="key">The catalog property to bind.</param>
    public TextExtension(string key) => Key = key;

    /// <summary>Gets or sets the catalog property to bind.</summary>
    [ConstructorArgument("key")]
    public string Key { get; set; } = string.Empty;

    /// <inheritdoc/>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        // the binding has to provide its own value, or the parser sets the Binding object itself and the
        // target property rejects it
        var binding = new Binding(Key) { Source = Core.Strings.Current };

        return binding.ProvideValue(serviceProvider);
    }
}
