using System.Windows;
using FileOrganixr.UI.ViewModels;

namespace FileOrganixr.UI;
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel mainViewModel)
    {
        ArgumentNullException.ThrowIfNull(mainViewModel);

        InitializeComponent();
        DataContext = mainViewModel;
    }
}
