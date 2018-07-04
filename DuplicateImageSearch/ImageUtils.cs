using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;

namespace DuplicateImageSearch
{
    public static class ImageUtils
    {
        public static int W = 8;
        public static int H = 8;

        public static Image GetImage(String filePath)
        {
            Image SourceImg = null;
            if (filePath.IndexOf(".webp") > 0)
            {
                WebP webp = new WebP();
                Bitmap bmp = webp.Load(filePath);
                SourceImg = bmp;
            }
            else
            {
                FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                byte[] bytes = new byte[fs.Length];
                fs.Read(bytes, 0, bytes.Length);
                fs.Close();
                MemoryStream ms = new MemoryStream(bytes);

                SourceImg = (Image)new Bitmap(ms);
                    
            }
            return SourceImg;
        }


        public static String[] GetHash(string filePath) {



            Image SourceImg = GetImage(filePath);

            if (SourceImg != null){// 读取到的图片有数据
                String[] R = new String[7];

                FileInfo fi = new FileInfo(filePath);
                long file_len = fi.Length;
                if (file_len < 1024) { //B
                    R[4] = file_len + " B";
                } else if (file_len >= 1024 && file_len < 1024 * 1024)
                { //KB
                    R[4] = file_len / 1024 + " KB";
                }
                else
                { //MB    
                    R[4] = file_len / (1024 * 1024) + " MB";
                }


                R[0] = filePath.Substring(filePath.LastIndexOf("\\"));
                R[1] = filePath;
                R[2] = SourceImg.Width + "";
                R[3] = SourceImg.Height + "";


                R[5] = fi.LastWriteTime.ToString();
                R[6] = ComputeDHash(SourceImg);
                SourceImg.Dispose();
                return R;
            }
            return null;
        }

        // Reduce size to w*h
        private static Image ReduceSize(Image SourceImg,int w,int h)
        {
            Image image = SourceImg.GetThumbnailImage(w, h, ()=>{return false;}, IntPtr.Zero);
            return image;
        }


        // Function : Compute the aHash
        private static String ComputeAHash(Image SourceImg)
        {
            Image image = ReduceSize(SourceImg, W, H);

            // Reduce Color
            Bitmap bitMap = new Bitmap(image);
            Byte[] grayValues = new Byte[image.Width * image.Height];
            for (int x = 0; x < image.Width; x++) { 
                for (int y = 0; y < image.Height; y++)
                {
                    Color color = bitMap.GetPixel(x, y);
                    byte grayValue = (byte)((color.R * 30 + color.G * 59 + color.B * 11) / 100);
                    grayValues[x * image.Width + y] = grayValue;
                }
            }

            // Average the colors
            int sum = 0;
            for (int i = 0; i < grayValues.Length; i++)
                sum += (int)grayValues[i];

            Byte average = Convert.ToByte(sum / grayValues.Length);

            // Get aHash
            char[] result = new char[grayValues.Length];
            for (int i = 0; i < grayValues.Length; i++)
            {
                if (grayValues[i] < average)
                    result[i] = '0';
                else
                    result[i] = '1';
            }
            return new String(result);
        }


        // Function : Compute the dHash
        private static String ComputeDHash(Image SourceImg)
        {
            // Resize Image
            Image image = ReduceSize(SourceImg, W+1, H+1);

            // Reduce Color
            Bitmap bitMap = new Bitmap(image);
            Byte[,] grayValues = new Byte[image.Width, image.Height];
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    Color color = bitMap.GetPixel(x, y);
                    byte grayValue = (byte)((color.R * 30 + color.G * 59 + color.B * 11) / 100);
                    grayValues[x,y] = grayValue;
                }
            }




            // Save the reuslt
            String row_dhash = "";
            String col_dhash = "";

           
            for (int i = 1; i < image.Width; i++){
                for (int j = 1; j < image.Height; j++){
                    // Compute row differnce
                    if (grayValues[i-1, j] > grayValues[i, j])
                        row_dhash += "1";
                    else
                        row_dhash += "0";

                    // Compute row differnce
                    if (grayValues[i, j-1] > grayValues[i, j])
                        col_dhash += "1";
                    else
                        col_dhash += "0";
                }
            }

 
            
            return row_dhash + col_dhash;
        }


        public static String GetHisogram(Image image)
        {
           
           
            Bitmap bitMap = new Bitmap(image);
            int[] histogram = new int[256];

            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    
                    Color color = bitMap.GetPixel(x, y);
                    byte grayValue = (byte)((color.R * 30 + color.G * 59 + color.B * 11) / 100); // Reduce Color
                    histogram[(int)grayValue]++;  // Compute histogram
                }
            }
    
            // Int[] Convert String
            String histogram_str = String.Join(",", histogram);

            return histogram_str;

        }

        public static String GetHisogram(String url) {
            // Get Image
            Image SourceImg = GetImage(url);
            return GetHisogram(SourceImg);
        }


        // Compare hash
        public static Int32 CalcSimilarDegree(string a, string b){
            if (a.Length != b.Length){
                throw new ArgumentException();
            }         
            int count = 0;
            for (int i = 0; i < a.Length; i++) {
                if (a[i] != b[i])
                    count++;
            }
            return count;
        }

        public static double CalHistogramSimilar(int[] h1, int[] h2) {
            if( h1.Length!=256 || h2.Length!=256 ){
                return -1.0;
            } 

            double result = 0.0;
            for (int i = 0; i < 256; i++) {
                if(Math.Max(h1[i], h2[i])!= 0)
                    result += 1 - (Math.Abs((double)h1[i] - (double)h2[i])/Math.Max(h1[i], h2[i]));
            }
     
            return result / 256;

        }

        public static double CalHistogramSimilar(String h1_str, String h2_str)
        {
            String[] h1_group = h1_str.Split(',');
            String[] h2_group = h2_str.Split(',');

            if (h1_group.Length !=256 || h2_group.Length != 256) {
                return -1.0;
            }

            double[] h1 = new double[256];
            double[] h2 = new double[256];
            for(int i = 0; i < 256; i++) {
                h1[i] = double.Parse(h1_group[i]);
                h2[i] = double.Parse(h2_group[i]);
            }

            double result = 0.0;
            for (int i = 0; i < 256; i++){
                if (h1[i]!=0.0 && h2[i]!=0.0)
                    result += 1 - (Math.Abs(h1[i] - h2[i]) / Math.Max(h1[i], h2[i]));
            }

            return result / 256;

        }

    }
}
