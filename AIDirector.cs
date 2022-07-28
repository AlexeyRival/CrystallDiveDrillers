using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class AIDirector : NetworkBehaviour
{
    public GameObject bug;
    public Generator generator;
    public bool moved,debugMode;
    public void SpawnSmallBugs()
    {
        RaycastHit hit;
        GameObject ob;
        for (int i = 0; i < 5; ++i)
        {
            Physics.Raycast(generator.startpoints[Random.Range(0, generator.startpoints.Count)], Vector3.down, out hit);
            ob = Instantiate(bug, hit.point, Quaternion.Euler(0, Random.Range(0, 360), 0));
            if (i == 0) { 
                ob.GetComponent<bug>().agression *= 1.1f; 
                ob.GetComponent<bug>().speed *= 1.1f; 
                ob.GetComponent<bug>().hp = (int)(ob.GetComponent<bug>().hp*1.1f); 
            }
            NetworkServer.Spawn(ob);
        }
    }
    void Update()
    {
        if (isServer&&!moved) {
            if (generator.isGeneratingCompleted) {
                SpawnSmallBugs();
                moved = true;
            }
        }
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.RightControl)) { debugMode = true; }
        if (debugMode) {
            if (Input.GetKeyDown(KeyCode.F1)) { SpawnSmallBugs(); }
            if (Input.GetKeyDown(KeyCode.F12)) { debugMode = false; }
        }
    }
    private void OnGUI()
    {
        if (debugMode) {
            GUI.Box(new Rect(Screen.width * 0.5f - 100, 40, 200, 25), "DEBUG MODE");
            GUI.Box(new Rect(0, 0, Screen.width / 12 * 1, 40), "F1 - SpawnSmallBags");
            GUI.Box(new Rect(Screen.width / 12 * 11, 0, Screen.width / 12 * 1, 40), "F12 - Disable Debug Mode");
        }
    }
}
