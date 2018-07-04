using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using Microsoft.VisualBasic.FileIO;

namespace DuplicateImageSearch
{
    public partial class Form1 : Form
    {
        private String FOLD_BAKCUP = "fold_backup.log";
        private List<String> fold_group { get; set; }
        private List<String> all_images_group { get; set; }
        private int thread_num = 5;
   


        private int progreassbar_num = 0;  
        private List<ImageHashObj> iho_group = null;
        private List<ImageGroup> ig_group = null;
        private List<ImageGroup> view_ig_group = null;
        private int process_group = 0;
        private double similar_val = 0.80;
        private int W = 32;
        private int H = 32;
        private int compare_load_num = 100;

        // 表格有关的数据
        private DataTable dt = null;

        private String left_url = "";
        private String right_url = "";
        private String row_id = "";
        private int row = -1;

        double[] weight = new double[3] { 0.70, 0.30, 0.0 };
        private int[,] compare_idx;
 

        private bool finish_readimg = false;

        public Form1()
        {
            InitializeComponent();
            fold_group = new List<string>();

            // 读取上一次选中的目录到txt
            if(File.Exists(this.FOLD_BAKCUP)){
                FileStream fs = new FileStream(this.FOLD_BAKCUP, FileMode.Open);
                StreamReader sr = new StreamReader(fs);
                String fold_str = sr.ReadLine();
                while(fold_str!=null) {
                    this.fold_group.Add(fold_str);
                    this.listBox2.Items.Add(fold_str);
                    fold_str = sr.ReadLine();
                }
                sr.Close();
                fs.Close();
            }

            this.dt = new DataTable("Similar_Images");
            dt.Columns.Add("编号", typeof(string));
            dt.Columns.Add("地址A", typeof(string));
            dt.Columns.Add("分辨率A", typeof(string));
            dt.Columns.Add("地址B", typeof(string));
            dt.Columns.Add("分辨率B", typeof(string));
            dt.Columns.Add("相似度", typeof(double));
            this.dataGridView1.RowsDefaultCellStyle.Font = new Font("宋体", 9);
            
            this.pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            this.pictureBox2.SizeMode = PictureBoxSizeMode.Zoom;
            this.dataGridView1.AllowUserToAddRows = false;
        }

        private void Form1_Load(object sender, EventArgs e){

        }

 
        private void AddFoldButton_Click(object sender, EventArgs e) {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "请选择图像所在文件夹";
            if (dialog.ShowDialog() == DialogResult.OK){
                String fold_str = dialog.SelectedPath;
                if(!this.fold_group.Contains(fold_str)){
                    this.fold_group.Add(fold_str);
                    this.listBox2.Items.Add(fold_str);
                }
            }

            // 保存选中的目录到txt
            StreamWriter sw = new StreamWriter(this.FOLD_BAKCUP, false);
            foreach (String fold_str in this.fold_group) {
                sw.WriteLine(fold_str);
            }
            sw.Close();
        }

        private void RemoveFold_Click(object sender, EventArgs e){
            if(this.listBox2.SelectedItem == null){
                MessageBox.Show("请选择要删除的文件夹");
            }else{
                String select_fold = this.listBox2.SelectedItem.ToString();
                this.fold_group.Remove(select_fold);
                this.listBox2.Items.Remove(select_fold);
                // 保存选中的目录到txt
                StreamWriter sw = new StreamWriter(this.FOLD_BAKCUP, false);
                foreach (String fold_str in this.fold_group)
                {
                    sw.WriteLine(fold_str);
                }
                sw.Close();    
            }   
        }

