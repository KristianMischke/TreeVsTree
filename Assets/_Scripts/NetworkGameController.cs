using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class NetworkGameController : MonoBehaviourPunCallbacks
{
    private GameLogic _gameLogic;
    private sbyte _thisPlayerId;
    private MapController _mapController;

    public Text moveText;
    public GameObject player1Win;
    public GameObject player2Win;
    
    // Start is called before the first frame update
    void Start()
    {
        _mapController = FindObjectOfType<MapController>();
        PhotonNetwork.ConnectUsingSettings();
    }
    
    public override void OnConnectedToMaster()
    {
        Debug.Log("OnConnectedToMaster() was called by PUN.");
        
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.IsVisible = false;
        roomOptions.MaxPlayers = 2;

        PhotonNetwork.JoinOrCreateRoom("test_room", roomOptions, TypedLobby.Default);
    }

    public void OnJoinedRoomFailed()
    {
        Debug.LogError("OnJoinedRoomFailed");
    }
    
    public override void OnJoinedRoom()
    {
        // TODO networked parameters & map select
        _mapController.GetGameStateFromTilemap(out var tiles, out var zeroIsOddColumn);
        _mapController.OnHexCellClicked += OnTileClicked;

        _gameLogic = new GameLogic(GameLogic.DefaultParameters, tiles, zeroIsOddColumn);
        
        _thisPlayerId = (sbyte)(PhotonNetwork.LocalPlayer.ActorNumber - 1); // NOTE: THIS IS BAD NEED BETTER WAY TO MAP TO IDS
        Debug.Log($"I am playerId {_thisPlayerId}");
        UpdateVisuals();
    }
    
    private void OnTileClicked(Vector2Int position)
    {
        if (_gameLogic.CurrentTurn == _thisPlayerId)
        {
            photonView.RPC(nameof(DoTurnRPC), RpcTarget.All, GameLogic.PlayerActions.GrowRoot, position.x, position.y);
        }
    }

    private void UpdateVisuals()
    {
        var playerActionTiles = new HashSet<Vector2Int>();
        if (_gameLogic.CurrentTurn == _thisPlayerId)
        {
            playerActionTiles = _gameLogic.GetValidRootGrowthTiles(_thisPlayerId);
        }
        var fogTiles = _gameLogic.GetFogPositions(_thisPlayerId);
        _mapController.SetMap(_gameLogic.Tiles, playerActionTiles, fogTiles);
        
        if(_gameLogic.CurrentTurn == _thisPlayerId)
        {
            moveText.text = "Moves left:" + _gameLogic.RemainingMovesThisTurn;
        }
        else
        {
            moveText.text = "Waiting...";
        }

        if(_gameLogic.GameOver)
        {
            if(_gameLogic.Winner == 0){
                player1Win.SetActive(true);
            }
            else if(_gameLogic.Winner == 1){
                player2Win.SetActive(true);
            }
        }
    }

    [PunRPC]
    public void DoTurnRPC(GameLogic.PlayerActions playerAction, int posX, int posY)
    {
        _gameLogic.DoAction(playerAction, new Vector2Int(posX, posY));
        UpdateVisuals();
    }
}
