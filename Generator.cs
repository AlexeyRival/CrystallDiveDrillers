using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Generator : NetworkBehaviour
{
    public int sizeX = 2;    
    public int sizeY = 2;
    public int sizeZ = 2;
    public GameObject platform,undestroyableground;
    public GameObject cluster;
    public ChunkManager manager;
    public static bool isWorking;
    public static Vector3 center;
    public List<Vector3> startpoints;
    public List<Vector3> cavepoints;
    public List<Vector3> tunnelpoints;
    public List<Resource> resources;
    private int generatedchunks = 0;
    public bool isGeneratingCompleted;
    private questtype currentquest;
    private int questtarget, questparam, questprogress;
    private string funnyname;
    [SyncVar]
    public SyncListInt resourcesCount;
    [SyncVar]
    public bool isPlatformStarted = false, isPlatformStopped;
    private Vector3 startpoint;
    [SyncVar]
    public int seed;
    [Command]
    public void CmdSetPlatformMoving(bool isStarted,bool isStopped) {
        isPlatformStarted = isStarted;
        isPlatformStopped = isStopped;
    }
    [Command]
    public void CmdAddResource(int id, int amout) {
        resourcesCount[id] += amout;
    }
    public void AddResource(int id, int amout) {
        CmdAddResource(id, amout);
    }
    // Start is called before the first frame update
    IEnumerator Start()
    {
        //resourcesCount = new SyncListInt();
        if (isServer) { 
            seed = Random.Range(0, int.MaxValue);
            for (int i = 0; i < resources.Count; ++i) { resourcesCount.Add(0); }
        }
        Random.seed = seed;
        center = new Vector3((sizeX) * 57 / 2, (sizeY) * 57 / 2, (sizeZ-1) * 57 / 2);
        startpoints = new List<Vector3>();
        cavepoints = new List<Vector3>();
        tunnelpoints = new List<Vector3>();
        for (int i = 0; i < 3; ++i)
        {
            Vector3 vec = new Vector3();
            vec.y = Random.Range(0, sizeY * 3) * 19 + 9;
            vec.x = Random.Range(0, sizeX * 3) * 19 + 9;
            vec.z = Random.Range(0, sizeZ * 3) * 19 + 9;
            cavepoints.Add(vec);

            Vector3 walker = vec;
            for (int ii = 0; ii < 25; ++ii)
            {
                if (Vector3.Distance(walker, center) < 13) { break; }
                    walker +=new Vector3(Random.Range(-3, 3), Random.Range(-3, 3), Random.Range(-3, 3)); 
                walker -= (walker-center) / 15;
                tunnelpoints.Add(walker);
            }
        }
        //квест
        funnyname = funnyA[Random.Range(0, funnyA.Length)] + " " + funnyB[Random.Range(0, funnyB.Length];
        currentquest = (questtype)Random.Range(0, 1);
        if (currentquest == questtype.Добыча) {
            questtarget = Random.Range(0, resources.Count);
            questparam = Random.Range(7, 10) * resources[questtarget].maxInBag;
        }
        yield return GenerateClusters();
    }
    IEnumerator GenerateClusters() {
        List<GameObject> objs = new List<GameObject>();
        GameObject ob;
        for (int x = 0; x < sizeX; ++x)
        {
            for (int y = 0; y < sizeY; ++y)
            {
                for (int z = 0; z < sizeZ; ++z)
                {
                    ob = Instantiate(cluster, new Vector3(x, y, z) * 57, Quaternion.identity);
                    for (int i = 0; i < ob.transform.childCount; ++i) {
                        objs.Add(ob.transform.GetChild(i).gameObject);
                    }
                    ++generatedchunks;
                    yield return new WaitForSeconds(0.2f);
                }
            }
        }
        isGeneratingCompleted = true;
        manager.transform.gameObject.SetActive(true);
        manager.chunks = objs.ToArray();
        manager.Recalculate();
    }
    public void StartPlatform() {
        try
        {
            startpoint = startpoints[Random.Range(0, startpoints.Count)];
        }
        catch { throw new System.Exception("BadSeedException"); }
        RaycastHit hit;
        Physics.Raycast(startpoint, - Vector3.up, out hit, 100);
        Debug.DrawRay(startpoint, - Vector3.up, Color.red, 100f);
        if (hit.transform)
        {
            startpoint= hit.point;
            Instantiate(undestroyableground,startpoint,Quaternion.identity);
            platform.transform.position = new Vector3(startpoint.x, platform.transform.position.y, startpoint.z);
            CmdSetPlatformMoving(true,false);
        }
        else {
            throw new System.Exception("BadSeedException");
        }
    }
    public void Update()
    {
        if (isPlatformStarted)
        {
            if (Vector3.Distance(platform.transform.position, startpoint) > 5f)
            {
                platform.transform.Translate(0, -4f * Time.deltaTime, 0);
            }
            else
            {
                CmdSetPlatformMoving(false, true);
            }
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        for (int i = 0; i < startpoints.Count; ++i) {
            Gizmos.DrawWireCube(startpoints[i], new Vector3(0.5f, 0.5f, 0.5f));
        }
        for (int i = 0; i < cavepoints.Count; ++i) {
            Gizmos.DrawWireSphere(cavepoints[i], 2);
        }
        Gizmos.color = new Color(0.8f, 0, 0.4f);
        for (int i = 0; i < tunnelpoints.Count; ++i) { 
            Gizmos.DrawWireSphere(tunnelpoints[i], 2);
        }
    }
    private int guishift;
    private void OnGUI()
    {
        if (!isGeneratingCompleted) {
            GUI.Box(new Rect(Screen.width * 0.5f - 150, Screen.height * 0.5f - 10, 300, 25), "Сгенерировано "+generatedchunks*9 + "/" + sizeX * sizeY * sizeZ * 9+" чанков");
        }
        guishift = 0;
        for (int i = 0; i < resourcesCount.Count; ++i) if (resourcesCount[i] != 0)
            {
                GUI.Box(new Rect(Screen.width -30, 200+guishift*30, 30, 30), resources[i].icon);
                GUI.Box(new Rect(Screen.width -60, 200+guishift*30, 30, 30), resourcesCount[i]+"");
                ++guishift;
            }
        //задание
        GUI.Box(new Rect(Screen.width - 400, 0, 400, 100), "");
        GUI.Box(new Rect(Screen.width - 300, 0, 300, 25), "Задание: " +currentquest);
        GUI.Box(new Rect(Screen.width - 300, 25, 300, 25), "" + funnyname);
    }
    public enum questtype { 
        Добыча
    }
    private readonly string[] funnyA = { "Зов", "Ужас", "Крик", "Поиск", "Защита" };
    private readonly string[] funnyB = { "Ужаса", "Вечности", "Пустоты", "Отчаяния", "Риска", "Власти" };
}