        // 点击分析按按钮
        private void button6_Click(object sender, EventArgs e) {
            // 刷新左侧和右侧
            this.left_url = "";
            this.left_title_label.Text = "";
            this.left_infos_label.Text = "";
            this.pictureBox1.Image = null;

            this.right_url = "";
            this.right_title_label.Text = "";
            this.right_infos_label.Text = "";
            this.pictureBox2.Image = null;



            this.textBox3.Text = "";
            this.progressBar2.Value = 0;
            // 读取相似度设置
            this.similar_val = double.Parse(this.numericUpDown1.Value.ToString()) / 100;
            if (this.fold_group.Count == 0){
                MessageBox.Show("请选择要添加的文件夹");
            } else {
                this.all_images_group = new List<String>();
                this.iho_group = new List<ImageHashObj>();
                this.process_group = 0;

                ImageUtils.W = W;
                ImageUtils.H = H;
                this.dt.Clear(); // 清空数据表

                foreach (String dir_path in fold_group){ // 遍历所有的图片
                    FileUtils.getAllImages(this.all_images_group, dir_path);
                }
                this.textBox1.Text = "0/" + this.all_images_group.Count; //显示有多少张图片
                this.progreassbar_num = 0; // 重置进度条
                this.textBox2.Text = "正在读取图片和计算图片的特征值";
                //// 如果图片数量过多则多线程处理
                int sub_num = all_images_group.Count / thread_num;
                if (sub_num == 0 || this.thread_num == 1) // 如果图片小于线程数则单线程处理
                {
                    // 读取所有图片并计算dhash特征值
                    for (int i = 0; i < all_images_group.Count; i++) {
                        String image_url = all_images_group[i];
                        String[] image_info_str = ImageUtils.GetHash(image_url);

                        if (image_info_str != null) {
                            ImageHashObj iho = new ImageHashObj();
                            iho.name = image_info_str[0];
                            iho.url = image_info_str[1];
                            iho.h = image_info_str[2];
                            iho.w = image_info_str[3];
                            iho.size = image_info_str[4];
                            iho.modify_time = image_info_str[5];
                            iho.dHash = image_info_str[6];
                            this.iho_group.Add(iho);
                        }
                        this.progreassbar_num++;
                        if(i%10==0)
                            SetTextMesssage("read_bar");
                    }
                    SetTextMesssage("read_bar");
                    getSimilarImages("所有图片的hash已经计算完成");

                    iho_group.Sort((x, y) => x.url.CompareTo(y.url)); // 计算完所有的hash之后对iho_group按照文件名排序

                    int count = 0;
                    this.ig_group = new List<ImageGroup>();
                    // 比较所有的图片
                    for (int left_idx = 0; left_idx < this.iho_group.Count -1 ; left_idx++) {
                        for (int right_idx = left_idx + 1; right_idx < this.iho_group.Count - 1; right_idx++) {
                            ImageHashObj left_iho = this.iho_group[left_idx];
                            ImageHashObj right_iho = this.iho_group[right_idx];

                            int dhash_degree = ImageUtils.CalcSimilarDegree(left_iho.dHash, right_iho.dHash); // dhash的Hamming距离
                            double dhash_similar = ((double)(W * H - dhash_degree)) / (W * H); // 计算dhash相似度
                            double images_similar = dhash_similar;

                            images_similar = Math.Round(images_similar, 3); // 保留三位小数
                            ImageGroup ig = new ImageGroup();
                            // 根据UTC时间戳生成唯一的主键
                            ig.id =  ++count  + "";
                            ig.left_url = left_iho.url;
                            ig.left_modify_time = left_iho.modify_time;
                            ig.left_size = left_iho.size;
                            ig.left_resolution = left_iho.w + "×" + left_iho.h;
                            ig.right_url = right_iho.url;
                            ig.right_modify_time = right_iho.modify_time;
                            ig.right_size = right_iho.size;
                            ig.right_resolution = right_iho.w + "×" + right_iho.h;

                            ig.simliar_val = images_similar;
                            this.ig_group.Add(ig);
                            this.progreassbar_num ++;
                            if (right_idx % 50 == 0)
                                SetTextMesssage("compare_bar");
                        }
                    }

                    
                    SetTextMesssage("compare_bar");

                    getSimilarImages("所有图片的比较已完成");

                    updateDataGridViewer();
                    MessageBox.Show("所有图片的比较已完成!");
                }  else   {
                    // 使用子线程来进行读取图片操作
                    for (int i = 0; i < this.thread_num; i++) {
                        int start_index = i * all_images_group.Count / thread_num;
                        sub_num = (i + 1 == thread_num ? all_images_group.Count - i * all_images_group.Count / thread_num : all_images_group.Count / thread_num);

                        List<String> sub_images_group = this.all_images_group.GetRange(start_index, sub_num);
                        Thread image_hashval_thread = new Thread(new ParameterizedThreadStart(getAllImagesHashVal));
                        image_hashval_thread.Start(sub_images_group);
                    }
                    // 完成读取后的操作
                    Thread finish_readimg_thread = new Thread(FinishReadImg);
                    finish_readimg_thread.Start();
                    // 使用子线程来进行图片比较操作
                    for (int i = 0; i < this.thread_num; i++)
                    {
                        Thread compare_thread = new Thread(new ParameterizedThreadStart(ComputeSimilar));
                        compare_thread.Start(i);
                    }
                    // 完成比较后的操作
                    Thread finish_compare_thread = new Thread(FinishCompareData);
                    finish_compare_thread.IsBackground = true;
                    finish_compare_thread.Start();



                }    
            }
        }

