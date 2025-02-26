using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace World{
public class WorldBuilder : Object
{
    public float WorldSize{get;set;}
    public int NumPointsEq{get;set;}
    public float perlHeight{get;set;}
    public float perlZoom{get;set;}
    public int Seed{get;set;}
    public int octaves{get;set;}
    public float persistance{get;set;}
    public float lacunarity{get;set;}

    public Mesh mesh{get;set;}
    public List<List<Vector3>> WorldPoints{get;set;}

    public AnimationCurve heightCurve;

    public GameObject World{get;set;}

    public Vector2 offset{get;set;}

    private Vector3[] points;

    // public TerrainType[] Terrains{get;set;}
    // public Material defMat;

    Gradient gradient;

    Texture2D texture;
    
    public int status;
    float maxTHeight, minTHeight;

    public WorldBuilder(float worldSize, int numPointsEq, GameObject w, int seed,float pHeight,float pZoom, int oct, float pers, float lac, Vector2 off){
        WorldSize=worldSize;
        NumPointsEq=numPointsEq;
        
        World=w;
        Seed=seed;
        perlHeight=pHeight;
        perlZoom=pZoom;
        
        // this.defMat=defMat;
        // gradient=grad;
        
        octaves=oct;
        persistance=pers;
        lacunarity=lac;
        offset=off;
        // Terrains=terrains;
        
        status=NO_WORLD;
    }
     public WorldBuilder(){
        status=NO_WORLD;
    }
    public IEnumerator GenerateWorld(){
        var falloff=FallOffMap.Instance.GenerateFallOff(NumPointsEq);
        status=IN_PROG;
        mesh=new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        float startTime=Time.time;
        maxTHeight=float.MinValue;
        minTHeight=float.MaxValue;

        Debug.Log("Starting world load! "+Time.time);
        // deb.Instance.Console="Starting world load! "+Time.time;

        // defMat.SetVector("_Scale",new Vector2(WorldSize/10,WorldSize/10));

        var vertices = new Vector3[(NumPointsEq+1)*(NumPointsEq+1)];
        if(Seed==-1){
            Seed=Random.Range(-999999,999999);
        }
        System.Random prng = new System.Random (Seed);
		Vector2[] octaveOffsets = new Vector2[octaves];
		for (int i = 0; i < octaves; i++) {
			float offsetX = prng.Next (-100000, 100000) + offset.x;
			float offsetY = prng.Next (-100000, 100000) + offset.y;
			octaveOffsets [i] = new Vector2 (offsetX, offsetY);
		}

        float halfWidth = NumPointsEq / 2f;
		float halfHeight = NumPointsEq / 2f;

        for (int i=0, z=0;z<=NumPointsEq;z++){
            for (int x=0;x<=NumPointsEq;x++){


                float y = 0;
                float amplitude = 1;
				float frequency = 1;
                

                for(int j=0;j<octaves;j++){
                    float xtg=(x-halfWidth) / perlZoom * frequency + octaveOffsets[j].x;
                    float ytg=(z-halfHeight) / perlZoom * frequency + octaveOffsets[j].y;

                    float perlinValue = Mathf.PerlinNoise (xtg, ytg);

                    y+=perlinValue*amplitude;
                    amplitude *= persistance;
					frequency *= lacunarity;

                    // y+=(Mathf.PerlinNoise((x+Seed)/perlZoom*(lacunarity*j),(z+Seed)/perlZoom*(lacunarity*j))*2-1)*perlHeight*(persistance*j);
                }
                y*=falloff[x,z];
                //Debug.Log(x+" "+y+" "+z);
                vertices[i] = new Vector3(x*WorldSize/NumPointsEq,y,z*WorldSize/NumPointsEq);
                if(z>NumPointsEq/20&z<NumPointsEq-NumPointsEq/20&x>NumPointsEq/20&x<NumPointsEq-NumPointsEq/20){
                    if(y>maxTHeight){
                        maxTHeight=y;
                        //Debug.Log(z+" "+y+" "+x);
                        //Debug.Log(NumPointsEq/50+" z "+(NumPointsEq-NumPointsEq/50)+", "+NumPointsEq/50+" x "+(NumPointsEq-NumPointsEq/50));
                    }
                    if(y<minTHeight){
                        minTHeight=y;
                    }
                }else{
                    
                }
                
                i++;
            }
            
            yield return null;
            
        }
        //Debug.Log(maxTHeight+" "+minTHeight);
        Debug.Log("Initial Noise map 1/3");
        // deb.Instance.Console="Initial Noise map 1/3";
        // Color[] colors = new Color[vertices.Length];
        Vector2[] uvs = new Vector2[vertices.Length];
        for (int i=0, z=0;z<=NumPointsEq;z++){
            for (int x=0;x<=NumPointsEq;x++){
                
                vertices[i]=new Vector3(vertices[i].x,heightCurve.Evaluate(Mathf.InverseLerp(minTHeight,maxTHeight,vertices[i].y))*perlHeight,vertices[i].z);
                uvs[i] = new Vector2((float)x / NumPointsEq, (float)z / NumPointsEq);
                i++ ;
                
                
            }
            
            yield return null;
        }
        Debug.Log("Color mapping and final vert height 2/3");
        // deb.Instance.Console="Color mapping and final vert height 2/3";
        
        var triangles = new int[NumPointsEq*NumPointsEq*6];
        int vert =0;
        int tris=0;
        for (int z=0;z<NumPointsEq;z++){
            for(int x=0;x<NumPointsEq;x++){
                triangles[tris+0]=vert+0;
                triangles[tris+1]=vert+NumPointsEq+1;
                triangles[tris+2]=vert+1;
                triangles[tris+3]=vert+1;
                triangles[tris+4]=vert+NumPointsEq+1;
                triangles[tris+5]=vert+NumPointsEq+2;
                vert++;
                tris+=6;
            }
            vert++;
            
            yield return null;
        }
        Debug.Log("Triangles 3/3");
        // deb.Instance.Console="Triangles 3/3";
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        
        // mesh.colors = colors;
        // texture=( TextureGenerator.TextureFromColourMap(colors,NumPointsEq+1,NumPointsEq+1));//"Base Texture",
        

        
        mesh.uv = uvs;
        mesh.RecalculateNormals();

        
        Debug.Log("Load time: "+(Time.time-startTime));
        // deb.Instance.Console="Load time: "+(Time.time-startTime);
        status=COMPLETE;

        UpdateWorldObj(new Vector3(0,0,0));
        WorldGenRunner.Instance.Finish();
    }


    public void UpdateWorldObj(Vector3 at){
        World.transform.position = at;
        World.GetComponent<MeshFilter>().mesh = mesh;
        World.GetComponent<MeshCollider>().sharedMesh = mesh;
        
        //World.GetComponent<MeshRenderer>().sharedMaterial.mainTexture=texture;

        // defMat.SetTexture("_ColorMap",texture);

        

    }

    public void drawGizmos(){
        
        for (int i=0;i<NumPointsEq*2;i++){
            Gizmos.color = new Color(i/(NumPointsEq*2),i/(NumPointsEq*2),i/(NumPointsEq*2));
            Debug.Log(points[i]);
            Gizmos.DrawSphere(points[i], 1f);
        }
    }
    public static int NO_WORLD = 0;
    public static int IN_PROG = 1;
    public static int COMPLETE = 2;
}
}
