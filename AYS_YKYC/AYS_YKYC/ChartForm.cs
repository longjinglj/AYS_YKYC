using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZedGraph;
using System.IO;

namespace AYS_YKYC
{
    public partial class ChartForm : Form
    {

        TreeNode node1 = new TreeNode("重要遥测");
        TreeNode node2 = new TreeNode("计算机");
        TreeNode node3 = new TreeNode("软件");
        TreeNode node4 = new TreeNode("GNC");
        TreeNode node5 = new TreeNode("热控");
        TreeNode node6 = new TreeNode("IMU_A");
        TreeNode node7 = new TreeNode("IMU_B");
        TreeNode node8 = new TreeNode("测控");
        TreeNode node9 = new TreeNode("数传");
        TreeNode node10 = new TreeNode("载荷3S");
        TreeNode node11 = new TreeNode("GNSS");
        TreeNode node12 = new TreeNode("OBC_B");
        TreeNode node13 = new TreeNode("系统延时遥测");
        TreeNode node14 = new TreeNode("GNC延时遥测");
        TreeNode[] NodeList;

        SQLiteConnection dbConnection = new SQLiteConnection("data source=mydb.db");

        Thread recurve_thread;
        bool recurve_flag = false;

        private delegate void SetDtCallback(DataTable dt);

        public ChartForm()
        {
            InitializeComponent();
        }

