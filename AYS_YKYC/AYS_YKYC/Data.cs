using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Configuration;
using System.Net.Sockets;
using System.Xml.Linq;
using System.Xml;

namespace AYS_YKYC
{
    class Data
    {
        public static List<byte> EpduBuf_List = new List<byte>();
        public static bool LastEpduDealed = false;
        public static bool AllThreadTag = false;
        public struct APID_Struct
        {
            public APIDForm apidForm;
            public string apidName;
        }

        public static List<APID_Struct> ApidList = new List<APID_Struct>();

        public static SqLiteHelper sql;
        public static TextBox TelemetryRealShowBox;

        public static DataTable dtVCDU = new DataTable();
        public static DataTable dtUSRP = new DataTable();//总控-->遥控服务器
        public static DataTable dtYC = new DataTable();//CRT-->遥测服务器
        public static DataTable dtYKLog = new DataTable();//遥控日志
        public static DataTable dtAPID = new DataTable();//APID列表

        //创建K令码表源文件数组
        public static byte[] KcmdText;

        //创建总控主备谁在当班的全局变量
        public static int MS1orMS2 = 1;
        public static ManualResetEvent WhoIsOnEvent = new ManualResetEvent(false);
        //创建本地控制远端控制的全局变量
        public static bool AutoCtrl = true;
        public static AutoResetEvent WhoIsControl = new AutoResetEvent(false);

        //创建明密状态变量，默认为明
        public static bool MingStatus = true;
        //创建密钥参数变量，默认为初始密钥
        public static bool MiYueStatus = true;
        //创建算法参数变量，默认为初始算法
        public static bool SuanFaStatus = true;


        //创建小回路比对应答事件
        public static ManualResetEvent WaitXHL_Return2ZK = new ManualResetEvent(false);
        public static byte ReturnCode = 0x00;
        //创建明密态全局变量
        public static bool MingMiTag = false;

        public static ManualResetEvent ServerConnectEvent = new ManualResetEvent(false);
        public static ManualResetEvent ServerConnectEvent2 = new ManualResetEvent(false);
        public struct CRT_STRUCT
        {
            public bool XHLEnable;
            public String CRTName;
            public PictureBox Led;

            public int TCMsgStatus;//返回码
            public TextBox mytextbox;//显示返回码
            public string Transfer2CRTa_TempStr;
            public Queue<byte[]> DataQueue_CRT;
            public bool CompareXHLResult;
            public int SendCount;//给CRT发送次数
            public int SendKB;//给CRT发送数据量
            public TextBox mytextbox_count;//显示发送次数
            public TextBox mytextbox_KB;//显示数据量

            public void LedOn()
            {
                this.Led.Image = Properties.Resources.green;
            }
            public void LedOff()
            {
                this.Led.Image = Properties.Resources.red;
            }
            public void init()
            {
                this.SendCount = 0;
                this.SendKB = 0;
                this.TCMsgStatus = 0;
                this.Transfer2CRTa_TempStr = "";
                this.LedOff();
                DataQueue_CRT = new Queue<byte[]>();
                CompareXHLResult = false;
            }
        }
        public static CRT_STRUCT DealCRTa = new CRT_STRUCT();

        public static Queue<byte[]> DataQueue_GT = new Queue<byte[]>();            //用于转发给高通地测，即KSA中继
        public static Queue<byte[]> DataQueue_USRP_telecmd = new Queue<byte[]>();        //用于给USRP发送遥控

        public static bool USRP_telecmd_IsConnected = false;

        //------------------------IP地址及端口号--------------------------
        public static string ZK_IP_Z = "10.65.33.1";//总控服务器IP地址(主)
        public static int ZK_PORT_Z = 5001;

        public static string ZK_IP_B = "10.65.33.2";//总控服务器IP地址(备)
        public static int ZK_PORT_B = 5002;

        public static string ExternNet_IP = "10.65.33.161";//外系统接口计算机IP地址
        public static int ExternNet_PORT = 5175;

