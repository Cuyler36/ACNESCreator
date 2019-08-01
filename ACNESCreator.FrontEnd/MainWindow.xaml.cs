using Microsoft.Win32;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Windows.Media.Imaging;
using System.Linq;
using System;
using System.Windows.Media;
using System.Collections.Generic;
using ACNESCreator.Core;
using GCNToolKit.Formats.Images;
using GCNToolKit.Formats.Colors;

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
            Filter = "All Supported Files|*.nes;*.fds;*yaz0;*.bin|NES ROM Files|*.nes|Famicom Disk System Files|*.fds|Yaz0 Compressed Files|*.yaz0|Binary Files|*.bin|All Files|*.*"
        };
        readonly OpenFileDialog SelectIconImageDialog = new OpenFileDialog
        {
            Filter = "PNG Files|*.png"
        };

        private bool _inProgress = false;
        private bool InProgress
        {
            get => _inProgress;
            set
            {
                GenerateButton.IsEnabled = !value;
                RegionComboBox.IsEnabled = !value;
                CanSaveCheckBox.IsEnabled = !value;
                GameNameTextBox.IsEnabled = !value;
                LocationTextBox.IsEnabled = !value;
                CompressCheckBox.IsEnabled = !value;
                BrowseButton.IsEnabled = !value;
                IsDnMe.IsEnabled = !value;
                ProgressBar.IsIndeterminate = value;
                _inProgress = value;
            }
        }

        private byte[] IconData;

        public MainWindow()
        {
            InitializeComponent();
            
            IconData = GCI.DefaultIconData;
            RefreshIconImage();
        }

        private void RefreshIconImage()
        {
            ushort[] Palette = new ushort[256];
            for (int i = 0; i < 256; i++)
            {
                Palette[i] = BitConverter.ToUInt16(IconData, 32 * 32 + i * 2).Reverse();
            }

            IconImage.Source = GetIconImage(IconData.Take(32 * 32).ToArray(), Palette);
        }

        private static BitmapSource GetIconImage(byte[] IconData, ushort[] Palette)
        {
            int[] IconImageData = C8.DecodeC8(IconData, Palette, 32, 32);
            byte[] IconImageDataArray = new byte[IconImageData.Length * 4];
            Buffer.BlockCopy(IconImageData, 0, IconImageDataArray, 0, IconImageDataArray.Length);

            return BitmapSource.Create(32, 32, 96, 96, PixelFormats.Bgra32, null, IconImageDataArray, 4 * 32);
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

        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!InProgress && !string.IsNullOrEmpty(GameNameTextBox.Text) && GameNameTextBox.Text.Length > 3 && !string.IsNullOrEmpty(LocationTextBox.Text)
                && File.Exists(LocationTextBox.Text))
            {
                InProgress = true;

                string GameName = GameNameTextBox.Text;
                string ROMLocation = LocationTextBox.Text;
                bool HasSaveFile = CanSaveCheckBox.IsChecked.Value;
                Region ACRegion = (Region)RegionComboBox.SelectedIndex;
                bool DnMe = IsDnMe.IsChecked.Value;

                byte[] ROMData = null;
                try
                {
                    ROMData = File.ReadAllBytes(ROMLocation);
                }
                catch
                {
                    MessageBox.Show("The NES ROM File couldn't be read! Please make sure it isn't open in any other program, and double check the location!",
                        "ROM File Read Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    InProgress = false;
                    return;
                }

                // Force compression on DnM+ & DnMe+ since it's size offset is 0xC instead of 0x12. This ensures that the size is retrieved from the Yaz0 header.
                if (Yaz0.IsYaz0(ROMData) || ROMData.Length > NES.MaxROMSize)
                {
                    CompressCheckBox.IsChecked = true;
                }

                bool Compress = !Yaz0.IsYaz0(ROMData) && CompressCheckBox.IsChecked.Value;
                NES NESFile = null;
                try
                {
                    await Task.Run(() => { NESFile = new NES(GameName, ROMData, HasSaveFile, ACRegion, Compress, DnMe, IconData); });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("An error occured while generating the NES info! Please ensure that all your file location is correct, and try again!",
                        "NES File Creation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    InProgress = false;
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    return;
                }

                if (!NESFile.IsROM && !NESFile.IsFDS)
                {
                    MessageBox.Show("Your file doesn't appear to be a NES ROM! It will be treated as a data patch, and will modify the game's memory instead!",
                        "ROM Info", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                var OutputLocation = Path.Combine(Path.GetDirectoryName(ROMLocation),
                    $"{GameName}_{RegionCodes[(int)ACRegion]}_NESData.gci");
                try
                {
                    var OutputData = NESFile.GenerateGCIFile();
                    using (var Stream = new FileStream(OutputLocation, FileMode.Create))
                    {
                        Stream.Write(OutputData, 0, OutputData.Length);
                        Stream.Flush();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("An error occured while saving the generated GCI file!", "GCI Creation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    InProgress = false;
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    return;
                }

                MessageBox.Show("The NES ROM was successfully injected! The file is located here:\r\n" + OutputLocation, "NES Save File Creation",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                InProgress = false;
            }
        }

        private void IsDnMe_Checked(object sender, RoutedEventArgs e)
        {
            RegionComboBox.IsEnabled = !IsDnMe.IsChecked.Value;
            RegionComboBox.SelectedIndex = 0;
            GameNameTextBox.MaxLength = RegionComboBox.IsEnabled ? 16 : 10;
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            if (SelectIconImageDialog.ShowDialog().Value && File.Exists(SelectIconImageDialog.FileName))
            {
                try
                {
                    BitmapImage Img = new BitmapImage();
                    Img.BeginInit();
                    Img.UriSource = new Uri(SelectIconImageDialog.FileName);
                    Img.EndInit();

                    if (Img.PixelWidth == 32 && Img.PixelHeight == 32)
                    {
                        //IconImage.Source = Img;

                        // Get the image data
                        byte[] PixelData = new byte[4 * 32 * 32];
                        Img.CopyPixels(PixelData, 4 * 32, 0);

                        int[] ImageData = new int[32 * 32];
                        Buffer.BlockCopy(PixelData, 0, ImageData, 0, PixelData.Length);

                        // Convert it to C8 format
                        IconData = new byte[0x600];
                        List<ushort> PaletteList = new List<ushort>();

                        for (int i = 0, idx = 0; i < 32 * 32; i++, idx += 4)
                        {
                            ushort RGB5A3Color = RGB5A3.ToRGB5A3(PixelData[idx + 3], PixelData[idx + 2], PixelData[idx + 1], PixelData[idx]);
                            if (!PaletteList.Contains(RGB5A3Color))
                            {
                                PaletteList.Add(RGB5A3Color);
                            }
                        }

                        ushort[] Palette = PaletteList.ToArray();
                        if (Palette.Length > 256)
                        {
                            Array.Resize(ref Palette, 256);
                        }

                        C8.EncodeC8(ImageData, Palette, 32, 32).CopyTo(IconData, 0);

                        for (int i = 0; i < PaletteList.Count; i++)
                        {
                            BitConverter.GetBytes(PaletteList[i].Reverse()).CopyTo(IconData, 0x400 + i * 2);
                        }

                        // Refresh the Icon Image
                        RefreshIconImage();
                    }
                    else
                    {
                        MessageBox.Show("The icon you selected is not 32x32 pixels! Please resize it to that and try again.", "Icon Import Error", MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
                catch
                {
                    MessageBox.Show("The icon you selected could not be imported! Please try again.", "Icon Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
