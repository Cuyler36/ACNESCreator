using Microsoft.Win32;
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
using System.IO;
using ACNESCreator.Core;

namespace ACNESCreator.FrontEnd
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly static string[] RegionCodes = new string[4] { "J", "E", "P", "U" };
        readonly OpenFileDialog SelectROMDialog = new OpenFileDialog
        {
            Filter = "All Supported Files|*.nes;*.bin|NES ROM Files|*.nes|Binary Files|*.bin|All Files|*.*"
        };

        public MainWindow()
        {
            InitializeComponent();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectROMDialog.ShowDialog().Value)
            {
                if (File.Exists(SelectROMDialog.FileName))
                {
                    LocationTextBox.Text = SelectROMDialog.FileName;
                }
            }
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(GameNameTextBox.Text) && GameNameTextBox.Text.Length > 3 && !string.IsNullOrEmpty(LocationTextBox.Text)
                && File.Exists(LocationTextBox.Text))
            {
                string GameName = GameNameTextBox.Text;
                string ROMLocation = LocationTextBox.Text;
                bool HasSaveFile = CanSaveCheckBox.IsChecked.Value;
                Region ACRegion = (Region)RegionComboBox.SelectedIndex;

                byte[] ROMData = null;
                try
                {
                    ROMData = File.ReadAllBytes(ROMLocation);
                }
                catch
                {
                    MessageBox.Show("The NES ROM File couldn't be read! Please make sure it isn't open in any other program, and double check the location!",
                        "ROM File Read Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                NES NESFile = null;
                try
                {
                    NESFile = new NES(GameName, ROMData, HasSaveFile, ACRegion);
                }
                catch
                {
                    MessageBox.Show("An error occured while generating the NES info! Please ensure that all your file location is correct, and try again!",
                        "NES File Creation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string OutputLocation = Path.GetDirectoryName(ROMLocation) + Path.DirectorySeparatorChar + GameName + "_" + RegionCodes[(int)ACRegion]
                        + "_NESData.gci";
                try
                {
                    byte[] OutputData = NESFile.GenerateGCIFile();
                    using (var Stream = new FileStream(OutputLocation, FileMode.Create))
                    {
                        Stream.Write(OutputData, 0, OutputData.Length);
                        Stream.Flush();
                    }
                }
                catch
                {
                    MessageBox.Show("An error occured while saving the generated GCI file!", "GCI Creation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                MessageBox.Show("The NES ROM was successfully injected! The file is located here:\r\n" + OutputLocation, "NES Save File Creation",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
