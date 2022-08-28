using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class Generator : NetworkBehaviour
{
    public customNetworkHUD network;
    public int sizeX = 2;    
    public int sizeY = 2;
    public int sizeZ = 2;
    public GameObject platform, planet, hub, blackscreen;
    public GameObject cluster;
    public GameObject mule, mulemarker;
    public ChunkManager manager;
    public static bool isWorking;
    //public Texture3D biomemap;
    public Texture3D biomemap;
    public Material biommat;
    //вектора всякие полезные
    public static Vector3 center;
    public List<Vector3> startpoints;
    public List<Vector3> bugspawnpoints;
    public List<Vector3> cavepoints;
    public List<Vector3> tunnelpoints;
    public List<Vector3> orepoints;

    public List<Resource> resources;
    private int generatedchunks = 0;
    public int generatedpoints = 0;
    private int generatingphase = 0;
    public bool isGeneratingCompleted, isStart;
    private questtype currentquest;
    private int questtarget, questparam;
    private string funnyname;
    private int addingresourcecount, addingresourceid;
    private bool isaddingresource;

    public characterclass[] classes;

    //музыка
    public AudioSource music;
    public AudioClip m_endtrack;


    //UI
    public GameObject UI,UIInventory, Currentmission,UICompleted;
    public RawImage resourcePic;
    public Image missionpic;
    public Image missiondifpic;
    public Text missionName;
    public Text missiontype;
    public GameObject loading;
    private int localseed;

    //загрузка
    private float[] playersprogress;

    //кароче это типа ВАЩЕ ТАКАЯ ТЕМА ОФИГЕННАЯ КАРОЧЕ В ЧЁМ СУТЬ 
    //
    //начисление кароче ОПЫТА
    public GameObject endmission;
    public GameObject UIaccountlevel;
    public Sprite[] levelprogress;
    private int targetxp;

    //поиск пути
    //public List<walkpoint> walkpoints;
    public ComputeShader turbopath;
    public TurboMarching.Walkpoint[] walkpoints;
    public int walkpointscount=0;
    public Vector3 pathendpoint,pathstartpoint;
    public List<int> path;

    //выбор миссии
    //public bool isMissionMenuOpened;
    public MissionMenuController missioncontroller;
    [SyncVar]
    public int questdifficulty;
    [SyncVar]
    private bool deleteitall;
    private bool deleteitalllocal;
    private Vector3 startplatfromposition;
    [SyncVar]
    private bool isFailure=false;
    private bool isQuestCompete,isQuestCompleteLocal;

    //дела отладочные
    private bool isShowDebugPathFinding;
    
    //сеть
    [SyncVar]
    public SyncListInt resourcesCount;
    [SyncVar]
    public int platformstatus = 0;
    //public bool isPlatformStarted = false, isPlatformStopped,isPlatformBack;
    private Vector3 startpoint;
    [SyncVar]
    public int seed;
    [SyncVar]
    public bool isStartGenerate;
    [Command]
    public void CmdSetPlarformStatus(int status) {
        platformstatus = status;
    }//0 - платформа на базе, 1 - платформа едет вниз, 2 - платформа стоит ждёт, 3 - платформа едет наверх
    [Command]
    public void CmdSetFailure(bool value) {
        isFailure = value;
    }
    public void CmdMoveMuleMarker(Vector3 newpos) {
        mulemarker.transform.position = newpos;
        //pathendpoint = mule.transform.position;//newpos;//
        //pathstartpoint = newpos; //mule.transform.position;//
        //List<Vector3> bufferpath = CalculatePath();
        //mule.GetComponent<mule>().SetPath(GetPath(mule.transform.position,newpos));
        mule.GetComponent<mule>().SetPath(GetNeoPath(mule.transform.position,newpos));
    }
    public List<Vector3> GetPath(Vector3 start,Vector3 end) {
        pathendpoint = start;
        pathstartpoint = end;
        List<Vector3> bufferpath = CalculatePath();
        bufferpath.RemoveAt(0);
        return bufferpath;
    }
    public List<Vector3> GetFastPath(Vector3 start, Vector3 end)
    {
        pathendpoint = start;
        pathstartpoint = end;
        //List<Vector3> bufferpath = CalculateFastPath();
        List<Vector3> bufferpath = CalculateUltraPath();
        if (bufferpath.Count > 0)
        {
            bufferpath.RemoveAt(0);
            bufferpath.Add(end);
        }
        return bufferpath;
    }
    public List<Vector3> GetNeoPath(Vector3 start, Vector3 end) 
    {
        pathendpoint = start;
        pathstartpoint = end;
        List<Vector3> bufferpath = CalculateNeoPath();
        if (bufferpath.Count > 0)
        {
            bufferpath.RemoveAt(0);
            bufferpath.Add(end);
        }
        return bufferpath;
    }
    [Command]
    public void CmdAddResource(int id, int amout) {
        resourcesCount[id] += amout;
    }
    [Command]
    public void CmdSetSeed(int newseed) {
        seed = newseed;
    }
    [Command]
    public void CmdSwitchDelete() {
        deleteitall = !deleteitall;
      //  walkpoints = new List<walkpoint>();
    }
    [Command]
    public void CmdClearResources() {
        for (int i = 0; i < resources.Count; ++i)
        {
            resourcesCount[i] = 0;
        }
    }
    public void AddResource(int id, int amout) {
        if(platformstatus==2)CmdAddResource(id, amout);
    }
    
    public void MoveMuleMarker(Vector3 newpos) {
        CmdMoveMuleMarker(newpos); 
    }
    private Dictionary<string, float> revivePoints;
    [SyncVar]
    private float lastreviver;
    //возрождение и всё что с ним связано
    public void AddRevivePoints(string name,float amout) {
        if (!revivePoints.ContainsKey(name)) { print(name); }
        revivePoints[name] += amout;
        lastreviver = revivePoints[name];
    }
    public float GetRevivePoints(string name) {
        return lastreviver;
    }
    public bool GetAlive(string name) {
        return revivePoints[name] >= 100;
    }
    public void AddDeathPlayer(string name) {
        if (!revivePoints.ContainsKey(name))
        {
            revivePoints.Add(name, 0);
        }
        else {
            revivePoints[name] = 0;
        }
    }
    [Command]
    public void CmdSetDifficulty(int value) {
        questdifficulty = value;
    }
    public void SetDifficulty(int value) {
        if (isServer) { CmdSetDifficulty(value); }
    }
    private void UpdateResources()
    {
        if (platformstatus == 0) { UIInventory.SetActive(false);return; }
        UIInventory.SetActive(true);
        int xshift;
        int activecount = 0;
        for (int i = 0; i < resourcesCount.Count; ++i)
        {
            UIInventory.transform.GetChild(i).gameObject.SetActive(resourcesCount[i] > 0);
            if (resourcesCount[i] > 0)
            {
                UIInventory.transform.GetChild(i).GetChild(3).GetComponent<Text>().text = resourcesCount[i].ToString();
                ++activecount;
            }
        }
        int iter = 0;
        for (int i = 0; i < resourcesCount.Count; ++i)
        {
            if (resourcesCount[i] > 0)
            {
                UIInventory.transform.GetChild(i).transform.localPosition = new Vector3(0,iter * -40, 0);
                ++iter;
            }
        }
    }
    private void UpdateAccountLevel() {
        UIaccountlevel.SetActive(true);
        UIaccountlevel.transform.Find("levelpic").GetComponent<Image>().sprite = levelprogress[network.accountxp];
        UIaccountlevel.transform.Find("level").GetComponent<Text>().text = network.accountlvl.ToString();
    }
    private void ActualiseMission()
    {
        isQuestCompete = GetQuestStatus();
        UIaccountlevel.SetActive(false);
        Currentmission.SetActive(true);
        Currentmission.transform.Find("Name").GetComponent<Text>().text = funnyname;
        Currentmission.transform.Find("Type Pic").GetComponent<Image>().sprite = missioncontroller.missiontypes[(int)currentquest].icon;
        Currentmission.transform.Find("Difficulty Pic").GetComponent<Image>().sprite = missioncontroller.difficulties[questdifficulty].icon;
        
        if (currentquest == questtype.Добыча) { Currentmission.transform.Find("Progress").GetComponent<Text>().text = resources[questtarget].materialName + " - " + resourcesCount[questtarget] + "/" + questparam; }
    }
    private void CloseMission(bool fail) {
        targetxp = 0;
        int xp_resource = 0;
        int xp_kills = 0;
        int xp_firstquest = 0;
        int xp_secondquest = 0;
        for (int i = 0; i < resourcesCount.Count; ++i) {
            xp_resource += resourcesCount[i];
        }
        if (currentquest == questtype.Добыча) {
            xp_resource += resourcesCount[questtarget]*2;
            if (resourcesCount[questtarget] >= questparam) {
                xp_firstquest = 2500;
            }
        }

        xp_kills += player.thisplayer.kills;

        xp_resource = (int)(xp_resource*missioncontroller.difficulties[questdifficulty].bonus * 0.01f);
        xp_kills= (int)(xp_kills * missioncontroller.difficulties[questdifficulty].bonus * 0.01f);
        xp_firstquest = (int)(xp_firstquest*missioncontroller.difficulties[questdifficulty].bonus * 0.01f);
        xp_secondquest = (int)(xp_secondquest*missioncontroller.difficulties[questdifficulty].bonus * 0.01f);
        
        if (fail)
        {
            xp_resource = 0;
            xp_kills /= 10;
            xp_firstquest /= 10;
            xp_secondquest = 0;
        }

        targetxp = xp_resource + xp_kills + xp_firstquest + xp_secondquest;


        endmission.transform.Find("resources").GetComponent<Text>().text=xp_resource+"XP";
        endmission.transform.Find("kills").GetComponent<Text>().text=xp_kills + "XP";
        endmission.transform.Find("firstquest").GetComponent<Text>().text=xp_firstquest + "XP";
        endmission.transform.Find("secondquest").GetComponent<Text>().text=xp_secondquest + "XP";
        endmission.transform.Find("all").GetComponent<Text>().text=targetxp+"XP";
        endmission.transform.Find("Level").GetComponent<Text>().text = "" + player.level;

        blackscreen.SetActive(true);
        endmission.SetActive(true);
        Currentmission.SetActive(false);
        UpdateAccountLevel();
    }
    public List<Vector3> CalculatePath() {
        List<Vector3> outpath = new List<Vector3>();
        marchingspace endchunk=manager.marchingspaces[0];
        Vector3 thispathendpoint=pathendpoint;

        bool grandbreakend=false,grandbreakstart=false;
        bool grandbreak = false;
        Dictionary<string, bool> isCalculated = new Dictionary<string, bool>();
        Vector3 resault = new Vector3(-1, -1, -1);
        for (int i = 0; i < manager.marchingspaces.Length; ++i) {
            manager.marchingspaces[i].ClearWeights();
        }
        for (int i = 0; i < manager.marchingspaces.Length; ++i)
        {
            foreach (var point in manager.marchingspaces[i].walkpoints) {
                //if (Vector3.Distance(point.Key, pathendpoint)<2) {
                if (FastDist(point.Key, pathendpoint, 4)) {
                    isCalculated[manager.marchingspaces[i].name] = true;
                    point.Value.weight = 1;
                    resault = manager.marchingspaces[i].CalculateWeights(pathstartpoint);
                    thispathendpoint = point.Key;
                    endchunk = manager.marchingspaces[i];
                    grandbreak = true;
                    Debug.DrawLine(point.Key, pathendpoint, new Color(0.6f, 0.2f, 0), 10f);
                    break;
                }
            }
            if (grandbreak) { break; }
        }
        int updatedchunks = 0,previosupdated=-1;
        grandbreak = false;
        if(resault==new Vector3(-1,-1,-1))for (int k = 0; k < sizeX * 10; ++k) {//должно хватить
            updatedchunks = 0;
            for (int i = 0; i < manager.marchingspaces.Length; ++i) if(isCalculated.ContainsKey(manager.marchingspaces[i].name)){
                        for (int ii = 0; ii < manager.marchingspaces[i].friends.Count; ++ii) if (!isCalculated.ContainsKey(manager.marchingspaces[i].friends[ii].name))
                            {
                                isCalculated[manager.marchingspaces[i].friends[ii].name] = true;
                                resault = manager.marchingspaces[i].friends[ii].CalculateWeights(pathstartpoint);
                                ++updatedchunks;
                                if (resault != new Vector3(-1, -1, -1))
                                {
                                    endchunk = manager.marchingspaces[i].friends[ii];
                                    grandbreak = true; break;
                                }
                            }
                    if (grandbreak) { break; }
            }
            if (previosupdated == updatedchunks||grandbreak) { break; }
            previosupdated = updatedchunks;
        }

     //   print(resault);
        Debug.DrawRay(resault, Vector3.up, Color.white, 10f);
        Debug.DrawRay(thispathendpoint, Vector3.up, Color.grey, 10f);
        outpath = CalculatePoint(endchunk, endchunk.walkpoints[resault], thispathendpoint);
        Debug.DrawLine(endchunk.center, resault, Color.cyan, 10f);
        if (outpath.Count>0) {
            for (int i = 0; i < outpath.Count; ++i) {
                Debug.DrawRay(outpath[i],Vector3.up,Color.magenta,10f);
            }
        }
        else
        {
            print(":(");
        }
        return outpath;
    
    }
    public List<Vector3> CalculateUltraPath() {
        List<Vector3> outpath = new List<Vector3>();
        marchingspace endchunk = manager.marchingspaces[0];
        Vector3 thispathendpoint = pathendpoint;

        bool grandbreakend = false, grandbreakstart = false;
        bool grandbreak = false;
        Dictionary<string, bool> isCalculated = new Dictionary<string, bool>();
        Vector3 resault = new Vector3(-1, -1, -1);
        double time = Time.realtimeSinceStartup;
        for (int i = 0; i < manager.marchingspaces.Length; ++i)
        {
            manager.marchingspaces[i].ClearWeights();
            manager.marchingspaces[i].isChecked = false;
        }
        for (int i = 0; i < manager.marchingspaces.Length; ++i)
        {
            foreach (var point in manager.marchingspaces[i].walkpoints)
            {
                if (FastDist(point.Key, pathendpoint, 4))
                {
                    isCalculated[manager.marchingspaces[i].name] = true;
                    point.Value.weight = 1;
                    resault = manager.marchingspaces[i].CalculateWeightsFast(pathstartpoint);
                    thispathendpoint = point.Key;
                    endchunk = manager.marchingspaces[i];
                    grandbreak = true;
                    Debug.DrawLine(point.Key, pathendpoint, new Color(0.6f, 0.2f, 0), 10f);
                    break;
                }
            }
            if (grandbreak) { break; }
        }

        if (resault == new Vector3(-1, -1, -1))
        {
            List<marchingspace> buflist = new List<marchingspace>();
            List<marchingspace> secondbuflist = new List<marchingspace>();
            for (int i = 0; i < manager.marchingspaces.Length; ++i) 
            {
                if (IsMSContainPoint(manager.marchingspaces[i], pathstartpoint)) 
                {
                    manager.marchingspaces[i].weight = 0;
                    buflist.Add(manager.marchingspaces[i]);
                    break;
                }
            }
            for (int k = 0; k < sizeX * 27 * 2; ++k)
            {
                for (int i = 0; i < buflist.Count; ++i)
                {
                    for (int ii = 0; ii < buflist[i].friends.Count; ++ii) if (buflist[i].friends[ii].weight == 9999)
                        {
                            buflist[i].friends[ii].weight = buflist[i].weight + 1;
                            secondbuflist.Add(buflist[i].friends[ii]);
                        }
                }
                buflist = secondbuflist;
                secondbuflist = new List<marchingspace>();
                if (buflist.Count == 0) { break; }
            }
            List<marchingspace> mslist = GetMSChainShort(endchunk, pathstartpoint);
            mslist.Reverse();

            for (int i = 0; i < mslist.Count; ++i)
            {
                resault = mslist[i].CalculateWeightsFast(pathstartpoint);
                if (resault != new Vector3(-1, -1, -1))
                {
                    endchunk = mslist[i];
                    break;
                }
            }
        }
        outpath = CalculatePoint(endchunk, endchunk.walkpoints[resault], thispathendpoint);
        return outpath;
    }
    public List<Vector3> CalculateNeoPath() 
    {
        List<Vector3> outpath = new List<Vector3>();

        //распределение следа

        int _kernelindex = turbopath.FindKernel("Clear");
        ComputeBuffer navbuffer = new ComputeBuffer(walkpoints.Length, sizeof(float) * 4 + sizeof(int));
        navbuffer.SetData(walkpoints);
        turbopath.SetBuffer(_kernelindex, "points", navbuffer);
        int numThreadsPerAxis = Mathf.CeilToInt(walkpoints.Length / (float)8);
        turbopath.Dispatch(_kernelindex, numThreadsPerAxis, 1, 1);
        _kernelindex = turbopath.FindKernel("Splat");
        turbopath.SetVector("startpoint", startpoint - transform.position);
        turbopath.SetBuffer(_kernelindex, "points", navbuffer);
        turbopath.Dispatch(_kernelindex, numThreadsPerAxis, 1, 1);
        _kernelindex = turbopath.FindKernel("Set");
        turbopath.SetBuffer(_kernelindex, "points", navbuffer);
        for (int i = 0; i < 100; ++i)
        {
            turbopath.SetInt("iter", i);
            turbopath.Dispatch(_kernelindex, numThreadsPerAxis, 1, 1);
        }
        print("есть обработка!");
        for (int i = 0; i < 10; ++i) { print(walkpoints[Random.Range(0, walkpoints.Length)].iter); }
        walkpoints = new TurboMarching.Walkpoint[walkpoints.Length];
        navbuffer.GetData(walkpoints, 0, 0, walkpoints.Length);
        navbuffer.Release();


        return outpath;
    }
    public List<Vector3> CalculateNeoPathOld() {
        List<Vector3> outpath = new List<Vector3>();
        TurboMarching origin=null;
        for (int i = 0; i < manager.turboMarchings.Length; ++i) {
            if (FastDist(manager.turboMarchings[i].center, pathstartpoint, 25)) 
            {
                origin = manager.turboMarchings[i];
            }
        }
        if(origin!=null)origin.SetNavigation(pathstartpoint);
        return outpath;
    }
    public void UpdateWalkGroup() {
        List<TurboMarching.Walkpoint> wpslist = new List<TurboMarching.Walkpoint>(2048);
        for (int i = 0; i < manager.turboMarchings.Length; ++i) 
        {
            wpslist.AddRange(manager.turboMarchings[i].walkpoints);
        }
        walkpoints = wpslist.ToArray();
    }
    public List<Vector3> CalculatePoint(marchingspace ms,walkpoint point, Vector3 target)
    {
        int _i, _ii;
        List<Vector3> outlist = new List<Vector3>();
        //if (FastDist(point.position,target,4)) {
        if (point.position == target) {
            outlist.Add(point.position);
            return outlist;
        }
        for (_i = 0; _i < point.friends.Count; ++_i)
        {
            if(ms.walkpoints.ContainsKey(point.friends[_i]))if (ms.walkpoints[point.friends[_i]].weight < point.weight)
            {
                outlist = CalculatePoint(ms, ms.walkpoints[point.friends[_i]], target);
                if (outlist.Count != 0) {
                    outlist.Add(point.position);
                    return outlist;
                }
            }
        }
            for (_i = 0; _i < ms.friends.Count; ++_i) {
            for (_ii = 0; _ii < marchingspace.neighborsTable.Length; ++_ii)
            {
                if (ms.friends[_i].walkpoints.ContainsKey(point.position + marchingspace.neighborsTable[_ii]))
                {
                    if (ms.friends[_i].walkpoints[point.position + marchingspace.neighborsTable[_ii]].weight < point.weight)
                    {
                        outlist = CalculatePoint(ms.friends[_i], ms.friends[_i].walkpoints[point.position + marchingspace.neighborsTable[_ii]], target);
                        if (outlist.Count != 0)
                        {
                            outlist.Add(point.position);
                            return outlist;
                        }
                    }
                }
            }
            
        }
        return outlist;
    }
    public void SetMSWeights(marchingspace ms) {
        for (int i = 0; i < ms.friends.Count; ++i) {
            if (ms.friends[i].weight != 0) { ms.weight = ms.friends[i].weight + 1; }
        }
    }
    public bool IsMSContainPoint(marchingspace ms, Vector3 target)
    {
        return (!((target.x < ms.transform.position.x || target.x > ms.transform.position.x + ms.sizeX) || (target.y < ms.transform.position.y || target.y > ms.transform.position.y + ms.sizeY) || (target.z < ms.transform.position.z || target.z > ms.transform.position.z + ms.sizeZ)));
    }
    public List<marchingspace> GetMSChain(marchingspace ms, Vector3 target) {
        List<marchingspace> mslist = new List<marchingspace>();
        ms.isChecked = true;
        if (!((target.x < ms.transform.position.x || target.x > ms.transform.position.x + ms.sizeX) || (target.y < ms.transform.position.y || target.y > ms.transform.position.y + ms.sizeY) || (target.z < ms.transform.position.z || target.z > ms.transform.position.z + ms.sizeZ))) 
        {
            Debug.DrawLine(target, ms.center, Color.cyan,30f);
            mslist.Add(ms);
            return mslist;
        }
        for (int i = 0; i < ms.friends.Count; ++i) if(!ms.friends[i].isChecked){
            mslist = GetMSChain(ms.friends[i],target);
            if (mslist.Count != 0) {
                Debug.DrawLine(ms.friends[i].center, ms.center, Color.blue, 30f);
                mslist.Add(ms);
                return mslist;
            }
        }
        return mslist;
    }
    public List<marchingspace> GetMSChainShort(marchingspace ms, Vector3 target) {
        List<marchingspace> mslist = new List<marchingspace>();
        ms.isChecked = true;
        if (ms.weight==0) 
        {
            Debug.DrawLine(target, ms.center, Color.cyan,30f);
            mslist.Add(ms);
            return mslist;
        }
        for (int i = 0; i < ms.friends.Count; ++i) if(ms.friends[i].weight<ms.weight){
            mslist = GetMSChain(ms.friends[i],target);
            if (mslist.Count != 0) {
                mslist.Add(ms);
                return mslist;
            }
        }
        return mslist;
    }
    public class walkpoint {
        public Vector3 position;
        public List<Vector3> friends;
        public float weight;
        public bool isBorder;
        public List<pointneighbor> pointneighbors;
        public walkpoint(Vector3 position,bool isBorder) {
            this.position = position;
            friends = new List<Vector3>();
            this.isBorder = isBorder;
            pointneighbors = new List<pointneighbor>();
        }
    }
    public struct pointneighbor 
    {
        public int friendchunkid;
        public Vector3 pointid;
        public pointneighbor(int friendchunkid, Vector3 pointid)
        {
            this.friendchunkid = friendchunkid;
            this.pointid = pointid;
        }
    }



    public void StartMission()
    {
        isStartGenerate = true;
    }
        IEnumerator Generate()
        {
        while (!isStart) { yield return new WaitForEndOfFrame(); }
        blackscreen.SetActive(true);
        loading.SetActive(true);
        //resourcesCount = new SyncListInt();
        if (isServer) {
            //seed = Random.Range(0, int.MaxValue);
            //seed = inputseed;
        }
        Random.seed = seed;
        funnyname = funnyA[Random.Range(0, funnyA.Length)] + " " + funnyB[Random.Range(0, funnyB.Length)];
        currentquest = (questtype)Random.Range(0, 1);
        if (currentquest == questtype.Добыча)
        {
            questtarget = Random.Range(0, resources.Count);
            questparam = Random.Range(2, 3) * resources[questtarget].maxInBag;//7,10
        }
        center = new Vector3((sizeX) * 58.5f / 2, (sizeY) * 58.5f / 2, (sizeZ) * 58.5f / 2);
        print(center);
        Debug.DrawRay(center, Vector3.up * 100f, Color.cyan, 30f);
        //walkpoints = new List<walkpoint>();
        walkpointscount = 0;
        generatedpoints = 0;
        path = new List<int>();
        startpoints = new List<Vector3>();
        bugspawnpoints = new List<Vector3>();
        orepoints = new List<Vector3>();
        cavepoints = new List<Vector3>();
        tunnelpoints = new List<Vector3>();
        Vector3 walker;
        Vector3 d_vec=new Vector3();

        float randomshake = 0.5f, stepsize = 1f;

        for (int i = 0; i < 3; ++i)
        {
            Vector3 vec = new Vector3();
            vec.y = Random.Range(1, sizeY * 3-1) * 19 + 9;
            vec.x = Random.Range(1, sizeX * 3-1) * 19 + 9;
            vec.z = Random.Range(1, sizeZ * 3-1) * 19 + 9;
            cavepoints.Add(vec);

            walker = vec;
            for (int ii = 0; ii < 50; ++ii)
            {
                d_vec = new Vector3();
                if (Vector3.Distance(walker, center) < 13) { break; }
                    walker +=new Vector3(Random.Range(-randomshake, randomshake), Random.Range(-randomshake, randomshake), Random.Range(-randomshake, randomshake));
                //walker -= (walker-center) / 15;

                if (walker.x > center.x) { d_vec.x = -stepsize; }
                if (walker.x < center.x) { d_vec.x = stepsize; }
                if (walker.y > center.y) { d_vec.y = -stepsize; }
                if (walker.y < center.y) { d_vec.y = stepsize; }
                if (walker.z > center.z) { d_vec.z = -stepsize; }
                if (walker.z < center.z) { d_vec.z = stepsize; }
                walker += d_vec;
                tunnelpoints.Add(walker);
            }
        }
        for (int i = 0; i < 4; ++i)
        {
            Vector3 secondcave = new Vector3(Random.Range(1, sizeX * 6 - 1) * 10 + 9, Random.Range(1, sizeY * 6 - 1) * 10 + 9, Random.Range(1, sizeZ * 6 - 1) * 10 + 9);
            int buftrgt = Random.Range(0, cavepoints.Count);
            cavepoints.Add(secondcave);
            walker = secondcave;
            float count = 50 + (Vector3.Distance(secondcave, cavepoints[buftrgt]) - 50) / 3;
            for (int ii = 0; ii < count; ++ii)
            {
                d_vec = new Vector3();
                if (Vector3.Distance(walker, cavepoints[buftrgt]) < 4) { break; }
                walker += new Vector3(Random.Range(-randomshake, randomshake), Random.Range(-randomshake, randomshake), Random.Range(-randomshake, randomshake));
                //walker -= (walker - cavepoints[buftrgt]) / 15;
                if (walker.x > center.x) { d_vec.x = -stepsize; }
                if (walker.x < center.x) { d_vec.x = stepsize; }
                if (walker.y > center.y) { d_vec.y = -stepsize; }
                if (walker.y < center.y) { d_vec.y = stepsize; }
                if (walker.z > center.z) { d_vec.z = -stepsize; }
                if (walker.z < center.z) { d_vec.z = stepsize; }
                walker += d_vec;
                tunnelpoints.Add(walker);
            }
        }
        yield return GenerateClusters();
        yield return GenerateOres();
        yield return new WaitForEndOfFrame();
        generatingphase = 1;
    //    if(isServer) yield return CalculateFriends();
        isGeneratingCompleted = true;
       // blackscreen.SetActive(false);
        loading.SetActive(false);
    }
    IEnumerator Start()
    {
        revivePoints = new Dictionary<string, float>();
        if (isServer)
        {
            for (int i = 0; i < resources.Count; ++i) { resourcesCount.Add(0); }
        }
        while (true) {
            if (!isStart)
            {
                yield return Generate();
            }
            else {
                yield return new WaitForEndOfFrame();
            }
        }
    }
    //помимо руды ещё и биомнорудную карту заполняет
    IEnumerator GenerateOres() {
        float f;
        int ind;
        FastNoiseLite noise = new FastNoiseLite();
        noise.SetSeed(seed);
        biomemap = new Texture3D(300, 120, 300, TextureFormat.RGB24, false);//TODO подогнать размеры под размер карты
        Color[] biomearray = new Color[biomemap.width * biomemap.height * biomemap.depth];
        for (int i = 0; i < orepoints.Count; ++i) {
            f = noise.GetNoise(orepoints[i].x*0.05f, orepoints[i].y * 0.05f, orepoints[i].z * 0.05f) +1f;
            f *= 100;
            ind = (int)f;
            int id = ind % (resources.Count);
            for (int ii = 0; ii < resources[id].maxInVein; ++ii) if (Random.Range(0, ii) < resources[id].maxInVein *0.75f)
                {
                    Vector3 vec = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
                    for (int ix = -resources[id].brushRadius; ix < resources[id].brushRadius; ++ix) for (int iy = -resources[id].brushRadius; iy < resources[id].brushRadius; ++iy)for (int iz = -resources[id].brushRadius; iz < resources[id].brushRadius; ++iz)
                        {
                                biomearray[(int)(orepoints[i].x + vec.x + ix) + (int)(orepoints[i].y + vec.y + iy) * biomemap.width + (int)(orepoints[i].z + vec.z + iz) * biomemap.height * biomemap.depth] = resources[ind % (resources.Count)].color * ((resources[id].brushRadius * 3 - (Mathf.Abs(ix) + Mathf.Abs(iy) + Mathf.Abs(iz))) / (resources[id].brushRadius * 2));
                        }//Instantiate(resources[ind % (resources.Count)].maskPrefab, orepoints[i] + vec, Quaternion.identity, manager.chunks[0].transform);
                    Instantiate(resources[id].orePrefab, orepoints[i] + vec, Quaternion.Euler(Random.Range(0, 360), Random.Range(0, 360), Random.Range(0, 360)), manager.chunks[0].transform).name = "" + resources[id].id;
                    
                }
        }
        for (int i = 0; i < 500; ++i) {
            biomearray[Random.Range(0, biomearray.Length)] = new Color(Random.Range(0f,1f),Random.Range(0f,1f),Random.Range(0f,1f));
        }
        biomemap.SetPixels(biomearray);
        biomemap.Apply();
        Material mat = new Material(biommat);
        mat.SetTexture("_biomeTex", biomemap);
        mat.name = "ырлы!";
        //biommat.SetTexture("3D",biomemap);
        for (int i = 0; i < manager.turboMarchings.Length; ++i) 
        {
            manager.turboMarchings[i].gameObject.GetComponent<Renderer>().material = mat;
        }
        yield return new WaitForEndOfFrame();
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
                    /*ob = Instantiate(cluster, new Vector3(x, y, z) * 57, Quaternion.identity);
                    for (int i = 0; i < ob.transform.childCount; ++i) {
                        ob.transform.GetChild(i).gameObject.name = $"{x}:{y}:{z}:{i}";
                        objs.Add(ob.transform.GetChild(i).gameObject);
                    }*/
                    for (int i = 0; i < cluster.transform.childCount; ++i)
                    {
                        //ob = Instantiate(cluster.transform.GetChild(i).gameObject, new Vector3(x, y, z) * 58.5f + cluster.transform.GetChild(i).transform.position, Quaternion.identity);
                        ob = Instantiate(cluster.transform.GetChild(i).gameObject, new Vector3(x, y, z) * 58.5f + new Vector3((i)/6/6,(i)/6%6, (i) % 6)*9.75f, Quaternion.identity);
                        ob.transform.name = $"{x}:{y}:{z}:{i}";
                        if (manager.TURBOMODE) { ob.GetComponent<TurboMarching>().generator = this; }
                        objs.Add(ob);
                        ++generatedchunks;//new WaitForSeconds(0.2f);
                        if(i%4==0)yield return new WaitForEndOfFrame();
                    }
                }
            }
        }
        manager.transform.gameObject.SetActive(true);
        manager.chunks = objs.ToArray();
        manager.Recalculate();
    }
    public void StartPlatform() {
        startplatfromposition = platform.transform.position;
        int spid = Random.Range(0, startpoints.Count);
            startpoint = startpoints[spid];
        mule.transform.position = startpoints[(spid + Random.Range(1, startpoints.Count)) % startpoints.Count];
        RaycastHit hit;
        Physics.Raycast(startpoint, - Vector3.up, out hit, 100);
        Debug.DrawRay(startpoint, - Vector3.up, Color.red, 100f);
        if (hit.transform)
        {
            startpoint= hit.point;
       //     Instantiate(undestroyableground,startpoint,Quaternion.identity);
            platform.transform.position = new Vector3(startpoint.x, platform.transform.position.y, startpoint.z);
            CmdSetPlarformStatus(1);
        }
        CmdClearResources();
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
        
        if (isQuestCompete)
        {
            CmdSetFailure(false);
            CmdSetPlarformStatus(3);
        }
    }
    public void GoBackFailure() {
        CmdSetFailure(true);
        CmdSetPlarformStatus(3);
    }
    public bool GetQuestStatus()
    {
        if (currentquest == questtype.Добыча)
        {
            if (resourcesCount[questtarget] >= questparam) { return true; }
        }
        return false;
    }
    public float GetLoadingStatus() {
        if (isServer)
        {
            //print(generatedchunks +"/"+ (sizeX * sizeY * sizeZ * 21f));
                return generatedchunks / (sizeX * sizeY * sizeZ * 216f) * 0.9f;
        }
        else { 
            return generatedchunks / (sizeX * sizeY * sizeZ * 216f);
        }
    }
    public void OrganizePlayersOnLoading() {
        playersprogress = new float[player.players.Count];
        GameObject ob;
        float shift= (playersprogress.Length * 150 / 2);
        for (int i = 0; i < playersprogress.Length; ++i) {
            ob = loading.transform.GetChild(1).GetChild(i).gameObject;
            ob.SetActive(true);
            ob.transform.Translate(shift, 0, 0);
            ob.transform.GetChild(1).GetComponent<Text>().text = player.players[i].nickname;
            try
            {
                ob.GetComponent<Image>().sprite = classes[player.players[i].characterclass].icon;
            }
            catch {
                print(player.players[i].characterclass);
            }
        }
    }
    public void Update()
    {
        if (isStartGenerate) {
            if (!isStart) {
                isStart = true;
                planet.SetActive(false);
                hub.SetActive(false);
                OrganizePlayersOnLoading();
            }
            int playersready = 0;
            for (int i = 0; i < playersprogress.Length; ++i) {
                playersprogress[i] = player.players[i].loadingstatus;
                if (playersprogress[i] == 1f) { ++playersready; }
            }

            if (isGeneratingCompleted&&playersready==playersprogress.Length) {
                if (isServer) {
                    StartPlatform();
                }
                isStartGenerate = false;//ЕСЛИ ВСЁ СЛОМАЕТСЯ Я ПОДВИНУЛ ВОТ ТУТ
            }
        }
        if (platformstatus!=0)
        {
            UI.SetActive(false);
            if (platformstatus == 1)
            {
                if (Vector3.Distance(platform.transform.position, startpoint) > 5f)
                {
                    platform.transform.Translate(0, -4f * Time.deltaTime, 0);
                }
                else
                {
                    CmdSetPlarformStatus(2);
                }
            }
            else if (platformstatus == 3)
            {
                if (platform.transform.position.y < 100f)
                {
                    platform.transform.Translate(0, 6f * Time.deltaTime, 0);
                }
                else
                {
                    CmdSetPlarformStatus(0);
                    UIaccountlevel.SetActive(true);
                    if (isServer) { CmdSwitchDelete(); platform.transform.position = startplatfromposition; }
                }
            }
            else if (platformstatus == 2) {
                ActualiseMission();
                if (isQuestCompleteLocal != isQuestCompete) {
                    isQuestCompleteLocal = isQuestCompete;
                    UICompleted.SetActive(true);
                    music.PlayOneShot(m_endtrack);
                }
                int diecount = 0;
                for (int i = 0; i < player.players.Count;++i) {
                    if (player.players[i].hp <= 0) { ++diecount; }
                }
                if (player.isDead && player.players.Count == diecount)
                {
                    GoBackFailure();
                }
            }
            UpdateResources();
            if (isFailure)
            {
                blackscreen.SetActive(true);
            }
        }
        if (platformstatus == 0) {
            if (seed != 0) {
                if (seed!=localseed) {
                    UI.SetActive(true);
                    localseed = seed;
                    Random.seed = seed;
                    funnyname = funnyA[Random.Range(0, funnyA.Length)] + " " + funnyB[Random.Range(0, funnyB.Length)];
                    currentquest = (questtype)Random.Range(0, 1);
                    if (currentquest == questtype.Добыча)
                    {
                        questtarget = Random.Range(0, resources.Count);
                        questparam = Random.Range(2, 3) * resources[questtarget].maxInBag;//7,10
                    }
                    resourcePic.texture = resources[questtarget].icon;
                    missionName.text = funnyname;
                    missiontype.text = currentquest.ToString();
                    missionpic.sprite = missioncontroller.missiontypes[(int)currentquest].icon;
                    missiondifpic.sprite = missioncontroller.difficulties[questdifficulty].icon;
                }
            }
        }
        if (deleteitall != deleteitalllocal) {
            UICompleted.SetActive(false);
            CloseMission(isFailure); 
            deleteitalllocal = deleteitall;
            planet.SetActive(true);
            hub.SetActive(true);
            for (int i = 0; i < GameObject.FindGameObjectsWithTag("Chunk").Length; ++i) {
                Destroy(GameObject.FindGameObjectsWithTag("Chunk")[i]);
            }
            for (int i = 0; i < GameObject.FindGameObjectsWithTag("Bug").Length; ++i) {
                Destroy(GameObject.FindGameObjectsWithTag("Bug")[i]);
                NetworkServer.Destroy(GameObject.FindGameObjectsWithTag("Bug")[i]);
            }
            isStart = false;
            isStartGenerate = false;
            isGeneratingCompleted = false;
            manager.transform.gameObject.SetActive(false);
            generatedchunks = 0;
            generatingphase = 0;
            //if (isServer) { CmdClearResources(); }
        }
        if (isaddingresource) {
            CmdAddResource(addingresourceid, addingresourcecount);
            isaddingresource = false;
        }
        if (player.dwarfclass != -1&&platformstatus==0)
        {
            UpdateAccountLevel();
        }
        if (targetxp > 0) {
            if (isFailure) {
                targetxp = 15;
            }
            if (targetxp > 40000)
            {
                player.xp+=500;
                targetxp-=500;
            }else
            if (targetxp > 5000)
            {
                player.xp+=40;
                targetxp-=40;
            }else
            if (targetxp > 200)
            {
                player.xp+=5;
                targetxp-=5;
            }
            else
            {
                ++player.xp;
                --targetxp;
            }
            endmission.transform.Find("xp").GetComponent<Text>().text = player.xp+"/" + player.levels[player.level].ToString();
            endmission.GetComponent<Slider>().value =((player.xp * 1f)/player.levels[player.level]);
            if (player.xp >= player.levels[player.level]&&player.level!=24)
            {
                player.xp -= player.levels[player.level];
                ++player.level;
                endmission.transform.Find("Level").GetComponent<Text>().text = ""+player.level;
                ++network.accountxp;
                if (network.accountxp == 6)
                {
                    ++network.accountlvl;
                    network.accountxp = 0;
                }
                UpdateAccountLevel();
            }
        }
        if (Input.GetKeyDown(KeyCode.Space)&&targetxp==0) {
            if (endmission.active) {
                endmission.SetActive(false);
                blackscreen.SetActive(false);
                network.dwarfxp[player.dwarfclass] = player.xp;
                network.dwarflevels[player.dwarfclass] = player.level;
                network.Save();
                CmdSetFailure(false);
            }
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
           // for (int i = 0; i < walkpoints.Count; ++i)
            {
            //    if (walkpoints[i].weight != 0)
                {
            //        Gizmos.color = new Color(0.4f, 0.8f*0.01f*walkpoints[i].weight, 0);
                }
           //     Gizmos.DrawCube(walkpoints[i].position, new Vector3(0.5f, 0.5f, 0.5f));
                Gizmos.color = new Color(0, 0.4f, 0.8f);
            }
            Gizmos.color = new Color(0.6f, 0, 0.6f);
            for (int i = 0; i < path.Count; ++i)
            {
             //   Gizmos.DrawCube(walkpoints[path[i]].position, new Vector3(0.6f, 0.6f, 0.6f));
            }
            Gizmos.color = new Color(0.4f, 0.8f,0);
            Gizmos.DrawCube(pathstartpoint, new Vector3(0.5f, 0.5f, 0.5f));
            Gizmos.color = new Color( 0.8f, 0.4f,0);
            Gizmos.DrawCube(pathendpoint, new Vector3(0.5f, 0.5f, 0.5f));
        }
    }
    private void OnGUI()
    {
        if (isStart)
        {
            if (!isGeneratingCompleted)
            {
                    loading.GetComponent<Slider>().value = GetLoadingStatus();
            }
            for (int i = 0; i < playersprogress.Length; ++i)
            {
                loading.transform.GetChild(1).GetChild(i).GetComponent<Slider>().value = playersprogress[i];
            }

        }
    }
    public enum questtype { 
        Добыча
    }
    public static readonly string[] funnyA = { "Зов", "Ужас", "Крик", "Поиск", "Защита","Предвосхищение","Ожидание","Стремление" };
    public static readonly string[] funnyB = { "Ужаса", "Вечности", "Пустоты", "Отчаяния", "Риска", "Власти","Наживы","Удачи","Восторга","Жизни" };

    public static bool FastDist(Vector3 a, Vector3 b, float squaredistance)
    {
        return (a - b).sqrMagnitude < squaredistance;
    }
}