        private void ChartForm_Load(object sender, EventArgs e)
        {
            NodeList = new TreeNode[] { node1, node2, node3, node4, node5, node6, node7, node8, node9, node10, node11, node12, node13, node14 };
            try
            {
                for (int i = 0; i < NodeList.Count(); i++)
                {
                    TreeNode nodet = NodeList[i];
                    treeView1.Nodes.Add(NodeList[i]);

                    List<string> TempList = Data.GetConfigNormal(Data.APIDconfigPath, NodeList[i].Text);
                    for (int j = 0; j < TempList.Count(); j++)
                    {
                        NodeList[i].Nodes.Add(new TreeNode(TempList[j]));
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }

            Trace.WriteLine("TreeView完成初始化！");

            DateTime timenow = System.DateTime.Now;
            String temp = timenow.ToString("yyyy-MM-dd HH:mm:ss");
            dateTimePicker2.Text = temp;
            DateTime timebefore = timenow.AddDays(-1);
            String tempbefore = timebefore.ToString("yyyy-MM-dd HH:mm:ss");
            dateTimePicker1.Text = tempbefore;

         
            z1.GraphPane.Title.Text = "图表";
            z1.GraphPane.XAxis.Title.Text = "时间";
            z1.GraphPane.YAxis.Title.Text = "值";
            z1.GraphPane.XAxis.CrossAuto = true;
            //z1.GraphPane.XAxis.MaxAuto = true;
            z1.GraphPane.XAxis.Type = ZedGraph.AxisType.Date;
          

            button1.Text = "关闭实时更新";
            recurve_thread = new Thread(refreshcurve);
            recurve_flag = true;
            recurve_thread.Start();
        }

        int ColorPos = 0;
        private Color ChooseColor()
        {
            Color[] ChoseColorList = new Color[] { Color.Yellow, Color.Black, Color.Red, Color.GreenYellow, Color.Green };
            ColorPos++;
            if (ColorPos >= ChoseColorList.Length) ColorPos = 0;
            return ChoseColorList[ColorPos];
        }

        private void treeView1_DoubleClick(object sender, EventArgs e)
        {
            for (int i = 0; i < NodeList.Count(); i++)
            {
                if (treeView1.SelectedNode.Parent == NodeList[i])
                {
                    Trace.WriteLine(treeView1.SelectedNode.Parent.Text + ":" + treeView1.SelectedNode.Text);

                    String TableparentName = treeView1.SelectedNode.Parent.Text + "_解析值";//数据库名字
                    String TableName = "table_" + treeView1.SelectedNode.Parent.Text + "_解析值";//要查询的数据库的名称

                    String SelectColum = treeView1.SelectedNode.Text;//对应数据库中的列（就是选中的项的名称）
                    //根据此处的APID-内容，进行下一步解析和处理

                    //查询数据库时的限定语句（时间限定）
                    string Str_Condition_time = "CreateTime >= '" + dateTimePicker1.Value.ToString("yyyy-MM-dd HH:mm:ss") + "'"
                                     + "and CreateTime <= '" + dateTimePicker2.Value.ToString("yyyy-MM-dd HH:mm:ss") + "'";

                    //最终查询数据库的cmd语句
                    string cmd = "Select CreateTime,[" + SelectColum + "] From " + TableName + " where " + Str_Condition_time;

                    SQLiteDataAdapter mAdapter = new SQLiteDataAdapter(cmd, dbConnection);
                    DataTable mTable = new DataTable(); // Don't forget initialize!新建1个DataTable类型
                    mAdapter.Fill(mTable);//将数据库中取出内容填到DataTable中



                    DataTable showtable = new DataTable();
                    DataView dv = mTable.DefaultView;
                    dv.Sort = "CreateTime desc";
                    showtable = dv.ToTable();
                    dataGridView1.DataSource = showtable;//将DataTable数据与dataGridview绑定
                    
                    double[] x = new double[mTable.Rows.Count];//x轴
                    double[] y = new double[mTable.Rows.Count];

                    try
                    {
                        //循环将DataTable中的时间和数值赋予x和y数组
                        for (int j = 0; j < mTable.Rows.Count; j++)
                        {
                            // Trace.WriteLine(mTable.Rows[j]["CreateTime"] + ":" + mTable.Rows[j][SelectColum]);
                            DateTime time = Convert.ToDateTime(mTable.Rows[j]["CreateTime"]);
                            x[j] = (double)new XDate(time);

                            string value = (string)mTable.Rows[j][SelectColum];
                            y[j] = double.Parse(value);
                        }
                    }
                    catch
                    {
                        TableparentName = treeView1.SelectedNode.Parent.Text + "_源码";//数据库名字
                        TableName = "table_" + treeView1.SelectedNode.Parent.Text + "_源码";//要查询的数据库的名称

                        SelectColum = treeView1.SelectedNode.Text;//对应数据库中的列（就是选中的项的名称）
                                                                  //根据此处的APID-内容，进行下一步解析和处理

                        //查询数据库时的限定语句（时间限定）
                        Str_Condition_time = "CreateTime >= '" + dateTimePicker1.Value.ToString("yyyy-MM-dd HH:mm:ss") + "'"
                                        + "and CreateTime <= '" + dateTimePicker2.Value.ToString("yyyy-MM-dd HH:mm:ss") + "'";

                        //最终查询数据库的cmd语句
                        cmd = "Select CreateTime,[" + SelectColum + "] From " + TableName + " where " + Str_Condition_time;

                        mAdapter = new SQLiteDataAdapter(cmd, dbConnection);
                        DataTable yuanmamTable = new DataTable(); // Don't forget initialize!新建1个DataTable类型
                        mAdapter.Fill(yuanmamTable);//将数据库中取出内容填到DataTable中


                        x = new double[yuanmamTable.Rows.Count];//x轴
                        y = new double[yuanmamTable.Rows.Count];

                        for (int j = 0; j < yuanmamTable.Rows.Count; j++)
                        {
                            // Trace.WriteLine(mTable.Rows[j]["CreateTime"] + ":" + mTable.Rows[j][SelectColum]);
                            DateTime time = Convert.ToDateTime(yuanmamTable.Rows[j]["CreateTime"]);
                            x[j] = (double)new XDate(time);

                            string value = (string)yuanmamTable.Rows[j][SelectColum];
                            y[j] = Convert.ToInt64(value.Substring(2, value.Length - 2), 16);
                        }
                    }

                    string showSelectColum = treeView1.SelectedNode.Parent.Text + ":" + SelectColum;
                    bool samecurveflag = false;


                    for (int m = 0; m < z1.GraphPane.CurveList.Count; m++)
                    {
                        CurveItem mycurve = z1.GraphPane.CurveList[m];
                        if (mycurve.Label.Text == showSelectColum)
                        {
                           
                            PointPairList list = mycurve.Points as PointPairList;

                            #region   删除曲线中时间以前的点
                            if (x.Length == 0)
                            {
                                list.Clear();
                            }
                            else
                            {
                                double fisttime = x[0];
                                for (int j = 0; j < list.Count; j++)
                                {
                                    if (list[j].X < fisttime)
                                    {
                                        list.Clear();
                                        break;
                                    }
                                }
                            }
                            #endregion

                            DateTime ori = new DateTime(1000, 1, 1, 0, 0, 0);
                            double lasttime = (double)new XDate(ori);
                            if (list.Count > 0)
                            {
                                lasttime = list[list.Count - 1].X;
                            }
                            
                            for (int j = 0; j < x.Length; j++)
                            {
                                if (x[j] > lasttime)
                                    list.Add(x[j], y[j]);
                            }
                            samecurveflag = true;

                            break;
                        }
                    }

                    if (!samecurveflag)
                        z1.GraphPane.AddCurve(showSelectColum, x, y, ChooseColor(), ZedGraph.SymbolType.Circle);//显示曲线
                    samecurveflag = false;

                   
                    int t = z1.GraphPane.CurveList.Count;
                    for (int m = 0; m < t; m++)
                    {
                        CurveItem mycurve = z1.GraphPane.CurveList[m];
                        Trace.WriteLine(mycurve.Label);
                    }

                    z1.AxisChange();
                    z1.Invalidate();

                    comboBox1.Items.Add(showSelectColum);

                   
                }
            }
        }


        #region   实时更新
        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == "开启实时更新")
            {
                button1.Text = "关闭实时更新";
                recurve_thread = new Thread(refreshcurve);
                recurve_flag = true;
                recurve_thread.Start();
            }
            else
            {
                recurve_thread.Abort();
                recurve_flag = false;
                button1.Text = "开启实时更新";
            }
        }

