using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace Server
{
    class Phone
    {
        bool changeNewLight = false;
        TcpListener Server;
        Socket Client;
        Thread Th_Clt;
        Light light;
        QueryDB queryDB;
        ColorModel color;
        Form1 f1;
        Timer timer1;
        public List<double> Object_Lab { get; set; }
        List<int> Light_RGB;
        List<double> Light_XYZ;
        List<double> Light_Lab;
        List<int> CCT_List;
        List<double> Rate;
        public string ServerIP { get; set; }
        public int ServerPort { get; set; }
        public Phone(Light L, Timer t, QueryDB Q, Form1 f)
        {
            CCT_List = new List<int>() { 5500,8000,22000,22000,9000,20500,
                                        17500,8000,5500,9000,7000,22000,
                                        6000,9000,22000,7500,5500,22000,
                                        5500,5500,5500,20000,20000,5500,5500,
                                        20000,20000,5500,5500,20000,5500,
                                        20000,5500,20000,20000 };//34 TCS'S BEST CCT WITH BEST CQS
            light = L;
            queryDB = Q;
            color = new ColorModel();
            timer1 = t;
            f1 = f;
        }
        public void ServerSub()
        {
            IPEndPoint EP = new IPEndPoint(IPAddress.Parse(ServerIP), ServerPort); //Server IP 和 Port
            Server = new TcpListener(EP); //建立伺服端監聽器(總機)
            Server.Start();
            while (true)
            {
                Client = Server.AcceptSocket(); //建立此客戶的連線物件Client
                Th_Clt = new Thread(Listen); //建立監聽這個客戶連線的獨立執行緒
                Th_Clt.IsBackground = true; //設定為背景執行緒
                Th_Clt.Start();
            }
        }
        //監聽客戶訊息的程式
        private void Listen()
        {
            Socket sck = Client;//複製Client通訊物件到個別客戶專用物件Sck
            Thread Th = Th_Clt;//複製執行緒Th_Clt到區域變數Th
            while (true)
            {
                try
                {
                    byte[] Byte = new byte[1023];    //建立接收資料用的陣列，長度須大於可能的訊息
                    int inLen = sck.Receive(Byte); //接收網路資訊(Byte陣列)
                    string Msg = Encoding.Default.GetString(Byte, 0, inLen);
                    string Mode = Msg.Substring(0, 1);
                    string Str = Msg.Substring(1);
                    string[] Content = Msg.Split(',');
                    int CCT;
                    double L;
                    string LR = "0";
                    string LG = "0";
                    string LB = "0";
                    switch (Mode)
                    {
                        case "M"://manual
                            Light_RGB = new List<int>() { };
                            changeNewLight = false;
                            if (Content[1] == "C")//select cct
                            {
                                CCT = int.Parse(Content[2]);
                                Query_RGB(CCT);
                                LR = Light_RGB[0].ToString();
                                LG = Light_RGB[1].ToString();
                                LB = Light_RGB[2].ToString();
                            }
                            else
                            {
                                LR = Content[1];
                                LG = Content[2];
                                LB = Content[3];
                                Light_RGB.Add(int.Parse(LR));
                                Light_RGB.Add(int.Parse(LG));
                                Light_RGB.Add(int.Parse(LB));
                            }
                            light.Send(LR, LG, LB);
                            break;
                        case "A": //自動模式
                            f1.index = -1;
                            Light_RGB = new List<int>() { };
                            changeNewLight = false;
                            light.Send("0", "0", "0");//ONLY D65
                            Thread.Sleep(1000);
                            f1.GetPicture();//鏡頭影像擷取
                            while (true)
                            {
                                if (f1.index != -1)
                                {
                                    int index = f1.index;
                                    CCT = CCT_List[index];
                                    Query_RGB(CCT);//讀取資料庫中的RGB
                                    LR = Light_RGB[0].ToString();
                                    LG = Light_RGB[1].ToString();
                                    LB = Light_RGB[2].ToString();
                                    light.Send(LR, LG, LB);//控制燈具
                                    break;
                                }
                            }
                            break;
                    }
                    f1.Light_RGB = Light_RGB;
                }
                catch (Exception)
                {

                }
            }
        }
        private void Query_RGB(int CCT)
        {
            string sqlStr = "SELECT * FROM  CCT_to_RGB WHERE CCT=@CCT ";
            string[] cct_parameter = new string[1] { "@CCT" };
            int[] data = new int[1] { CCT };
            queryDB.Command(sqlStr);
            queryDB.AddParameter(cct_parameter, data);
            Light_RGB = queryDB.Get_RGB();
        }
        private void Adjust_L(List<double> XYZ, double rate)
        {
            Light_RGB = new List<int>() { };
            double X = XYZ[0] * rate;
            double Y = XYZ[1] * rate;
            double Z = XYZ[2] * rate;
            Light_RGB = color.XYZ_to_RGB(X.ToString(), Y.ToString(), Z.ToString());
            while (true)
            {
                if (Light_RGB != null)
                {
                    light.Send(Light_RGB[0].ToString(), Light_RGB[1].ToString(), Light_RGB[2].ToString());
                    break;
                }
            }
        }
    }
}
