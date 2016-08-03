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
using System.Net;
using System.IO;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;

namespace SinoCloudWisdomBarCodeGenerator
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint ProcessId);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        int[] range = { 26, 26, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 26, 26 };
        DispatcherTimer dispatcherTimer;
        const string logFilename = "scw_barcode.log";
        string serverUrlKey = "TransferURL";
        string transferSwitchKey = "TransferSwitch";
        string timerIntervalKey = "TimerInterval";
        bool transferSwitch;
        string serverUrl = "";
        string barcode;
        const string barcodeFlagSwitch0 = "-0";
        const string barcodeFlagSwitch1 = "-1";
        string barcodeFlag;
#if DEBUG
        int ImgFileInd = 0;
#endif

        public MainWindow()
        {
            InitializeComponent();
            myCheckBox.Visibility = Visibility.Hidden;
#if DEBUG
            myCheckBox.Visibility = Visibility.Visible;
            myCheckBox.IsEnabled = true;
#endif
            if (myCheckBox.IsEnabled)
            {
                dispatcherTimer = new DispatcherTimer();
                dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
                int timerInterval;
                int.TryParse(ConfigurationManager.AppSettings[timerIntervalKey], out timerInterval);
                if (0 >= timerInterval)
                {
                    timerInterval = 1;
                }
                dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
            }

            myLabel.Content += new System.IO.FileInfo(logFilename).FullName;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            transferSwitch = "true" == ConfigurationManager.AppSettings[transferSwitchKey];
            if (transferSwitch)
            {
                serverUrl = ConfigurationManager.AppSettings[serverUrlKey];
                if (string.IsNullOrEmpty(serverUrl))
                {
                    MessageBox.Show(serverUrlKey + " not found in configuration.\r\nprogram will exit.");
                    Application.Current.Shutdown();
                }
            }
        }

        public static BitmapSource ToBitmapSource(System.Drawing.Bitmap src)
        {
            if (src == null)
            {
                return null;
            }
            IntPtr ip = src.GetHbitmap();
            BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(ip, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            DeleteObject(ip);//释放对象

            return bs;
        }

        public static Bitmap ToBitmap(BitmapSource src)
        {
            if (src == null)
            {
                return null;
            }
            int width = src.PixelWidth;
            int height = src.PixelHeight;
            Bitmap result = new Bitmap(width, height);
            BitmapData bits = result.LockBits(new System.Drawing.Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            int size = width * height * 4;
            byte[] argb = new byte[size];
            src.CopyPixels(argb, bits.Stride, 0);
            System.Runtime.InteropServices.Marshal.Copy(argb, 0, bits.Scan0, size);
            return result;
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

        System.Diagnostics.Process GetActiveProcess()
        {
            IntPtr hwnd = GetForegroundWindow();
            uint pid;
            GetWindowThreadProcessId(hwnd, out pid);
            System.Diagnostics.Process p = System.Diagnostics.Process.GetProcessById((int)pid);
            return p;
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            var currentProc = System.Diagnostics.Process.GetCurrentProcess();
            var activeProc = GetActiveProcess();
            if (currentProc.Id != activeProc.Id)
            {
                return;
            }

            var sim = new InputSimulator();

            Random rand = new Random((int)DateTime.Now.Ticks);
            int n = range.Length;
            int i = 0;
            while (i < n)
            {
                int maxVal = range[i++];
                int key = rand.Next(maxVal);
                char ch;
                if (maxVal == 10)
                {
                    ch = (char)('0' + key);
                }
                else
                {
                    ch = (char)('a' + key);
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

        private void post2Url(string postUrl, string content, Encoding dataEncode)
        {
            try
            {
                HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(new Uri(postUrl));
                webReq.Method = "POST";
                webReq.ContentType = "application/x-www-form-urlencoded";
                byte[] byteArray = dataEncode.GetBytes(content); //转化
                webReq.ContentLength = byteArray.Length;
                System.IO.Stream newStream = webReq.GetRequestStream();
                newStream.Write(byteArray, 0, byteArray.Length);//写入参数
                newStream.Close();
                HttpWebResponse response = (HttpWebResponse)webReq.GetResponse();

                StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.Default);
                string ret = sr.ReadToEnd();
                sr.Close();
                response.Close();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            //System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(postUrl);
            //request.Method = "POST";
            //System.Net.HttpWebResponse response =(HttpWebResponse)request.GetResponse();
            //if (response != null && response.StatusCode == HttpStatusCode.OK)
            //{
            //    using (StreamReader sr = new StreamReader(cnblogsRespone.GetResponseStream()))
            //    {
            //        html = sr.ReadToEnd();
            //    }
            //}
            //return ;
        }

        private void get2Url(string url)
        {
            try
            {
                HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(new Uri(url));
                webReq.Method = "GET";
                HttpWebResponse response = (HttpWebResponse)webReq.GetResponse();

                StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.Default);
                string ret = sr.ReadToEnd();
                sr.Close();
                response.Close();
            }
            catch (System.Exception ex)
            {
#if DEBUG
                MessageBox.Show(ex.Message + "  \r\n\t" + url);
#endif
            }

            //System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(postUrl);
            //request.Method = "POST";
            //System.Net.HttpWebResponse response =(HttpWebResponse)request.GetResponse();
            //if (response != null && response.StatusCode == HttpStatusCode.OK)
            //{
            //    using (StreamReader sr = new StreamReader(cnblogsRespone.GetResponseStream()))
            //    {
            //        html = sr.ReadToEnd();
            //    }
            //}
            //return ;
        }

        private void myTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            myListView.Items.Clear();
            //myListView.Items.Insert(0, e.Text);
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

            if (string.IsNullOrEmpty(barcode) || content != barcode)
            {
                barcodeFlag = barcodeFlagSwitch0;
            }
            else
            {
                if (barcodeFlag == barcodeFlagSwitch1)
                {
                    barcodeFlag = barcodeFlagSwitch0;
                }
                else
                {
                    barcodeFlag = barcodeFlagSwitch1;
                }
            }
            barcode = content;
            content += barcodeFlag;
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            System.Drawing.Bitmap logo = null;// SinoCloudWisdomBarCodeGenerator.Properties.Resources.logo;
            //RenderQrCode(content, logo, 6, "H");
            RenderQrCode(content, logo, 10, "H");
#if DEBUG
            Type t = myImage.Source.GetType();
            Bitmap bm = ToBitmap((myImage.Source as BitmapSource));
            string ImgDir = "barcodeImage";
            if (!Directory.Exists(ImgDir))
            {
                Directory.CreateDirectory(ImgDir);
            }
            bm.Save(System.IO.Path.Combine(ImgDir, ImgFileInd.ToString("D5") + "-" + content + ".png"));
            ImgFileInd++;
#endif

            if (myListBox.Items.Count >= 1000)
            {
                myListBox.Items.Clear();
            }
            myListBox.Items.Insert(0, content);
            sw.Stop();
            myListView.Items.Insert(0, sw.Elapsed.ToString());
            myListView.Items.Insert(0, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:ffff", System.Globalization.DateTimeFormatInfo.InvariantInfo));

            textBox.Text = "";

            System.IO.StreamWriter writer = System.IO.File.AppendText(logFilename);
            writer.WriteLine(content);
            writer.Close();
            if (transferSwitch)
            {
                get2Url(serverUrl + "?barcode=" + barcode);
                //post2Url(serverUrl, "barcode=" + barcode, Encoding.UTF8);
            }
        }
    }
}