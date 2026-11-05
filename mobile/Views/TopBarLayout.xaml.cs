using Microsoft.Maui.Controls;

namespace AumoFinance.Views;

public partial class TopBarLayout : ContentView
{
    public TopBarLayout()
    {
        InitializeComponent();
    }

    // expose TopHeader supaya code-behind halaman bisa mengaksesnya bila perlu
    public TopBarView Header => TopHeader;
}
