using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lights : MonoBehaviour
{
    [SerializeField] LayerMask CollisionMask;
    [SerializeField] Transform Player;
    [SerializeField] int LightCount;
    public List<Light> lights = new List<Light>();
    
    public static Lights Instance;
    private void Awake() {
        Instance = this;
    }

    private void OnDrawGizmos() {
        for(int i=0;i<lights.Count;i++){
            Debug.DrawLine(lights[i].transform.position, Player.position);
        }
    }
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
   
    }
    void FixedUpdate(){
        bool isntSorted = true;
        while(isntSorted){
            isntSorted = false;
            for(int i=0;i<lights.Count-1;i++){
                Vector3 dir1 = Player.position - lights[i].transform.position;
                float dis1 = dir1.magnitude;
                bool sight1 = !Physics.Raycast(lights[i].transform.position, dir1, dis1, CollisionMask);
                Vector3 dir2 = Player.position - lights[i+1].transform.position;
                float dis2 = dir2.magnitude;
                bool sight2 = !Physics.Raycast(lights[i+1].transform.position, dir2, dis2, CollisionMask);

                if(sight1!=sight2){
                    if(sight2){
                        isntSorted=true;
                        Light l = lights[i];
                        lights[i] = lights[i+1];
                        lights[i+1] = l;
                    }
                }else{
                    if(dis1>dis2){
                        isntSorted=true;
                        Light l = lights[i];
                        lights[i] = lights[i+1];
                        lights[i+1] = l;
                    }
                }
            }
        }

        for(int i=0;i<lights.Count;i++){
            lights[i].enabled=i<LightCount;
        }

    }

}
