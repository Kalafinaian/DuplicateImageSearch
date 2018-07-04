using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DuplicateImageSearch
{
    public static class FileUtils
    {
        private static String[] image_type = new String[] { ".jpg", ".webp", ".gif",".bmp", ".jpeg",".tif",".png" };


 
        public static void getAllImages(List<String> all_images_group, String dir_path)
        {
            String[] dirs = System.IO.Directory.GetDirectories(dir_path); // 遍历多少个文件夹

            foreach (String dir in dirs) {
                getAllImages(all_images_group, dir);
            }

            String[] files = System.IO.Directory.GetFiles(dir_path);
            foreach (String file in files)
            {
                if (file.LastIndexOf('.') > 0) // 文件有后缀名
                {
                    String file_type = file.Substring(file.LastIndexOf('.')); // 获取文件的type
                    file_type = file_type.ToLower(); // 文件扩展名转为小写

                    if (image_type.Contains(file_type) && !all_images_group.Contains(file))  // 如果是图片格式且之前没有出现在保存数组中
                    {
                        //Console.WriteLine(file);
                        all_images_group.Add(file);
                    }

                }
              

            }


        }


        static void Main()
        {


            //String dir = "E:/360VIP/Image";
            //List<String> all_images_group = new List<string>();
            //FileUtils.getAllImages(all_images_group, dir);
            //Console.WriteLine(all_images_group.Count);

            //// 打开所有的图片
            //for (int i = 0; i < all_images_group.Count; i++)
            //{
            //    String image_url = all_images_group[i];
            //    Console.WriteLine(image_url);
            //    String image_hashval = SimilarPhoto.GetHash(image_url);
                
            //    Console.WriteLine(image_hashval);
            //    Console.WriteLine();
            //    //this.textBox1.Text = (i + 1) + "/" + this.all_images_group.Count; //显示有多少张图片

            //}

            //String image_url = "E:/360VIP/Artist/[Uncensored] Tarakanovich/003_0000yb90.webp";
            //String image_hashval = SimilarPhoto.GetHash(image_url);
            //Console.WriteLine(image_url);
            //Console.WriteLine(image_hashval);
            //Console.ReadKey();
        }
    }
}
