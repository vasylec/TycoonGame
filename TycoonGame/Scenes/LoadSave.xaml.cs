using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TycoonGame.Scenes
{
    /// <summary>
    /// Interaction logic for LoadSave.xaml
    /// </summary>
    public partial class LoadSave : Page
    {
        private MainMenu _parentWindow;

        public LoadSave(MainMenu parent)
        {
            InitializeComponent();
            _parentWindow = parent;

            slot1.Content = App.saveName;


        }

        private void LoadSave_Loaded(object sender, RoutedEventArgs e)
        {
            // Aplică font și setări vizuale pe tot conținutul ferestrei
            UIHelper.ApplyPixelFontAndSettings(this);
        }

        private void btnMainMenu_Click(object sender, RoutedEventArgs e)
        {
            _parentWindow.GoBack();
        }
    }
}
