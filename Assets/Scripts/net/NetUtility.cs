using System;
using System.Diagnostics;
using System.Reflection.Emit;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;


public enum OpCode 
{
    KEEP_ALIVE,
    WELCOME,
    START_GAME,
    MAKE_MOVE,
    REMATCH
}
public class NetUtility
{
    public static void OnData (DataStreamReader stream, NetworkConnection cnn, Server server = null)
    {
        NetMessages msg = null;
        var opCode = (OpCode)stream.ReadByte();//check first byte for the opcode
        switch (opCode)
        {
            case OpCode.KEEP_ALIVE: msg = new NetKeepAlive(stream); UnityEngine.Debug.Log("KeepAlive"); break;
            case OpCode.WELCOME: msg = new NetWelcome(stream); UnityEngine.Debug.Log("Welcome"); break;
            case OpCode.START_GAME: msg = new NetStartGame(stream); UnityEngine.Debug.Log("StartGame"); break;
            //case OpCode.MAKE_MOVE: msg = new NetMakeMove(stream); break;
            //case OpCode.REMATCH: msg = new NetRematch(stream); break;
            default:
                UnityEngine.Debug.LogError("Message recieved with no OpCode");
                break;
        }

        if (server != null)
            msg.ReceivedOnServer(cnn);
        else
            msg.ReceivedOnClient();
    }

    public static Action<NetMessages> C_KEEP_ALIVE;
    public static Action<NetMessages> C_WELCOME;
    public static Action<NetMessages> C_START_GAME;
    public static Action<NetMessages> C_MAKE_MOVE;
    public static Action<NetMessages> C_REMATCH;
    public static Action<NetMessages, NetworkConnection> S_KEEP_ALIVE;//server cares who sent the message
    public static Action<NetMessages, NetworkConnection> S_WELCOME;
    public static Action<NetMessages, NetworkConnection> S_START_GAME;
    public static Action<NetMessages, NetworkConnection> S_MAKE_MOVE;
    public static Action<NetMessages, NetworkConnection> S_REMATCH;
}
