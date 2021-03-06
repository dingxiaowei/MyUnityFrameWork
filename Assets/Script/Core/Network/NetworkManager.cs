﻿using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using MiniJSON;

public class NetworkManager 
{
    static INetworkInterface s_network;

    private static bool s_isConnect;

    public static bool IsConnect
    {
        get { return NetworkManager.s_isConnect; }
        set { NetworkManager.s_isConnect = value; }
    }

    public static void Init<T>() where T : INetworkInterface,new ()
    {
        //提前加载网络事件派发器，避免异步冲突
        InputManager.LoadDispatcher<InputNetworkConnectStatusEvent>();
        InputManager.LoadDispatcher<InputNetworkMessageEvent>();

        s_network = new T();
        s_network.m_messageCallBack = ReceviceMeaasge;
        s_network.m_ConnectStatusCallback = ConnectStatusChange;

        ApplicationManager.s_OnApplicationUpdate += Update;
    }

    public static void Dispose()
    {
        InputManager.UnLoadDispatcher<InputNetworkConnectStatusEvent>();
        InputManager.UnLoadDispatcher<InputNetworkMessageEvent>();

        s_network.m_messageCallBack = null;
        s_network.m_ConnectStatusCallback = null;
        s_network = null;

        ApplicationManager.s_OnApplicationUpdate -= Update;
    }

    public static void Connect()
    {
        s_network.GetIPAddress();
        s_network.Connect();
    }

    public static void DisConnect()
    {
        s_network.Close();
    }

    public static void SendMessage(string messageType ,Dictionary<string,object> data)
    {
        if(IsConnect)
        {
            s_network.SendMessage(messageType,data);
        }
    }

    public static void SendMessage(Dictionary<string, object> data)
    {
        if (IsConnect)
        {
            s_network.SendMessage(data["MT"].ToString(), data);
        }
    }

    static void ReceviceMeaasge(NetWorkMessage message)
    {
        s_messageList.Add(message);
    }

    static void Dispatch(NetWorkMessage msg)
    {
        try
        {
            InputNetworkEventProxy.DispatchMessageEvent(msg.m_MessageType, msg.m_data);
        }
        catch (Exception e)
        {
            Debug.LogError("Message Error:->" + Json.Serialize(msg.m_data) + "<-\n" + e.ToString());
        }
    }

    static void ConnectStatusChange(NetworkState status)
    {
        s_statusList.Add(status);
    }

    static void Dispatch(NetworkState status)
    {
        if (status == NetworkState.Connected)
        {
            s_isConnect = true;
        }
        else
        {
            s_isConnect = false;
        }


        InputNetworkEventProxy.DispatchStatusEvent(status);
    }


    #region Update

    static List<NetworkState> s_statusList = new List<NetworkState>();
    static List<NetWorkMessage> s_messageList = new List<NetWorkMessage>();

    //将消息的处理并入主线程
    static void Update()
    {
        if (s_messageList.Count >0)
        {
            for (int i = 0; i < s_messageList.Count; i++)
            {
                Dispatch(s_messageList[i]);
            }
            s_messageList.Clear();
        }

        if (s_statusList.Count > 0)
        {
            for (int i = 0; i < s_statusList.Count; i++)
            {
                Dispatch(s_statusList[i]);
            }
            s_statusList.Clear();
        }
    }

    #endregion
}

public delegate void MessageCallBack(NetWorkMessage receStr);
public delegate void ConnectStatusCallBack(NetworkState connectStstus);

public enum NetworkState
{
    Connected,
    Connecting,
    ConnectBreak,
    FaildToConnect,
}

public class NetWorkMessage
{
   public string m_MessageType;

   public Dictionary<string, object> m_data;
}

