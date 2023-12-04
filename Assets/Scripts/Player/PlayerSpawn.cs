using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PlayerSpawn : MonoBehaviourPunCallbacks {

    public static PlayerSpawn Instance;

    [SerializeField] private GameObject prefab;
    /*[SerializeField] */private float yPos = -0.5f;

    [SerializeField] private GameObject[] spawnPoints;

    [SerializeField] private Text playerList;

    [SerializeField] private GameObject cam;
    public GameObject Cam { get { return cam; } }

    private int playerLastCount = 0;

    void Awake() {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);
    }

    IEnumerator Start() {
        Hashtable _properties = new Hashtable() {
            { "KC", 0 },
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(_properties);

        playerLastCount = 0;

        spawnPoints = GameObject.FindGameObjectsWithTag("Respawn");

        Vector3 _spawnPos = GetSpawnPosition();

        Init();

        yield return new WaitForSeconds(1f); // Hay que añadir este Delay para que funcione correctamente.
        // Crea un prefab para todos los usuarios conectados en la posicion indicada.
        PhotonNetwork.Instantiate(prefab.name, _spawnPos, prefab.transform.rotation);
    }

    private void Update() {
        if (PhotonNetwork.InRoom) {
            if (PhotonNetwork.CurrentRoom.PlayerCount != playerLastCount) {   // Compruba que el contador se ha actualizado.
                //Debug.Log($"Players in room: {PhotonNetwork.CurrentRoom.PlayerCount}");

                playerList.text = $"Players in room: {PhotonNetwork.CurrentRoom.PlayerCount}";
                playerLastCount = PhotonNetwork.CurrentRoom.PlayerCount;
            }
        }
    }

    void Init() {
        // Bloqueamos el cursor.
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public Vector3 GetSpawnPosition() {
        Vector3 _spawnPos = spawnPoints[Random.Range(0, spawnPoints.Length)].transform.position;
        _spawnPos.y = yPos;

        return _spawnPos;
    }

    public void AddToKillCounter() {
        PhotonNetwork.CurrentRoom.CustomProperties["KC"] = (int) PhotonNetwork.CurrentRoom.CustomProperties["KC"] + 1;

        /*int _killCounter = (int)PhotonNetwork.CurrentRoom.CustomProperties["KC"];
        _killCounter++;
        PhotonNetwork.CurrentRoom.CustomProperties["KC"] = _killCounter;*/

        PhotonNetwork.CurrentRoom.SetCustomProperties(PhotonNetwork.CurrentRoom.CustomProperties);
    }

    public override void OnDisconnected(DisconnectCause cause) {
        base.OnDisconnected(cause);

        SceneManager.LoadScene(0);
    }
}
