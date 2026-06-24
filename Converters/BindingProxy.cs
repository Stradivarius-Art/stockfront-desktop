using System.Windows;

namespace stockfront.Converters;

/// <summary>
/// Carries a DataContext into a resource so elements that are <i>not</i> in the visual tree (here, a
/// <see cref="System.Windows.Controls.ColumnDefinition"/>) can still bind to it. Declared in an
/// element's resources as <c>Data="{Binding}"</c> and referenced via <c>StaticResource</c>; the
/// standard WPF "binding proxy" pattern. A <see cref="Freezable"/> so it inherits the owner's
/// DataContext.
/// </summary>
public sealed class BindingProxy : Freezable
{
    protected override Freezable CreateInstanceCore() => new BindingProxy();

    public object? Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    public static readonly DependencyProperty DataProperty =
        DependencyProperty.Register(nameof(Data), typeof(object), typeof(BindingProxy));
}
