using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.ComponentModel;
using System.Windows.Media.Animation;
using System.IO;
using System.Diagnostics;
using System.Windows.Interop;
using dColor = System.Windows.Media.Color;
//using Emgu.CV;
//using Emgu.CV.Structure;
//using MjpegProcessor;

namespace WpfApp11
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BackgroundWorker listenerWorker = new BackgroundWorker { WorkerReportsProgress = true, WorkerSupportsCancellation = true };

        // From client (phone)
        IPEndPoint iPEndPointR;
        TcpListener listener;
        IPAddress iPAddress;

        // To server (arduino)
        IPEndPoint iPEndPointF0;
        IPEndPoint iPEndPointF1;
        TcpClient tcpClient;
        NetworkStream networkStream;

        String recievedData = "";

        Tuple<int, int, int> rgbTuple;
        int listeningPort = 9000;

        // Define storyboards
        Storyboard sb1;
        Storyboard sb2;
        Storyboard sb3;

        //MainColor
        string result;
        string[] ContentObj;

        //webcam
        //private Capture _capture = null;
        //private Mat _frame;

        //IPCAM
        //MjpegDecoder _mjpeg;

        public MainWindow()
        {
            InitializeComponent();
            DrawDefaultUI();
        }

        private void DrawDefaultUI()
        {
            // Attach storyboards
            sb1 = FindResource("Animate_Button_1") as Storyboard;
            sb2 = FindResource("Animate_Button_2") as Storyboard;
            sb3 = FindResource("Animate_Button_3") as Storyboard;

            ServerToggle_Button.IsEnabled = false;
            ServerToggle_Button.Content = "請稍後....";
            ServerHint_Block.Text = "正在準備....";

            ArduinoConnect_Button.Content = "連接燈具";
            Arduino2Connect_Button.Content = "連接燈具";
            //--- Spotlight_1
            //slider
            R_Slider.IsEnabled = false;
            G_Slider.IsEnabled = false;
            B_Slider.IsEnabled = false;
            //textbox
            R_Block.IsEnabled = false;
            G_Block.IsEnabled = false;
            B_Block.IsEnabled = false;
            R_Block.Text = "0";
            G_Block.Text = "0";
            B_Block.Text = "0";
            //button
            SendRgb_Button.IsEnabled = false;
            Brighten_Button.IsEnabled = false;
            D65_Button.IsEnabled = false;

            //--- Spotlight_2
            //slider
            R2_Slider.IsEnabled = false;
            G2_Slider.IsEnabled = false;
            B2_Slider.IsEnabled = false;
            //textbox
            R2_Block.IsEnabled = false;
            G2_Block.IsEnabled = false;
            B2_Block.IsEnabled = false;
            R2_Block.Text = "0";
            G2_Block.Text = "0";
            B2_Block.Text = "0";
            //button
            SendRgb2_Button.IsEnabled = false;
            Brighten2_Button.IsEnabled = false;
            //tracking
            BtnTracking.IsEnabled = true;//false
            BtnStop.IsEnabled = false;

            Status_Block.Text = "";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Register events
            listenerWorker.DoWork += ListenerWorker_DoWork;
            listenerWorker.ProgressChanged += ListenerWorker_ProgressChanged;
            listenerWorker.RunWorkerCompleted += ListenerWorker_RunWorkerCompleted;

            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;

            // Manually check network information
            if (NetworkInterface.GetIsNetworkAvailable()) CoreStart();
            else CoreStop();

        }

        private void ListenerWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!listenerWorker.CancellationPending)
            {
                try
                {
                    using (TcpClient client = listener.AcceptTcpClient())
                    {
                        string lastConnectString = string.Format("最後連接裝置：\"{0}\"", ((System.Net.IPEndPoint)client.Client.RemoteEndPoint).Address);
                        listenerWorker.ReportProgress(0, new Tuple<string, string>(lastConnectString, ""));

                        try
                        {
                            NetworkStream networkStream = client.GetStream();
                            networkStream.ReadTimeout = 1000;
                            networkStream.WriteTimeout = 1000;

                            Byte[] recievedBytes = new byte[256];
                            while (networkStream.Read(recievedBytes, 0, recievedBytes.Length) > 0)
                            {
                                recievedData = System.Text.Encoding.ASCII.GetString(recievedBytes, 0, recievedBytes.Length);
                                listenerWorker.ReportProgress(0, new Tuple<string, string>(null, recievedData));
                            }
                        }
                        catch (Exception ex) { } // Ignore exceptions
                    }
                }
                catch (Exception ex)
                {
                    return;
                }
            }
        }

        private void ListenerWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Tuple<string, string> statusMessage = e.UserState as Tuple<string, string>;
            if (statusMessage.Item1 != null)
            {
                ServerHint_Block.Text = statusMessage.Item1;
                Status_Block.Text = "已連接客戶端，若要切換客戶端，請點選\"重新啟動\"按鈕來顯示此伺服器位置。";
            }
            if (statusMessage.Item2 != null && statusMessage.Item2.Length != 0)
            {
                Recieved_Box.AppendText("Client -> " + statusMessage.Item2 + "\n");
                if (char.Parse(statusMessage.Item2.Substring(0, 1)) == '#')
                {
                    RgbUnpack(statusMessage.Item2.Substring(1));
                }
                else if (char.Parse(statusMessage.Item2.Substring(0, 1)) == '@')
                {
                    CmdProcess(statusMessage.Item2.Substring(1));
                }
            }

        }

        async private void ListenerWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // Stop the service, and restart
            CoreStop();
            await Task.Delay(100);
            if (NetworkInterface.GetIsNetworkAvailable()) CoreStart();
        }

        private void CmdProcess(string command)
        {
            // 請在這邊處理導向
        }
        private void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            if (e.IsAvailable)
            {
                this.Dispatcher.Invoke(() =>
                {
                    CoreStart();
                });
            }
            else
            {
                this.Dispatcher.Invoke(() =>
                {
                    CoreStop();
                });
            }
        }

        async private void CoreStart()
        {
            ServerToggle_Button.IsEnabled = false;
            Status_Block.Text = "已連接網路，正在啟動服務....";

            iPAddress = null;
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    iPAddress = ip;
                    break;
                }
            }

            if (iPAddress != null) // IP address assigned
            {
                ServerHint_Block.Text = string.Format("請將手機連接到 IP: \"{0}\" Port: \"{1}\"", iPAddress.ToString(), listeningPort);
            }
            else // IP address failed to assign
            {
                ServerHint_Block.Text = "無法取得 IP 位置";
            }

            iPEndPointR = new IPEndPoint(iPAddress, listeningPort);
            listener = new TcpListener(iPEndPointR);
            ServerToggle_Button.Content = "重新啟動";
            ServerToggle_Button.IsEnabled = true;
            try
            {
                listener.Start();
            }
            catch (Exception ex)
            {
                ServerToggle_Button.IsEnabled = true;
                ServerToggle_Button.Content = "重試";
                ServerHint_Block.Text = "無法啟動伺服器，按「重試」重新啟動";
                Status_Block.Text = "無法啟動伺服器，連接閘道可能已被占用";
                return;
            }
            Status_Block.Text = "已連接網路，等待裝置連接....";
            while (listenerWorker.IsBusy)
            {
                listenerWorker.CancelAsync();
                await Task.Delay(100);
            }
            listenerWorker.RunWorkerAsync();
        }

        private void CoreStop()
        {
            if (listener != null) listener.Stop();
            ServerToggle_Button.IsEnabled = false;
            if (NetworkInterface.GetIsNetworkAvailable()) // Determine the message should be showed
            {
                ServerToggle_Button.Content = "重新啟動....";
                ServerHint_Block.Text = "請稍後....";
                Status_Block.Text = "正在重新啟動伺服器....";
            }
            else
            {
                ServerToggle_Button.Content = "已停止";
                ServerHint_Block.Text = "請先將伺服器連上網路";
                Status_Block.Text = "已斷開網路，請將伺服器連上網路";
            }
        }
        private void ShowMainColor()
        {

            FileStream Fst = new FileStream(@"output/resultCA.txt", FileMode.Open);//@"D:\user\Desktop\color_ROI\output\resultCA.txt
            StreamReader sr = new StreamReader(Fst);
            result = sr.ReadToEnd().ToString();

            ContentObj = result.Split(',');
            // top1
            R1.Text = "R:" + ContentObj[0].Trim() + " ";
            G1.Text = "G:" + ContentObj[1];
            B1.Text = "B:" + ContentObj[2];
            P1.Text = "最高占比色:" + ContentObj[3] + "%";
            C1.Background = new SolidColorBrush(dColor.FromArgb(255, (byte)int.Parse(ContentObj[0]), (byte)int.Parse(ContentObj[1]), (byte)int.Parse(ContentObj[2])));
            //2
            R2.Text = "R:" + ContentObj[4].Trim() + " ";
            G2.Text = "G:" + ContentObj[5];
            B2.Text = "B:" + ContentObj[6];
            P2.Text = "第二占比色:" + ContentObj[7] + "%";
            C2.Background = new SolidColorBrush(dColor.FromArgb(255, (byte)int.Parse(ContentObj[4]), (byte)int.Parse(ContentObj[5]), (byte)int.Parse(ContentObj[6])));
            //3
            R3.Text = "R:" + ContentObj[8].Trim() + " ";
            G3.Text = "G:" + ContentObj[9];
            B3.Text = "B:" + ContentObj[10];
            P3.Text = "第三占比色:" + ContentObj[11] + "%";
            C3.Background = new SolidColorBrush(dColor.FromArgb(255, (byte)int.Parse(ContentObj[8]), (byte)int.Parse(ContentObj[9]), (byte)int.Parse(ContentObj[10])));
            string R1_ = ContentObj[0].Trim();
            string G1_ = ContentObj[1].Trim();
            string B1_ = ContentObj[2].Trim();
            string R2_ = ContentObj[4].Trim();
            string G2_ = ContentObj[5].Trim();
            string B2_ = ContentObj[6].Trim();
            string R3_ = ContentObj[8].Trim();
            string G3_ = ContentObj[9].Trim();
            string B3_ = ContentObj[10].Trim();

            string[] strArr = new string[9];//引數列表
            string sArguments = @"convert\RGB_to_Lab_1030.py";
            strArr[0] = R1_;
            strArr[1] = G1_;
            strArr[2] = B1_;
            strArr[3] = R2_;
            strArr[4] = G2_;
            strArr[5] = B2_;
            strArr[6] = R3_;
            strArr[7] = G3_;
            strArr[8] = B3_;
            //RunPythonScript(sArguments,"", strArr);
            RunCMD(sArguments, "", strArr);
        }//end of ShowMainColor
        private void RgbUnpack(string hex)
        {
            int[] values = new int[3];
            try
            {
                string tmp = int.Parse(hex, System.Globalization.NumberStyles.HexNumber).ToString("D9");
                values[0] = int.Parse(tmp.Substring(0, 3));
                values[1] = int.Parse(tmp.Substring(3, 3));
                values[2] = int.Parse(tmp.Substring(6, 3));
                //Recieved_Box.AppendText(string.Format("Red:{0} Green:{1} Blue:{2}\n", values[0], values[1], values[2]));
                FowardRgb(0, values);
            }
            catch (Exception ex)
            {
                // Nothing
                return;
            }
        }

        async private void FowardRgb(int device, int[] rgbValue)
        {
            /*
           R_Slider.Value = (double)rgbValue[0] / 255 * 10;
           G_Slider.Value = (double)rgbValue[1] / 255 * 10;
           B_Slider.Value = (double)rgbValue[2] / 255 * 10;
           */
            R_Slider.Value = (double)rgbValue[0];
            G_Slider.Value = (double)rgbValue[1];
            B_Slider.Value = (double)rgbValue[2];
            await FowardMessage(device, string.Format("R{0:D3}G{1:D3}B{2:D3}", rgbValue[0], rgbValue[1], rgbValue[2]));
        }
        async private void FowardRgb2(int device, int[] rgbValue)
        {
            R2_Slider.Value = (double)rgbValue[0];
            G2_Slider.Value = (double)rgbValue[1];
            B2_Slider.Value = (double)rgbValue[2];
            await FowardMessage(device, string.Format("R{0:D3}G{1:D3}B{2:D3}", rgbValue[0], rgbValue[1], rgbValue[2]));
        }

        async private Task<bool> FowardMessage(int device, string message)
        {
            try
            {
                tcpClient = new System.Net.Sockets.TcpClient();
                if (device == 0)
                {
                    await tcpClient.ConnectAsync(iPEndPointF0.Address, iPEndPointF0.Port);
                }
                else
                {
                    await tcpClient.ConnectAsync(iPEndPointF1.Address, iPEndPointF1.Port);
                }
                networkStream = tcpClient.GetStream();
                networkStream.ReadTimeout = 1000;
                networkStream.WriteTimeout = 1000;
                System.Text.Encoding enc = System.Text.Encoding.UTF8;
                byte[] sendBytes = enc.GetBytes(message);
                await networkStream.WriteAsync(sendBytes, 0, sendBytes.Length);
                await Task.Delay(10);
                tcpClient.Close();
                Sent_Box.AppendText("Device " + device + " <- " + message + "\n");
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        #region//for CA python
        //呼叫python核心程式碼
        public static void RunPythonScript(string sArgName, string args = "", params string[] teps)
        {
            Process p = new Process();
            string path = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + sArgName;// 獲得python檔案的絕對路徑（將檔案放在c#的debug資料夾中可以這樣操作）
            //path = sArgName;//(因為我沒放debug下，所以直接寫的絕對路徑,替換掉上面的路徑了) @"C:\Users\user\Desktop\test\" + sArgName;
            p.StartInfo.FileName = @"C:\Users\User\anaconda3\envs\spotlight\python.exe";//python環境  <--測試用
            //p.StartInfo.FileName = "../../../../../spotlight/python.exe";//python環境

            string sArguments = path;
            foreach (string sigstr in teps)
            {
                sArguments += " " + sigstr;//傳遞引數
            }

            sArguments += " " + args;

            p.StartInfo.Arguments = sArguments;//傳入引數
            //MessageBox.Show(sArguments);
            p.StartInfo.UseShellExecute = false;//是否使用外殼程式
            p.StartInfo.RedirectStandardOutput = true;//輸出
            p.StartInfo.RedirectStandardInput = true;//輸入
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;

            p.Start();/*
            // To avoid deadlocks, always read the output stream first and then wait.  
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            Console.WriteLine(output);
            p.Close();*/

            p.BeginOutputReadLine();
            p.OutputDataReceived += new DataReceivedEventHandler(p_OutputDataReceived);
            Console.ReadLine();
            p.WaitForExit();
            p.Close();



        }
        public static void RunCMD(string sArgName, string args = "", params string[] teps)
        {
            Process p = new Process();
            string path = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase + sArgName;

            p.StartInfo.FileName = "cmd.exe";

            string sArguments = path;
            foreach (string sigstr in teps)
            {
                sArguments += " " + sigstr;//傳遞引數
            }

            sArguments += " " + args;
            string WriteLine = @"C:\Users\User\anaconda3\envs\spotlight\python.exe " + sArguments;
            //MessageBox.Show(WriteLine);

            //p.StartInfo.Arguments = sArguments;//傳入引數
            //MessageBox.Show(sArguments);
            p.StartInfo.UseShellExecute = false;//是否使用外殼程式
            p.StartInfo.RedirectStandardOutput = true;//輸出
            p.StartInfo.RedirectStandardInput = true;//輸入
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;

            p.OutputDataReceived += new DataReceivedEventHandler(p_OutputDataReceived);
            //启动程序
            p.Start();
            //向cmd窗口发送输入信息
            p.StandardInput.WriteLine(WriteLine);

            p.StandardInput.AutoFlush = true;
            p.BeginOutputReadLine();
        }
        //輸出列印的資訊
        static void p_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                AppendText(e.Data + Environment.NewLine);
            }
        }
        public delegate void AppendTextCallback(string text);
        public static void AppendText(string text)
        {
            //Console.WriteLine(text);//此處在控制檯輸出.py檔案print的結果 
            Debug.WriteLine(text);
            //MessageBox.Show(text);          
        }
        #endregion
        #region //for webcam
        private void ComponentDispatcher_ThreadIdle(object sender, EventArgs e)
        {
            /*using (var imageFrame = _capture.QueryFrame().ToImage<Bgr, Byte>())
            {
                if (imageFrame != null)
                {
                    IMGwebcam.Source = ConvertBitmapToImageSource(imageFrame.Bitmap);
                }
            }*/
        }
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //_capture.Dispose();
        }
        /*private ImageSource ConvertBitmapToImageSource(Bitmap imToConvert)
        {
            Bitmap bmp = new Bitmap(imToConvert);
            MemoryStream ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();
            ImageSource sc = (ImageSource)image;
            return sc;
        }*/

        #endregion
    }

    public partial class MainWindow : Window
    {
        private void ServerToggle_Button_Click(object sender, RoutedEventArgs e)
        {
            sb1.Begin();
            if (listenerWorker.IsBusy)
            {
                listener.Stop();
                listenerWorker.CancelAsync();
            }
            else
            {
                if (NetworkInterface.GetIsNetworkAvailable()) CoreStart();
                else CoreStop();
            }
        }//ServerToggle_Button_Click

        async private void ArduinoConnect_Button_Click(object sender, RoutedEventArgs e)
        {
            sb2.Begin();
            IPAddress ip;
            if (IPAddress.TryParse(ArduinoIP_Box.Text, out ip))
            {
                ArduinoConnect_Button.Content = "正在連接....";
                ArduinoConnect_Button.IsEnabled = false;
                try
                {
                    iPEndPointF0 = new IPEndPoint(ip, int.Parse(ArduinoPort_Box.Text));
                    if (tcpClient != null) tcpClient.Dispose();
                    if (!await FowardMessage(0, "Hello, World!")) throw new Exception();
                    FowardRgb(0, new int[3] { 0, 0, 0 });
                    ArduinoConnect_Button.Content = "已連接";
                    Status_Block.Text = "已連接到燈具，若要切換燈具，請更改位置後點選\"已連接\"按鈕即可。";
                    //slider
                    R_Slider.IsEnabled = true;
                    G_Slider.IsEnabled = true;
                    B_Slider.IsEnabled = true;
                    //textbox
                    R_Block.IsEnabled = true;
                    G_Block.IsEnabled = true;
                    B_Block.IsEnabled = true;
                    //button
                    SendRgb_Button.IsEnabled = true;
                    D65_Button.IsEnabled = true;
                    Brighten_Button.IsEnabled = true;
                    SendRgb2_Button.IsEnabled = true;
                    Brighten2_Button.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    iPEndPointF0 = null;
                    tcpClient = new TcpClient();
                    ArduinoConnect_Button.Content = "連接失敗";
                    //slider
                    R_Slider.IsEnabled = false;
                    G_Slider.IsEnabled = false;
                    B_Slider.IsEnabled = false;
                    R_Slider.Value = 0;
                    G_Slider.Value = 0;
                    B_Slider.Value = 0;
                    //textbox
                    R_Block.IsEnabled = false;
                    G_Block.IsEnabled = false;
                    B_Block.IsEnabled = false;
                    //button
                    SendRgb_Button.IsEnabled = false;
                    D65_Button.IsEnabled = false;
                    Brighten_Button.IsEnabled = false;
                    SendRgb2_Button.IsEnabled = false;
                    Brighten2_Button.IsEnabled = false;

                    /*
                    R2_Slider.IsEnabled = false;
                    G2_Slider.IsEnabled = false;
                    B2_Slider.IsEnabled = false;
                    R2_Slider.Value = 0;
                    G2_Slider.Value = 0;
                    B2_Slider.Value = 0;
                    SendRgb2_Button.IsEnabled = false;
                    */
                }
                finally
                {
                    ArduinoConnect_Button.IsEnabled = true;
                }
            }
        }

        async private void Arduino2Connect_Button_Click(object sender, RoutedEventArgs e)
        {
            sb2.Begin();
            IPAddress ip;
            if (IPAddress.TryParse(Arduino2IP_Box.Text, out ip))
            {
                Arduino2Connect_Button.Content = "正在連接....";
                Arduino2Connect_Button.IsEnabled = false;
                try
                {
                    iPEndPointF1 = new IPEndPoint(ip, int.Parse(Arduino2Port_Box.Text));
                    if (tcpClient != null) tcpClient.Dispose();
                    if (!await FowardMessage(1, "Hello, World!")) throw new Exception();
                    FowardRgb(1, new int[3] { 0, 0, 0 });
                    Arduino2Connect_Button.Content = "已連接";
                    Status_Block.Text = "已連接到燈具，若要切換燈具，請更改位置後點選\"已連接\"按鈕即可。";
                    //slider
                    R2_Slider.IsEnabled = true;
                    G2_Slider.IsEnabled = true;
                    B2_Slider.IsEnabled = true;
                    //textbox
                    R2_Block.IsEnabled = true;
                    G2_Block.IsEnabled = true;
                    B2_Block.IsEnabled = true;
                    //button
                    SendRgb2_Button.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    iPEndPointF1 = null;
                    tcpClient = new TcpClient();
                    Arduino2Connect_Button.Content = "連接失敗";
                    //slider
                    R2_Slider.IsEnabled = false;
                    G2_Slider.IsEnabled = false;
                    B2_Slider.IsEnabled = false;
                    R2_Slider.Value = 0;
                    G2_Slider.Value = 0;
                    B2_Slider.Value = 0;
                    //textbox
                    R2_Block.IsEnabled = false;
                    G2_Block.IsEnabled = false;
                    B2_Block.IsEnabled = false;
                    //button
                    SendRgb2_Button.IsEnabled = false;
                }
                finally
                {
                    Arduino2Connect_Button.IsEnabled = true;
                }
            }
        }

        async private void Arduino2ColorUpdate_Click(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            if (b.Tag != null) await FowardMessage(1, b.Tag.ToString());
        }

        private void R_Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            R_Block.Text = ((int)(R_Slider.Value)).ToString();
        }

        private void G_Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            G_Block.Text = ((int)(G_Slider.Value)).ToString();
        }

        private void B_Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            B_Block.Text = ((int)(B_Slider.Value)).ToString();
        }

        private void R2_Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            R2_Block.Text = ((int)(R2_Slider.Value)).ToString();
        }

        private void G2_Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            G2_Block.Text = ((int)(G2_Slider.Value)).ToString();
        }

        private void B2_Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            B2_Block.Text = ((int)(B2_Slider.Value)).ToString();
        }

        private void SendRgb_Button_Click(object sender, RoutedEventArgs e)
        {
            sb3.Begin();
            //int[] sliderValue = new int[3] { (int)(R_Slider.Value / 10 * 255), (int)(G_Slider.Value / 10 * 255), (int)(B_Slider.Value / 10 * 255) };
            int[] sliderValue = new int[3] { (int)(R_Slider.Value), (int)(G_Slider.Value), (int)(B_Slider.Value) };
            FowardRgb(0, sliderValue);
        }
        private void Brighten_Button_Click(object sender, RoutedEventArgs e)
        {
            FileStream Fst = new FileStream(@"output/resultCCTRGB.txt", FileMode.Open);//@"D:\user\Desktop\color_ROI\output\resultCA.txt
            StreamReader sr = new StreamReader(Fst);
            result = sr.ReadToEnd().ToString();

            ContentObj = result.Split(',');

            // Int32.TryParse(ContentObj[0], out number);
            int bestR = Int32.Parse(ContentObj[0]);
            int bestG = Int32.Parse(ContentObj[1]);
            int bestB = Int32.Parse(ContentObj[2]);

            //Console.WriteLine("bestRGB type" + bestR.GetType() + "   " + bestG.GetType() + "   " + bestB.GetType());

            int[] BrightenValue = new int[3] { bestR, bestG, bestB };
            FowardRgb(0, BrightenValue);
        }
        private void D65_Button_Click(object sender, RoutedEventArgs e)
        {
            int[] BrightenValue = new int[3] { 180, 200, 100 };
            FowardRgb(0, BrightenValue);
        }


        private void SendRgb2_Button_Click(object sender, RoutedEventArgs e)
        {
            //int[] slider2Value = new int[3] { (int)(R2_Slider.Value / 10 * 255), (int)(G2_Slider.Value / 10 * 255), (int)(B2_Slider.Value / 10 * 255) };
            int[] slider2Value = new int[3] { (int)(R2_Slider.Value), (int)(G2_Slider.Value), (int)(B2_Slider.Value) };
            FowardRgb2(1, slider2Value);
        }
        private void Brighten2_Button_Click(object sender, RoutedEventArgs e)
        {
            FileStream Fst = new FileStream(@"output/resultCCTRGB.txt", FileMode.Open);//@"D:\user\Desktop\color_ROI\output\resultCA.txt
            StreamReader sr = new StreamReader(Fst);
            result = sr.ReadToEnd().ToString();

            ContentObj = result.Split(',');

            // Int32.TryParse(ContentObj[0], out number);
            int bestR = Int32.Parse(ContentObj[0]);
            int bestG = Int32.Parse(ContentObj[1]);
            int bestB = Int32.Parse(ContentObj[2]);

            //Console.WriteLine("bestRGB type" + bestR.GetType() + "   " + bestG.GetType() + "   " + bestB.GetType());

            int[] BrightenValue = new int[3] { bestR, bestG, bestB };
            FowardRgb2(1, BrightenValue);
        }
        private void Recieved_Box_TextChanged(object sender, TextChangedEventArgs e)
        {
            Recieved_Box.ScrollToEnd();
        }

        private void Sent_Box_TextChanged(object sender, TextChangedEventArgs e)
        {
            Sent_Box.ScrollToEnd();
        }

        private void BtnTracking_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("請確保追蹤目標目前為靜止狀態", "提醒", MessageBoxButton.OK, MessageBoxImage.Warning);

            string[] strArr = new string[1];
            string sArguments = @"tracking_IPCAM.py";//python
            //strArr[0] = "0";//傳入參數
            strArr[0] = "http://root:A107222@192.168.0.162/mjpg/1/video.mjpg";//IPCAM
            RunPythonScript(sArguments, "-u", strArr);


            //接收IPCAM即時畫面並顯示
            //_mjpeg = new MjpegDecoder();
            //_mjpeg.FrameReady += mjpeg_FrameReady;
            //_mjpeg.ParseStream(new Uri("http://root:A107222@192.168.0.162/mjpg/1/video.mjpg"));


            BtnStop.IsEnabled = true;

        }
        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            //停止串流
            //_mjpeg.StopStream();
        }
        /*private void mjpeg_FrameReady(object sender, FrameReadyEventArgs e)
        {
            //播放即時畫面
            IMGIPcam.Source = e.BitmapImage;
        }*/

        private void BtnWebcam_Click(object sender, RoutedEventArgs e)
        {
            //===call python          
            string[] strArr = new string[1];//引數列表
            string sArguments = @"ROI_color.py";//這裡是python的檔名字
            strArr[0] = "1";
            RunPythonScript(sArguments, "-u", strArr);
            //RunCMD(sArguments, "", strArr);

            //====透過emgu把webcam畫面接到xaml上
            this.Closing += MainWindow_Closing;
            //_capture = new Capture(0);    //預設鏡頭 測試用
            //_capture = new Capture(1);//新版emgu改成VideoCapture
            //_capture.ImageGrabbed += _capture_ImageGrabbed;
            //_frame = new Mat();
            //_capture.Start();
            ComponentDispatcher.ThreadIdle += ComponentDispatcher_ThreadIdle;

            ShowMainColor();

            BtnTracking.IsEnabled = true;
        }
    }
}//end


