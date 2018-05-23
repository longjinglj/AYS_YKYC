using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

using System.Collections;
using System.Collections.Specialized;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Data;
using System.Windows.Forms;
using System.Configuration;
using Microsoft.Win32;
using System.Diagnostics;
using System.Timers;

namespace AYS_YKYC
{
    public class ServerAPP
    {
        public static ManualResetEvent allDone = new ManualResetEvent(false);
        private static byte[] RecvBuf = new byte[1024];
        private static int ServerPort = 8888;
        static Socket ServerSocket;
        public bool ServerOn = false;//监听总控_YK
        List<Socket> ClientSocketList = new List<Socket>();

        private static int ServerPort2 = 7777;
        static Socket ServerSocket2;
        public bool ServerOn_YC = false;//监听总控_YC
        List<Socket> ClientSocketList2 = new List<Socket>();



        public void ServerStart()
        {
            Trace.WriteLine("------------------------进入ServerStart");
            ClientAPP.ClientUSRP_Telecmd.ClientIP = ConfigurationManager.AppSettings["Client_USRP_Ip"];
            ServerOn = true;
            ClientAPP.ClientUSRP_Telecmd.IsConnected = false;
            //     new Thread(() => { WhichZKOn(); }).Start();
            ServerPort = int.Parse(ConfigurationManager.AppSettings["LocalPort1_YK"]);
            ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPAddress address = IPAddress.Parse(ConfigurationManager.AppSettings["LocalIP1"]);
            try
            {
                ServerSocket.Bind(new IPEndPoint(address, ServerPort));
                ServerSocket.Listen(10);
                ServerSocket.BeginAccept(new AsyncCallback(onCall_telecmd), ServerSocket);//继续接受其他客户端的连接  
            }
            catch (Exception ex)
            {
                MyLog.Error("启动-->遥控-->服务器监听USRP线程失败，检查IP设置和网络连接");
                Trace.WriteLine(ex.Message);
                ServerOn = false;
            }
            Trace.WriteLine("------------------------退出ServerStart");
        }


        public void ServerStart2()
        {
            Trace.WriteLine("------------------------进入ServerStart2");
            ClientAPP.ClientUSRP_Telemetry.ClientIP = ConfigurationManager.AppSettings["Client_USRP_Ip"];
            ServerOn_YC = true;
            ClientAPP.ClientUSRP_Telemetry.IsConnected = false;

            ServerPort2 = int.Parse(ConfigurationManager.AppSettings["LocalPort1_YC"]);
            ServerSocket2 = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress address2 = IPAddress.Parse(ConfigurationManager.AppSettings["LocalIP1"]);
            try
            {
                ServerSocket2.Bind(new IPEndPoint(address2, ServerPort2));
                ServerSocket2.Listen(10);
                ServerSocket2.BeginAccept(new AsyncCallback(onCall_telemetry), ServerSocket2);
            }
            catch (Exception ex)
            {
                MyLog.Error("启动<--遥测<--服务器监听USRP线程失败，检查IP设置和网络连接");
                Trace.WriteLine(ex.Message);
                ServerOn_YC = false;
            }
            Trace.WriteLine("------------------------退出ServerStart2");
        }

