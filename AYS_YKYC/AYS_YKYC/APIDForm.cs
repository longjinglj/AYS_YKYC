﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace H07_YKYC
{
    public partial class APIDForm : WeifenLuo.WinFormsUI.Docking.DockContent
    {
        MainForm myform;
        string CurrentApidName;
        DataTable dt = new DataTable();

        public Queue<byte[]> DataQueue = new Queue<byte[]>();   //处理APID对应的Queue
        public APIDForm(string apidstr, H07_YKYC.MainForm parent)
        {
            InitializeComponent();
            this.Text = apidstr;
            CurrentApidName = apidstr;
            myform = parent;
        }

        private void APIDForm_Load(object sender, EventArgs e)
        {
            dt.Columns.Add("接收时间", typeof(string));
            dt.Columns.Add("EPDU数据域", typeof(string));

            dataGridView_EPDU.DataSource = dt;

            new Thread(() => { DealWithEPDU(); }).Start();
        }

        private void APIDForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            for(int i=0;i<myform.dataGridView3.Rows.Count;i++)
            {
                if(CurrentApidName==(string)Data.dtAPID.Rows[i]["名称"])
                {
                    DataGridViewCheckBoxCell checkCell = (DataGridViewCheckBoxCell)myform.dataGridView3.Rows[i].Cells[0];
                    checkCell.Value = false;             

                    break;

                }
            }
        }

        private void DealWithEPDU()
        {
            while(Data.AllThreadTag)
            {
                if(DataQueue.Count>0)
                {
                    byte[] Epdu = DataQueue.Dequeue();

                    string timestr = string.Format("{0}-{1:D2}-{2:D2} {3:D2}:{4:D2}:{5:D2}", DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
                    string epdustr = null;
                    for(int i=0;i<Epdu.Length;i++)
                    {
                        epdustr += Epdu[i].ToString("x2");
                    }
                    DataRow dr = dt.NewRow();
                    dr["接收时间"] = timestr;

                    dr["EPDU数据域"] = epdustr;
                    dt.Rows.Add(dr);

                    //int index = this.dataGridView_EPDU.Rows.Add();
                    //this.dataGridView_EPDU.Rows[index].Cells[0].Value = timestr;
                    //this.dataGridView_EPDU.Rows[index].Cells[1].Value = epdustr;
                    this.dataGridView_EPDU.BeginInvoke(new Action(() => { this.dataGridView_EPDU.Refresh(); }));



                }
                else
                {
                    Thread.Sleep(1000);                    
                }
            }
        }

    }
}
