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
using System.Net.Sockets;
using System.Diagnostics;

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

        int[] range = { 26, 26, 10, 10, 10, 10, 10, 10, 10, 10, 10, 26, 26 };
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

        public void WorkThreadFunction()
        {
            batchGenerateQRImage("image-L", QRCodeGenerator.ECCLevel.L); ImgFileInd = 0;
            batchGenerateQRImage("image-M", QRCodeGenerator.ECCLevel.M); ImgFileInd = 0;
            batchGenerateQRImage("image-Q", QRCodeGenerator.ECCLevel.Q); ImgFileInd = 0;
            batchGenerateQRImage("image-H", QRCodeGenerator.ECCLevel.H); ImgFileInd = 0;
        }
#endif

        public MainWindow()
        {
            InitializeComponent();
            myCheckBox.Visibility = Visibility.Hidden;
#if DEBUG
            myCheckBox.Visibility = Visibility.Visible;
            myCheckBox.IsEnabled = true;

            //generateQRImage("ee5174665187vh-0", ".", QRCodeGenerator.ECCLevel.Q, null, false, true);
            //System.Threading.Thread thread = new System.Threading.Thread(new System.Threading.ThreadStart(WorkThreadFunction));
            //thread.Start();
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

        public static void AddAddressToAcl(string address, string domain, string user)
        {
            string args = string.Format(@"http add urlacl url={0} user={1}\{2}", address, domain, user);

            ProcessStartInfo startInfo = new ProcessStartInfo("netsh", args);
            startInfo.Verb = "runas";
            startInfo.CreateNoWindow = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.UseShellExecute = true;

            Process.Start(startInfo).WaitForExit();
        }

        HttpListener listener = new HttpListener();

        public void HttpListenerThread()
        {
            try
            {
                string url = "http://+:8676/zyzh/";
                //AddAddressToAcl(url, Environment.UserDomainName, Environment.UserName);
                AddAddressToAcl(url, "", "Everyone");

                // 读取计算机的名称
                string name = System.Net.Dns.GetHostName();
                IPAddress[] address = Dns.GetHostAddresses(name);
                foreach(var addr in address)
                {
                    //AddAddressToAcl(addr.ToString(), Environment.UserDomainName, Environment.UserName);
                }

                listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;//指定身份验证 Anonymous匿名访问
                //listerner.Prefixes.Add("http://+:8676/");
                //listerner.Prefixes.Add("http://*:8676/zyzh/");
                listener.Prefixes.Add("http://+:8676/zyzh/");
                //listerner.Prefixes.Add("http://localhost:8676/zyzh/");
                listener.Start();
                Console.WriteLine("WebServer Start Successed.......");
                while (true)
                {
                    //等待请求连接
                    //没有请求则GetContext处于阻塞状态
                    HttpListenerContext ctx = listener.GetContext();
                    ctx.Response.StatusCode = 200;//设置返回给客服端http状态代码
                    string content = ctx.Request.QueryString["barcode"];

                    //使用Writer输出http响应代码
                    using (StreamWriter writer = new StreamWriter(ctx.Response.OutputStream))
                    {
                        Console.WriteLine("hello");
                        writer.WriteLine("<html><head><title>The WebServer Test</title></head><body>");
                        writer.WriteLine("<div style=\"height:20px;color:blue;text-align:center;\"><p> hello {0}</p></div>", content);
                        writer.WriteLine("<ul>");

                        foreach (string header in ctx.Request.Headers.Keys)
                        {
                            writer.WriteLine("<li><b>{0}:</b>{1}</li>", header, ctx.Request.Headers[header]);

                        }
                        writer.WriteLine("</ul>");
                        writer.WriteLine("</body></html>");

                        writer.Close();
                        ctx.Response.Close();
                    }

                    if (content != null)
                    {
                        var sim = new InputSimulator();
                        sim.Keyboard.TextEntry(content);
                        sim.Keyboard.KeyPress(VirtualKeyCode.RETURN);
                    }

                }
            }
            catch (HttpListenerException ex)
            {
                if ("GetContext" != ex.TargetSite.Name)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            //listener.Stop();
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

            System.Threading.Thread threadHttp = new System.Threading.Thread(new System.Threading.ThreadStart(HttpListenerThread));
            threadHttp.Start();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (listener.IsListening)
            {
                listener.Stop();
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

        private QRCode GetQrCode(string content, QRCodeGenerator.ECCLevel eccLevel, bool forceUtf8 = false, bool utf8BOM = false)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(content, eccLevel, forceUtf8, utf8BOM);
            return new QRCode(qrCodeData);
        }

        System.Diagnostics.Process GetActiveProcess()
        {
            IntPtr hwnd = GetForegroundWindow();
            uint pid;
            GetWindowThreadProcessId(hwnd, out pid);
            System.Diagnostics.Process p = System.Diagnostics.Process.GetProcessById((int)pid);
            return p;
        }

        private string getRandom(int[] range)
        {
            string content = "";
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
                    ch = (char)('A' + key);
                }
                content += ch;
            }

            return content;
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            var currentProc = System.Diagnostics.Process.GetCurrentProcess();
            var activeProc = GetActiveProcess();
            if (currentProc.Id != activeProc.Id)
            {
                return;
            }

            string content = getRandom(range);
            var sim = new InputSimulator();
            sim.Keyboard.TextEntry(content);
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
                byte[] byteArray = dataEncode.GetBytes(content);
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
            System.Drawing.Bitmap logo = SinoCloudWisdomBarCodeGenerator.Properties.Resources.logo;
            QRCode qrCode = GetQrCode(content, QRCodeGenerator.ECCLevel.H, false, true);
            System.Drawing.Bitmap m_Bitmap = qrCode.GetGraphic(20, System.Drawing.Color.Black, System.Drawing.Color.White, logo, 6, 6, true);
            myImage.Source = ToBitmapSource(m_Bitmap);
#if DEBUG
            string ImgDir = "barcodeImage";
            if (!Directory.Exists(ImgDir))
            {
                Directory.CreateDirectory(ImgDir);
            }
            ImgFileInd++;
            m_Bitmap.Save(System.IO.Path.Combine(ImgDir, ImgFileInd.ToString("D5") + "-" + content + ".png"));
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

#if DEBUG
        private void generateQRImage(string content, string ImgDir, QRCodeGenerator.ECCLevel eccLevel, System.Drawing.Bitmap logo = null, bool forceUtf8 = false, bool utf8BOM = false)
        {
            QRCode qrCode = GetQrCode(content, eccLevel, forceUtf8, forceUtf8);
            System.Drawing.Bitmap m_Bitmap = qrCode.GetGraphic(10, System.Drawing.Color.Black, System.Drawing.Color.White, logo, 6, 6, true);
            if (!Directory.Exists(ImgDir))
            {
                Directory.CreateDirectory(ImgDir);
            }
            ImgFileInd++;
            m_Bitmap.Save(System.IO.Path.Combine(ImgDir, ImgFileInd.ToString("D5") + "-" + content + ".png"));
        }

        private void batchGenerateQRImage(string ImgDir, QRCodeGenerator.ECCLevel ecc)
        {
            System.Drawing.Bitmap logo = SinoCloudWisdomBarCodeGenerator.Properties.Resources.logo;
            while (ImgFileInd < 50000)
            {
                string content = getRandom(range) + barcodeFlagSwitch0;
                generateQRImage(content, ImgDir, ecc, logo, false, true);
            }
        }

#endif

    }
}
