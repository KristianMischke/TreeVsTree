using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public class MapLoader : MonoBehaviour
{
    public int index;
    public bool fogOfWar;
    public int startingMoves;
    public int tilesToWin;

    // Start is called before the first frame update
    void Start()
    {
        index = 1;
        fogOfWar = true;
        startingMoves = 2;
        tilesToWin = 40;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void setIndex(int newIndex){
        index = newIndex + 1;
    } 
    public void setFog(bool fog){
        fogOfWar = fog;
    }
    public void setMoves(string moves){
        int.TryParse(moves, out startingMoves);
        if(startingMoves <= 0){
            startingMoves = 2;
        }
    }

    public void SwapScene(int sceneIndex){
        SceneManager.LoadScene(sceneIndex);
    }


}
