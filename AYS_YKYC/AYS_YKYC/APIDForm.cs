using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace AYS_YKYC
{
    public partial class APIDForm : WeifenLuo.WinFormsUI.Docking.DockContent
    {
        MainForm myform;
        public string CurrentApidName;
        public int TotalLen = 0;//数据域总长度，多少个byte

        DataTable dt = new DataTable();
        DataTable dtAPid = new DataTable();


        public Queue<byte[]> DataQueue = new Queue<byte[]>();   //处理APID对应的Queue
        public APIDForm(string apidstr, AYS_YKYC.MainForm parent)
        {
            InitializeComponent();
            this.Text = apidstr;
            CurrentApidName = apidstr;
            myform = parent;
        }

        private void APIDForm_Load(object sender, EventArgs e)
        {
            dataGridView1.AllowUserToResizeRows = false;
            dtAPid.Columns.Add("序号", typeof(string));
            dtAPid.Columns.Add("名称", typeof(string));
            dtAPid.Columns.Add("占位", typeof(string));
            dtAPid.Columns.Add("值", typeof(string));
            dtAPid.Columns.Add("解析值", typeof(string));

            List<string> List = Data.GetConfigNormal(Data.APIDconfigPath, CurrentApidName);

            if (List.Count >= 1)
            {
                TotalLen = 0;

                string[] StringList1 = new string[List.Count+1];
                string[] StringList2 = new string[List.Count+1];
                StringList1[0] = "CreateTime";
                StringList2[0] = "TEXT";
                for (int i = 0; i < List.Count; i++)
                {
                    DataRow dr = dtAPid.NewRow();
                    dr["序号"] = i + 1;
                    dr["名称"] = List[i];
                    dr["占位"] = Data.GetConfigStr(Data.APIDconfigPath, CurrentApidName, List[i], "len");
                    dtAPid.Rows.Add(dr);
                    TotalLen += int.Parse((string)dr["占位"]);//这里算出来总共多少位

                    StringList1[i+1] = "'" + List[i] + "'";


                    StringList2[i+1] = "TEXT";
                }

                Trace.WriteLine("占位TotlaLen=" + TotalLen.ToString());

                TotalLen = TotalLen / 8;//算出来多少Byte

                dataGridView1.DataSource = dtAPid;

                Data.sql.CreateTable("table_"+CurrentApidName+"_源码", StringList1, StringList2);
                Data.sql.CreateTable("table_" + CurrentApidName + "_解析值", StringList1, StringList2);
                new Thread(() => { DealWithEPDU(); }).Start();
            }
            else
            {
                Trace.WriteLine("尚未配置此APID");
            }
        }

        private void APIDForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            for (int i = 0; i < myform.dataGridView3.Rows.Count; i++)
            {
                if (CurrentApidName == (string)Data.dtAPID.Rows[i]["名称"])
                {
                    DataGridViewCheckBoxCell checkCell = (DataGridViewCheckBoxCell)myform.dataGridView3.Rows[i].Cells[0];
                    checkCell.Value = false;

                    for (int j = 0; j < Data.ApidList.Count; j++)
                    {
                        if (CurrentApidName == Data.ApidList[j].apidName)
                        {
                            Data.ApidList.Remove(Data.ApidList[j]);
                            break;
                        }
                    }
                    break;
                }
            }
        }


        private void DealWithEPDU()
        {
            while (Data.AllThreadTag)
            {
                if (DataQueue.Count > 0)
                {
                    byte[] Epdu = DataQueue.Dequeue();

                    //string timestr = string.Format("{0}-{1:D2}-{2:D2} {3:D2}:{4:D2}:{5:D2}", DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
                    //string epdustr = null;
                    //for (int i = 0; i < Epdu.Length; i++)
                    //{
                    //    epdustr += Epdu[i].ToString("x2");
                    //}

                    if ((Epdu.Length-6) < TotalLen)
                    {
                        MessageBox.Show(CurrentApidName + "收到EPDU长度错误，无法解析!!");
                    }
                    else
                    {
                        string tempstr = "";//将EPDU转化为二进制string
                        for (int i = 6; i < Epdu.Length; i++) tempstr += Convert.ToString(Epdu[i], 2).PadLeft(8, '0');

                        Trace.WriteLine(tempstr.Length);

                        string[] TempStringList = new string[dtAPid.Rows.Count+1];
                        string timestr = string.Format("{0}-{1:D2}-{2:D2} {3:D2}:{4:D2}:{5:D2}", DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
                        TempStringList[0] = timestr;

                        string[] TempStringList2 = new string[dtAPid.Rows.Count + 1];
                        TempStringList2[0] = timestr;
                        for (int j = 0; j < dtAPid.Rows.Count; j++)
                        {
                            int len = int.Parse((string)dtAPid.Rows[j]["占位"]);


                            long t = Convert.ToInt64(tempstr.Substring(0, len), 2);

                            int padleft = int.Parse((string)dtAPid.Rows[j]["占位"]);

                            if (padleft == 8 || padleft == 16 || padleft == 32 || padleft == 48)
                            {
                                padleft = 2 * (padleft / 8);
                            }
                            else
                            {
                                padleft = 2 * (padleft / 8) + 2;
                            }

                            string initialvalue = "0x" + t.ToString("x").PadLeft(padleft, '0');

                            dtAPid.Rows[j]["值"] = initialvalue;


                           /////////////////////////////////////////////////////
                            string typestr = Data.GetConfigStr(Data.APIDconfigPath, CurrentApidName, (string)dtAPid.Rows[j]["名称"], "fmul");

                            string analystr = Data.GetAnalystr(typestr,initialvalue);

                            //////////////////////////////////////////////////////////////////

                            dtAPid.Rows[j]["解析值"] = analystr;

                            tempstr = tempstr.Substring(len, tempstr.Length - len);

                            TempStringList[j + 1] = (string)dtAPid.Rows[j]["值"];

                            TempStringList2[j + 1] = (string)dtAPid.Rows[j]["解析值"];
                        }
                        

                        Data.sql.InsertValues("table_" + CurrentApidName+"_源码", TempStringList);

                        Data.sql.InsertValues("table_" + CurrentApidName+"_解析值", TempStringList2);
                    }

                    //int index = this.dataGridView_EPDU.Rows.Add();
                    //this.dataGridView_EPDU.Rows[index].Cells[0].Value = timestr;
                    //this.dataGridView_EPDU.Rows[index].Cells[1].Value = epdustr;
                   
                }
                else
                {
                    Thread.Sleep(500);
                }
            }
        }

        private void dataGridView1_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {

        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
         
           
        }
    }
}













