using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Nivel
{
    /// <summary>
    /// Interaction logic for ControlPanel.xaml
    /// </summary>
    public partial class ControlPanel : Window
    {
        public ControlPanel()
        {
            InitializeComponent();
        }

        private void DrawPinpoint_Checked(object sender, RoutedEventArgs e)
        {
            Main.State = StateCode.DrawPinPoint;
        }

        private void SubstituteText_Checked(object sender, RoutedEventArgs e)
        {
            Main.State = StateCode.ChangeText;
        }

        private void OnlyText_RB_Checked(object sender, RoutedEventArgs e)
        {
            Main.State = StateCode.DrawText;
        }

        private void Tabcontrol_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void label_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (label.SelectedItem is ComboBoxItem selectedItem)
            {
                string selectedValue = selectedItem.Content?.ToString();

                if (selectedItem != null)
                    Main.Label = selectedValue;
            }
        }

        private void label_Loaded(object sender, RoutedEventArgs e)
        {
            if (label.SelectedItem is ComboBoxItem selectedItem)
            {
                string selectedValue = selectedItem.Content?.ToString();

                if (selectedItem != null)
                    Main.Label = selectedValue;
            }
        }
    }
}
