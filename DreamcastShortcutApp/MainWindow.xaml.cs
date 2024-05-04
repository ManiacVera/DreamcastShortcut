using DreamcastShortcutApp.ViewModel;
using System.Windows;
namespace DreamcastShortcutApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(MainWindowVM vm)
        {
            InitializeComponent();
            this.DataContext = vm;
        }

        private void Language_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ((MainWindowVM)this.DataContext).LanguageSelectedCommand.Execute(null);
        }
    }
}