        // 获取所有图片的hashval
        private void getAllImagesHashVal(object obj){  
            List<String> sub_images_group = (List<String>) obj;

            // 打开所有的图片
            for (int i = 0; i < sub_images_group.Count; i++) {
                String image_url = sub_images_group[i];
                String[] image_info_str = ImageUtils.GetHash(image_url);
                if (image_info_str != null) {
                    ImageHashObj iho = new ImageHashObj();

                    iho.name = image_info_str[0];
                    iho.url = image_info_str[1];
                    iho.h = image_info_str[2];
                    iho.w = image_info_str[3];
                    iho.size = image_info_str[4];
                    iho.modify_time = image_info_str[5];
                    iho.dHash = image_info_str[6];
                 
                    lock (this.iho_group) {
                        this.iho_group.Add(iho);
                    }
                }

                lock ((object)this.progreassbar_num) {
                    this.progreassbar_num++;
                }

                // 如果UTC时间是整20毫秒
                TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
                long time_int = (long)(ts.TotalMilliseconds*1000);
                if (time_int % 30 == 0) {
                    SetTextMesssage("read_bar");
                }

            }

            // 公共变量锁住
            lock ((object)this.process_group){
                this.process_group += 1;
            }
        }

        // 定义一个代理，用于更新ProgressBar的值（Value）及在执行方法的时候，返回方法的处理信息。
        private delegate void SetPos(String com);//代理
        private void SetTextMesssage(String com) // 进度条值更新函数（参数必须跟声明的代理参数一样）
        {
            if (this.InvokeRequired){
                SetPos setpos = new SetPos(SetTextMesssage); // 需要代理的函数名
                this.Invoke(setpos,new object[] { com});
            }else{
                // lock必须锁定一个object
                if(com == "read_bar") {
                    lock ((object)this.progreassbar_num)
                    {
                        this.textBox1.Text = this.progreassbar_num + "/" + this.all_images_group.Count;
                        this.progressBar1.Value = Convert.ToInt32(100 * this.progreassbar_num / this.all_images_group.Count);
                    }

                }
                else if (com == "compare_bar")
                {     
                    lock ((object)this.progreassbar_num)
                    {
                        int total_num = this.iho_group.Count*(this.iho_group.Count - 1)/2;
                        this.textBox3.Text = this.progreassbar_num + "/" + total_num;
                        this.progressBar2.Value = Convert.ToInt32(100 * this.progreassbar_num / total_num);
                    }     
                }
               
            }
        }

