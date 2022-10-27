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
    public Transform[] spawnpoints;
    public int[] dwarflevels=new int[8];
    public int[] dwarfxp=new int[8];
    public int[] dwarfeliteranks=new int[8];
    public int[] dwarfprestige=new int[8];
    public int accountlvl, accountxp;
    public GameObject classselector, blackscreen;
    public NetworkDiscovery networkDiscovery;

    private const string version = "p-0 D-1";
    //механизм защиты
    private string requiredpassword, password;
    private bool activated=true;

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
        if (QualitySettings.GetQualityLevel() > 2) 
        {
            FMODUnity.RuntimeManager.CoreSystem.setSoftwareChannels(32); 
        }
        else
        {
            FMODUnity.RuntimeManager.CoreSystem.setSoftwareChannels(16);
        }
        startPositions.Add(platformposition);
        try
        {
            StreamReader file = new StreamReader(Application.dataPath + "/save.cdddat");
            nickname = file.ReadLine();
            accountxp = int.Parse(file.ReadLine());
            accountlvl = int.Parse(file.ReadLine());
            for (int i = 0; i < dwarflevels.Length; ++i)
            {
                dwarfxp[i] = int.Parse(file.ReadLine());
                dwarflevels[i] = int.Parse(file.ReadLine());
                dwarfeliteranks[i] = int.Parse(file.ReadLine());
                dwarfprestige[i] = int.Parse(file.ReadLine());
            }
            file.Close();
        }
        catch
        {
            nickname = "Player" + Random.Range(0, 29032002);
            dwarflevels = new int[8];
            dwarfxp = new int[8];
            dwarfeliteranks = new int[8];
            dwarfprestige = new int[8];
        }
        characterclass = -1;
        requiredpassword = ""+ (System.DateTime.Now.DayOfYear * System.DateTime.Now.Hour * 618);
        char[] arr = requiredpassword.ToCharArray();
        System.Array.Reverse(arr);
        requiredpassword = new string(arr);
        print(requiredpassword);
        networkDiscovery.Initialize();
    }
    public void Save()
    {
        StreamWriter file = new StreamWriter(Application.dataPath + "/save.cdddat");
        file.WriteLine(nickname);
        file.WriteLine(accountxp);
        file.WriteLine(accountlvl);
        for (int i = 0; i < dwarflevels.Length; ++i)
        {
            file.WriteLine(dwarfxp[i]);
            file.WriteLine(dwarflevels[i]);
            file.WriteLine(dwarfeliteranks[i]);
            file.WriteLine(dwarfprestige[i]);
        }
        file.Close();
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
        player.transform.position = spawnpoints[conn.connectionId].position;
        NetworkServer.AddPlayerForConnection(conn, player, playerControllerId);
    }
    private string address;
    public string nickname="Playyyer";
    public int characterclass;
    public void SetClass(int id) {
        characterclass = id;
        blackscreen.SetActive(false);
        classselector.SetActive(false);
        player.xp = dwarfxp[id];
        player.level = dwarflevels[id];
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    public void OpenClassSelector() {
        classselector.SetActive(true);
        blackscreen.SetActive(true);
        for (int i = 0; i < dwarflevels.Length; ++i) {
            classselector.transform.GetChild(i).GetChild(1).GetComponent<Text>().text = dwarflevels[i] + " Lvl " + dwarfeliteranks[i] + " Elr " + dwarfprestige[i] + " Prl";
        }
    }
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
                Save();
                OpenClassSelector();
                networkDiscovery.broadcastData = nickname;
                networkDiscovery.StartAsServer();
            }
            if (GUI.Button(new Rect(Screen.width * 0.5f + 150, Screen.height * 0.5f, 50, 25), "LH")) { address = "127.0.0.1"; }
            if (GUI.Button(new Rect(Screen.width * 0.5f + 200, Screen.height * 0.5f, 50, 25), "RVL")) { address = "26.219.110.5"; }

            GUI.Box(new Rect(Screen.width * 0.5f - 400, Screen.height * 0.5f, 200, 200), "");
            if (!networkDiscovery.isClient) 
            {
                if (GUI.Button(new Rect(Screen.width * 0.5f - 400, Screen.height * 0.5f - 25, 200, 25), "Искать сервера")) { 
                    networkDiscovery.StartAsClient(); 
                }
            }
            else 
            {
                int i = 0;
                foreach (var b in networkDiscovery.broadcastsReceived)
                {
                    if (GUI.Button(new Rect(Screen.width * 0.5f - 400, Screen.height * 0.5f + i * 25, 200, 25),System.Text.Encoding.Unicode.GetString(b.Value.broadcastData))) { address = b.Value.serverAddress; }
                              ++i;
                }
            }
            

            if(address!="")if (GUI.Button(new Rect(Screen.width * 0.5f, Screen.height * 0.5f + 40, 125, 20), "Подключиться"))
            {
                transform.GetChild(0).gameObject.SetActive(false);
                singleton.networkAddress = address;
                singleton.networkPort = 7777;
                singleton.StartClient();
                Save();
                connected = true;
                OpenClassSelector();
                //networkDiscovery.st();
            }
            GUI.Box(new Rect(Screen.width - 200, Screen.height - 60, 200, 30), "vk.com/cdd_official");
            GUI.Box(new Rect(Screen.width - 200, Screen.height - 30, 200, 30), "ver " + version);
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
