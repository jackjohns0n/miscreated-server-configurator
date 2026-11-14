using System.Windows.Controls;

namespace MiscreatedServerConfigurator.App.Views;

/// <summary>
/// Weather-section panel that turns desired day and night hours into the two time-scale CVARs.
/// </summary>
public partial class DayNightPanel : UserControl
{
    /// <summary>Creates the panel.</summary>
    public DayNightPanel()
    {
        InitializeComponent();
    }
}
