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
    public GameObject mule, mulemarker;
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
    private int addingresourcecount, addingresourceid;
    private bool isaddingresource;
    //поиск пути
    public List<walkpoint> walkpoints;
    public Vector3 pathendpoint,pathstartpoint;
    public List<int> path;

    //дела отладочные
    private bool isShowDebugPathFinding;
    
    //сеть
    [SyncVar]
    public bool DropPhase = false;
    [SyncVar]
    public SyncListInt resourcesCount;
    [SyncVar]
    public bool isPlatformStarted = false, isPlatformStopped,isPlatformBack;
    private Vector3 startpoint;
    [SyncVar]
    public int seed;
    [Command]
    public void CmdSetPlatformMoving(bool isStarted,bool isStopped,bool isBack) {
        isPlatformStarted = isStarted;
        isPlatformStopped = isStopped;
        isPlatformBack = isBack;
    }
    public void CmdMoveMuleMarker(Vector3 newpos) {
        mulemarker.transform.position = newpos;
        pathendpoint = newpos;
        pathstartpoint = mule.transform.position;
        List<Vector3> bufferpath = CalculatePath();
        mule.GetComponent<mule>().SetPath(bufferpath);
    }
    [Command]
    public void CmdSetDropPhase(bool phase) {
        DropPhase = phase;
    }
    [Command]
    public void CmdAddResource(int id, int amout) {
        resourcesCount[id] += amout;
    }
    public void AddResource(int id, int amout) {
        CmdAddResource(id, amout);
    }
    
    public void MoveMuleMarker(Vector3 newpos) {
        CmdMoveMuleMarker(newpos); 
    }

    private List<int> walkpointspool;
    private List<Vector3> CalculatePath()
    {
        walkpointspool = new List<int>();
        for (int i = 0; i < walkpoints.Count; ++i)
        {
            if (Vector3.Distance(walkpoints[i].position, pathendpoint) < 2f)
            {
                walkpoints[i].weight = Vector3.Distance(walkpoints[i].position, pathendpoint);
                walkpointspool.AddRange(walkpoints[i].friends);
            }
            else {
                walkpoints[i].weight = 0;
            }
        }
        bool grandbreak=false;
        int finallid = 0;
        for (int k = 0; k < 200; ++k)
        {
            List<int> buffrepool = walkpointspool;
            walkpointspool = new List<int>();
            for (int i = 0; i < buffrepool.Count; ++i)
            {
                if (Vector3.Distance(walkpoints[buffrepool[i]].position, pathstartpoint) < 2f) { finallid = buffrepool[i]; grandbreak = true;break; }
                if (walkpoints[buffrepool[i]].weight != 0) {
                    CheckFriends(walkpoints[buffrepool[i]]);
                }
            }
            if (grandbreak) break;
        }
        path = new List<int>();
        CalculatePathList(walkpoints[finallid]);
        List<Vector3> outputpath = new List<Vector3>();
        for (int i = 0; i < path.Count; ++i) { outputpath.Add(walkpoints[path[i]].position); }
        return outputpath;
    }
    private float min;
    public bool CalculatePathList(walkpoint point) {
        if (point.weight < 2f) { return true; }
        for (int i = 0; i < point.friends.Count; ++i) {
            if (walkpoints[point.friends[i]].weight < point.weight) {
                path.Add(point.friends[i]);
                if (CalculatePathList(walkpoints[point.friends[i]])) { return true; }
            }
        }
        return false;
    }
    public void CheckFriends(walkpoint point)
    {
        for (int i = 0; i < point.friends.Count; ++i) if (walkpoints[point.friends[i]].weight == 0)
            {
                walkpoints[point.friends[i]].weight = point.weight+ Vector3.Distance(point.position, walkpoints[point.friends[i]].position);
                walkpointspool.AddRange(walkpoints[point.friends[i]].friends);
            }
    }

    void CalculateFriends() {
        for (int i = 0; i < walkpoints.Count; ++i) {
            for (int ii = 0; ii < walkpoints.Count; ++ii) if (i != ii) {
                    if (Vector3.Distance(walkpoints[i].position, walkpoints[ii].position) < 1.5f) {
                        walkpoints[i].friends.Add(ii);
                    }
                }
        }
    }

    public class walkpoint {
        public Vector3 position;
        public List<int> friends;
        public float weight;
        public bool isBlocked;
        public walkpoint(Vector3 position) {
            this.position = position;
            friends = new List<int>();
        }
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
        walkpoints = new List<walkpoint>();
        path = new List<int>();
        startpoints = new List<Vector3>();
        cavepoints = new List<Vector3>();
        tunnelpoints = new List<Vector3>();
        Vector3 walker;
        for (int i = 0; i < 3; ++i)
        {
            Vector3 vec = new Vector3();
            vec.y = Random.Range(1, sizeY * 3-1) * 19 + 9;
            vec.x = Random.Range(1, sizeX * 3-1) * 19 + 9;
            vec.z = Random.Range(1, sizeZ * 3-1) * 19 + 9;
            cavepoints.Add(vec);

            walker = vec;
            for (int ii = 0; ii < 25; ++ii)
            {
                if (Vector3.Distance(walker, center) < 13) { break; }
                    walker +=new Vector3(Random.Range(-2, 2), Random.Range(-2, 2), Random.Range(-2, 2)); 
                walker -= (walker-center) / 15;
                tunnelpoints.Add(walker);
            }
        }
        for (int i = 0; i < 4; ++i)
        {
            Vector3 secondcave = new Vector3(Random.Range(1, sizeX * 3 - 1) * 19 + 9, Random.Range(1, sizeY * 3 - 1) * 19 + 9, Random.Range(1, sizeZ * 3 - 1) * 19 + 9);
            int buftrgt = Random.Range(0, cavepoints.Count);
            cavepoints.Add(secondcave);
            walker = secondcave;
            for (int ii = 0; ii < 30+ (Vector3.Distance(secondcave, cavepoints[buftrgt])-50)/3; ++ii)
            {
                if (Vector3.Distance(walker, cavepoints[buftrgt]) < 4) { break; }
                walker += new Vector3(Random.Range(-2, 2), Random.Range(-1, 1), Random.Range(-2, 2));
                walker -= (walker - cavepoints[buftrgt]) / 15;
                tunnelpoints.Add(walker);
            }
        }
        //квест
        funnyname = funnyA[Random.Range(0, funnyA.Length)] + " " + funnyB[Random.Range(0, funnyB.Length)];
        currentquest = (questtype)Random.Range(0, 1);
        if (currentquest == questtype.Добыча) {
            questtarget = Random.Range(0, resources.Count);
            questparam = Random.Range(2, 3) * resources[questtarget].maxInBag;//7,10
        }
        yield return GenerateClusters();
        CalculateFriends();
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
        CmdSetDropPhase(true);
        try
        {
            startpoint = startpoints[Random.Range(0, startpoints.Count)];
            mule.transform.position = startpoints[Random.Range(0, startpoints.Count)];
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
            CmdSetPlatformMoving(true,false,false);
        }
        Physics.Raycast(mule.transform.position, - Vector3.up, out hit, 100);
        Debug.DrawRay(mule.transform.position, - Vector3.up, Color.red, 100f);
        if (hit.transform)
        {
            mule.transform.position = hit.point;
        }
        else {
            throw new System.Exception("BadSeedException");
        }
    }
    public void GoBack() {
        bool isQuestCompeted = GetQuestStatus();
        if (isQuestCompeted) {
            CmdSetPlatformMoving(true, false, true);
        }
    }
    public bool GetQuestStatus()
    {
        if (currentquest == questtype.Добыча)
        {
            if (resourcesCount[questtarget] >= questparam) { return true; }
        }
        return false;
    }
    public void Update()
    {
        if (isPlatformStarted)
        {
            if (!isPlatformBack)
            {
                if (Vector3.Distance(platform.transform.position, startpoint) > 5f)
                {
                    platform.transform.Translate(0, -4f * Time.deltaTime, 0);
                }
                else
                {
                    CmdSetPlatformMoving(false, true, false);
                }
            }
            else
            {
                if (Vector3.Distance(platform.transform.position, startpoint) < 100f)
                {
                    platform.transform.Translate(0, 6f * Time.deltaTime, 0);
                }
                else
                {
                    CmdSetPlatformMoving(false, true, false);
                }
            }
        }
        if (isaddingresource) {
            CmdAddResource(addingresourceid, addingresourcecount);
            isaddingresource = false;
        }
        if (Input.GetKeyDown(KeyCode.F3)) { isShowDebugPathFinding = !isShowDebugPathFinding; }
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
        if (isShowDebugPathFinding) {
            Gizmos.color = new Color(0, 0.4f, 0.8f);
            for (int i = 0; i < walkpoints.Count; ++i)
            {
                if (walkpoints[i].weight != 0)
                {
                    Gizmos.color = new Color(0.4f, 0.8f*0.01f*walkpoints[i].weight, 0);
                }
                Gizmos.DrawCube(walkpoints[i].position, new Vector3(0.5f, 0.5f, 0.5f));
                Gizmos.color = new Color(0, 0.4f, 0.8f);
            }
            Gizmos.color = new Color(0.6f, 0, 0.6f);
            for (int i = 0; i < path.Count; ++i)
            {
                Gizmos.DrawCube(walkpoints[path[i]].position, new Vector3(0.6f, 0.6f, 0.6f));
            }
            Gizmos.color = new Color(0.4f, 0.8f,0);
            Gizmos.DrawCube(pathstartpoint, new Vector3(0.5f, 0.5f, 0.5f));
            Gizmos.color = new Color( 0.8f, 0.4f,0);
            Gizmos.DrawCube(pathendpoint, new Vector3(0.5f, 0.5f, 0.5f));
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
        if (currentquest == questtype.Добыча) { 
            GUI.Box(new Rect(Screen.width - 300, 50, 300, 25), "Добудьте " + resources[questtarget].materialName);
            GUI.Box(new Rect(Screen.width - 300, 75, 300, 25), resourcesCount[questtarget] + "/" + questparam);
            GUI.Box(new Rect(Screen.width - 400, 0, 100, 100), resources[questtarget].icon);
        }
    }
    public enum questtype { 
        Добыча
    }
    private readonly string[] funnyA = { "Зов", "Ужас", "Крик", "Поиск", "Защита" };
    private readonly string[] funnyB = { "Ужаса", "Вечности", "Пустоты", "Отчаяния", "Риска", "Власти" };
}
