using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = System.Random;

public class NetworkGameController : MonoBehaviourPunCallbacks
{
    public static NetworkGameController Instance;
    
    private GameLogic.GameParameters _currentGameParameters;
    private GameLogic _gameLogic;
    private sbyte _thisPlayerId;
    private MapController _mapController;
    private Random _random;

    // Start is called before the first frame update
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        _random = new Random();
        _mapController = FindObjectOfType<MapController>();
        PhotonNetwork.ConnectUsingSettings();
    }
    
    public override void OnConnectedToMaster()
    {
        Debug.Log("OnConnectedToMaster() was called by PUN.");
    }

    public void CreateRoomWithSettings(GameLogic.GameParameters gameParameters)
    {
        var roomName = _random.Next(1000, 9999);

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.IsVisible = false;
        roomOptions.MaxPlayers = gameParameters.NumPlayers;
        _currentGameParameters = gameParameters;
        FillRoomPropertiesWithGameParams(roomOptions, gameParameters);

        PhotonNetwork.CreateRoom(roomName.ToString(), roomOptions);
    }

    public void FillRoomPropertiesWithGameParams(RoomOptions roomOptions, GameLogic.GameParameters gameParameters)
    {
        var properties = typeof(GameLogic.GameParameters).GetProperties();
        foreach (var property in properties)
        {
            roomOptions.CustomRoomProperties[property.Name] = property.GetValue(gameParameters);
        }
    }
    
    public GameLogic.GameParameters GetCurrentRoomGameParams()
    {
        GameLogic.GameParameters currentParams = new GameLogic.GameParameters();
        foreach (var roomProperty in PhotonNetwork.CurrentRoom.CustomProperties)
        {
            var propertyInfo = typeof(GameLogic.GameParameters).GetProperty((string)roomProperty.Key);
            if (propertyInfo != null)
            {
                propertyInfo.SetValue(currentParams, roomProperty.Value);
            }
        }
        return currentParams;
    }

    public void OnJoinedRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"OnJoinedRoomFailed {returnCode} {message}");
    }
    
    public override void OnJoinedRoom()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            _currentGameParameters = GetCurrentRoomGameParams();
            SceneManager.LoadScene(_currentGameParameters.MapName);
        }
    }
    
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount == _currentGameParameters.NumPlayers
            && PhotonNetwork.IsMasterClient)
        {
            // if we're the master client and we have everyone in the room, tell everyone to start the game
            photonView.RPC(nameof(StartGame), RpcTarget.All);
        }
    }
    
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        var playerId = (sbyte)(otherPlayer.ActorNumber - 1);
        Debug.LogWarning($"Player {playerId} left");
        
        // TODO: show message in UI that they disconnected

        _gameLogic?.PlayerLeft(playerId);
    }

    [PunRPC]
    public void StartGame()
    {
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
        
        UIController.Instance.SetTurnText(_gameLogic.CurrentTurn == _thisPlayerId, _gameLogic.RemainingMovesThisTurn);

        if(_gameLogic.GameOver)
        {
            UIController.Instance.ShowWinner(_gameLogic.Winner);
        }
    }

    [PunRPC]
    public void DoTurnRPC(GameLogic.PlayerActions playerAction, int posX, int posY)
    {
        _gameLogic.DoAction(playerAction, new Vector2Int(posX, posY));
        UpdateVisuals();
    }
}
