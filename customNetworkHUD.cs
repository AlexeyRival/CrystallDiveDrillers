using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class customNetworkHUD : NetworkManager
{
    public bool isServer;
    private bool connected;
    public int progress;
    public bool isElite;
    public Transform platformposition;
    #region singleton
    public static customNetworkHUD Instanse { get; private set; }
    #endregion
    private void Awake()
    {
        singleton = this;
        Instanse = this;
    }
    private void Start()
    {
        startPositions.Add(platformposition);
        nickname = "Player" + Random.Range(0, 29032002);
    }
    public void SpawnAny(int index) {
        NetworkServer.Spawn(Instantiate(spawnPrefabs[index]));
    }
    public void Disconnect() {
        singleton.StopClient();
    }
    public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
    {
        GameObject player = Instantiate(playerPrefab);
        player.transform.position = platformposition.position;
        NetworkServer.AddPlayerForConnection(conn, player, playerControllerId);
    }
    private string address;
    public string nickname="Playyyer";
    private void OnGUI()
    {
        if (!connected)
        {
            GUI.Box(new Rect(Screen.width * 0.5f - 125, Screen.height * 0.5f, 50, 20), "IP:");
            address = GUI.TextField(new Rect(Screen.width * 0.5f - 75, Screen.height * 0.5f, 200, 20), address);
            GUI.Box(new Rect(Screen.width * 0.5f - 125, Screen.height * 0.5f + 20, 50, 20), "ИМЯ:");
            nickname = GUI.TextField(new Rect(Screen.width * 0.5f - 75, Screen.height * 0.5f + 20, 200, 20), nickname);
            if (GUI.Button(new Rect(Screen.width * 0.5f - 125, Screen.height * 0.5f + 40, 125, 20), "Создать"))
            {
                isServer = true;
                transform.GetChild(0).gameObject.SetActive(false);
                singleton.networkPort = 7777;
                singleton.StartHost();
                connected = true;
            }
            if (GUI.Button(new Rect(Screen.width * 0.5f, Screen.height * 0.5f + 40, 125, 20), "Подключиться"))
            {
                transform.GetChild(0).gameObject.SetActive(false);
                singleton.networkAddress = address;
                singleton.networkPort = 7777;
                singleton.StartClient();
                connected = true;
            }
            if (GUI.Button(new Rect(0, Screen.height - 20, 120, 20), "Выйти"))
            {
                Application.Quit();
            }
        }
        if (isServer)
        {
       //     GUI.Box(new Rect(Screen.width - 250, 0, 250, 25), "Это сервер");
    //        GUI.Box(new Rect(Screen.width * 0.5f - 150, Screen.height * 0.1f - 10, 300, 20), "Сервер работает. Зайдите с клиента на localhost");
          //  for (int p = 0; p < Players.Count; p++)
            {
              //  GUI.Box(new Rect(Screen.width - 250, 25 + 25 * p, 250, 25), Players[p].nick + "");
            }
        }
    }
}