        private void FinishReadImg(){
            while (true){
                if (this.process_group == this.thread_num)
                    break;
            }
            SetTextMesssage("read_bar");
            this.process_group = 0;
            this.progreassbar_num = 0;

            getSimilarImages("图片读取和特征计算已完成");
            
            iho_group.Sort((x, y) => x.url.CompareTo(y.url)); // 计算完所有的hash之后对iho_group按照文件名排序

            // 统计两两比较的组数
            this.compare_idx = new int[iho_group.Count * (iho_group.Count - 1) / 2,2];
            for (int i = 0, index =0; i < iho_group.Count - 1; i++){
                for (int j = i + 1; j < iho_group.Count; j++){
                    compare_idx[index, 0] = i;
                    compare_idx[index, 1] = j;
                    index++;
                }
            }
            //MessageBox.Show(compare_idx.Length + "");
            // 使用子线程来比较所有图片
            if (this.ig_group != null)
                this.ig_group.Clear();
            else
                this.ig_group = new List<ImageGroup>();



            this.compare_load_num = (int)Math.Pow(10.0, (int)(Math.Log10(this.compare_idx.Length/2)) - 2);

            this.finish_readimg = true;
        }

        // 所有图片比较完成后的显示
        private void FinishCompareData(){
            while (true){
                if (this.finish_readimg && this.process_group == this.thread_num){
                    break;
                }
            Thread.Sleep(1);
                    
            }
            SetTextMesssage("compare_bar");
            this.process_group = 0;
            this.progreassbar_num = 0;

            getSimilarImages("图片的比较已完成");
            updateDataGridViewer();
            MessageBox.Show("图片比较完成!");

            this.finish_readimg = false;
        }

        // 多线程计算相似度
        private void ComputeSimilar(object obj) {
            while (true){
                if (this.finish_readimg)
                    break;
                Thread.Sleep(1);
            }
            int N = this.compare_idx.Length/2;
            int group_idx = (int)obj;

            int start_index = group_idx * N / this.thread_num; // 比较开始的坐标
            int end_idex = -1;
            if(group_idx + 1 == this.thread_num){
                end_idex = N - 1;
            } else {
                end_idex = start_index + N / this.thread_num - 1;
            }
              
            for (int i= start_index; i<= end_idex; i++){
                int left_idx = this.compare_idx[i,0];
                int right_idx = this.compare_idx[i,1];

                ImageHashObj left_iho = this.iho_group[left_idx];
                ImageHashObj right_iho = this.iho_group[right_idx];

                int dhash_degree = ImageUtils.CalcSimilarDegree(left_iho.dHash, right_iho.dHash); // dhash的Hamming距离
                double dhash_similar = ((double)(W * H - dhash_degree)) / (W * H); // 计算dhash相似度
                double images_similar = dhash_similar;

                //double size1 = double.Parse(left_iho.h) / double.Parse(left_iho.w);
                //double size2 = double.Parse(right_iho.h) / double.Parse(right_iho.w);
                //double size_similar = 1 - (Math.Max(size1, size2) - Math.Min(size1, size2)) / Math.Max(size1, size2);
                //if (size_similar < 0.90){
                //    if (left_iho.hisogram == null)
                //        left_iho.hisogram = ImageUtils.GetHisogram(left_iho.url);

                //    if (right_iho.hisogram == null)
                //        right_iho.hisogram = ImageUtils.GetHisogram(right_iho.url);

                //    double hisogram_similar = ImageUtils.CalHistogramSimilar(left_iho.hisogram, right_iho.hisogram); // 计算直方图相似度
                //    images_similar = dhash_similar * weight[0] + hisogram_similar * weight[1];
                //}
             


                ImageGroup ig = new ImageGroup();
                ig.id = i + "";

                ig.left_url = left_iho.url;
                ig.left_modify_time = left_iho.modify_time;
                ig.left_size = left_iho.size;
                ig.left_resolution = left_iho.w + "×" + left_iho.h;

                ig.right_url = right_iho.url;
                ig.right_modify_time = right_iho.modify_time;
                ig.right_size = right_iho.size;
                ig.right_resolution = right_iho.w + "×" + right_iho.h;

                ig.simliar_val = images_similar;
                lock (this.ig_group) {
                    this.ig_group.Add(ig);
                }

                lock ((object)this.progreassbar_num){
                    this.progreassbar_num++;
                }
                // 如果UTC时间是整100毫秒
                if (i % this.compare_load_num == 0){
                    TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
                    long time_int = (long)(ts.TotalMilliseconds * 1000); 
                    SetTextMesssage("compare_bar");
                }
               
            }
            lock ((object)this.process_group) {
                this.process_group++;
            }
        }