        void refreshcurve()
        {
            while (recurve_flag)
            {
                int curvecount = z1.GraphPane.CurveList.Count;

                for (int m = 0; m < curvecount; m++)
                {
                    CurveItem mycurve = z1.GraphPane.CurveList[m];
                    string[] gettablename = mycurve.Label.Text.Split(':');

                    String TableName = "table_" + gettablename[0] + "_解析值";//要查询的数据库的名称

                    String SelectColum = gettablename[1];//对应数据库中的列（就是选中的项的名称）
                    //根据此处的APID-内容，进行下一步解析和处理

                    //查询数据库时的限定语句（时间限定）
                    string Str_Condition_time = "CreateTime >= '" + dateTimePicker1.Value.ToString("yyyy-MM-dd HH:mm:ss") + "'"
                                     + "and CreateTime <= '" + dateTimePicker2.Value.ToString("yyyy-MM-dd HH:mm:ss") + "'";

                    //最终查询数据库的cmd语句
                    string cmd = "Select CreateTime,[" + SelectColum + "] From " + TableName + " where " + Str_Condition_time;

                    SQLiteDataAdapter mAdapter = new SQLiteDataAdapter(cmd, dbConnection);
                    DataTable mTable = new DataTable(); // Don't forget initialize!新建1个DataTable类型
                    mAdapter.Fill(mTable);//将数据库中取出内容填到DataTable中
                                          //dataGridView1.DataSource = mTable;//将DataTable数据与dataGridview绑定
                    if (m == (curvecount - 1))
                    {
                        DataTable showtable = new DataTable();
                        DataView dv = mTable.DefaultView;
                        dv.Sort = "CreateTime desc";
                        showtable = dv.ToTable();
                        SetDT(showtable);
                    }

                    double[] x = new double[mTable.Rows.Count];//x轴
                    double[] y = new double[mTable.Rows.Count];

                    try
                    {
                        //循环将DataTable中的时间和数值赋予x和y数组
                        for (int j = 0; j < mTable.Rows.Count; j++)
                        {
                            // Trace.WriteLine(mTable.Rows[j]["CreateTime"] + ":" + mTable.Rows[j][SelectColum]);
                            DateTime time = Convert.ToDateTime(mTable.Rows[j]["CreateTime"]);
                            x[j] = (double)new XDate(time);

                            string value = (string)mTable.Rows[j][SelectColum];
                            y[j] = double.Parse(value);
                        }

                    }
                    catch
                    {
                        TableName = "table_" + gettablename[0] + "_源码";//要查询的数据库的名称

                        SelectColum = gettablename[1];//对应数据库中的列（就是选中的项的名称）
                                                      //根据此处的APID-内容，进行下一步解析和处理

                        //查询数据库时的限定语句（时间限定）
                        Str_Condition_time = "CreateTime >= '" + dateTimePicker1.Value.ToString("yyyy-MM-dd HH:mm:ss") + "'"
                                        + "and CreateTime <= '" + dateTimePicker2.Value.ToString("yyyy-MM-dd HH:mm:ss") + "'";

                        //最终查询数据库的cmd语句
                        cmd = "Select CreateTime,[" + SelectColum + "] From " + TableName + " where " + Str_Condition_time;

                        mAdapter = new SQLiteDataAdapter(cmd, dbConnection);
                        DataTable yuanmamTable = new DataTable(); // Don't forget initialize!新建1个DataTable类型
                        mAdapter.Fill(yuanmamTable);//将数据库中取出内容填到DataTable中

                        x = new double[yuanmamTable.Rows.Count];//x轴
                        y = new double[yuanmamTable.Rows.Count];

                        for (int j = 0; j < yuanmamTable.Rows.Count; j++)
                        {
                            // Trace.WriteLine(mTable.Rows[j]["CreateTime"] + ":" + mTable.Rows[j][SelectColum]);
                            DateTime time = Convert.ToDateTime(yuanmamTable.Rows[j]["CreateTime"]);
                            x[j] = (double)new XDate(time);

                            string value = (string)yuanmamTable.Rows[j][SelectColum];
                            y[j] = Convert.ToInt64(value.Substring(2, value.Length - 2), 16);
                        }

                    }


                    string showSelectColum = mycurve.Label.Text;

                    //Color originalcolor = mycurve.Color;
                    //z1.GraphPane.CurveList.RemoveAt(0);
                    //z1.GraphPane.AddCurve(showSelectColum, x, y, originalcolor, ZedGraph.SymbolType.Circle);//显示曲线



                    PointPairList list = mycurve.Points as PointPairList;
                    DateTime ori = new DateTime(1000, 1, 1, 0, 0, 0);
                    double lasttime= (double)new XDate(ori);

                    #region   删除曲线中时间以前的点
                    if(x.Length==0)
                    {
                        list.Clear();
                    }
                    else
                    {
                        double fisttime = x[0];
                        for (int j = 0; j < list.Count; j++)
                        {
                            if (list[j].X < fisttime)
                            {
                                list.Clear();
                                break;
                            }
                        }
                    }
                    #endregion

                    if (list.Count>0)
                    {
                         lasttime = list[list.Count - 1].X;
                    }
                  
                   
                    for (int j = 0; j < x.Length; j++)
                    {
                        if (x[j] > lasttime)
                            list.Add(x[j], y[j]);
                    }
                 

                    z1.AxisChange();
                    z1.Invalidate();

                  
                }



                Thread.Sleep(2000);
            }
        }


