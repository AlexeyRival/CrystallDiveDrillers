using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class AIDirector : NetworkBehaviour
{
    public GameObject bug, mimicbug, opressor, ironOpressor;
    public Generator generator;
    public MissionMenuController missioncontroller;
    public difficulty currentdifficulty;
    public bool moved, debugMode;
    private const string version = "Дионис";
    public float time, swarmmeter;
    private int phase=0;
    public void SpawnSmallBugs()
    {
        RaycastHit hit;
        GameObject ob;
        bool isMimicBug;
        for (int i = 0; i < 5; ++i)
        {
            isMimicBug = Random.Range(0, 100) < 15;
            Physics.Raycast(generator.bugspawnpoints[Random.Range(0, generator.bugspawnpoints.Count)], Vector3.down, out hit);
            ob = Instantiate(isMimicBug ? mimicbug : bug, hit.point, Quaternion.Euler(0, Random.Range(0, 360), 0));
            if (i == 0) {
                ob.GetComponent<bug>().agression *= 1.1f;
                ob.GetComponent<bug>().speed *= 1.1f;
                ob.GetComponent<bug>().hp = (int)(ob.GetComponent<bug>().hp * 1.1f);
            }
            NetworkServer.Spawn(ob);
        }
    }
    public void SpawnOpressor()
    {
        RaycastHit hit;
        bool isIronOpressor = Random.Range(0,100)<15;
        Physics.Raycast(generator.bugspawnpoints[Random.Range(0, generator.bugspawnpoints.Count)], Vector3.down, out hit);
        GameObject ob = Instantiate(isIronOpressor?ironOpressor:opressor, hit.point, Quaternion.Euler(0, Random.Range(0, 360), 0));
        if (isIronOpressor)
        {
            ob.GetComponent<bug>().agression *= 1.1f;
            ob.GetComponent<bug>().speed *= 1.1f;
            ob.GetComponent<bug>().hp = (int)(ob.GetComponent<bug>().hp * 1.1f);
        }
        NetworkServer.Spawn(ob);

    }
    public void SpawnMidBugs() 
    {
        for (int i = 0; i < Random.Range(2, 4); ++i) {
            SpawnSmallBugs();
        }
    }
    public void ResetAll() {
        phase = 0;
        time = 0;
        swarmmeter = 0;
    }
    void Update()
    {
        if (isServer)
        {
            if (!moved)
            {
                if (generator.isGeneratingCompleted)
                {
                    SpawnSmallBugs();
                    currentdifficulty = missioncontroller.difficulties[generator.questdifficulty];
                    moved = true;
                }
            }
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.RightControl)) { debugMode = true; }
            if (debugMode)
            {
                if (Input.GetKeyDown(KeyCode.F1)) { SpawnSmallBugs(); }
                if (Input.GetKeyDown(KeyCode.F2)) { SpawnMidBugs(); }
                if (Input.GetKeyDown(KeyCode.F4)) { SpawnOpressor(); }
                if (Input.GetKeyDown(KeyCode.F11)) { ResetAll(); }
                if (Input.GetKeyDown(KeyCode.F12)) { debugMode = false; }
            }
            if (moved)
            {
                time += Time.deltaTime;
                swarmmeter += Time.deltaTime * 0.05f;
                if (time >= 50&&phase==0) { 
                    if (Random.Range(0, 100) < 30) { SpawnSmallBugs(); time = 0; } else { phase = 1; }
                }
                if (time >= 100) { 
                    phase = 0;time = 0;
                    if (currentdifficulty.OpressorSpawnChance!=0&&Random.Range(0, 100) < currentdifficulty.OpressorSpawnChance + swarmmeter * 0.1f) { SpawnOpressor(); }else
                    if (Random.Range(0, 100) < 80+swarmmeter) { SpawnMidBugs(); } 
                    
                }
                if (!generator.isGeneratingCompleted) { moved = false;ResetAll(); }
            }
        }
    }
    private void OnGUI()
    {
        if (debugMode) {
            GUI.Box(new Rect(Screen.width * 0.5f - 100, 40, 200, 25), "DEBUG MODE - "+version);
            GUI.Box(new Rect(0, 0, Screen.width / 12 * 1, 40), "F1 - SpawnSmallBugs");
            GUI.Box(new Rect(Screen.width / 12 * 1, 0, Screen.width / 12 * 1, 40), "F2 - SpawnMidBugs");
            GUI.Box(new Rect(Screen.width / 12 * 3, 0, Screen.width / 12 * 1, 40), "F4 - SpawnOpressor");
            GUI.Box(new Rect(Screen.width / 12 * 10, 0, Screen.width / 12 * 1, 40), "F11 - Сбросить всё");
            GUI.Box(new Rect(Screen.width / 12 * 11, 0, Screen.width / 12 * 1, 40), "F12 - Отключить Debug Mode");
            GUI.Box(new Rect(Screen.width - 200, Screen.height * 0.4f, 200, 30), "Время: " + time);
            GUI.Box(new Rect(Screen.width - 200, Screen.height * 0.4f + 30, 200, 30), "Счётчик Роя: " + swarmmeter);
            GUI.Box(new Rect(Screen.width - 200, Screen.height * 0.4f + 60, 200, 30), "Фаза: " + phase);
        }
    }
}
