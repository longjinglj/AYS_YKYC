using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using System.IO;
using System.Diagnostics;

namespace AYS_YKYC
{
    public partial class SettingForm : Form
    {
        public MainForm mform;

        String KcmdPath;//K令码表路径
        public SettingForm(AYS_YKYC.MainForm parent)
        {
            InitializeComponent();
            mform = parent;
            foreach (Control ctr in this.Controls)
            {
                ctr.Enabled = false;
            }
            this.pictureBox1.Enabled = true;
        }

        private void SettingForm_Load(object sender, EventArgs e)
        {
            textBox_LocalIP1.Text = ConfigurationManager.AppSettings["LocalIP1"];
            textBox_LocalPort1_YK.Text = ConfigurationManager.AppSettings["LocalPort1_YK"];
            textBox_LocalPort1_YC.Text = ConfigurationManager.AppSettings["LocalPort1_YC"];

            textBox_LocalIP2.Text = ConfigurationManager.AppSettings["LocalIP2"];        

            textBox_USRP_IP.Text = ConfigurationManager.AppSettings["Client_USRP_Ip"];
            
            textBox_CRTa_IP.Text = ConfigurationManager.AppSettings["Server_CRTa_Ip"];

            textBox_CRTa_Port.Text = ConfigurationManager.AppSettings["Server_CRTa_Port"];
            textBox_CRTa_Port2.Text = ConfigurationManager.AppSettings["Server_CRTa_Port2"];

        }

        private void SettingForm_Paint(object sender, PaintEventArgs e)
        {
            Console.WriteLine("here is SettingForm_Paint!!!");

        }

        bool lockstate = true;
        private void pictureBox1_Click(object sender, EventArgs e)
        {
            if (lockstate)
            {
                MyLog.Info("系统设置--解锁");
                lockstate = false;
                this.pictureBox1.Image = Properties.Resources.unlocked2;


                foreach (Control ctr in this.Controls)
                {
                    ctr.Enabled = true;
                }

            }
            else
            {
                MyLog.Info("系统设置--锁定");

                lockstate = true;
                this.pictureBox1.Image = Properties.Resources.locked;

                foreach (Control ctr in this.Controls)
                {
                    ctr.Enabled = false;
                }
                this.pictureBox1.Enabled = true;

            }
        }


        private void SettingForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (lockstate)
            {
                MyLog.Info("载入各项配置参数");

                mform.SetConfigValue("LocalIP1", textBox_LocalIP1.Text);
                mform.SetConfigValue("LocalPort1_ZK", textBox_LocalPort1_YK.Text);

                mform.SetConfigValue("LocalIP2", textBox_LocalIP2.Text);


                mform.SetConfigValue("Client_USRP_Ip", textBox_USRP_IP.Text);
    


                mform.SetConfigValue("Server_CRTa_Ip", textBox_CRTa_IP.Text);

                mform.SetConfigValue("Server_CRTa_Port", textBox_CRTa_Port.Text);
                mform.SetConfigValue("Server_CRTa_Port2", textBox_CRTa_Port2.Text);
            }
            else
            {
                DialogResult dr = MessageBox.Show("参数未保存，是否确定关闭此窗体？", "关闭提示", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if(dr == DialogResult.No)
                {
                    e.Cancel = true;
                }
                else
                {
                    e.Cancel = false;
                }
            }
            mform.mySettingForm = null;
        }
    }
}