        #endregion

        #region 委托刷新dataGridView1

        private void SetDT(DataTable dt)
        {
            if (this.InvokeRequired)
            {
                SetDtCallback d = new SetDtCallback(SetDT);
                this.Invoke(d, new object[] { dt });
            }
            else
            {
                this.dataGridView1.DataSource = dt;
            }
        }


        #endregion


        private void button2_Click(object sender, EventArgs e)
        {
            for (int m = 0; m < z1.GraphPane.CurveList.Count; m++)
            {
                CurveItem mycurve = z1.GraphPane.CurveList[m];
                if (mycurve.Label.Text == comboBox1.Text)
                {
                    z1.GraphPane.CurveList.RemoveAt(m);
                    comboBox1.Items.Remove(comboBox1.Text);
                    z1.AxisChange();
                    z1.Invalidate();
                    break;
                }
            }
        }

        private void ShowXAxis()
        {
            while (true)
            {



                Thread.Sleep(1000);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            z1.GraphPane.CurveList.Clear();
            z1.AxisChange();
            z1.Invalidate();
            comboBox1.Items.Clear();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            DateTime timenow = System.DateTime.Now;
            String temp = timenow.ToString("yyyy-MM-dd HH:mm:ss");
            dateTimePicker2.Text = temp;
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {

        }

        private void dataGridView1_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {

        }

        private void ChartForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (recurve_thread != null)
            {
                recurve_thread.Abort();
                recurve_flag = false;
            }
        }

        private void btn_LogCtr_Click(object sender, EventArgs e)
        {
            if (btn_LogCtr.Text == "侧边栏隐藏>>>")
            {
                btn_LogCtr.Text = "<<<侧边栏显示";
                this.splitContainer1.Panel1Collapsed = true;
                this.splitContainer2.Panel2Collapsed = true;
            }
            else
            {
                btn_LogCtr.Text = "侧边栏隐藏>>>";
                this.splitContainer1.Panel1Collapsed = false;
                this.splitContainer2.Panel2Collapsed = false;

            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            string str = Application.StartupPath;

            str = str + @"\配置文件\";
            //str = str + "1.txt";
            str = str + "chartform" + ".txt";
            System.IO.StreamWriter streamwriter = new StreamWriter(str);

            int curvecount = z1.GraphPane.CurveList.Count;

            for (int m = 0; m < curvecount; m++)
            {
                CurveItem mycurve = z1.GraphPane.CurveList[m];
                str = mycurve.Label.Text;
                streamwriter.WriteLine(str);
            }
            streamwriter.Flush();
            streamwriter.Close();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            openFileDialog1.InitialDirectory = Program.GetStartupPath() + @"配置文件\chartform配置文件\";
            string openpath="";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                //   openFileDialog1.InitialDirectory = Program.GetStartupPath() + @"LogData\";
                try
                {
                     openpath = openFileDialog1.FileName;
                }
                catch
                {
                    MyLog.Info("配置文件打开失败");
                    return;   
                }
            }



            string str = Application.StartupPath;
           

            TextReader reader = new StreamReader(openpath);
            string line;
           

            line = reader.ReadLine();
           
            while(line!=""&&line!=null)
            {
                string[] gettablename = line.Split(':');

                String TableName = "table_" + gettablename[0] + "_解析值";//要查询的数据库的名称

                String SelectColum = gettablename[1];//对应数据库中的列（就是选中的项的名称）
                String TableparentName = gettablename[0] + "_解析值";//数据库名字
         

                //查询数据库时的限定语句（时间限定）
                string Str_Condition_time = "CreateTime >= '" + dateTimePicker1.Value.ToString("yyyy-MM-dd HH:mm:ss") + "'"
                                 + "and CreateTime <= '" + dateTimePicker2.Value.ToString("yyyy-MM-dd HH:mm:ss") + "'";

                //最终查询数据库的cmd语句
                string cmd = "Select CreateTime,[" + SelectColum + "] From " + TableName + " where " + Str_Condition_time;

                SQLiteDataAdapter mAdapter = new SQLiteDataAdapter(cmd, dbConnection);
                DataTable mTable = new DataTable(); // Don't forget initialize!新建1个DataTable类型
                mAdapter.Fill(mTable);//将数据库中取出内容填到DataTable中



                DataTable showtable = new DataTable();
                DataView dv = mTable.DefaultView;
                dv.Sort = "CreateTime desc";
                showtable = dv.ToTable();
                dataGridView1.DataSource = showtable;//将DataTable数据与dataGridview绑定

                double[] x = new double[mTable.Rows.Count];//x轴
                double[] y = new double[mTable.Rows.Count];

                try
                {
                    //循环将DataTable中的时间和数值赋予x和y数组
                    for (int j = 0; j < mTable.Rows.Count; j++)
                    {
                        // Trace.WriteLine(mTable.Rows[j]["CreateTime"] + ":" + mTable.Rows[j][SelectColum]);
                        DateTime time = Convert.ToDateTime(mTable.Rows[j]["CreateTime"]);
                        x[j] = (double)new XDate(time);

                        string value = (string)mTable.Rows[j][SelectColum];
                        y[j] = double.Parse(value);
                    }
                }
                catch
                {
                    TableparentName = gettablename[0] + "_源码";//数据库名字
                    TableName = "table_" + gettablename[0] + "_源码";//要查询的数据库的名称

                    SelectColum = gettablename[1];//对应数据库中的列（就是选中的项的名称）
                                                              //根据此处的APID-内容，进行下一步解析和处理

                    //查询数据库时的限定语句（时间限定）
                    Str_Condition_time = "CreateTime >= '" + dateTimePicker1.Value.ToString("yyyy-MM-dd HH:mm:ss") + "'"
                                    + "and CreateTime <= '" + dateTimePicker2.Value.ToString("yyyy-MM-dd HH:mm:ss") + "'";

                    //最终查询数据库的cmd语句
                    cmd = "Select CreateTime,[" + SelectColum + "] From " + TableName + " where " + Str_Condition_time;

                    mAdapter = new SQLiteDataAdapter(cmd, dbConnection);
                    DataTable yuanmamTable = new DataTable(); // Don't forget initialize!新建1个DataTable类型
                    mAdapter.Fill(yuanmamTable);//将数据库中取出内容填到DataTable中


                    x = new double[yuanmamTable.Rows.Count];//x轴
                    y = new double[yuanmamTable.Rows.Count];

                    for (int j = 0; j < yuanmamTable.Rows.Count; j++)
                    {
                        // Trace.WriteLine(mTable.Rows[j]["CreateTime"] + ":" + mTable.Rows[j][SelectColum]);
                        DateTime time = Convert.ToDateTime(yuanmamTable.Rows[j]["CreateTime"]);
                        x[j] = (double)new XDate(time);

                        string value = (string)yuanmamTable.Rows[j][SelectColum];
                        y[j] = Convert.ToInt64(value.Substring(2, value.Length - 2), 16);
                    }
                }

                string showSelectColum = gettablename[0] + ":" + SelectColum;
                bool samecurveflag = false;


                for (int m = 0; m < z1.GraphPane.CurveList.Count; m++)
                {
                    CurveItem mycurve = z1.GraphPane.CurveList[m];
                    if (mycurve.Label.Text == showSelectColum)
                    {

                        PointPairList list = mycurve.Points as PointPairList;
                        #region   删除曲线中时间以前的点
                        if (x.Length == 0)
                        {
                            list.Clear();
                        }
                        else
                        {
                            double fisttime = x[0];
                            for (int j = 0; j < list.Count; j++)
                            {
                                if (list[j].X < fisttime)
                                {
                                    list.Clear();
                                    break;
                                }
                            }
                        }
                        #endregion


                        double lasttime = list[list.Count - 1].X;
                        for (int j = 0; j < x.Length; j++)
                        {
                            if (x[j] > lasttime)
                                list.Add(x[j], y[j]);
                        }
                        samecurveflag = true;

                        break;
                    }
                }

                if (!samecurveflag)
                    z1.GraphPane.AddCurve(showSelectColum, x, y, ChooseColor(), ZedGraph.SymbolType.Circle);//显示曲线
                samecurveflag = false;

                int t = z1.GraphPane.CurveList.Count;
                for (int m = 0; m < t; m++)
                {
                    CurveItem mycurve = z1.GraphPane.CurveList[m];
                    Trace.WriteLine(mycurve.Label);
                }

                z1.AxisChange();
                z1.Invalidate();

                comboBox1.Items.Add(showSelectColum);
                line = reader.ReadLine();

            }




        }
    }
}
