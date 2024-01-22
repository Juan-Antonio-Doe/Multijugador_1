using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviourPunCallbacks {

    [SerializeField] private string roomName = "Default Room";
    private int lastCount = 0;

    [field: Header("Room Item settings")]
    [field: SerializeField] private RoomItem roomItemPrefab { get; set; }
    [field: SerializeField] private Transform roomListLayout { get; set; }

    private List<RoomInfo> cachedRoomList { get; set; } = new List<RoomInfo>();
	
    void Start() {
        lastCount = 0;
        PhotonNetwork.NickName = "Zero";

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        Connect();
    }

    private void Update() {
        if (PhotonNetwork.InRoom) {
            if (PhotonNetwork.CurrentRoom.PlayerCount != lastCount) {   // Compruba que el contador se ha actualizado.
                //Debug.Log($"Players in room: {PhotonNetwork.CurrentRoom.PlayerCount}");
                lastCount = PhotonNetwork.CurrentRoom.PlayerCount;
            }
        }
    }

    public override void OnConnectedToMaster() {
        // Entramos en el lobby de Photon.
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby() {
        Debug.Log("Joined lobby");
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList) {
        if (roomList != null && roomItemPrefab != null) {

            // Comprobamos si se ha recibido alguna actualización. [Profesor]
            /*if (roomList.Count > 0) {
                Debug.Log($"Received update for {roomList.Count} rooms");
            }*/

            // Destruimos todos los RoomItem que haya en el layout. [Profesor]
            /*for (int i = 0; i < roomListLayout.childCount; i++) {
                Destroy(roomListLayout.GetChild(i).gameObject);
            }*/
            for (int i = roomListLayout.childCount - 1; i >= 0; i--) {
                Destroy(roomListLayout.GetChild(i).gameObject);
            }

            foreach (RoomInfo room in roomList) {

                // [Profesor]
                /*if (cachedRoomList.Contains(room)) {
                    //if (room.PlayerCount == 0 || !room.IsOpen || !room.IsVisible) {
                    if (room.RemovedFromList) {
                        cachedRoomList.Remove(room);
                    }
                    else {
                        cachedRoomList[cachedRoomList.IndexOf(room)] = room;
                    }
                }
                else {
                    cachedRoomList.Add(room);
                }*/

                /// IA Code:
                if (!room.IsOpen || !room.IsVisible || room.RemovedFromList) {
                    if (cachedRoomList.Contains(room)) {
                        cachedRoomList.Remove(room);
                    }
                } else {
                    if (!cachedRoomList.Contains(room)) {
                        cachedRoomList.Add(room);
                    } else {
                        int index = cachedRoomList.IndexOf(room);
                        cachedRoomList[index] = room;
                    }
                }
            }

            foreach (RoomInfo room in cachedRoomList) {
                RoomItem roomInfo = Instantiate(roomItemPrefab, roomListLayout);
                roomInfo.SetRoomInfo(room);
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
