using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Server
{
    class Camera
    {
        Capture ca;
        PictureBox pic;
        Bitmap bmp;
        Bitmap bmp1;
        Mat mat;
        public Camera(Capture c, PictureBox p, Bitmap b)
        {
            ca = c;
            pic = p;
            bmp = b;
            ca.ImageGrabbed += Capture_I;
            ca.Start();
        }
        private void Capture_I(object sender, EventArgs e)
        {
            mat = new Mat();
            ca.Retrieve(mat);
            using (MemoryStream ms = new MemoryStream())
            {
                mat.ToImage<Bgr, byte>().Bitmap.Save(ms, ImageFormat.Bmp);
                bmp = mat.ToImage<Bgr, byte>().Bitmap;
                pic.Image = bmp;
            }
        }
        public void GetFrame(string CameraNo)
        {

            using (MemoryStream ms = new MemoryStream())
            {
                switch (CameraNo)
                {
                    case "0":
                        if (bmp != null)
                        {
                            bmp.Save("CamPic/0.png");
                        }
                        break;
                    case "1":
                        if (bmp != null)
                        {
                            bmp.Save("CamPic/1.png");
                        }
                        break;
                    case "2":
                        if (bmp != null)
                        {
                            bmp.Save("CamPic/2.png");
                        }
                        break;
                    case "3":
                        if (bmp != null)
                        {
                            bmp.Save("CamPic/3.png");
                        }
                        break;
                }
            }
        }
    }
}