        // 代理和更新UI线程的界面
        delegate void ChangeBoxVal(String str);
        private void getSimilarImages(String str) {
            if (this.InvokeRequired) // 获取一个标志此调用是否来自非UI线程
            {
                this.Invoke(new ChangeBoxVal(getSimilarImages), str);
            }else{
                this.textBox2.Text = str;
            }

        }

        // 代理和更新UI线程的DataGridViewer
        delegate void delegateDataGridViewer();
        private void updateDataGridViewer() {
            if (this.InvokeRequired) // 获取一个标志此调用是否来自非UI线程
            {
                this.Invoke(new delegateDataGridViewer(updateDataGridViewer));
            }else{
                GetDataTable("new");
            }
        }

        // 显示相似图片左右两侧
        private void dataGridView1_SelectionChanged(object sender, EventArgs e) {
            if (this.dataGridView1.SelectedRows != null && this.dataGridView1.SelectedRows.Count > 0) {
                int current_row = this.dataGridView1.SelectedRows[0].Index; //int current_row = this.dataGridView1.CurrentRow.Index;
                DataGridViewCell cell = this.dataGridView1.Rows[current_row].Cells[0];
                if (cell.Value.ToString() != "") {
                    this.row_id = cell.Value.ToString(); // 当前行的编号
              

                    this.row = this.dataGridView1.CurrentCell.RowIndex;  // 当前行行数

                    List<ImageGroup> select_ig = this.ig_group.Where(x => x.id == this.row_id).ToList();  //查询是哪一个ImageGroup     
                    ImageGroup ig = select_ig[0];

                    this.left_url = ig.left_url;
                    this.right_url = ig.right_url;

                    this.pictureBox1.Image = ImageUtils.GetImage(this.left_url);
                    this.left_title_label.Text = this.left_url.Substring(this.left_url.LastIndexOf("\\") + 1);
                    this.left_infos_label.Text = ig.left_modify_time + "   " + ig.left_size + "   " + ig.left_resolution;

                    this.pictureBox2.Image = ImageUtils.GetImage(this.right_url);
                    this.right_title_label.Text = this.right_url.Substring(this.right_url.LastIndexOf("\\") + 1);
                    this.right_infos_label.Text = ig.right_modify_time + "   " + ig.right_size + "   " + ig.right_resolution;
                }
            }

        }

       
        // 左侧图片删除按钮
        private void left_delbtn_Click(object sender, EventArgs e){
            DeleteImage(this.left_url, "左侧");
        }

        // 右侧图片删除按钮
        private void right_delbtn_Click(object sender, EventArgs e){
            DeleteImage(this.right_url, "右侧");
        }

