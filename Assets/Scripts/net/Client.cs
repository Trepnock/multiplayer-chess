using System;
using System.Net;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.VisualScripting;
//using UnityEditor.MemoryProfiler;
using UnityEngine;

public class Client : MonoBehaviour
{
    public static Client Instance { get; set; }
    private void Awake()
    {
        Instance = this;
    }
    public NetworkDriver driver;
    private NetworkConnection connection;
    private bool isActive = false;

    public Action connectionDropped;

    public void Init(string ip, ushort port)
    {
        driver = NetworkDriver.Create();
        NetworkEndpoint endpoint = NetworkEndpoint.Parse(ip, port);//allows anyone to connect, is accept endpoint of (any Ipv4)

        connection = driver.Connect(endpoint);
        isActive = true;
        Debug.Log("got to here " + driver.Connect(endpoint));//how do we get confirmation of what the fuck is going on
        if(driver.Bound)
        {
            Debug.Log("the driver says it's bound, whatever that does for us");
        }
        else
        {
            Debug.Log("the driver isn't bound that's probably bad");
        }
            RegisterToEvent();
    }
    public void Shutdown()
    {
        if (isActive)
        {
            UnregisterToEvent();
            driver.Dispose();
            connection = default(NetworkConnection);
            isActive = false;
        }
    }
    public void OnDestroy()
    {
        Shutdown();
    }

    public void Update()
    {
        if (!isActive)
            return;
         
        driver.ScheduleUpdate().Complete();
        CheckAlive();
        UpdateMessagePump();//se we're binding but we're not getting a message
    }

    private void CheckAlive()
    {
        if (!connection.IsCreated && isActive)
        {
            Debug.Log("you lost connection to server");
            connectionDropped?.Invoke();
            Shutdown();
        }
    }


    private void UpdateMessagePump()
    {
        DataStreamReader stream;
        NetworkEvent.Type cmd;
        while ((cmd = connection.PopEvent(driver, out stream)) != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                Debug.Log("connect event");
                SendToServer(new NetWelcome());
            }
            else if (cmd == NetworkEvent.Type.Data)
            {
                Debug.Log("Data event");
                NetUtility.OnData(stream, default(NetworkConnection));
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                Debug.Log("disconnect event");
                Debug.Log("Client disconnected from server");
                connection = default(NetworkConnection);
                connectionDropped?.Invoke();
                Shutdown();
            }
        }
        
    }

    public void SendToServer (NetMessages msg)
    {
        Debug.Log("client sendtoserver");
        DataStreamWriter writer;
        driver.BeginSend(connection, out writer);
        Debug.Log("client sendtoserver Serialize");
        msg.Serialize(ref writer);
        Debug.Log("client sendtoserver end Serialize");
        driver.EndSend(writer);
        Debug.Log("client sendtoserver end");
    }

    private void RegisterToEvent()
    {
        NetUtility.C_KEEP_ALIVE += OnKeepAlive;
    }
    private void UnregisterToEvent()
    {
        NetUtility.C_KEEP_ALIVE -= OnKeepAlive;
    }
    private void OnKeepAlive(NetMessages nm)
    {
        SendToServer(nm);
    }
}
