using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AYS_YKYC
{
    public partial class QueryMyDB : Form
    {
        public MainForm mform;
        public QueryMyDB(AYS_YKYC.MainForm parent)
        {
            InitializeComponent();
            mform = parent;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string TableName = "table_Telemetry";
            switch (comboBox1.Text)
            {
                case "接收遥测VCDU":
                    TableName = "table_Telemetry";
                    break;
                case "解析EPDU":
                    TableName = "table_Epdu";
                    break;
                case "发送遥控":
                    TableName = "table_Telecmd";
                    break;
                default:
                    TableName = comboBox1.Text;
                    break;
            }

            //select * from table_All where CreateTime >= '2018-03-23 17:07:19' and CreateTime < '2019-08-02 00:00:00'
            string Str_Condition_time = "CreateTime >= '" + dateTimePicker1.Value.ToString("yyyy-MM-dd HH:mm:ss") + "'"
                                        + "and CreateTime <= '" + dateTimePicker2.Value.ToString("yyyy-MM-dd HH:mm:ss") + "'";

            string Str_Conditon1 = "";
            if (textBox2.Text != null && textBox2.Text.Length >= 4)
            {
                string TypedStr = textBox2.Text;
                string[] list = TypedStr.Split('=');
                Str_Conditon1 = "and " + list[0] + " = " + "'" + list[1] + "'";
            }

            string Str_Conditon2 = "";
            if (textBox3.Text != null && textBox3.Text.Length >= 4)
            {
                string TypedStr = textBox3.Text;
                string[] list = TypedStr.Split('=');
                Str_Conditon2 = "and " + list[0] + " = " + "'" + list[1] + "'";
            }

            try
            {
                SQLiteConnection dbConnection = new SQLiteConnection("data source=mydb.db");
                string cmd = "Select * From " + TableName + " where "
                    + Str_Condition_time + Str_Conditon1 + Str_Conditon2;
                Trace.WriteLine(cmd);
                SQLiteDataAdapter mAdapter = new SQLiteDataAdapter(cmd, dbConnection);
                DataTable mTable = new DataTable(); // Don't forget initialize!
                mAdapter.Fill(mTable);

                // 绑定数据到DataGridView
                dataGridView1.DataSource = mTable;

                dataGridView1.Columns[0].Width = 50;
                dataGridView1.Columns[dataGridView1.ColumnCount - 1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                dataGridView1.Columns[1].Width = 150;
            }
            catch(Exception ex)
            {
                //MessageBox.Show("输入的条件不符合格式，请重新输入！");
                MyLog.Error("输入的条件不符合格式，请重新输入！");
                MyLog.Error(ex.Message);
            }
          
        }

        private void QueryMyDB_Load(object sender, EventArgs e)
        {
            dateTimePicker2.Value = DateTime.Now;
            dateTimePicker1.Value = DateTime.Now.AddDays(-1);

            comboBox1.SelectedIndex = comboBox1.Items.IndexOf("解析EPDU");

            SQLiteConnection dbConnection = new SQLiteConnection("data source=mydb.db");
            string cmd = "Select name From sqlite_master where type = " + "'table'";
            SQLiteDataAdapter mAdapter = new SQLiteDataAdapter(cmd, dbConnection);
            DataTable mTable = new DataTable(); // Don't forget initialize!
            mAdapter.Fill(mTable);

            int count = mTable.Rows.Count;
            bool AlreadyInComb = false;
            for (int i = 0; i < count; i++)
            {
                for(int j=0;j<comboBox1.Items.Count;j++)
                {
                 
                    if ((string)mTable.Rows[i][0]==comboBox1.GetItemText(comboBox1.Items[j]))
                    {
                        AlreadyInComb = true;
                        break;
                    }
                    else
                    {
                        AlreadyInComb = false; 
                    }
                }

                if(!AlreadyInComb) comboBox1.Items.Add(mTable.Rows[i][0]); 
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            string fileName = "";
            string saveFileName = "";
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.DefaultExt = "xlsx";
            saveDialog.Filter = "Excel文件|*.xlsx";
            saveDialog.FileName = fileName;

            String Path = Program.GetStartupPath() + @"数据库导出\";
            if (!Directory.Exists(Path))
                Directory.CreateDirectory(Path);
            saveDialog.InitialDirectory = Path;
            saveDialog.ShowDialog();
            saveFileName = saveDialog.FileName;
            if (saveFileName.IndexOf(":") < 0) return; //被点了取消
            Microsoft.Office.Interop.Excel.Application xlApp = new Microsoft.Office.Interop.Excel.Application();
            if (xlApp == null)
            {
                //MessageBox.Show("无法创建Excel对象，您的电脑可能未安装Excel");
                MyLog.Error("无法创建Excel对象，您的电脑可能未安装Excel");
                return;
            }
            Microsoft.Office.Interop.Excel.Workbooks workbooks = xlApp.Workbooks;
            Microsoft.Office.Interop.Excel.Workbook workbook =
                        workbooks.Add(Microsoft.Office.Interop.Excel.XlWBATemplate.xlWBATWorksheet);
            Microsoft.Office.Interop.Excel.Worksheet worksheet =
                        (Microsoft.Office.Interop.Excel.Worksheet)workbook.Worksheets[1];//取得sheet1 
                                                                                         //写入标题             
            for (int i = 0; i < dataGridView1.ColumnCount; i++)
            { worksheet.Cells[1, i + 1] = dataGridView1.Columns[i].HeaderText; }
            //写入数值
            for (int r = 0; r < dataGridView1.Rows.Count; r++)
            {
                for (int i = 0; i < dataGridView1.ColumnCount; i++)
                {
                    worksheet.Cells[r + 2, i + 1] = dataGridView1.Rows[r].Cells[i].Value;
                }
                System.Windows.Forms.Application.DoEvents();
            }
            worksheet.Columns.EntireColumn.AutoFit();//列宽自适应
            //MessageBox.Show(fileName + "资料保存成功", "提示", MessageBoxButtons.OK);
            MyLog.Info(fileName + "资料保存成功");
            if (saveFileName != "")
            {
                try
                {
                    workbook.Saved = true;
                    workbook.SaveCopyAs(saveFileName);  //fileSaved = true;                 
                }
                catch (Exception ex)
                {//fileSaved = false;                      
                    //MessageBox.Show("导出文件时出错,文件可能正被打开！\n" + ex.Message);
                    MyLog.Error("导出文件时出错,文件可能正被打开！\n" + ex.Message);
                }
            }
            xlApp.Quit();
            GC.Collect();//强行销毁 
        }

        private void QueryMyDB_FormClosed(object sender, FormClosedEventArgs e)
        {
            mform.mySqlForm = null;
        }
    }
}