        // 删除一个图片文件的操作
        private void DeleteImage(String url, String location) {
            if (url == "" || url == null){
                MessageBox.Show("还没有分析好的图片");
            } else
            {
                MessageBoxButtons messbutton = MessageBoxButtons.OKCancel;
                DialogResult action = MessageBox.Show("删除"+ location + "的\n" + url + " 到回收站中？", "删除", messbutton);
                if (action == DialogResult.OK) //确认则删除文件到回收站  
                {
                    try  {
                        FileSystem.DeleteFile(url, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    }    catch {
                    }
                    // 如果文件删除成功！
                    if (!File.Exists(url))
                    {
                        MessageBox.Show(url + "已经移动到回收站");
                        // 删除对应的图片组，删除条件为url
                        for (int i = 0; i < this.ig_group.Count;) {
                            if (this.ig_group[i].left_url == url || this.ig_group[i].right_url == url)
                                this.ig_group.Remove(this.ig_group[i]);
                            else
                                i++;        
                        }
                        // 删除对应的行
                        DataRow[] deleteRow = null;
                        int delete_num = 0;
                      
                        deleteRow = this.dt.Select("地址A='" + url + "' or 地址B='" +  url + "'");
                      
                        // Datatable 中删除相应的数据
                        if (deleteRow != null) {
                            delete_num = deleteRow.Length;
                            // 如果表格中还有数据, 则重新定位
                            if (this.dt.Rows.Count > 0) {
                                if (this.row + delete_num < this.dt.Rows.Count){
                                    this.dataGridView1.Rows[this.row + delete_num].Selected = true;
                                } else {
                                    this.dataGridView1.Rows[this.row - 1].Selected = true;
                                }
                            }
                            foreach (DataRow dr in deleteRow)
                                this.dt.Rows.Remove(dr);
                        }
   
                        // 刷新左侧和右侧
                        this.left_url = "";
                        this.left_title_label.Text = "";
                        this.left_infos_label.Text = "";
                        this.pictureBox1.Image = null;

                        this.right_url = "";
                        this.right_title_label.Text = "";
                        this.right_infos_label.Text = "";
                        this.pictureBox2.Image = null;      
                    }
                }
            }
        }

        // 建立表格中的数据
        private void GetDataTable(String action) {
            // 重新加载数据表
            this.dt.Clear();

            // 选择符合条件的ImageGroup数据
            if(action == "new"){
                if (this.view_ig_group != null)
                    this.view_ig_group.Clear();
                this.view_ig_group = this.ig_group.Where(x => x.simliar_val >= this.similar_val).ToList();
            } else if (action == "filter"){
                // 选择符合条件的ImageGroup数据
                this.view_ig_group = this.view_ig_group.Where(x => x.simliar_val >= this.similar_val).ToList();
            }

            // DataGridView填充所有的数据
            for (int i = 0; i < this.view_ig_group.Count; i++){
                DataRow new_dr = this.dt.NewRow();
                this.dt.Rows.Add(new_dr);

                ImageGroup ig = this.view_ig_group[i];
                this.dt.Rows[i][0] = ig.id;
                this.dt.Rows[i][1] = ig.left_url;
                this.dt.Rows[i][2] = ig.left_resolution;
                this.dt.Rows[i][3] = ig.right_url;
                this.dt.Rows[i][4] = ig.right_resolution;
                this.dt.Rows[i][5] = ig.simliar_val * 100;
            }
            this.dataGridView1.DataSource = this.dt;  
            // 设置dataGrid表格的宽度
            this.dataGridView1.Columns[0].Width = 60;
            this.dataGridView1.Columns[1].Width = 250;
            this.dataGridView1.Columns[2].Width = 85;
            this.dataGridView1.Columns[3].Width = 250;
            this.dataGridView1.Columns[4].Width = 85;
            this.dataGridView1.Columns[5].Width = 80;

        }

        // 在文件夹中打开左边的图片
        private void button8_Click(object sender, EventArgs e) {
            OpenImage(this.left_url);
        }

        // 在文件夹中打开右边的图片
        private void button10_Click(object sender, EventArgs e) {
            OpenImage(this.right_url);
        }

        // 在文件夹中打开一个文件的操作
        private void OpenImage(String url) {
            if (File.Exists(url)){
                System.Diagnostics.Process.Start("Explorer.exe", "/select," + url);
            } else{
                MessageBox.Show("系统不存在" + url);
            }
        }

        // 设置相似度过滤的按钮
        private void button5_Click(object sender, EventArgs e){
            this.similar_val = double.Parse(this.numericUpDown1.Value.ToString())/100;
            if (this.ig_group == null){
                MessageBox.Show("还没有读取数据！");
            } else {
                GetDataTable("filter");
            }
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }
    }
}
