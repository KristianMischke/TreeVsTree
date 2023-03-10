using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Random = System.Random;

public class NetworkGameController : MonoBehaviourPunCallbacks
{
    public static NetworkGameController Instance;
    
    private GameLogic.GameParameters _currentGameParameters;
    private GameLogic _gameLogic;
    private sbyte _thisPlayerId;
    private MapController _mapController;
    private Random _random;

    [Header("TESTING ONLY")]
    public bool LocalTesting = false;
    public GameLogic.GameParameters TestingParameters;

    // Start is called before the first frame update
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            DestroyImmediate(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        Instance = this;

        _random = new Random();
    }

    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
    }
    
    public override void OnConnectedToMaster()
    {
        Debug.Log("OnConnectedToMaster() was called by PUN.");

        if (LocalTesting)
        {
            CreateRoomWithSettings(TestingParameters);
        }
    }

    public void CreateRoomWithSettings(GameLogic.GameParameters gameParameters)
    {
        var roomName = _random.Next(1000, 9999);

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.IsVisible = false;
        roomOptions.MaxPlayers = gameParameters.NumPlayers;
        gameParameters.seed = roomName;
        _currentGameParameters = gameParameters;
        roomOptions.CustomRoomProperties = GetHashtablePropertiesWithGameParams(gameParameters);

        PhotonNetwork.CreateRoom(roomName.ToString(), roomOptions);
    }

    public void JoinRoomWithCode(string code)
    {
        PhotonNetwork.JoinRoom(code);
    }

    public Hashtable GetHashtablePropertiesWithGameParams(GameLogic.GameParameters gameParameters)
    {
        var hashtable = new Hashtable();
        var fields = typeof(GameLogic.GameParameters).GetFields();
        foreach (var field in fields)
        {
            hashtable.Add(field.Name, field.GetValue(gameParameters));
        }

        return hashtable;
    }
    
    public GameLogic.GameParameters GetCurrentRoomGameParams()
    {
        object boxedParams = new GameLogic.GameParameters();
        foreach (var roomProperty in PhotonNetwork.CurrentRoom.CustomProperties)
        {
            var fieldInfo = typeof(GameLogic.GameParameters).GetField((string)roomProperty.Key);
            if (fieldInfo != null)
            {
                fieldInfo.SetValue(boxedParams, roomProperty.Value);
            }
        }
        return (GameLogic.GameParameters)boxedParams;
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"OnJoinedRoomFailed {returnCode} {message}");
        SceneManager.LoadScene("Main Menu");
    }
    
    public override void OnJoinedRoom()
    {
        Debug.Log($"OnJoinedRoom");
        if (!PhotonNetwork.IsMasterClient)
        {
            _currentGameParameters = GetCurrentRoomGameParams();
        }
        else
        {
            var loadingScene = SceneManager.LoadSceneAsync(_currentGameParameters.MapName);
            loadingScene.completed += MasterLoadedIntoMap;
        }
    }

    private void MasterLoadedIntoMap(AsyncOperation asyncOperation)
    {
        if (LocalTesting)
        {
            StartGame();
        }
        UIController.Instance.ShowRoomCode(PhotonNetwork.CurrentRoom.Name);
    }
    
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount == _currentGameParameters.NumPlayers
            && PhotonNetwork.IsMasterClient)
        {
            // if we're the master client and we have everyone in the room, tell everyone to start the game
            photonView.RPC(nameof(LoadIntoGame), RpcTarget.All);
        }
    }
    
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        var playerId = (sbyte)(otherPlayer.ActorNumber - 1);
        Debug.LogWarning($"Player {playerId} left");
        
        // TODO: show message in UI that they disconnected

        _gameLogic?.PlayerLeft(playerId);
        UpdateVisuals();
    }

    [PunRPC]
    public void LoadIntoGame()
    {
        if(!PhotonNetwork.IsMasterClient)
        {
            // master is already in the map
            var loadingScene = SceneManager.LoadSceneAsync(_currentGameParameters.MapName);
            loadingScene.completed += (x) => StartGame();
        }
        else
        {
            StartGame();
        }
    }
    
    public void StartGame()
    {
        _mapController = FindObjectOfType<MapController>();

        _mapController.GetGameStateFromTilemap(out var tiles, out var zeroIsOddColumn);
        _mapController.OnHexCellClicked += OnTileClicked;

        _gameLogic = new GameLogic(_currentGameParameters, tiles, zeroIsOddColumn);
        
        _thisPlayerId = (sbyte)(PhotonNetwork.LocalPlayer.ActorNumber - 1); // NOTE: THIS IS BAD NEED BETTER WAY TO MAP TO IDS
        Debug.Log($"I am playerId {_thisPlayerId}");
        UpdateVisuals();
    }
    
    private void OnTileClicked(Vector2Int position)
    {
        if (_gameLogic.CurrentTurn == _thisPlayerId || LocalTesting)
        {
            photonView.RPC(nameof(DoTurnRPC), RpcTarget.All, GameLogic.PlayerActions.GrowRoot, position.x, position.y);
        }
    }

    private void UpdateVisuals()
    {
        var playerActionTiles = new HashSet<Vector2Int>();
        var actingPlayer = LocalTesting ? _gameLogic.CurrentTurn : _thisPlayerId;

        if (_gameLogic.CurrentTurn == actingPlayer)
        {
            playerActionTiles = _gameLogic.GetValidRootGrowthTiles(actingPlayer);
        }
        
        var fogTiles = _gameLogic.GetFogPositions(actingPlayer);
        _mapController.SetMap(_gameLogic.Tiles, playerActionTiles, fogTiles);
        
        UIController.Instance.SetTurnText(_gameLogic.CurrentTurn == actingPlayer, _gameLogic.RemainingMovesThisTurn);

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
