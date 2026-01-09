using System;
using TMPro;
using UnityEngine;


public enum CameraAngle
{
    menu,
    white,
    black
}
public class GameUI : MonoBehaviour
{
    [SerializeField] private Animator menuAnimator;
    [SerializeField] private TMP_InputField addressInput;
    [SerializeField] private GameObject[] cameraAngles;
    [SerializeField] private ushort port;

    public static GameUI Instance { set; get; }

    public Server server;
    public Client client;

    

    private void Awake()
    {
        Instance = this;
        RegisterEvents();
    }
    //camera
    public void ChangeCamera(CameraAngle index)
    {
        for (int i = 0; i < cameraAngles.Length; i++)
        {
            cameraAngles[i].SetActive(false);
        }
        cameraAngles[(int)index].SetActive(true);
    }
    //buttons
    public void OnLocalGameButton()
    {
        menuAnimator.SetTrigger("inGame");
        server.Init(port);
        client.Init("127.0.0.1", port);//8469
    }
    public void OnOnlineGameButton()
    {
        menuAnimator.SetTrigger("startMenu");
    }

    public void OnOnlineHostButton ()
    {
        server.Init(port);
        client.Init("127.0.0.1", port);
        menuAnimator.SetTrigger("OnlineMenu");
    }

    public void OnOnlineConnectButton()
    {
        Debug.Log($"attempting to connect to {addressInput.text}");
        client.Init(addressInput.text, port);
    }
    public void OnOnlineBackButton()
    {
        menuAnimator.SetTrigger("baseStateTrigger");
    }
    public void OnHostBackButton()
    {
        menuAnimator.SetTrigger("startMenu");
        server.Shutdown();
        client.Shutdown();
    }
    private void RegisterEvents()
    {
        NetUtility.C_START_GAME += OnStartGameClient;
    }
    private void UnRegisterEvents()
    {
        NetUtility.C_START_GAME -= OnStartGameClient;
    }
    private void OnStartGameClient(NetMessages messages)
    {
        menuAnimator.SetTrigger("inGame");
    }
}