        public void ServerStop()
        {
            Trace.WriteLine("------------------------进入ServerStop");
            ServerOn = false;
            Data.USRP_telecmd_IsConnected = false;

            ClientAPP.ClientUSRP_Telecmd.IsConnected = false;
            Data.ServerConnectEvent.Set();
            try
            {
                ServerSocket.Close();
                foreach (var sock in ClientSocketList)
                {
                    if (sock.Connected)
                    {
                        sock.Shutdown(SocketShutdown.Both);
                        sock.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MyLog.Error("ServerStop无法关闭，错误原因:" + ex.Message);
            }
            Trace.WriteLine("------------------------退出ServerStop");
        }


        public void ServerStop2()
        {
            Trace.WriteLine("------------------------进入ServerStop2");
            ServerOn_YC = false;
            ClientAPP.ClientUSRP_Telemetry.IsConnected = false;
            Data.ServerConnectEvent2.Set();
            try
            {
                ServerSocket2.Close();
                foreach (var sock in ClientSocketList2)
                {
                    if (sock.Connected)
                    {
                        sock.Shutdown(SocketShutdown.Both);
                        sock.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MyLog.Error("ServerStop2无法关闭，错误原因:" + ex.Message);
            }
            Trace.WriteLine("------------------------退出ServerStop2");
        }

        private void onCall_telecmd(IAsyncResult ar)
        {
            Trace.WriteLine("onCall----遥控服务器");
            Socket serverSoc = (Socket)ar.AsyncState;
            if (ServerOn)
            {
                try
                {
                    Socket ClientSocket = serverSoc.EndAccept(ar);
                    if (serverSoc != null)
                    {
                        serverSoc.BeginAccept(new AsyncCallback(onCall_telecmd), serverSoc);
                        IPEndPoint tmppoint = (IPEndPoint)ClientSocket.RemoteEndPoint;
                        String RemoteIpStr = tmppoint.Address.ToString();
                        Trace.WriteLine(RemoteIpStr);

                        if (RemoteIpStr == ClientAPP.ClientUSRP_Telecmd.ClientIP)
                        {
                            ClientAPP.ClientUSRP_Telecmd.IsConnected = true;
                            Data.ServerConnectEvent.Set();

                            new Thread(() => { RecvFromUSRP(ClientSocket); }).Start();
                            new Thread(() => { SendToUSRP(ClientSocket); }).Start();

                            Data.USRP_telecmd_IsConnected = true;

                            ClientSocketList.Add(ClientSocket);
                        }
                    }
                }
                catch (SocketException ex)
                {
                    Trace.WriteLine("onCall-Exception-遥控服务器" + ex.Message);
                }
            }
            else
            {
                Console.WriteLine("Server already off!!!");
            }
        }


        private void onCall_telemetry(IAsyncResult ar)
        {
            Trace.WriteLine("onCall----遥测服务器");
            Socket serverSoc = (Socket)ar.AsyncState;
            if (ServerOn_YC)
            {
                try
                {
                    Socket ClientSocket = serverSoc.EndAccept(ar);
                    if (serverSoc != null)
                    {
                        serverSoc.BeginAccept(new AsyncCallback(onCall_telemetry), serverSoc);
                        IPEndPoint tmppoint = (IPEndPoint)ClientSocket.RemoteEndPoint;
                        String RemoteIpStr = tmppoint.Address.ToString();
                        Console.WriteLine(RemoteIpStr);
                        if (RemoteIpStr == ClientAPP.ClientUSRP_Telemetry.ClientIP)
                        {
                            ClientAPP.ClientUSRP_Telemetry.IsConnected = true;
                            Data.ServerConnectEvent2.Set();

                            lock (Data.EpduBuf_List)
                                Data.EpduBuf_List.Clear();

                            new Thread(() => { RecvFromClientUSRP(ClientSocket); }).Start();

                            new Thread(() => { DisPatchEpdu(); }).Start();


                            ClientSocketList2.Add(ClientSocket);
                        }
                    }
                }
                catch (SocketException ex)
                {
                    Trace.WriteLine(ex.Message);
                }
            }
            else
            {
                Console.WriteLine("Server already off!!!");
            }
        }

        #region onCall原始代码
        //private void onCall(IAsyncResult ar)
        //{
        //    Trace.WriteLine("onCall----遥控服务器");
        //    Socket serverSoc = (Socket)ar.AsyncState;
        //    if (ServerOn)
        //    {
        //        try
        //        {
        //            Socket ClientSocket = serverSoc.EndAccept(ar);
        //            if (serverSoc != null)
        //            {
        //                serverSoc.BeginAccept(new AsyncCallback(onCall), serverSoc);
        //                IPEndPoint tmppoint = (IPEndPoint)ClientSocket.RemoteEndPoint;
        //                String RemoteIpStr = tmppoint.Address.ToString();
        //                Trace.WriteLine(RemoteIpStr);
        //                Trace.WriteLine(tmppoint.Port.ToString());
        //                if (RemoteIpStr == ClientAPP.ClientUSRP_Telecmd.ClientIP)
        //                {
        //                    //----------发送登陆信息-----------
        //                    ClientSocket.Send(Function.Make_login_frame(Data.Data_Flag_Real, Data.ZK_S1));
        //                    MyLog.Info("遥控前端服务器---->向总控（主）发送登陆信息");

        //                    ClientAPP.ClientUSRP_Telecmd.IsConnected = true;
        //                    Data.ServerConnectEvent.Set();

        //                    new Thread(() => { RecvFromUSRP(ClientSocket); }).Start();
        //                    new Thread(() => { SendToUSRP(ClientSocket); }).Start();

        //                    ClientSocketList.Add(ClientSocket);
        //                }
        //            }
        //        }
        //        catch (SocketException ex)
        //        {
        //            Trace.WriteLine("onCall-Exception-遥控服务器" + ex.Message);  
        //        }
        //    }
        //    else
        //    {
        //        Console.WriteLine("Server already off!!!");
        //    }
        //}
        #endregion

        #region onCall_GT原始代码
        //private void onCall_GT(IAsyncResult ar)
        //{
        //    Trace.WriteLine("onCall----遥测服务器");
        //    Socket serverSoc = (Socket)ar.AsyncState;
        //    if (ServerOn_YC)
        //    {
        //        try
        //        {
        //            Socket ClientSocket = serverSoc.EndAccept(ar);
        //            if (serverSoc != null)
        //            {
        //                serverSoc.BeginAccept(new AsyncCallback(onCall_GT), serverSoc);
        //                IPEndPoint tmppoint = (IPEndPoint)ClientSocket.RemoteEndPoint;
        //                String RemoteIpStr = tmppoint.Address.ToString();
        //                Console.WriteLine(RemoteIpStr);
        //                if (RemoteIpStr == ClientAPP.ClientUSRP_Telemetry.ClientIP)
        //                {
        //                    //----------发送登陆信息-----------
        //                    ClientSocket.Send(Function.Make_login_frame(Data.Data_Flag_Real, Data.ZK_S1));
        //                    MyLog.Info("遥测前端服务器---->向总控（主）发送登陆信息");

        //                    ClientAPP.ClientUSRP_Telemetry.IsConnected = true;
        //                    Data.ServerConnectEvent2.Set();

        //                    new Thread(() => { RecvFromClientUSRP(ClientSocket); }).Start();
        //                    new Thread(() => { SendToClientGT(ClientSocket); }).Start();

        //                    ClientSocketList2.Add(ClientSocket);
        //                }
        //            }
        //        }
        //        catch (SocketException ex)
        //        {
        //            Trace.WriteLine(ex.Message);
        //        }
        //    }
        //    else
        //    {
        //        Console.WriteLine("Server already off!!!");
        //    }
        //}
        #endregion
        /// <summary>
        /// 收到总控发来的网络数据包，解析并推入对应的队列中
        /// </summary>
        /// <param name="data"></param>
        public void deal_zk_data(byte[] data, int RecvNum, string timestr, string RemoteIpStr)
        {
            byte[] DealData = new byte[RecvNum];//收到的实际数组，data后面可能包含0？
            Trace.WriteLine("收到数据量" + RecvNum.ToString());

            Array.Copy(data, DealData, RecvNum);

            #region 处理VCDU-MPDU
            string VCIDstr = Convert.ToString(DealData[0], 2).PadLeft(8, '0').Substring(0, 2);
            Data.dtVCDU.Rows[0]["版本号"] = Convert.ToString(DealData[0], 2).PadLeft(8, '0').Substring(0, 2);
            Data.dtVCDU.Rows[0]["SCID"] = Convert.ToString(DealData[0], 2).PadLeft(8, '0').Substring(2, 6) + Convert.ToString(DealData[1], 2).PadLeft(8, '0').Substring(0, 2);
            Data.dtVCDU.Rows[0]["VCID"] = Convert.ToString(DealData[1], 2).PadLeft(8, '0').Substring(2, 6);
            Data.dtVCDU.Rows[0]["虚拟信道帧计数"] = "0x" + DealData[2].ToString("x2") + DealData[3].ToString("x2") + DealData[4].ToString("x2");
            Data.dtVCDU.Rows[0]["回放"] = Convert.ToString(DealData[5], 2).PadLeft(8, '0').Substring(0, 1);
            Data.dtVCDU.Rows[0]["保留"] = Convert.ToString(DealData[5], 2).PadLeft(8, '0').Substring(1, 7);
            //            Data.dtVCDU.Rows[0]["插入域"] = Convert.ToString(DealData[6], 2).PadLeft(8, '0');
            Data.dtVCDU.Rows[0]["插入域"] = DealData[6].ToString("x2") + DealData[7].ToString("x2") + DealData[8].ToString("x2")
                + DealData[9].ToString("x2") + DealData[10].ToString("x2") + DealData[11].ToString("x2")
                + DealData[12].ToString("x2") + DealData[13].ToString("x2");
            Data.dtVCDU.Rows[0]["备用"] = Convert.ToString(DealData[14], 2).PadLeft(8, '0').Substring(0, 5);

            //Data.dtVCDU.Rows[0]["首导头指针"] = Convert.ToString(DealData[14], 2).PadLeft(8, '0').Substring(5, 3) + Convert.ToString(DealData[15], 2).PadLeft(8, '0');
            int mpdu_point = (int)((DealData[14] & 0x07) << 8) + (int)DealData[15];
            Data.dtVCDU.Rows[0]["首导头指针"] = "0x" + mpdu_point.ToString("x3");

            #region 更新星上时间
            byte[] XStime = new byte[4];
            Array.Copy(data, 6, XStime, 0, 4);


            int value = (XStime[0] << 24) + (XStime[1] << 16) + (XStime[2] << 8) + XStime[3];
            DateTime starttime = new DateTime(2018, 5, 1, 0, 0, 0);
            DateTime time = Function.BytesToDateTime(value, starttime);
            Data.XStime_str = time.Year + "-" + time.Month + "-" + time.Day + " " + time.Hour + ":" + time.Minute + ":" + time.Second;




            #endregion

            String epdu_data = "";
            for (int i = 16; i < RecvNum; i++)
            {
                epdu_data += DealData[i].ToString("x2");
            }

            Data.sql.InsertValues("table_Telemetry", new string[] { "YK", timestr, RemoteIpStr,
                (string)Data.dtVCDU.Rows[0]["版本号"],(string)Data.dtVCDU.Rows[0]["SCID"],(string)Data.dtVCDU.Rows[0]["VCID"],
                (string)Data.dtVCDU.Rows[0]["虚拟信道帧计数"],(string) Data.dtVCDU.Rows[0]["回放"],(string)Data.dtVCDU.Rows[0]["保留"],(string)Data.dtVCDU.Rows[0]["插入域"],
            (string)Data.dtVCDU.Rows[0]["备用"] ,(string) Data.dtVCDU.Rows[0]["首导头指针"],epdu_data});

            #endregion 处理VCDU-MPDU




            #region 处理EPDU

            lock (Data.EpduBuf_List)
            {
                if (Data.EpduBuf_List.Count > 0)
                {
                    byte[] Last_Epdu = new byte[mpdu_point];
                    Array.Copy(DealData, 16, Last_Epdu, 0, mpdu_point);
                    for (int j = 0; j < mpdu_point; j++) Data.EpduBuf_List.Add(Last_Epdu[j]);//将上次剩余数据推入List再解析
                }

                int Temp_Len = RecvNum - mpdu_point - 16;//EPDU数据域长度
                byte[] Temp_Epdu = new byte[Temp_Len];
                Array.Copy(DealData, 16 + mpdu_point, Temp_Epdu, 0, Temp_Len);
                for (int j = 0; j < Temp_Len; j++) Data.EpduBuf_List.Add(Temp_Epdu[j]);//将数据推入List再解析

            }
            #endregion
        }

        private void DisPatchEpdu()
        {//根据APID分配EPDU
            while (Data.AllThreadTag)
            {
                if (Data.EpduBuf_List.Count > 6)
                {
                    int epdu_len = Data.EpduBuf_List[4] * 256 + Data.EpduBuf_List[5];
                    if (epdu_len < 1024)//避免出现异常情况时，由于epdu_len长度太长一直不处理
                    {
                        int epdu_apid = (int)((Data.EpduBuf_List[0] & 0x07) << 8) + (int)Data.EpduBuf_List[1];
                        string epdu_APID = "0x" + epdu_apid.ToString("x3");

                        bool NeedClearTag = true;//若List头位置不是合理的APID，则清空List
                        foreach (DataRow dr in Data.dtAPID.Rows)
                        {
                            if ((string)dr["APID"] == epdu_APID)
                            {
                                NeedClearTag = false;
                                break;
                            }
                        }

                        if (NeedClearTag)
                        {
                            MyLog.Error("不连续EPDU检测到，清空缓存");
                            lock (Data.EpduBuf_List)
                            {
                                Data.EpduBuf_List.Clear();
                            }
                        }

                        lock (Data.EpduBuf_List)
                        {
                            if (Data.EpduBuf_List.Count >= epdu_len + 7)
                            {
                                byte[] Epdu_Frame = new byte[epdu_len + 7];

                                Data.EpduBuf_List.CopyTo(0, Epdu_Frame, 0, epdu_len + 7);
                                Data.EpduBuf_List.RemoveRange(0, epdu_len + 7);


                                #region 处理EPDU
                                string Version = Convert.ToString(Epdu_Frame[0], 2).PadLeft(8, '0').Substring(0, 3);
                                string Type = Convert.ToString(Epdu_Frame[0], 2).PadLeft(8, '0').Substring(3, 1);
                                string DataTag = Convert.ToString(Epdu_Frame[0], 2).PadLeft(8, '0').Substring(4, 1);

                                int apid = (int)((Epdu_Frame[0] & 0x07) << 8) + (int)Epdu_Frame[1];
                                string APID = "0x" + apid.ToString("x3");

                                string DivTag = Convert.ToString(Epdu_Frame[2], 2).PadLeft(8, '0').Substring(0, 2);

                                int bagcount = (int)((Epdu_Frame[2] & 0x3f) << 8) + (int)Epdu_Frame[3];
                                string BagCount = "0x" + apid.ToString("x3");

                                string BagLen = "0x" + Epdu_Frame[4].ToString("x2") + Epdu_Frame[5].ToString("x2");

                                String DataStr = "";
                                for (int i = 6; i < epdu_len + 1; i++)
                                {
                                    DataStr += Epdu_Frame[i].ToString("x2");
                                }

                                string timestr = string.Format("{0}-{1:D2}-{2:D2} {3:D2}:{4:D2}:{5:D2}", DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);

                                Data.sql.InsertValues("table_Epdu",
                                    new string[] { "EPDU", timestr, Version, Type, DataTag, APID, DivTag, BagCount, BagLen, DataStr });


                                Trace.WriteLine("DealEpdu---APID-------" + APID);

                                foreach (DataRow dr in Data.dtAPID.Rows)///
                                {
                                    if ((string)dr["APID"] == APID)
                                    {
                                        dr["数量"] = (int)dr["数量"] + 1;

                                        foreach (Data.APID_Struct item in Data.ApidList)
                                        {
                                            if (item.apidName == (string)dr["名称"])
                                            {
                                                item.apidForm.DataQueue.Enqueue(Epdu_Frame);
                                            }
                                        }
                                    }
                                }
                                #endregion 处理EPDU
                            }
                        }


                        Thread.Sleep(1);

                    }
                    else
                    {
                        lock (Data.EpduBuf_List)
                        {
                            Data.EpduBuf_List.Clear();
                        }

                    }
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
        }

        #region 遥测端口发送，仅用作参考
        //private void SendToClientGT(object ClientSocket)
        //{
        //    Trace.WriteLine("启动遥测前段设-->总控转发线程");
        //    Socket myClientSocket = (Socket)ClientSocket;
        //    while (ServerOn_YC && myClientSocket.Connected)
        //    {
        //        if (Data.DataQueue_GT.Count > 0)
        //        {
        //            Byte[] temp = Data.DataQueue_GT.Dequeue();
        //            myClientSocket.Send(temp);

        //            SaveFile.DataQueue_out5.Enqueue(temp);

        //            Data.dtYC.Rows[3]["数量"] = (int)Data.dtYC.Rows[3]["数量"] + 1;
        //        }
        //    }
        //}

        #endregion
        private void SendToUSRP(object ClientSocket)
        {
            Trace.WriteLine("启动服务器-->USRP发送线程");
            Socket myClientSocket = (Socket)ClientSocket;
            while (ServerOn && myClientSocket.Connected)
            {
                if (Data.DataQueue_USRP_telecmd.Count > 0)
                {
                    Byte[] temp = Data.DataQueue_USRP_telecmd.Dequeue();
                    myClientSocket.Send(temp);
                    //存储发给USRP的遥控数据
                    SaveFile.DataQueue_out2.Enqueue(temp);
                    Data.dtUSRP.Rows[3][1] = (int)Data.dtUSRP.Rows[3][1] + 1;
                }
            }
        }
        /// <summary>
        /// 接受USRP的遥测数据
        /// </summary>
        /// <param name="ClientSocket"></param>
        private void RecvFromClientUSRP(object ClientSocket)
        {
            Trace.WriteLine("RecvFromUSRP_telemetry!!");
            Socket myClientSocket = (Socket)ClientSocket;
            while (ServerOn_YC && myClientSocket.Connected)
            {
                try
                {
                    byte[] RecvBufZK1 = new byte[2048];
                    int RecvNum = myClientSocket.Receive(RecvBufZK1);
                    if (RecvNum > 0)
                    {
                        String tempstr = "";
                        byte[] RecvBufToFile = new byte[RecvNum];
                        for (int i = 0; i < RecvNum; i++)
                        {
                            RecvBufToFile[i] = RecvBufZK1[i];
                            tempstr += RecvBufZK1[i].ToString("x2");
                        }
                        Trace.WriteLine(tempstr);

                        string timestr = string.Format("{0}-{1:D2}-{2:D2} {3:D2}:{4:D2}:{5:D2}", DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
                        Data.TelemetryRealShowBox.BeginInvoke(
                            new Action(() => { Data.TelemetryRealShowBox.AppendText(timestr + "\n" + tempstr + "\n"); }));

                        //存储从USRP发来的遥测数据
                        SaveFile.DataQueue_out1.Enqueue(RecvBufToFile);

                        //数据库存储
                        IPEndPoint tmppoint = (IPEndPoint)myClientSocket.RemoteEndPoint;
                        String RemoteIpStr = tmppoint.Address.ToString();

                        if (RecvNum > 22)
                        {
                            MyLog.Info("收到遥测数据量：" + RecvNum.ToString());
                            try
                            {
                                deal_zk_data(RecvBufZK1, RecvNum, timestr, RemoteIpStr);

                            }
                            catch (Exception e)
                            {
                                Trace.WriteLine("RecvFromClientZK_YC--deal_zk_data Exception:" + e.Message);
                            }
                            Data.dtUSRP.Rows[0][1] = (int)Data.dtUSRP.Rows[0][1] + 1;
                        }
                        else
                        {
                            MyLog.Error("收到遥测数据帧长度异常：" + RecvNum.ToString());
                        }
                    }
                    else
                    {
                        Trace.WriteLine("收到数据少于等于0！");
                        break;
                    }
                }
                catch (Exception e)
                {
                    Trace.WriteLine("RecvFromClientZK_YC Exception:" + e.Message);

                    if (myClientSocket.Connected)
                    {
                        myClientSocket.Shutdown(SocketShutdown.Both);
                        myClientSocket.Close();
                    }
                    ClientAPP.ClientUSRP_Telemetry.IsConnected = false;

                    break;
                }
            }

            if (myClientSocket.Connected)
            {
                Trace.WriteLine("服务器主动关闭socket!");
                myClientSocket.Shutdown(SocketShutdown.Both);
                myClientSocket.Close();
            }

            ClientAPP.ClientUSRP_Telemetry.IsConnected = false;
            Trace.WriteLine("leave Server YC!!");
            Data.ServerConnectEvent2.Set();
        }

        #region 遥测端口接收，无需使用，仅用作参考
        private void RecvFromUSRP(object ClientSocket)
        {
            Trace.WriteLine("RecvFromUSRP_TeleCMD only for heart use!!");
            Socket myClientSocket = (Socket)ClientSocket;
            while (ServerOn && myClientSocket.Connected)
            {
                try
                {
                    byte[] RecvBufUSRP = new byte[4096];
                    int RecvNum = myClientSocket.Receive(RecvBufUSRP);

                    if (RecvNum > 0)
                    {
                        String tempstr = "";
                        byte[] RecvBufToFile = new byte[RecvNum];
                        for (int i = 0; i < RecvNum; i++)
                        {
                            RecvBufToFile[i] = RecvBufUSRP[i];
                            tempstr += RecvBufUSRP[i].ToString("x2");
                        }
                        Trace.WriteLine(tempstr);
                    }
                    else
                    {
                        Trace.WriteLine("收到数据少于等于0！");
                        break;
                    }
                }
                catch (Exception e)
                {
                    Trace.WriteLine("RecvFromUSRP Exception:" + e.Message);
                    if (myClientSocket.Connected)
                    {
                        myClientSocket.Shutdown(SocketShutdown.Both);
                        myClientSocket.Close();
                    }
                    ClientAPP.ClientUSRP_Telecmd.IsConnected = false;
                    break;
                }
            }

            if (myClientSocket.Connected)
            {
                Trace.WriteLine("服务器主动关闭socket!");
                myClientSocket.Shutdown(SocketShutdown.Both);
                myClientSocket.Close();
            }

            ClientAPP.ClientUSRP_Telecmd.IsConnected = false;
            Trace.WriteLine("leave RecvFromUSRP!!");
            Data.ServerConnectEvent.Set();
        }
        #endregion
    }
}