        //public static string KERNAL_IP = "10.65.33.175";//核心舱模拟器IP地址
        //public static int KERNAL_PORT_B = 5175;

        //public static string GT_IP = "192.168.0.15";//高通IP地址
        //public static int GT_PORT = 9000;
        //public static string LOCAL_IP = "192.168.0.5";//与高通连接的本地IP地址
        //public static int LOCAL_PORT = 9000;

        //----------------------------航天器编号---------------------------------------

        public static byte[] Num_MTC = new byte[8] { (byte)'T', (byte)'G', (byte)'M',
                      (byte)'T', (byte)'C',(byte) '1',(byte)'0', (byte)'1' };//梦天初样TGMTC001
        public static byte[] Num_X07 = new byte[8] { (byte)'X', (byte)'0', (byte)'7',
                      (byte)' ', (byte)' ',(byte) ' ',(byte)' ', (byte)' ' };//X07     

        //--------------------------数据标识-----------------------------------
        public static byte Data_Flag_Real = (byte)'R';//实时
        public static byte Data_Flag_Replay = (byte)'A';//回放

        //--------------------------信息标识-----------------------------------
        public static byte[] InfoFlag_Login = new byte[4] { (byte)'L', (byte)'O', (byte)'G', (byte)'N' };//签到信息
        public static byte[] InfoFlag_Time = new byte[4] { (byte)'U', (byte)'C', (byte)'L', (byte)'K' };//校时信息
        public static byte[] InfoFlag_Set = new byte[4] { (byte)'S', (byte)'E', (byte)'T', (byte)'P' };//地面设备设置命令
        public static byte[] InfoFlag_Stat = new byte[4] { (byte)'D', (byte)'A', (byte)'T', (byte)'S' };//地面设备状态信息
        public static byte[] InfoFlag_SetOk = new byte[4] { (byte)'S', (byte)'A', (byte)'C', (byte)'K' };//地面设备设置命令应答

        public static byte[] InfoFlag_CACK = new byte[4] { (byte)'C', (byte)'A', (byte)'C', (byte)'K' };//遥控指令应答
        public static byte[] InfoFlag_KACK = new byte[4] { (byte)'K', (byte)'A', (byte)'C', (byte)'K' };//遥控注数应答
        public static byte[] InfoFlag_ACKR = new byte[4] { (byte)'A', (byte)'C', (byte)'K', (byte)'R' };//小回路比对应答
        public static byte[] InfoFlag_DAGF = new byte[4] { (byte)'D', (byte)'A', (byte)'G', (byte)'F' };
        public static byte[] InfoFlag_DCUZ = new byte[4] { (byte)'D', (byte)'C', (byte)'U', (byte)'Z' };//对地测控通道下行遥测源码
        public static byte[] InfoFlag_DMTC = new byte[4] { (byte)'D', (byte)'M', (byte)'Y', (byte)'C' };//对地测控上行注数数据
        //-------------------------辅助标识------------------------------------
        public static byte Help_Flag = (byte)':';

        //----------------------信息来源/目的地址名称--------------------------
        public static byte[] ZK_S1 = new byte[3] { (byte)'M', (byte)'S', (byte)'1' };//总控服务器（主）
        public static byte[] ZK_S2 = new byte[3] { (byte)'M', (byte)'S', (byte)'2' };//总控服务器（备）
        public static byte[] TMF = new byte[3] { (byte)'T', (byte)'M', (byte)'F' };//遥测前端设备
        public static byte[] TCF = new byte[3] { (byte)'T', (byte)'C', (byte)'F' };//遥控前端设备
        public static byte[] IPC = new byte[3] { (byte)'I', (byte)'P', (byte)'C' };//外系统接口计算机



        public static string YKconfigPath = Program.GetStartupPath() + @"配置文件\遥控指令配置.xml";

        public static string YCconfigPath = Program.GetStartupPath() + @"配置文件\遥测APID配置.xml";

