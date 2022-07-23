using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class mule : NetworkBehaviour
{
    public bool isStartWalking;
    public List<Vector3> path;
    private int currentpoint;
    private RaycastHit hit;
    // Update is called once per frame
    public void SetPath(List<Vector3> path) {
        this.path = path;
        currentpoint = 0;
        if(path.Count>0)isStartWalking = true;
    }
    void Update()
    {
        if (isServer) {
            if (isStartWalking)
            {
                if (currentpoint == path.Count) { isStartWalking = false;return; }
                transform.LookAt(path[currentpoint]);
                transform.Translate(0, 0, Time.deltaTime * 2);
                transform.Rotate(0,90,0);
                if (Vector3.Distance(transform.position, path[currentpoint]) < 1f) {
                    ++currentpoint;
                }
            }
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.67f, 0, 0.54f, 0.67f);
        if (isStartWalking) {
            for (int i = 0; i < path.Count; ++i) {
                Gizmos.DrawCube(path[i], new Vector3(0.4f, 0.4f, 0.4f));
            }
        }
    }
}
