using Avalonia.Controls;
using POKEMON.ViewModels;

namespace POKEMON.Views
{
    public partial class MainView : UserControl
    {
        public MainView()
        {

            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}

