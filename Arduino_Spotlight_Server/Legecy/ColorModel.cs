using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class ColorModel
    {
        List<int> RGB;
        List<double> XYZ;
        List<double> Lab;
        FileSystem fs;
        Thread Cvt;
        string result;
        bool CanWrite = true;
        int priority = 0;
        FileStream Fst;
        public ColorModel()
        {
        }

        public int Judge()
        {
            List<int> C = new List<int>() { 3000,3500,4000,4500,5000,5500,6000,6500,7000,7500,8000,8500,9000,9500,10000
                                           ,10500,11000,11500,12000,12500,13000,13500,14000,14500,15000,15500,16000,16500
                                           ,17000,17500,18000,18500,19000,19500,20000,20500,21000,21500,22000,22500,23000
                                           ,23500,24000,24500,25000};
            Random r = new Random();
            int i = r.Next(0, 16);
            int CCT = C[i];
            return CCT;
        }
        public List<double> RGB_to_XYZ(string R, string G, string B)
        {
            result = null;
            string cmd;
            // do { Thread.Sleep(10); } while (CanWrite != false);
            cmd = "RGB_to_XYZ," + R + "," + G + "," + B + ",";
            CanWrite = false;
            fs = new FileSystem();
            using (StreamWriter w = File.AppendText(@"log/cmdlog.txt"))
            {
                fs.Log(cmd, w);
            }
            Cvt = new Thread(WaitForTurn);
            Cvt.Start();
            do { Thread.Sleep(10); } while (result == null);
            string[] XYZd = result.Split(',');
            XYZ = new List<double>() { };
            XYZ.Add(double.Parse(XYZd[0]));
            XYZ.Add(double.Parse(XYZd[1]));
            XYZ.Add(double.Parse(XYZd[2]));
            return XYZ;
        }
        public List<double> RGB_to_Lab(string R1, string G1, string B1, string R2, string G2, string B2, string R3, string G3, string B3)
        {

            result = null;
            string cmd;
            do { Thread.Sleep(10); } while (Cvt != null);
            cmd = "RGB_to_Lab," + R1 + "," + G1 + "," + B1 + "," + R2 + "," + G2 + "," + B2 + "," + R3 + "," + G3 + "," + B3 + ",";

            fs = new FileSystem();
            using (StreamWriter w = File.AppendText(@"log/cmdlog.txt"))
            {
                fs.Log(cmd, w);
            }
            Cvt = new Thread(WaitForTurnL);
            Cvt.Start();
            do { Thread.Sleep(10); } while (result == null);
            string[] Labd = result.Split(',');
            Lab = new List<double>();
            Lab.Add(double.Parse(Labd[0]));
            Lab.Add(double.Parse(Labd[1]));
            Lab.Add(double.Parse(Labd[2]));
            Lab.Add(double.Parse(Labd[3]));
            Lab.Add(double.Parse(Labd[4]));
            Lab.Add(double.Parse(Labd[5]));
            Lab.Add(double.Parse(Labd[6]));
            Lab.Add(double.Parse(Labd[7]));
            Lab.Add(double.Parse(Labd[8]));
            return Lab;
        }
        public List<int> XYZ_to_RGB(string X, string Y, string Z)
        {

            result = null;
            string cmd;
            do { Thread.Sleep(10); } while (Cvt != null);
            cmd = "XYZ_to_RGB," + X + "," + Y + "," + Z + ",";

            fs = new FileSystem();
            using (StreamWriter w = File.AppendText(@"log/cmdlog.txt"))
            {
                fs.Log(cmd, w);
            }
            Cvt = new Thread(WaitForTurn);
            Cvt.Start();
            do { Thread.Sleep(10); } while (result == null);
            string[] Rgbi = result.Split(',');
            RGB = new List<int>();
            RGB.Add(int.Parse(Rgbi[0]));
            RGB.Add(int.Parse(Rgbi[1]));
            RGB.Add(int.Parse(Rgbi[2]));
            return RGB;
        }
        public int GetIndex(string R1, string G1, string B1, string R2, string G2, string B2, string R3, string G3, string B3, double rate1, double rate2, double rate3)
        {
            result = null;
            string cmd;
            do { Thread.Sleep(10); } while (Cvt != null);
            cmd = "GetIndex," + R1 + "," + G1 + "," + B1 + "," + R2 + "," + G2 + "," + B2 + "," + R3 + "," + G3 + "," + B3 + "," + rate1 + "," + rate2 + "," + rate3 + ",";

            fs = new FileSystem();
            using (StreamWriter w = File.AppendText(@"log/cmdlog.txt"))
            {
                fs.Log(cmd, w);
            }
            Cvt = new Thread(WaitForTurn);
            Cvt.Start();
            do { Thread.Sleep(10); } while (result == null);
            return int.Parse(result);
        }
        int count = 0;
        void WaitForTurn()
        {
            using (FileStream fs = File.Create(@"log/lck/OnLock.lck"))
            {
                // your code

            }

            do
            {
                Thread.Sleep(10);

                if (!File.Exists(@"log/lck/OnLock.lck"))
                {
                    Fst = new FileStream(@"log/result.txt", FileMode.Open);
                    StreamReader sr = new StreamReader(Fst);
                    result = sr.ReadToEnd().ToString();

                    Fst.Close();
                    sr.Close();
                    File.Delete(@"log/result.txt");
                    count++;
                }
            } while (result == null && count != 1);
            count = 0;
            Cvt = null;
        }
        void WaitForTurnL()
        {
            using (FileStream fs = File.Create(@"log/lck/OnLockL.lck"))
            {
                // your code

            }

            do
            {
                Thread.Sleep(10);

                if (!File.Exists(@"log/lck/OnLockL.lck"))
                {
                    Fst = new FileStream(@"log/resultL.txt", FileMode.Open);
                    StreamReader sr = new StreamReader(Fst);
                    result = sr.ReadToEnd().ToString();

                    Fst.Close();
                    sr.Close();
                    File.Delete(@"log/resultL.txt");
                    count++;
                }
            } while (result == null && count != 1);
            count = 0;
            Cvt = null;
        }
    }
}