        public static string APIDconfigPath = Program.GetStartupPath() + @"配置文件\APID详细配置.xml";
        public static void SaveConfig(string Path, string key, string value)
        {
            XDocument xDoc = XDocument.Load(Path);
            XmlReader reader = xDoc.CreateReader();

            bool Matched = false;//是否已在XML中

            foreach (var p in xDoc.Root.Elements("add"))
            {
                if (p.Attribute("key").Value == key)
                {
                    p.Attribute("value").Value = value;
                    Matched = true;
                }
            }
            if (Matched == false)
            {
                XElement element = new XElement("add", new XAttribute("key", key), new XAttribute("value", value));
                xDoc.Root.Add(element);
            }

            xDoc.Save(Path);
            //var query = from p in xDoc.Root.Elements("add")
            //            where p.Attribute("key").Value == "DAModifyA1"
            //            orderby p.Value
            //            select p.Value;

            //foreach (string s in query)
            //{
            //    Console.WriteLine(s);
            //}

        }

        public static string GetConfig(string Path, string key)
        {
            XDocument xDoc = XDocument.Load(Path);
            XmlReader reader = xDoc.CreateReader();
            string value = "Error";

            var query = from p in xDoc.Root.Elements("add")
                        where p.Attribute("key").Value == key
                        select p.Attribute("value").Value;

            foreach (string s in query)
            {
                value = s;
            }

            //foreach (var p in xDoc.Root.Elements("add"))
            //{
            //    if (p.Attribute("key").Value == key)
            //    {
            //        value = p.Attribute("value").Value;
            //    }
            //}
            return value;

        }

        public static List<string> GetConfigNormal(string Path, string type)
        {
            XDocument xDoc = XDocument.Load(Path);
            XmlReader reader = xDoc.CreateReader();

            var query = from p in xDoc.Root.Elements(type)
                        select p.Attribute("key").Value;

            List<string> list = new List<string>();
            foreach (string s in query)
            {
                list.Add(s);
            }
            return list;
        }

        public static string GetConfigStr(string Path, string type, string key, string name)
        {
            XDocument xDoc = XDocument.Load(Path);
            XmlReader reader = xDoc.CreateReader();
            string value = "Error";
            try
            {
                var query = from p in xDoc.Root.Elements(type)
                            where p.Attribute("key").Value == key
                            select p.Attribute(name).Value;
                foreach (string s in query)
                {
                    value = s;
                }
            }
            catch
            {
                value = "null;";
            }
            return value;
        }



