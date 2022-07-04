using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class player : NetworkBehaviour
{
    public GameObject head;
    public float speed = 75f;
    public GameObject sphereDestroyer,flare;
    public TextMesh nickobject,hpobject;
    private Rigidbody rb;
    private Vector3 mousedelta;
    private RaycastHit hit;
    public List<Resource> resources;
    public Animator animator;
    private Generator generator;
    private bool isTeleported = false;
    private float attackcooldown = 0;
    private float flarecooldown = 0;
    private int localhp = 100;
    private float magnitude;
    private bool seeContainer;
    private float containercooldown = 0;
    private GameObject[] allitems;
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
    void CmdSpawnFlare() {
        GameObject ob= Instantiate(flare, head.transform.position, head.transform.rotation);
        ob.GetComponent<Rigidbody>().AddRelativeForce(0,0,20,ForceMode.Impulse);
        Destroy(ob, 30f);
        NetworkServer.Spawn(ob);
    }
    [Command]
    void CmdAddResource(int id,int amout) {
        resourcesCount[id]+=amout;
    }
    [Command]
    void CmdStartFuckinLAGS() {
       // for (int i = 0; i < GameObject.FindGameObjectsWithTag("Player").Length; ++i) {
       //     GameObject.FindGameObjectsWithTag("Player")[i].transform.parent = generator.platform.transform;
       // }
        generator.StartPlatform();
    }
    [Command]
    void CmdSetNick(string nick) {
        nickname = nick;
    }
    [Command]
    void CmdDmg(int dmg) {
        hp = hp-dmg;
    }
    public void Attack()
    {
        if (isLocalPlayer)
        {
            Physics.Raycast(head.transform.position, head.transform.forward, out hit, 6);
            if (hit.transform)
            {
                CmdSpawnDestroyer(hit.point);
            }
        }
    }
    void Start()
    {
        generator = GameObject.Find("ChungGenerator").GetComponent<Generator>();
        if (isServer) for (int i = 0; i < resources.Count; ++i) { resourcesCount.Add(0); }
        if (isLocalPlayer)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            CmdSetNick(GameObject.Find("network").GetComponent<customNetworkHUD>().nickname);
            nickobject.gameObject.SetActive(false);
        }
        else {
            head.transform.GetChild(0).gameObject.SetActive(false);
        }
        rb = GetComponent<Rigidbody>();
        nickobject.text = nickname;
    }

    // Update is called once per frame
    void Update()
    {
        if (isLocalPlayer)
        {
            if (attackcooldown > 0) { attackcooldown -= Time.deltaTime;
                animator.SetBool("Attack", false);
            }
            if (flarecooldown > 0)
            {
                flarecooldown -= Time.deltaTime;
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
            if (Input.GetKey(KeyCode.E) && seeContainer&&containercooldown<=0) {
                for (int i = 0; i < resourcesCount.Count; ++i) {
                    if (resourcesCount[i] > 0) {
                        CmdAddResource(i, -1);
                        generator.AddResource(i, 1);
                        containercooldown = 0.1f;
                        break;
                    }
                }
            }
            if (Input.GetKeyDown(KeyCode.Space)) { rb.AddRelativeForce(0, 450f, 0, ForceMode.Impulse); }
            if (Input.GetKey(KeyCode.Mouse0))
            {
                if (attackcooldown<=0)
                {
                    animator.SetBool("Attack", true);
                    attackcooldown = 1f;
                    Attack();
                }
                
            }
            if (Input.GetKeyDown(KeyCode.F)) {
                if (flarecooldown <= 0)
                {
                    flarecooldown = 4f;
                    CmdSpawnFlare();
                }
            }
            if (Input.GetKeyDown(KeyCode.F12)) {
                CmdStartFuckinLAGS();
            }
            //контейнер
            seeContainer = false;
            Physics.Raycast(head.transform.position, head.transform.forward, out hit, 6);
            if (hit.transform)
            {
             //   print(hit.transform.tag);
                if (hit.transform.CompareTag("Container"))
                {
                    seeContainer = true;
                }
            }

            //  if (GameObject.Find("SphereDestroyer(Clone)")) { marchingspace.isChecking = true;  } else { marchingspace.isChecking = false; }
            magnitude = rb.velocity.magnitude;
        }
        if (localhp != hp) {
            localhp = hp;
            hpobject.text = "" + hp;
        }
        if (generator.isPlatformStarted) { transform.parent = generator.platform.transform;if (!isTeleported) { transform.localPosition = new Vector3(Random.Range(-2f,2f),2, Random.Range(-2f, 2f)); isTeleported = true; } }
        if (generator.isPlatformStopped) { transform.parent = null; }
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
                CmdDmg((int)(magnitude-10));
            }
        }
    }
    private int guishift;
    private void OnGUI()
    {
        if (isLocalPlayer)
        {
            guishift = 0;
            for (int i = 0; i < resourcesCount.Count; ++i)if(resourcesCount[i]!=0)
            {
                GUI.Box(new Rect(Screen.width * 0.5f - 200 + guishift * 60, Screen.height - 105, 60, 60), resources[i].icon);
                GUI.Box(new Rect(Screen.width * 0.5f - 200 + guishift * 60, Screen.height - 55, 60, 20), resources[i].name);
                GUI.Box(new Rect(Screen.width * 0.5f - 200 + guishift * 60, Screen.height - 25, 60, 25), resourcesCount[i] + "");
                    ++guishift;
            }
            GUI.Box(new Rect(0, Screen.height - 25, 200, 25), "HP: " + hp);
            GUI.Box(new Rect(0, Screen.height - 50, 200, 25), "SPD: " + magnitude);
            GUI.Box(new Rect(Screen.width - 30, Screen.height - (50 - 50 * flarecooldown * 0.25f), 30, (50 - 50 * flarecooldown * 0.25f)), "f");
            if (seeContainer) { GUI.Box(new Rect(Screen.width * 0.5f - 20, Screen.height * 0.5f + 60, 40, 40), "[E]"); }
        }
    }
}
