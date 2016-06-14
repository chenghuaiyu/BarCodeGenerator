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
using BarCodeExample;
using QRCoder;
using System.Windows.Threading;
using WindowsInput;
using WindowsInput.Native;

namespace SinoCloudWisdomBarCodeGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //[System.Runtime.InteropServices.DllImport("gdi32.dll")]
        //public static extern bool DeleteObject(IntPtr hObject);

        const int len = 13;
        DispatcherTimer dispatcherTimer;
        const string logFilename = "log_scw_barcode.txt";
        public MainWindow()
        {
            InitializeComponent();
            
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            
            myLabel.Content += new System.IO.FileInfo(logFilename).FullName;
        }

        //Encode Data to create Barcode
        private void CreateBarCode(string productCode)
        {
            ////////////////////////////////////////////
            // Encode The Code with the help of Encoder 
            ///////////////////////////////////////////
            Barcodes barCode = new Barcodes();
            barCode.BarcodeType = Barcodes.BarcodeEnum.Encoder;
            barCode.Data = productCode;
            barCode.CheckDigit = Barcodes.YesNoEnum.No;
            barCode.encode();

            //Dimensions of the Bars, you can use according to your need
            int thinWidth;
            int thickWidth;
            thinWidth = 2;
            thickWidth = 3 * thinWidth;

            string encodedText = barCode.EncodedData; //Encoded product code
            string humanText = barCode.HumanText; //Human readable product code

            /////////////////////////////////////
            // Draw The Barcode
            /////////////////////////////////////
            int length = encodedText.Length;
            int currentPos = 10;
            int currentTop = 10;
            int currentColor = 0;
            int height = 100;
            myCanvas.Children.Clear();
            for (int i = 0; i < length; i++)
            {
                Rectangle rectangle = new Rectangle(); //Create a rectangle which will form as Barcode
                rectangle.Height = height;

                if (currentColor == 0)
                {
                    currentColor = 1;
                    rectangle.Fill = new SolidColorBrush(Colors.Black);
                }
                else
                {
                    currentColor = 0;
                    rectangle.Fill = new SolidColorBrush(Colors.White);
                }

                Canvas.SetLeft(rectangle, currentPos);
                Canvas.SetTop(rectangle, currentTop);

                if (encodedText[i] == 't')
                {
                    rectangle.Width = thinWidth;
                    currentPos += thinWidth;
                }
                else if (encodedText[i] == 'w')
                {
                    rectangle.Width = thickWidth;
                    currentPos += thickWidth;
                }

                //Bind Barcode to XAML Canvas 
                myCanvas.Children.Add(rectangle);
            }


            ////////////////////////////////////////////////
            // Add the Human Readable Text and its alignment
            ///////////////////////////////////////////////
            TextBlock block = new TextBlock(); //WPF Text Block
            block.Text = humanText; //Set human readable product code
            block.FontSize = 32;
            block.FontFamily = new FontFamily("Courier New");
            Rect rect = new Rect(0, 0, 0, 0);
            block.Arrange(rect);
            Canvas.SetLeft(block, (currentPos - block.ActualWidth) / 2);
            Canvas.SetTop(block, currentTop + height + 5);
            myCanvas.Children.Add(block);
        }

        public static BitmapSource ToBitmapSource(System.Drawing.Bitmap src)
        {
            if (src == null)
            {
                return null;
            }
            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(src.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }
        private void RenderQrCode(string content, System.Drawing.Bitmap imgIcon, int iconSize = 6, string level = "M")
        {
            QRCodeGenerator.ECCLevel eccLevel = (QRCodeGenerator.ECCLevel)(level == "L" ? 0 : level == "M" ? 1 : level == "Q" ? 2 : 3);
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(content, eccLevel))
                {
                    using (QRCode qrCode = new QRCode(qrCodeData))
                    {
                        System.Drawing.Bitmap m_Bitmap = qrCode.GetGraphic(20, System.Drawing.Color.Black, System.Drawing.Color.White,
                            imgIcon, iconSize);
                        //IntPtr ip = m_Bitmap.GetHbitmap();
                        //BitmapSource bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                        //    ip, IntPtr.Zero, Int32Rect.Empty,
                        //    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                        //DeleteObject(ip);
                        //myImage.Source = bitmapSource;
                        myImage.Source = ToBitmapSource(m_Bitmap);

                        //this.pictureBoxQRCode.Size = new System.Drawing.Size(pictureBoxQRCode.Width, pictureBoxQRCode.Height);
                        ////Set the SizeMode to center the image.
                        //this.pictureBoxQRCode.SizeMode = PictureBoxSizeMode.CenterImage;

                        //pictureBoxQRCode.SizeMode = PictureBoxSizeMode.StretchImage;
                    }
                }
            }
        }
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            var sim = new InputSimulator();

            Random rand = new Random((int)DateTime.Now.Ticks);
            int n = 13;
            while (n-- > 0)
            {
                int key = rand.Next(36);
                char ch;
                if (key < 10)
                {
                    ch = (char)('0' + key);
                }
                else
                {
                    ch = (char)('a' + key - 10);
                }
                sim.Keyboard.TextEntry(ch.ToString());
            }
            sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
        }

        private void myCheckBox_Checked(object sender, RoutedEventArgs e)
        {
        }

        private void myCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            if ((bool)checkBox.IsChecked)
            {
                dispatcherTimer.Start();
            }
            else
            {
                dispatcherTimer.Stop();
            }

            myTextBox.Focus();
        }

        private void myTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            myListView.Items.Insert(0, e.Text);
            if (e.Text != "\n" && e.Text != "\r")
            {
                return;
            }

            var textBox = sender as TextBox;
            string content = textBox.Text;
            if (content.Length == 0)
            {
                return;
            }

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            System.Drawing.Bitmap logo = SinoCloudWisdomBarCodeGenerator.Properties.Resources.logo;
            RenderQrCode(content, logo, 10, "M");
            myListBox.Items.Insert(0, content);
            sw.Stop();
            myListView.Items.Insert(0, sw.Elapsed.ToString());
            myListView.Items.Insert(0, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:ffff", System.Globalization.DateTimeFormatInfo.InvariantInfo));

            CreateBarCode(content);
            textBox.Text = "";

            System.IO.StreamWriter writer = System.IO.File.AppendText(logFilename);
            writer.WriteLine(content);
            writer.Close();
        }
    }
}