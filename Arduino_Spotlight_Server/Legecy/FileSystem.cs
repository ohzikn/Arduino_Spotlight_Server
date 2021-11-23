using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class FileSystem
    {
        public void Log(string logMessage, TextWriter w)
        {
            w.WriteLine($":{logMessage}");

        }
        public String Read()
        {

            FileStream fs = new FileStream(@"log/result.txt", FileMode.Open);
            StreamReader sr = new StreamReader(fs);
            sr.ReadToEnd().ToString();
            return sr.ReadToEnd().ToString();

        }
    }
}
