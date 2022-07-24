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
    public GameObject box;
    public GameObject r_ul, r_ur, r_dl, r_dr;//root
    public GameObject p_ul, p_ur, p_dl, p_dr;//point
    public GameObject leg_ul, leg_ur, leg_dl, leg_dr;//point
    private Vector3 v_ul, v_ur, v_dl, v_dr;
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
     //   box.transform.localPosition = new Vector3(0, 0.685f, 0);
        for (int i = 0; i < cornertable.Length; ++i)
        {
            if (Physics.Raycast(transform.position, cornertable[i], out hit, 1f))
            {
       //         box.transform.position=(box.transform.position-cornertable[i]*hit.distance*0.5f);
            }
        }
        p_dl.transform.position = v_dl;
        p_dr.transform.position = v_dr;
        p_ul.transform.position = v_ul;
        p_ur.transform.position = v_ur;
        if (Physics.Raycast(r_dl.transform.position, Vector3.down, out hit, 1f)) {
            if (Vector3.Distance(p_dl.transform.position, hit.point) > 0.75f) { 
                v_dl= hit.point; 
            }
        }
        if (Physics.Raycast(r_ul.transform.position, Vector3.down, out hit, 1f)) {
            if (Vector3.Distance(p_ul.transform.position, hit.point) > 0.75f)
            {
                v_ul = hit.point;
            }
        }
        if (Physics.Raycast(r_dr.transform.position, Vector3.down, out hit, 1f)) {
            if (Vector3.Distance(p_dr.transform.position, hit.point) > 0.75f)
            {
                v_dr = hit.point;
            }
        }
        if (Physics.Raycast(r_ur.transform.position, Vector3.down, out hit, 1f)) {
            if (Vector3.Distance(p_ur.transform.position, hit.point) > 0.75f)
            {
                v_ur = hit.point;
            }
        }
        leg_dl.transform.LookAt(r_dl.transform.position);
        leg_dl.transform.Rotate(90,0,0);
        leg_dr.transform.LookAt(r_dr.transform.position);
        leg_dr.transform.Rotate(90,0,0);
        leg_ul.transform.LookAt(r_ul.transform.position);
        leg_ul.transform.Rotate(90,0,0);
        leg_ur.transform.LookAt(r_ur.transform.position);
        leg_ur.transform.Rotate(90,0,0);
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
    private readonly Vector3[] cornertable = {
        new Vector3(1,0,0),
        new Vector3(-1,0,0),
        new Vector3(0,1,0),
        new Vector3(0,-1,0),
        new Vector3(0,0,1),
        new Vector3(0,0,-1),
    };
}
