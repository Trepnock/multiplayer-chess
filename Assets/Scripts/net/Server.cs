using System;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.VisualScripting;
using UnityEngine;
using static Unity.Burst.Intrinsics.X86.Avx;

public class Server : MonoBehaviour
{
    public static Server Instance { get; set; }
    private void Awake ()
    {
        Instance = this;
    }

    public NetworkDriver driver;
    private NativeList<NetworkConnection> connections;

    private bool isActive = false;
    private const float keepAliveTickRate = 20.0f;
    private float lastKeepAlive; //prevent timeout, probably at 30s

    public Action connectionDropped;

    public void Init(ushort port)
    {
        driver = NetworkDriver.Create();
        NetworkEndpoint endpoint = NetworkEndpoint.AnyIpv4;//allows anyone to connect, is accept endpoint of (any Ipv4)
        endpoint.Port = port;

        if(driver.Bind(endpoint) != 0)
        {
            Debug.Log("unable to bind on port " + endpoint.Port);
            return;
        }
        else
        {
            driver.Listen();
            isActive = true;
            if (driver.Listening)
                Debug.Log("Currently listening on port " + endpoint.Port);
        }

        connections = new NativeList<NetworkConnection>(2, Allocator.Persistent);
    }
    public void Shutdown()
    {
        if (isActive)
        {
            driver.Dispose();
            connections.Dispose();
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
        KeepAlive();
        //Debug.Log("I'm screaming into the fucking void");

        driver.ScheduleUpdate().Complete();//moving this after update pump changes how many checkpoint 0s I get
        CleanupConnections();
        AcceptNewConnections();
        UpdateMessagePump();

        /*CleanupConnections();//testing order
        AcceptNewConnections();
        driver.ScheduleUpdate().Complete();
        UpdateMessagePump();*/

    }

    private void KeepAlive()
    {
        if (Time.time - lastKeepAlive > keepAliveTickRate)
        {
            lastKeepAlive = Time.time;
            Broadcast(new NetKeepAlive());
        }
    }
    private void CleanupConnections()
    {
        for (int i = 0; i < connections.Length; i++)
        {
            if (!connections[i].IsCreated)
            {
                connections.RemoveAtSwapBack(i);
                --i;
            }
        }
    }
    private void AcceptNewConnections()
    {
        //Debug.Log("accepting new connections");//will happen on update so just a short term check
        NetworkConnection c;
        //ACCEPT() "accepts any new incoming connections" returns networkconnection,
        ///*so I'm creating variable c, 
        ///to hold whatever connection driver.Accept lets in, 
        ///and as long as it's not defaul/empty/whatever, I add it to my list
        ///

        while ((c = driver.Accept()) != default(NetworkConnection))//why the fuck is this a while not an if? or a foreach? hell, what does it even do?
        {
            connections.Add(c);
            Debug.Log("got a live one");//happening twice?
        }
    }
    private void UpdateMessagePump()
    {
        DataStreamReader stream;
        /*for (int i = 0; i <= connections.Length; i++)
        {
            Debug.Log("i is " + i);
            Debug.Log(connections[i].IsCreated);
            NetworkEvent.Type cmd;
            while ((cmd = driver.PopEventForConnection(connections[i], out stream)) != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Data)
                {
                    NetUtility.OnData(stream, connections[i], this);
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client disconnected from server");
                    connections[i] = default(NetworkConnection);
                    connectionDropped?.Invoke();
                    Shutdown();//only because a 2 person game, no need to go on if 1 drops
                }
            }
        }*/
        int i = -1;
        foreach (var connection in connections)
        {
            Debug.Log("checkpoint 0");
            i++;
            NetworkEvent.Type cmd;

            while ((cmd = driver.PopEventForConnection(connection, out stream)) != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Data)
                {
                    Debug.Log("checkpoint 1");
                    NetUtility.OnData(stream, connection, this);
                    Debug.Log("checkpoint 2");
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client disconnected from server");
                    //connections[i] = default(NetworkConnection);
                    connection.Disconnect(driver);
                    connectionDropped?.Invoke();
                    Shutdown();//only because a 2 person game, no need to go on if 1 drops
                }
            }
        }
    }

    public void Broadcast(NetMessages msg)
    {
        for (int i = 0; connections.Length > i; i++)
        {
            if (connections[i].IsCreated)
            {
                //Debug.Log($"Sending {msg.Code} to : {connections[i].InternalID}");
                SendToClient(connections[i], msg);
            }
        }
    }
    public void SendToClient(NetworkConnection conn, NetMessages msg)
    {
        DataStreamWriter writer;
        driver.BeginSend(conn, out writer);
        msg.Serialize(ref writer);
        driver.EndSend(writer);
    }
}
