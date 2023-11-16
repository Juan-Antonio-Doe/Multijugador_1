using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSpawn : MonoBehaviour {

    [SerializeField] private GameObject prefab;
    /*[SerializeField] */private float yPos = 0.25f;

    [SerializeField] private Text playerList;

    private int lastCount = 0;

    IEnumerator Start() {
        lastCount = 0;

        Vector3 _spawnPos = new Vector3(0, yPos, 0);
        _spawnPos.x = Random.Range(-10, 10);
        _spawnPos.z = Random.Range(-10, 10);
        Init();

        yield return new WaitForSeconds(1f); // Hay que añadir este Delay para que funcione correctamente.
        // Crea un prefab para todos los usuarios conectados en la posicion indicada.
        PhotonNetwork.Instantiate(prefab.name, _spawnPos, prefab.transform.rotation);
    }

    private void Update() {
        if (PhotonNetwork.InRoom) {
            if (PhotonNetwork.CurrentRoom.PlayerCount != lastCount) {   // Compruba que el contador se ha actualizado.
                //Debug.Log($"Players in room: {PhotonNetwork.CurrentRoom.PlayerCount}");

                playerList.text = $"Players in room: {PhotonNetwork.CurrentRoom.PlayerCount}";
                lastCount = PhotonNetwork.CurrentRoom.PlayerCount;
            }

            if (Input.GetKeyDown(KeyCode.Escape)) {
                Init();
            }
        }
    }

    void Init() {
        // Bloqueamos el cursor.
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

}
