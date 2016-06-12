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
       public MainWindow()
        {
            InitializeComponent();
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
           barCode.CheckDigit = Barcodes.YesNoEnum.Yes;
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
           myCanvas.Children.Clear();
           for (int i = 0; i < length; i++)
           {
               Rectangle rectangle = new Rectangle(); //Create a rectangle which will form as Barcode
               rectangle.Height = 200;

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
           Canvas.SetTop(block, currentTop + 205);
           myCanvas.Children.Add(block);
       }

		public static BitmapSource ToBitmapSource( System.Drawing.Bitmap src)
		{
			if (src == null)
			{
				return null;
			}
			return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(src.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
		}
       private void RenderQrCode(string content, string iconImgPath, int iconSize = 6, string level = "M")
       {
           QRCodeGenerator.ECCLevel eccLevel = (QRCodeGenerator.ECCLevel)(level == "L" ? 0 : level == "M" ? 1 : level == "Q" ? 2 : 3);
           using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
           {
               using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(content, eccLevel))
               {
                   using (QRCode qrCode = new QRCode(qrCodeData))
                   {
                       System.Drawing.Bitmap imgIcon = null;
                       if (iconImgPath.Length > 0)
                       {
                           try
                           {
                               imgIcon = new  System.Drawing.Bitmap(iconImgPath);
                           }
                           catch (Exception)
                           {
                           }
                       }

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

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            //myListView.Items.Add(e.Key.ToString()+"\t" + e.SystemKey.ToString() + "\t"+e.ToString());
            if (e.Key != System.Windows.Input.Key.Enter)
            //    if (e.Key > System.Windows.Input.Key.D0 && e.Key < System.Windows.Input.Key.Z)
            //{
            //    return;
            //}
            //    if (e.Key == System.Windows.Input.Key.System && e.SystemKey == System.Windows.Input.Key.LeftAlt)
            {
                return;
            }

            var textBox = sender as TextBox;
            string content = textBox.Text;
            myListBox.Items.Insert(0, content);
            CreateBarCode(content);
            textBox.Text = "";

        }

        private void myTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            myListView.Items.Insert(0, e.Text);
            if(e.Text != "\n" && e.Text != "\r")
            {
                return;
            }

            var textBox = sender as TextBox;
            string content = textBox.Text;
            if (content.Length == 0)
            {
                return;
            }
            RenderQrCode(content, "./logo.png", 6, "M");
            myListBox.Items.Insert(0, content);
            CreateBarCode(content);
            textBox.Text = "";
        }
    }
}
