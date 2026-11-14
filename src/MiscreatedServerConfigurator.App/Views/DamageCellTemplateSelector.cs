using System.Windows;
using System.Windows.Controls;

using MiscreatedServerConfigurator.App.ViewModels;

namespace MiscreatedServerConfigurator.App.Views;

/// <summary>
/// Picks the editor for a damage-matrix cell, or a placeholder when the template has no such CVAR.
/// </summary>
public sealed class DamageCellTemplateSelector : DataTemplateSelector
{
    /// <summary>Template used when the cell has a catalog or file CVAR.</summary>
    public DataTemplate? Present { get; set; }

    /// <summary>Template used when the reference template has no line for the pair.</summary>
    public DataTemplate? Missing { get; set; }

    /// <summary>Selects a template for one matrix cell.</summary>
    /// <param name="item">A <see cref="DamageCellViewModel"/>.</param>
    /// <param name="container">The parent element.</param>
    /// <returns>The present or missing template.</returns>
    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        if (item is DamageCellViewModel cell && cell.IsMissing)
        {
            return Missing;
        }

        return Present;
    }
}
