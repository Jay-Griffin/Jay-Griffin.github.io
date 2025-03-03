using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;
using UnityEngine.AI;

public class MazeMaker : MonoBehaviour
{
    [SerializeField] public int cellWidth;
    [SerializeField] public int cellHeight;
    [SerializeField] float RandomRemoveChance;
    [SerializeField] List<MazePiece> MazePieceList;
    [SerializeField] NavMeshSurface navMeshSurface;
    public int LeekCount;
    // [SerializeField] NavMesh navMesh;
    [SerializeField] GameObject leek;
    [SerializeField] SettingsStuff settings;

    cellData[,] Cells;

    // Start is called before the first frame update
    void Start()
    {
        LeekCount=2;
        Cells = new cellData[cellHeight,cellWidth];
        for(int y=0; y<cellHeight; y++){
            for(int x=0; x<cellWidth; x++){
                Cells[y,x] = new cellData();
                Cells[y,x].Inst();
            }
        }
        settings.UpdateFromFile();
        StartMazeMaker();
    }

    void StartMazeMaker(){
        int x = cellWidth/2;
        int y = 0;
        Cells[y,x].TopWall=false;
        // Cells[y,x].BeenVisited=true;
        pickDirection(x,y);
        remRandom();
        Cells[cellHeight-1,x].BottomWall=false;
        // ValidateGamePieces();
        InstancePieces();
        navMeshSurface.BuildNavMesh();

        placeLeeks();
    }
    public void WhatTheSigma(){
        navMeshSurface.BuildNavMesh();
    }

    void placeLeeks(){
        for(int i=0; i<LeekCount; i++){
            bool failed=true;
            while(failed){
                float y = Random.Range(0,cellHeight*6-3);
                float x = Random.Range(0,cellWidth*6-3);
                NavMeshHit hit;
                failed = ! NavMesh.SamplePosition(new Vector3(x,0,-y),out hit, 1f, NavMesh.AllAreas);
                if(!failed){
                    Instantiate(leek, hit.position, Quaternion.identity);
                }
            }
        }
        Gates.leekCount = LeekCount;


    }
    void InstancePieces(){
        for(int y = 0; y< cellHeight; y++){
            for(int x = 0; x<cellWidth; x++){
                List<MazePiece> pieceList = GetMazePieces(Cells[y,x].LeftWall,Cells[y,x].RightWall,Cells[y,x].TopWall,Cells[y,x].BottomWall);
                int roll = Random.Range(0,pieceList.Count);
                Instantiate(pieceList[roll].gameObject,new Vector3(x*6,0,-y*6),Quaternion.identity);
            }
        }
    }

    void remRandom(){
        for(int y = 0;y< cellHeight;y++){
            for(int x = 0;x<cellWidth;x++){
                if(y!=0){
                    if(Cells[y,x].TopWall){
                        float roll = Random.Range(0f,1f);
                        if(roll<RandomRemoveChance){
                            Cells[y,x].TopWall = false;
                            Cells[y-1,x].BottomWall = false;
                        }
                    }
                }
                if(y!=cellHeight-1){
                    if(Cells[y,x].BottomWall){
                        float roll = Random.Range(0f,1f);
                        if(roll<RandomRemoveChance){
                            Cells[y,x].BottomWall = false;
                            Cells[y+1,x].TopWall = false;
                        }
                    }
                }
                if(x!=0){
                    if(Cells[y,x].LeftWall){
                        float roll = Random.Range(0f,1f);
                        if(roll<RandomRemoveChance){
                            Cells[y,x].LeftWall = false;
                            Cells[y,x-1].RightWall = false;
                        }
                    }
                }
                if(x!=cellWidth-1){
                    if(Cells[y,x].LeftWall){
                        float roll = Random.Range(0f,1f);
                        if(roll<RandomRemoveChance){
                            Cells[y,x].RightWall = false;
                            Cells[y,x+1].LeftWall = false;
                        }
                    }
                }
            }
        }
    }

    void ValidateGamePieces(){
        bool left = false;
        bool right = false;
        bool top = false;
        bool bottom = false;

        while(!left||!right||!top||!bottom){
            List<MazePiece> pieceList = GetMazePieces(left,right,top,bottom);
            if(pieceList.Count==0){
                Debug.LogWarning("Missing Piece for config - Left: "+left+" Right: "+right+" Top: "+top+" Bottom: "+bottom);
            }else{
                Debug.Log(pieceList.Count+" pieces found for config - Left: "+left+" Right: "+right+" Top: "+top+" Bottom: "+bottom);
            }
            left=!left;
            if(!left){
                right=!right;
            }
            if(!left&&!right){
                top=!top;
            }
            if(!left&&!right&&!top){
                bottom=!bottom;
            }
        }
    }

    List<MazePiece> GetMazePieces(bool left, bool right, bool top, bool bottom){
        List<MazePiece> toReturn = new List<MazePiece>();
        foreach(MazePiece piece in MazePieceList){
            if(piece.LeftWall == left&&piece.RightWall == right&&piece.TopWall == top&&piece.BottomWall == bottom){
                if(piece.Weight>0){
                    for(int i=0;i<piece.Weight;i++){
                        toReturn.Add(piece);
                    }
                }else{
                    toReturn.Add(piece);
                }
            }
        }
        return toReturn;
    }

    void pickDirection(int x, int y){
        Cells[y,x].BeenVisited=true;


        while(true){
            List<Directions> dirs = new List<Directions>();
            
            if(y!=0){
                if(!Cells[y-1,x].BeenVisited){
                    
                    dirs.Add(Directions.Top);
                }
            }
            if(y!=cellHeight-1){
                if(!Cells[y+1,x].BeenVisited){
                    dirs.Add(Directions.Bottom);
                }
            }
            if(x!=0){
                if(!Cells[y,x-1].BeenVisited){
                    dirs.Add(Directions.Left);
                }
            }
            if(x!=cellWidth-1){
                if(!Cells[y,x+1].BeenVisited){
                    dirs.Add(Directions.Right);
                }
            }

            if(dirs.Count==0){
                break;
            }
            
            int roll = Random.Range(0,dirs.Count);
            switch(dirs[roll]){
                case Directions.Top:
                    // Debug.Log(y);
                    Cells[y,x].TopWall=false;
                    Cells[y-1,x].BottomWall=false;
                    pickDirection(x,y-1);
                    break;
                case Directions.Bottom:
                    Cells[y,x].BottomWall=false;
                    Cells[y+1,x].TopWall = false;
                    pickDirection(x,y+1);
                    break;
                case Directions.Left:
                    Cells[y,x].LeftWall=false;
                    Cells[y,x-1].RightWall=false;
                    pickDirection(x-1,y);
                    break;
                case Directions.Right:
                    Cells[y,x].RightWall=false;
                    Cells[y,x+1].LeftWall=false;
                    pickDirection(x+1,y);
                    break;
            }

        }
        
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
struct cellData{
    public bool HasLeek;
    public bool BeenVisited;
    public bool LeftWall;
    public bool RightWall;
    public bool TopWall;
    public bool BottomWall;
    public void Inst(){
        HasLeek=false;
        BeenVisited=false;
        LeftWall=true;
        RightWall=true;
        TopWall=true;
        BottomWall=true;
    }
}
[System.Serializable]
struct MazePiece{
    public GameObject gameObject;
    public bool LeftWall;
    public bool RightWall;
    public bool TopWall;
    public bool BottomWall;
    public int Weight;
}

[System.Serializable]
enum Directions{
    Top,
    Bottom,
    Right,
    Left,
}


