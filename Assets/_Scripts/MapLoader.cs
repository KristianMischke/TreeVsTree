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
        if(startingMoves <= 1){
            startingMoves = 2;
        }
    }
    public void setWinTiles(string toWin){
        int.TryParse(toWin, out tilesToWin);
        if(tilesToWin <= 0){
            tilesToWin = 40;
        }
    }

    public void SwapScene(int sceneIndex){
        index = sceneIndex;
        SwapScene();
    }
    public void SwapScene(){
        SubmitSettings();
        SceneManager.LoadScene(index);
    }

    private void SubmitSettings(){
        GameLogic.GameParameters logic = new GameLogic.GameParameters
        {
            MapName = "Map" + index,
        
            NumPlayers = 2,
            TilesForVictory = tilesToWin,
            FogOfWarEnabled = fogOfWar,
            
            PlayerDefaultTurnCount = startingMoves,
            FirstPlayerFirstTurnCount = startingMoves - 1,
        };

        NetworkGameController.Instance.CreateRoomWithSettings(logic);
    }

    public void joinGame(string code){
        NetworkGameController.Instance.JoinRoomWithCode(code);
    }

}
