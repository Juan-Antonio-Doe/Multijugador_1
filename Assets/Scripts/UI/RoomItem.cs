using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RoomItem : MonoBehaviour {

	[field: Header("Room Item settings")]
	[field: SerializeField] private TMP_Text roomInfoText { get; set; }

	private RoomInfo roomInfo { get; set; }

	public void SetRoomInfo(RoomInfo roomInfo) {
        this.roomInfo = roomInfo;
        roomInfoText.text = $"{roomInfo.Name} - ({roomInfo.PlayerCount}/{roomInfo.MaxPlayers})";
    }

	public void JoinRoom() {
        if (roomInfo != null)
            PhotonNetwork.JoinRoom(roomInfo.Name);
    }

    void OnDestroy() {
        if (gameObject == null)
            return;
    }
}
