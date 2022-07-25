using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class mule : NetworkBehaviour
{
    public bool isStartWalking;
    public float speed = 2f;
    public List<Vector3> path;
    private int currentpoint;
    private RaycastHit hit;
    public GameObject box;
    public GameObject leg_fr, leg_fl, leg_br, leg_bl;//ляжки
    public GameObject foot_fr, foot_fl, foot_br, foot_bl;//голени
    public GameObject point_fr, point_fl, point_br, point_bl;//точки сброса
    public Vector3 v_bl, v_br, v_fl, v_fr;
    public Vector3 t_bl, t_br, t_fl, t_fr;
    public Vector3 s_bl, s_br, s_fl, s_fr;
    private bool lock_bl, lock_br, lock_fl, lock_fr;
    private float bfr;
    private float spd;
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
                transform.Translate(0, 0, Time.deltaTime * speed);
                transform.Rotate(-90,0,0);
                transform.Rotate(0,0,45);
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

        bfr = Mathf.Sin(Vector3.Distance(s_fr, v_fr) * Mathf.PI)*0.5f + 0.45f;
        leg_fr.transform.LookAt(new Vector3(v_fr.x, v_fr.y+bfr, v_fr.z));
        leg_fr.transform.Rotate(-90,90,90);

        bfr = Mathf.Sin(Vector3.Distance(s_fl, v_fl) * Mathf.PI) * 0.5f + 0.45f;
        leg_fl.transform.LookAt(new Vector3(v_fl.x, v_fl.y + bfr, v_fl.z));
        leg_fl.transform.Rotate(-90, 90, 90);

        bfr = Mathf.Sin(Vector3.Distance(s_br, v_br) * Mathf.PI) * 0.5f + 0.45f;
        leg_br.transform.LookAt(new Vector3(v_br.x, v_br.y + bfr, v_br.z));
        leg_br.transform.Rotate(-90, 90, 90);

        bfr = Mathf.Sin(Vector3.Distance(s_bl, v_bl) * Mathf.PI) * 0.5f + 0.45f;
        leg_bl.transform.LookAt(new Vector3(v_bl.x, v_bl.y + bfr, v_bl.z));
        leg_bl.transform.Rotate(-90, 90, 90);

        foot_fr.transform.LookAt(v_fr);
        foot_fr.transform.Rotate(-90, 90, 90);
        foot_fl.transform.LookAt(v_fl);
        foot_fl.transform.Rotate(-90, 90, 90);
        foot_br.transform.LookAt(v_br);
        foot_br.transform.Rotate(-90, 90, 90);
        foot_bl.transform.LookAt(v_bl);
        foot_bl.transform.Rotate(-90, 90, 90);

        spd = Time.deltaTime * 13f*speed;//4
        if (Vector3.Distance(s_fr, v_fr) > 0.001f) { v_fr = Vector3.Slerp(v_fr, t_fr, spd); } else { lock_fr = false; }
        if (Vector3.Distance(s_fl, v_fl) > 0.001f) { v_fl = Vector3.Slerp(v_fl, t_fl, spd); } else { lock_fl = false; }
        if (Vector3.Distance(s_br, v_br) > 0.001f) { v_br = Vector3.Slerp(v_br, t_br, spd); } else { lock_br = false; }
        if (Vector3.Distance(s_bl, v_bl) > 0.001f) { v_bl = Vector3.Slerp(v_bl, t_bl, spd); } else { lock_bl = false; }

        if (Physics.Raycast(point_bl.transform.position, -point_bl.transform.up, out hit, 2.5f)) {
            if (Vector3.Distance(t_bl, hit.point) > 1f&&!(lock_bl||lock_br||lock_fl)) { 
                t_bl= hit.point;
                s_bl= hit.point;
                lock_bl = true;
            }
        }
        if (Physics.Raycast(point_br.transform.position, -point_br.transform.up, out hit, 2.5f)) {
            if (Vector3.Distance(t_br, hit.point) > 1f && !(lock_bl || lock_br || lock_fr)) { 
                t_br= hit.point;
                s_br= hit.point;
                lock_br = true;
            }
        }
        if (Physics.Raycast(point_fl.transform.position, -point_fl.transform.up, out hit, 2.5f)) {
            if (Vector3.Distance(t_fl, hit.point) > 1f && !(lock_fl || lock_fr || lock_bl)) { 
                t_fl= hit.point;
                s_fl= hit.point;
                lock_fl = true;
            }
        }
        if (Physics.Raycast(point_fr.transform.position, -point_fr.transform.up, out hit, 2.5f)) {
            if (Vector3.Distance(t_fr, hit.point) > 1f && !(lock_fl || lock_fr || lock_br)) { 
                t_fr= hit.point;
                s_fr= hit.point;
                lock_fr = true;
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
    private readonly Vector3[] cornertable = {
        new Vector3(1,0,0),
        new Vector3(-1,0,0),
        new Vector3(0,1,0),
        new Vector3(0,-1,0),
        new Vector3(0,0,1),
        new Vector3(0,0,-1),
    };
}
