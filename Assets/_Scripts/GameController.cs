using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    private RootTileData[,] _tiles;//TODO keep track of tiles in here

    public struct Player
    {
        public int Id;
        public int TilesControled;

        /// <summary>Number of moves that a player can make in a single turn</summary>
        public int NumMoves;
    }

    private int TilesForVictory;
    private int TurnNumber = 0;

    private int NumPlayers = 2;
    private int PlayerTurn = 0;

    private Player[] Players;

    // Start is called before the first frame update
    void Start()
    {
        InitializePlayers();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void InitializePlayers(){
        Players = new Player[NumPlayers];

        for(int i = 0; i < NumPlayers; i++){
            Players[i] = new Player
            {
                Id = i,
                TilesControled = 1,
                NumMoves = 2
            };
        }
    }

    private void NextTurn(){
        TurnNumber++;
        PlayerTurn++;
        if(PlayerTurn >= NumPlayers){
            PlayerTurn = 0;
        }
    }

    private int CheckVictory(){
        int victory = -1;
        
        foreach(Player player in Players){
            if(player.TilesControled >= TilesForVictory){
                victory = player.Id;
            }
        }
        // TODO else case for taking base tree and way to check if base tree taken

        return victory;
    }

}
