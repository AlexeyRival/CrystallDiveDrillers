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
    public GameObject platform, undestroyableground, blackscreen;
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
    public int generatedpoints = 0;
    private int generatingphase = 0;
    public bool isGeneratingCompleted, isStart;
    private questtype currentquest;
    private int questtarget, questparam;
    private string funnyname;
    private int addingresourcecount, addingresourceid;
    private bool isaddingresource;

    public characterclass[] classes;

    //UI
    public GameObject UI,UIInventory, Currentmission;
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
        mule.GetComponent<mule>().SetPath(GetPath(mule.transform.position,newpos));
    }
    public List<Vector3> GetPath(Vector3 start,Vector3 end) {
        pathendpoint = start;
        pathstartpoint = end;
        List<Vector3> bufferpath = CalculatePath();
        bufferpath.RemoveAt(0);
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
                UIInventory.transform.GetChild(i).GetChild(1).GetComponent<Text>().text = resourcesCount[i].ToString();
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

        print(resault);
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
    public List<Vector3> CalculatePoint(marchingspace ms,walkpoint point, Vector3 target)
    {
        List<Vector3> outlist = new List<Vector3>();
        //if (FastDist(point.position,target,4)) {
        if (point.position == target) {
            outlist.Add(point.position);
            return outlist;
        }
        for (int i = 0; i < point.friends.Count; ++i)
        {
            if(ms.walkpoints.ContainsKey(point.friends[i]))if (ms.walkpoints[point.friends[i]].weight < point.weight)
            {
                outlist = CalculatePoint(ms, ms.walkpoints[point.friends[i]], target);
                if (outlist.Count != 0) {
                    outlist.Add(point.position);
                    return outlist;
                }
            }
        }

            for (int i = 0; i < ms.friends.Count; ++i) {
            for (int ii = 0; ii < marchingspace.neighborsTable.Length; ++ii)
            {
                if (ms.friends[i].walkpoints.ContainsKey(point.position + marchingspace.neighborsTable[ii]))
                {
                    if (ms.friends[i].walkpoints[point.position + marchingspace.neighborsTable[ii]].weight < point.weight)
                    {
                        outlist = CalculatePoint(ms.friends[i], ms.friends[i].walkpoints[point.position + marchingspace.neighborsTable[ii]], target);
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
    public class walkpoint {
        public Vector3 position;
        public List<Vector3> friends;
        public float weight;
        public bool isBorder;
        public walkpoint(Vector3 position,bool isBorder) {
            this.position = position;
            friends = new List<Vector3>();
            this.isBorder = isBorder;
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
        center = new Vector3((sizeX) * 57 / 2, (sizeY) * 57 / 2, (sizeZ-1) * 57 / 2);
        //walkpoints = new List<walkpoint>();
        walkpointscount = 0;
        generatedpoints = 0;
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
        yield return GenerateClusters();
        yield return new WaitForEndOfFrame();
        generatingphase = 1;
    //    if(isServer) yield return CalculateFriends();
        isGeneratingCompleted = true;
        blackscreen.SetActive(false);
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
                        ob = Instantiate(cluster.transform.GetChild(i).gameObject, new Vector3(x, y, z) * 57 + cluster.transform.GetChild(i).transform.position, Quaternion.identity);
                        ob.transform.name = $"{x}:{y}:{z}:{i}";
                        objs.Add(ob);
                        ++generatedchunks;
                        yield return new WaitForEndOfFrame();//new WaitForSeconds(0.2f);
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
        bool isQuestCompeted = GetQuestStatus();
        if (isQuestCompeted)
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
                return generatedchunks / (sizeX * sizeY * sizeZ * 27f) * 0.9f;
        }
        else { 
            return generatedchunks / (sizeX * sizeY * sizeZ * 27f);
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
            CloseMission(isFailure); 
            deleteitalllocal = deleteitall;
            for (int i = 0; i < GameObject.FindGameObjectsWithTag("Cluster").Length; ++i) {
                Destroy(GameObject.FindGameObjectsWithTag("Cluster")[i]);
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
