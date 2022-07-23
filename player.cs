using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class player : NetworkBehaviour
{
    public GameObject head;
    public AudioSource Audio;
    public float speed = 75f;
    public GameObject sphereDestroyer,flare,death;
    public TextMesh hpobject;
    private Rigidbody rb;
    private Vector3 mousedelta;
    private RaycastHit hit;
    public List<Resource> resources;
    public Animator animator;
    private Generator generator;
    private MissionMenuController missionMenuController;
    private bool isTeleported = false;
    private float attackcooldown = 0;
    private float flarecooldown = 0;
    private int localhp = 101;
    private float magnitude;
    private bool seeContainer, seeInteraction, gobackinteraction, missionselectorinteraction;
    private float containercooldown = 0;
    private GameObject targetobject;
    private bool truestart = false;

    //смерть
    private GameObject mydeath;
    private bool deathInteraction;
    private bool isDead;

    //щиты
    private int shield=100;
    private float shieldcooldown;

    //инвентарь и всё прочее
    [SyncVar]
    public int characterclass=-1;
    public characterclass[] classes;
    public GameObject UIobject,UIInventory;
    public Text UIhp,UIshld;
    public Slider hpbar, shldbar, flarebar;
    public Image classpic;
    public GameObject expscreen;

    //другие игроки
    public GameObject UIPlayerPrefab,UIPlayersbase;
    private static List<player> players;
    private int updatetimer;
    private int localclass=-1;
    
    private GameObject[] allitems;
    [SyncVar]
    public bool isDropToContainer;
    [SyncVar]
    public int hp = 100;
    [SyncVar]
    public string nickname="Player";
    [SyncVar]
    public SyncListInt resourcesCount;
    // Start is called before the first frame update

    [Command]
    void CmdSpawnDestroyer(Vector3 pos) {
        GameObject ob= Instantiate(sphereDestroyer, pos, Quaternion.identity);
        Destroy(ob, 0.1f);
        NetworkServer.Spawn(ob);
    }
    [Command]
    void CmdMoveMarker(Vector3 pos) {
        generator.MoveMuleMarker(pos);
    }
    [Command]
    void CmdSpawnDeath() {
        GameObject ob = Instantiate(death, transform.position, transform.rotation);
        mydeath = ob;
        ob.transform.name = nickname;
        NetworkServer.Spawn(ob);
        generator.AddDeathPlayer(nickname);
    }
    [Command]
    void CmdAddRevivePoints(string name,float amout) {
        generator.AddRevivePoints(name, amout);
    }
    [Command]
    void CmdTryRevive()
    {
        if (generator.GetAlive(nickname)) {
            NetworkServer.Destroy(mydeath);
            hp = 100;
            generator.AddRevivePoints(nickname, -100);
        }
    }
    [Command]
    void CmdSpawnFlare() {
        GameObject ob= Instantiate(flare, head.transform.position, head.transform.rotation);
        ob.GetComponent<Rigidbody>().AddRelativeForce(0,0,20,ForceMode.Impulse);
        Destroy(ob, 30f);
        NetworkServer.Spawn(ob);
    }
    [Command]
    void CmdAddResource(int id,int amout) {
        resourcesCount[id]+=amout;
        if(!isLocalPlayer)if (amout < 0) {
            generator.AddResource(id, -amout);
        }
    }
    [Command]
    void CmdStartMission() {
        generator.StartMission();
        //generator.StartPlatform();
    }
    [Command]
    void CmdEndMission() {
        generator.GoBack();
    }
    [Command]
    void CmdSetNick(string nick) {
        nickname = nick;
    }
    [Command]
    void CmdSetClass(int characterclass) {
        this.characterclass = characterclass;
    }
    [Command]
    void CmdDmg(int dmg) {
        hp = hp-dmg;
    }
    [Command]
    void CmdIsDrop(bool isdrop) {
        isDropToContainer = isdrop;
    }
    public void Dmg(int dmg) {
        if (dmg-shield < 10)
        {
            shield -= dmg;
            if (shield < 0) shield = 0;
        }
        else {
            shield = 0;
            CmdDmg(dmg-shield);
        }
        shieldcooldown = 4f;
    }
    private bool isClear = false;
    private void Clear() {
        for (int i = 0; i < resourcesCount.Count; ++i) {
            if (resourcesCount[i] > 0) { CmdAddResource(i, -resourcesCount[i]); }
        }
        CmdDmg(-(100 - hp));
        isClear = true;
    }
    public void Attack()
    {
            Physics.Raycast(head.transform.position, head.transform.forward, out hit, 6);
            if (hit.transform)
            {
                CmdSpawnDestroyer(hit.point);
            }
    }
    private void AddPlayer(player player,int ix) {
       GameObject ui = Instantiate(UIPlayerPrefab, UIPlayersbase.transform.position, UIPlayersbase.transform.rotation, UIPlayersbase.transform);
        ui.name = player.netId.ToString();
        ui.transform.Translate(ix * 90, 0, 0);
        ui.transform.Find("Name").GetComponent<Text>().text = player.nickname;
        ui.transform.Find("HPbar").GetComponent<Slider>().value = player.hp*0.01f;
        if(player.characterclass!=-1)ui.GetComponent<Image>().sprite = classes[player.characterclass].icon;
    }
    private void UpdatePlayer(player player) {
        GameObject ui = UIPlayersbase.transform.Find(player.netId.ToString()).gameObject;
        if (ui)
        {
            ui.transform.Find("HPbar").GetComponent<Slider>().value = player.hp * 0.01f;
            ui.transform.Find("Name").GetComponent<Text>().text = player.nickname;
            ui.GetComponent<Image>().sprite = classes[player.characterclass].icon;
        }
    }
    private void UpdateResources() {
        int xshift;
        int activecount=0;
        for (int i = 0; i < resourcesCount.Count; ++i) {
            UIInventory.transform.GetChild(i).gameObject.SetActive(resourcesCount[i] > 0);
            if (resourcesCount[i] > 0)
            {
                UIInventory.transform.GetChild(i).GetChild(1).GetComponent<Text>().text = resourcesCount[i].ToString();
                ++activecount;
            }
        }
        xshift = (activecount / 2)*-50;
        int iter = 0;
        for (int i = 0; i < resourcesCount.Count; ++i)
        {
            if (resourcesCount[i] > 0)
            {
                UIInventory.transform.GetChild(i).transform.localPosition = new Vector3(xshift + iter * 60, 0, 0);
                ++iter;
            }
        }
    }
        private void Start()
    {
        UIPlayersbase = GameObject.Find("Canvas").transform.Find("UI").transform.Find("PlayersBase").gameObject;
        if (isServer) for (int i = 0; i < resources.Count; ++i) { resourcesCount.Add(0); }
        generator = GameObject.Find("ChungGenerator").GetComponent<Generator>();
        missionMenuController = GameObject.Find("MissionMenuController").GetComponent<MissionMenuController>();
        if (!isLocalPlayer) { head.transform.GetChild(0).gameObject.SetActive(false); }
        rb = GetComponent<Rigidbody>();
        if (players == null) players = new List<player>();
        if (!isLocalPlayer)
        {
            AddPlayer(this, players.Count);
            players.Add(this);
        }
    }
    void TrueStart()
    {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            CmdSetNick(GameObject.Find("network").GetComponent<customNetworkHUD>().nickname);
            CmdSetClass(GameObject.Find("network").GetComponent<customNetworkHUD>().characterclass);
            CmdDmg(1);
            hpobject.gameObject.SetActive(false);
            UIobject = GameObject.Find("Canvas").transform.Find("UI").gameObject;
            UIobject.SetActive(true);
            UIhp = UIobject.transform.Find("HeartImage").transform.Find("HPText").GetComponent<Text>();
            hpbar = UIobject.transform.Find("HeartImage").transform.Find("HealthBorder").GetComponent<Slider>();
            UIshld = UIobject.transform.Find("ShieldImage").transform.Find("ShieldText").GetComponent<Text>();
            shldbar = UIobject.transform.Find("ShieldImage").transform.Find("ShieldBorder").GetComponent<Slider>();
            flarebar = UIobject.transform.Find("Flare").GetComponent<Slider>();
            classpic = UIobject.transform.Find("ClassBorder").GetComponent<Image>();
            classpic.sprite = classes[GameObject.Find("network").GetComponent<customNetworkHUD>().characterclass].icon;
            UIInventory = UIobject.transform.Find("Inventory").gameObject;
    }

    // Update is called once per frame
    void Update()
    {

        if (isLocalPlayer&&!truestart) { if (GameObject.Find("network").GetComponent<customNetworkHUD>().characterclass != -1) { truestart = true; TrueStart(); } return; }
        if (isLocalPlayer&&!missionMenuController.isMissionMenuOpened)
        {
            if (!isDead)
            {
                if (attackcooldown > 0)
                {
                    attackcooldown -= Time.deltaTime;
                    animator.SetBool("Attack", false);
                }
                if (flarecooldown > 0)
                {
                    flarecooldown -= Time.deltaTime;
                    flarebar.value = 1f-flarecooldown*0.25f;
                }
                if (containercooldown > 0)
                {
                    containercooldown -= Time.deltaTime;
                }
                mousedelta.x = Input.GetAxis("Mouse X");
                mousedelta.y = Input.GetAxis("Mouse Y");
                transform.Rotate(0, mousedelta.x * Time.deltaTime * 100f, 0);
                head.transform.Rotate(-mousedelta.y * Time.deltaTime * 100f, 0, 0);
                if (Input.GetKey(KeyCode.W)) { transform.Translate(0, 0, Time.deltaTime * 0.1f * speed); }
                if (Input.GetKey(KeyCode.A)) { transform.Translate(Time.deltaTime * -0.1f * speed, 0, 0); }
                if (Input.GetKey(KeyCode.S)) { transform.Translate(0, 0, Time.deltaTime * -0.1f * speed); }
                if (Input.GetKey(KeyCode.D)) { transform.Translate(Time.deltaTime * 0.1f * speed, 0, 0); }
                if (Input.GetKey(KeyCode.E))
                {
                    if (seeContainer && containercooldown <= 0)
                    {
                        for (int i = 0; i < resourcesCount.Count; ++i)
                        {
                            if (resourcesCount[i] > 0)
                            {
                                CmdAddResource(i, -1);
                                CmdIsDrop(true);
                                generator.AddResource(i, 1);
                                containercooldown = 0.1f;
                                break;
                            }
                        }
                    }
                    if (gobackinteraction)
                    {
                        if (generator.platformstatus == 0)
                        {
                            CmdStartMission();
                        }
                        else if(generator.platformstatus == 2)
                        {
                            CmdEndMission();
                        }
                        gobackinteraction = false;
                    }
                    if (deathInteraction&&targetobject) {
                        CmdAddRevivePoints(targetobject.name, Time.deltaTime*50);
                    }
                    if (missionselectorinteraction) {
                        missionMenuController.GenerateMissions();
                        //generator.ShowMissions();
                    }
                }
                else
                {
                    CmdIsDrop(false);
                }
                if (Input.GetKeyDown(KeyCode.Space)) { rb.AddRelativeForce(0, 450f, 0, ForceMode.Impulse); }
                if (Input.GetKey(KeyCode.Mouse0))
                {
                    if (attackcooldown <= 0)
                    {
                        animator.SetBool("Attack", true);
                        attackcooldown = 1f;
                        Attack();
                    }

                }
                if (Input.GetKeyDown(KeyCode.F))
                {
                    if (flarecooldown <= 0)
                    {
                        flarecooldown = 4f;
                        CmdSpawnFlare();
                    }
                }
                if (Input.GetKeyDown(KeyCode.C))
                {
                    Physics.Raycast(head.transform.position, head.transform.forward, out hit, 6);
                    if (hit.transform)
                    {
                        CmdMoveMarker(hit.point);
                    }
                }


                //взаимодействие
                seeInteraction = false;
                missionselectorinteraction = false;
                gobackinteraction = false;
                deathInteraction = false;
                seeContainer = false;
                Physics.Raycast(head.transform.position, head.transform.forward, out hit, 6);
                if (hit.transform)
                {
                    if (hit.collider.transform.CompareTag("Container"))
                    {
                        seeContainer = true;
                        seeInteraction = true;
                    }
                    if (hit.collider.transform.name == "redbutton")
                    {
                        seeInteraction = true;
                        gobackinteraction = true;
                    }
                    if (hit.collider.transform.name == "missionselector")
                    {
                        seeInteraction = true;
                        missionselectorinteraction = true;
                    }
                    if (hit.collider.transform.CompareTag("Death"))
                    {
                        seeInteraction = true;
                        deathInteraction = true;
                        targetobject = hit.collider.transform.gameObject;
                    }
                }
                Physics.Raycast(transform.position, -transform.up, out hit, 6);
                if (hit.transform)
                {
                    transform.parent = transform;
                }

                //  if (GameObject.Find("SphereDestroyer(Clone)")) { marchingspace.isChecking = true;  } else { marchingspace.isChecking = false; }
                magnitude = rb.velocity.magnitude;
                if (hp < 0)
                {
                    isDead = true;
                    CmdSpawnDeath();
                }

                //щиты
                shldbar.value = shield * 0.01f;
                UIshld.text = shield.ToString();
                if (shield < 100)
                {
                    if (shieldcooldown > 0) { shieldcooldown -= Time.deltaTime; } else { ++shield; shieldcooldown = 0.1f; }
                }

                //инвентарь
                UpdateResources();

                //ui игроки
                if (updatetimer == 0) {
                    for (int i = 0; i < players.Count; ++i) {
                        UpdatePlayer(players[i]);
                    }
                    updatetimer = 300;
                }
                else { --updatetimer; }
            }
            else {
                if (hp == 100)
                {
                    isDead = false;
                }
                else {
                    CmdTryRevive();
                }
            }
        }
        if (localhp != hp) {
            localhp = hp;
            if (!isLocalPlayer) { hpobject.text = nickname + "\n" + hp; } else { UIhp.text = hp.ToString(); hpbar.value = hp*0.01f; }
        }
        
        if (generator.platformstatus==1||generator.platformstatus==3) { if (isLocalPlayer&&!isClear) { Clear(); } transform.parent = generator.platform.transform;if (!isTeleported) { transform.localPosition = new Vector3(Random.Range(-2f,2f),2, Random.Range(-2f, 2f)); isTeleported = true; } }
        if (generator.platformstatus==0||generator.platformstatus==2) { transform.parent = null;isTeleported = false;isClear = false; }
        allitems = GameObject.FindGameObjectsWithTag("Item");
        int id;
        for (int i = 0; i < allitems.Length; ++i) {
            id = int.Parse(allitems[i].name);
            if (resourcesCount[id] < resources[id].maxInBag)
            {
                if (Vector3.Distance(allitems[i].transform.position, transform.position) < 3f)
                {
                    allitems[i].transform.position -= (allitems[i].transform.position - transform.position) * Time.deltaTime * 2;
                }
            }
        }

        //украшательства
        if (characterclass!=-1&&localclass != characterclass) {
            localclass = characterclass;
            Audio.PlayOneShot(classes[characterclass].greetings);
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Item"))
        {
            int id = int.Parse(collision.gameObject.name);
            if(resourcesCount[id]<resources[id].maxInBag){
                if (isLocalPlayer)
                {
                    CmdAddResource(id,1);
                }
                Destroy(collision.gameObject);
            }
        }
        if (isLocalPlayer)
        {
           // print(magnitude);
            if (magnitude > 10f) {
                Dmg((int)((magnitude-10)*4));
            }
        }
    }
    private readonly int[] levels = {
    3000,
    4000,
    5000,
    6000,
    7000,
    8000,
    9000,
    10000,
    11000,
    12000,
    13000,
    14000,
    15000,
    15500,
    16000,
    16500,
    17000,
    17500,
    18000,
    18500,
    19000,
    19500,
    20000,
    20500
    };
}