        #region 解析值
        public static string GetAnalystr(string type, string strvalue)
        {
            string result = "error";
            string[] temp = new string[2];
            temp = strvalue.Split('x');
            strvalue = temp[1];
            switch (type)
            {
                case "十六进制显示":
                    {
                        result = strvalue;
                    }
                    break;
                case "十进制显示":
                    {
                        long value = Convert.ToInt64(strvalue, 16);
                        result = Convert.ToString(value, 10);
                    }
                    break;
                case "温度1方式显示"://12位
                    {
                        long valuetemp = Convert.ToInt64(strvalue, 16);
                        double value = ((float)valuetemp / 4096) * 3.3;
                        result = (value * 196.078 - 273).ToString("f2");
                    }
                    break;
                case "温度2":
                    {
                        long valuetemp = Convert.ToInt64(strvalue, 16);
                        double value = ((float)valuetemp / 256) * 5;
                        result = (value * 196.078 - 273).ToString("f2");
                    }
                    break;
                case "枚举1方式显示":
                    {
                        long value = Convert.ToInt64(strvalue, 16);
                        if (value == 0x11U)
                            result = "上电";
                        else
                           if (value == 0x22U)
                            result = "晶振失效复位";
                        else
                           if (value == 0x44U)
                            result = "看门狗复位";
                        else
                           if (value == 0x88U)
                            result = "仿真器复位";
                        else
                           if (value == 0x99U)
                            result = "CPU复位";
                        else
                           if (value == 0xAAU)
                            result = "软件复位";
                        else
                           if (value == 0x55U)
                            result = "外部复位按钮复位";
                        else
                           if (value == 0x00U)
                            result = "位置复位";
                    }
                    break;
                case "枚举2":
                case "枚举2方式显示":
                    {
                        long value = Convert.ToInt64(strvalue, 16);
                        if (value == 0x00)//000
                            result = "待机";
                        else
                           if (value == 0x01)//001
                            result = "擦除";
                        else
                           if (value == 0x02)///010
                            result = "记录";
                        else
                           if (value == 0x03)//011
                            result = "回放";
                        else
                           if (value == 0x04)//100
                            result = "测试";
                        else
                           if (value == 0x05)//101
                            result = "单载波";
                        else
                           if (value == 0x06)//110
                            result = "非法";
                        else
                           if (value == 0x07)//111
                            result = "非法";
                    }
                    break;
                case "枚举3":
                case "枚举3方式显示":
                    {
                        long value = Convert.ToInt64(strvalue, 16);
                        if (value == 0x00)//00
                            result = "待机";
                        else
                           if (value == 0x01)//01
                            result = "记录";
                        else
                           if (value == 0x02)///10
                            result = "回放";
                        else
                           if (value == 0x03)//11
                            result = "擦除";

                    }
                    break;
                case "枚举4方式显示":
                    {
                        long value = Convert.ToInt64(strvalue, 16);
                        if (value == 0x00)//0
                            result = "正常定位";
                        else
                           if (value == 0x01)//1
                            result = "GPS时间无效";
                        else
                           if (value == 0x02)///
                            result = "有故障星";
                        else
                           if (value == 0x03)//
                            result = "PDOP值太大";
                        else
                           if (value == 0x04)//
                            result = "冷捕-无任何信息";
                        else
                           if (value == 0x05)//
                            result = "热捕-已经有星历、时间、位置等信息";
                        else
                           if (value == 0x08)//
                            result = "没有可用卫星";
                        else
                           if (value == 0x09)//
                            result = "只有1课可用卫星";
                        else
                           if (value == 0x0A)//
                            result = "只有2颗可用卫星";
                        else
                           if (value == 0x0B)//
                            result = "只有3颗可用卫星";
                        else
                           if (value == 0x0D)//
                            result = "高度超差";
                        else
                           if (value == 0x0E)//
                            result = "频度超差";
                        else
                           if (value == 0x0F)//
                            result = "速度超差";

                    }
                    break;

                case "FLOAT"://32wei
                    {
                        uint num = uint.Parse(strvalue, System.Globalization.NumberStyles.AllowHexSpecifier);
                        byte[] floatvals = BitConverter.GetBytes(num);
                        float f = BitConverter.ToSingle(floatvals, 0);
                        result = f.ToString("f2");
                    }
                    break;
                case "浮点显示"://64位
                    {

                        UInt64 num = UInt64.Parse(strvalue, System.Globalization.NumberStyles.AllowHexSpecifier);
                        byte[] floatvals = BitConverter.GetBytes(num);
                        double f = BitConverter.ToDouble(floatvals, 0);
                        result = f.ToString("f2");
                    }
                    break;
                case "电压1方式显示":
                case "8位3.3V转换*1":
                    {
                        long valuetemp = Convert.ToInt64(strvalue, 16);
                        double value = ((double)valuetemp / 256) * 3.3;
                        result = (value).ToString("f2");
                    }
                    break;
                case "电流1方式显示":
                case "12位3.3V转换*1":
                    {
                        long valuetemp = Convert.ToInt64(strvalue, 16);
                        double value = ((double)valuetemp / 4096) * 3.3;
                        result = (value).ToString("f2");
                    }
                    break;

                case "电压2方式显示":
                case "8位5V转换*1":
                    {
                        long valuetemp = Convert.ToInt64(strvalue, 16);
                        double value = ((double)valuetemp / 256) * 5;
                        result = (value).ToString("f2");
                    }
                    break;
                case "12位5V转换*1":
                    {
                        long valuetemp = Convert.ToInt64(strvalue, 16);
                        double value = ((double)valuetemp / 4096) * 5;
                        result = (value).ToString("f2");
                    }
                    break;
                case "8位3.3V转换*2":
                    {
                        long valuetemp = Convert.ToInt64(strvalue, 16);
                        double value = ((double)valuetemp / 256) * 3.3 * 2;
                        result = (value).ToString("f2");
                    }
                    break;
                case "12位3.3V转换*2":
                    {
                        long valuetemp = Convert.ToInt64(strvalue, 16);
                        double value = ((double)valuetemp / 4096) * 3.3 * 2;
                        result = (value).ToString("f2");
                    }
                    break;
                case "8位5V转换*2":
                    {
                        long valuetemp = Convert.ToInt64(strvalue, 16);
                        double value = ((double)valuetemp / 256) * 5 * 2;
                        result = (value).ToString("f2");
                    }
                    break;
                case "12位5V转换*2":
                    {
                        long valuetemp = Convert.ToInt64(strvalue, 16);
                        double value = ((double)valuetemp / 4096) * 5 * 2;
                        result = (value).ToString("f2");
                    }
                    break;

                case "8位3.3V转换*3":
                    {
                        long valuetemp = Convert.ToInt64(strvalue, 16);
                        double value = ((double)valuetemp / 256) * 3.3 * 3;
                        result = (value).ToString("f2");
                    }
                    break;
                case "12位3.3V转换*3":
                    {
                        long valuetemp = Convert.ToInt64(strvalue, 16);
                        double value = ((double)valuetemp / 4096) * 3.3 * 3;
                        result = (value).ToString("f2");
                    }
                    break;
                case "8位5V转换*3":
                    {
                        long valuetemp = Convert.ToInt64(strvalue, 16);
                        double value = ((double)valuetemp / 256) * 5 * 3;
                        result = (value).ToString("f2");
                    }
                    break;
                case "12位5V转换*3":
                    {
                        long valuetemp = Convert.ToInt64(strvalue, 16);
                        double value = ((double)valuetemp / 4096) * 5 * 3;
                        result = (value).ToString("f2");
                    }
                    break;
                case "单位0.1m":
                    {
                        long value = Convert.ToInt64(strvalue, 16);
                        result = ((float)value * 0.1).ToString("f1");
                    }
                    break;
                case "布尔显示":
                case "布尔方式显示":
                    {
                        long value = Convert.ToInt64(strvalue, 16);
                        if (value == 0x00)
                            result = "否";
                        else
                            result = "是";
                    }
                    break;
                case "布尔2显示":
                    {
                        long value = Convert.ToInt64(strvalue, 16);
                        if (value == 0x00)
                            result = "是";
                        else
                            result = "否";
                    }
                    break;
                case "布尔3显示":
                    {
                        long value = Convert.ToInt64(strvalue, 16);
                        if (value == 0x55)
                            result = "飞轮A";
                        else
                            if (value == 0xAA)
                            result = "飞轮B";
                    }
                    break;
                case "布尔0：飞轮A，1：飞轮B":
                    {
                        long value = Convert.ToInt64(strvalue, 16);
                        if (value == 0x01)
                            result = "飞轮A";
                        else
                            if (value == 0x00)
                            result = "飞轮B";
                    }
                    break;
                case "北京时间2017年1月1日12时0分0秒为起点的累积秒值":
                    {
                        long value = Convert.ToInt64(strvalue, 16);
                        DateTime starttime = new DateTime(2017, 1, 1, 0, 0, 0);
                        DateTime time = Function.BytesToDateTime(value, starttime);
                        result = time.Year + "-" + time.Month + "-" + time.Day + "-" + time.Hour + "-" + time.Minute + "-" + time.Second;
                    }
                    break;
                case "result = (float)(data&gt;&gt;3)/16":
                    {
                        long value = Convert.ToInt64(strvalue, 16);
                        result = ((value >> 3) / 16).ToString("f2");
                    }
                    break;
                case "result = (float)(data&amp;0xfff8)/2000":
                    {
                        long value = Convert.ToInt64(strvalue, 16);
                        result = ((value & 0xfff8) / 2000).ToString("f2");
                    }
                    break;
                case "result = (float)(data&amp;0x1fff) / 2.5":
                    {
                        long value = Convert.ToInt64(strvalue, 16);
                        result = ((value & 0x1ffff) / 2.5).ToString("f2");
                    }
                    break;
                case "result = (float)(data&amp;0x1fff) / 10":
                    {
                        long value = Convert.ToInt64(strvalue, 16);
                        result = ((value & 0x1ffff) / 10).ToString("f2");
                    }
                    break;
                case "DATA*0.001":
                    {
                        long value = Convert.ToInt64(strvalue, 16);
                        result = (value * 0.001).ToString("f3");
                    }
                    break;
                case "DATA*0.04":
                    {
                        long value = Convert.ToInt64(strvalue, 16);
                        result = (value * 0.04).ToString("f2");
                    }
                    break;
                case "AA锁定，55失锁":
                    {
                        long value = Convert.ToInt64(strvalue, 16);
                        if (value == 0xAA)
                            result = "锁定";
                        else
                            if (value == 0x55)
                            result = "失锁";
                    }
                    break;
                case "2018-5-1 0:0:0开始的秒计数":
                    {
                        long value = Convert.ToInt64(strvalue, 16);
                        DateTime starttime = new DateTime(2018, 5, 1, 0, 0, 0);
                        DateTime time = Function.BytesToDateTime(value, starttime);
                        result = time.Year + "-" + time.Month + "-" + time.Day + "-" + time.Hour + "-" + time.Minute + "-" + time.Second;
                    }
                    break;
                case "1为CAN，0为I2C":
                    {
                        long value = Convert.ToInt64(strvalue, 16);
                        if (value == 0x01)
                            result = "CAN";
                        else
                            result = "I2C";
                    }
                    break;
                case "14位有符号数*0.05°/s":
                    {
                        long value = Convert.ToInt64(strvalue, 16);
                        double tempvalue = ((value & 0x3FFF) < 0x2000) ?
                        ((value & 0x3FFF) * 0.05) :
                        (((~(value & 0x3FFF) & 0x3FFF) + 1) * (-0.05));
                        result = tempvalue.ToString("f2");
                    }
                    break;
                case "14位有符号数*0.00333g":
                    {
                        long value = Convert.ToInt64(strvalue, 16);
                        double tempvalue = ((value & 0x3FFF) < 0x2000) ?
                        ((value & 0x3FFF) * 0.00333) :
                        (((~(value & 0x3FFF) & 0x3FFF) + 1) * (-0.00333));
                        result = tempvalue.ToString("f5");
                    }
                    break;
                case "14位有符号数*0.002418V":
                    {
                        long value = Convert.ToInt64(strvalue, 16);
                        double tempvalue = ((value & 0x3FFF) < 0x2000) ?
                        ((value & 0x3FFF) * 0.002418) :
                        (((~(value & 0x3FFF) & 0x3FFF) + 1) * (-0.002418));
                        result = tempvalue.ToString("f5");
                    }
                    break;
                case "14位有符号数*0.0005gauss":
                    {
                        long value = Convert.ToInt64(strvalue, 16);
                        double tempvalue = ((value & 0x3FFF) < 0x2000) ?
                        ((value & 0x3FFF) * 0.0005) :
                        (((~(value & 0x3FFF) & 0x3FFF) + 1) * (-0.0005));
                        result = tempvalue.ToString("f4");
                    }
                    break;
                case "12位有符号数*0.14℃+25℃":
                    {
                        long value = Convert.ToInt64(strvalue, 16);
                        double tempvalue = ((value & 0x0FFF) < 0x0800) ?
                        ((value & 0x0FFF) * 0.14 + 25) :
                        (((~(value & 0x0FFF) & 0x0FFF) + 1) * (-0.14) + 25);
                        result = tempvalue.ToString("f4");
                    }
                    break;
                case "0x01，0x02":
                    {
                        long value = Convert.ToInt64(strvalue, 16);
                        if (value == 0x01)
                            result = "0x01";
                        else
                            result = "0x02";
                    }
                    break;
                case "00为空，11为满，01或10为非空非满":
                    {
                        long value = Convert.ToInt64(strvalue, 16);
                        if (value == 0x00)
                            result = "空";
                        else
                            if (value == 0x11)
                            result = "满";
                        else
                            if (value == 0x01 || value == 0x10)
                            result = "非空非满";
                    }
                    break;
                case "0：频点1,1：频点2，2：频点3,3：频点4":
                    {
                        long value = Convert.ToInt64(strvalue, 16);
                        if (value == 0x00)
                            result = "频点1";
                        else
                            if (value == 0x01)
                            result = "频点2";
                        else
                            if (value == 0x10)
                            result = "频点3";
                        else
                            if (value == 0x11)
                            result = "频点4";
                    }
                    break;
                case "0:1.2Kbps，1:2.4Kbps，2:4.8Kbps，3:9.6Kbps":
                    {
                        long value = Convert.ToInt64(strvalue, 16);
                        if (value == 0x00)
                            result = "1.2Kbps";
                        else
                            if (value == 0x01)
                            result = "2.4Kbps";
                        else
                            if (value == 0x10)
                            result = "4.8Kbps";
                        else
                            if (value == 0x11)
                            result = "9.6Kbps";
                    }
                    break;
                case "0.1m":
                    {
                        long value = Convert.ToInt64(strvalue, 16);

                        result = ((float)value * 0.1).ToString("f1");
                    }
                    break;
                case "0.01m/s":
                    {
                        long value = Convert.ToInt64(strvalue, 16);

                        double tempvalue = ((value) < 0x8000) ?
                                               ((value & 0x7FFF) * 0.01) :
                                               (((~(value & 0x7FFF) & 0x7FFF) + 1) * (-0.01));
                        result = tempvalue.ToString("f2");
                    }
                    break;
            }
            return result;

        }
        #endregion
        //public static string GetConfigStr(string Path, string key, string name)
        //{
        //    XDocument xDoc = XDocument.Load(Path);
        //    XmlReader reader = xDoc.CreateReader();
        //    string value = "Error";
        //    var query = from p in xDoc.Root.Elements("add")
        //                where p.Attribute("key").Value == key
        //                select p.Attribute(name).Value;

        //    foreach (string s in query)
        //    {
        //        value = s;
        //    }

        //    return value;
        //}

        public static void SaveConfigStr(string Path, string type, string key, string name, string value)
        {
            XDocument xDoc = XDocument.Load(Path);
            XmlReader reader = xDoc.CreateReader();

            bool Matched = false;//是否已在XML中

            foreach (var p in xDoc.Root.Elements(type))
            {
                if (p.Attribute("key").Value == key)
                {
                    p.Attribute(name).Value = value;
                    Matched = true;
                }
            }
            if (Matched == false)
            {
                XElement element = new XElement(type, new XAttribute("key", key), new XAttribute(name, value));
                xDoc.Root.Add(element);
            }

            xDoc.Save(Path);
            //var query = from p in xDoc.Root.Elements("add")
            //            where p.Attribute("key").Value == "DAModifyA1"
            //            orderby p.Value
            //            select p.Value;

            //foreach (string s in query)
            //{
            //    Console.WriteLine(s);
            //}

        }

    }
}
