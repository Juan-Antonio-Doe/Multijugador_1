using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviourPunCallbacks {

    [SerializeField] private string roomName = "Default Room";
    private int lastCount = 0;
	
    void Start() {
        lastCount = 0;
        PhotonNetwork.NickName = "Zero";

        Connect();
    }

    private void Update() {
        if (PhotonNetwork.InRoom) {
            if (PhotonNetwork.CurrentRoom.PlayerCount != lastCount) {   // Compruba que el contador se ha actualizado.
                Debug.Log($"Players in room: {PhotonNetwork.CurrentRoom.PlayerCount}");
                lastCount = PhotonNetwork.CurrentRoom.PlayerCount;
            }
        }
    }

    public void Connect() {
        PhotonNetwork.AutomaticallySyncScene = true;    // Activa la sincronizacion de escenas locales (cambiar el Mater Client, cambie el usuario).

        PhotonNetwork.SendRate = 10;    // Numero de veces que se envia la informacion por segundo.

        PhotonNetwork.ConnectUsingSettings(); // Conecta con Photon
    }

    public void CreateRoom() {
        PhotonNetwork.CreateRoom(roomName); // Crea la sala. Null genera un string al azar.
    }

    public void JoinRoom() {
        //PhotonNetwork.JoinRandomRoom(); // Conecta con cualquier sala existente al azar.
        PhotonNetwork.JoinRoom(roomName);
    }

    public override void OnCreatedRoom() {
        Debug.Log("Created a room");
        if (PhotonNetwork.IsMasterClient) {
            PhotonNetwork.LoadLevel("Game");
        }
    }

    // Se llama automáticamente cuando nos unimos a una room.
    public override void OnJoinedRoom() {
        Debug.Log($"Joined room: {PhotonNetwork.CurrentRoom.Name}");
        //Debug.Log($"Players in room: {PhotonNetwork.CurrentRoom.PlayerCount}");
    }

    public void ChangeRoomName(string _roomName) {
        roomName = _roomName;
    }

    public void ChangeNickName(string _nickName) {
        PhotonNetwork.NickName = _nickName;
    }
}
