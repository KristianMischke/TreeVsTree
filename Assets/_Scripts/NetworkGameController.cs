using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NetworkGameController : MonoBehaviourPunCallbacks
{
    // Start is called before the first frame update
    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
    }
    
    public void OnConnectedToServer()
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
        
        // PhotonNetwork.LocalPlayer.ActorNumber
    }

    [PunRPC]
    public void SendTurn()
    {
        
    }
}
