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

    [SerializeField] private Text endText;

    [SerializeField] private int maxKills = 10;

    private int playerLastCount = 0;

    GUIStyle style = new GUIStyle();

    void Awake() {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);
    }

    IEnumerator Start() {
        playerLastCount = 0;

        spawnPoints = GameObject.FindGameObjectsWithTag("Respawn");

        Vector3 _spawnPos = GetSpawnPosition();

        Init();

        yield return new WaitForSeconds(1f); // Hay que a�adir este Delay para que funcione correctamente.

        Hashtable _properties = new Hashtable() {
            { "KC", 0 },
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(_properties);

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

            if (PhotonNetwork.MasterClient.CustomProperties == null) 
                return;

            int _kills = (int)PhotonNetwork.MasterClient.CustomProperties["K"];
            int _deaths = (int)PhotonNetwork.MasterClient.CustomProperties["D"];
            //Debug.Log($"MasterClient:: Kills: <color=red>{_kills}</color> | Deaths: <color=blue>{_deaths}</color>");
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

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged) {
        if (propertiesThatChanged != null && propertiesThatChanged.ContainsKey("KC")) {
            if ((int)propertiesThatChanged["KC"] >= maxKills) {
                EndGame(propertiesThatChanged);
            }
        }
    }

    void EndGame(Hashtable propertiesThatChanged) {
        //Debug.Log($"Game Ended: \n Current deaths: {(int)propertiesThatChanged["KC"]}");
        cam.SetActive(true);

        int _highestKills = 0;
        string _winner = "";

        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++) {
            int _kills = (int)PhotonNetwork.PlayerList[i].CustomProperties["K"];

            if (_kills > _highestKills) {
                _highestKills = _kills;
                _winner = PhotonNetwork.PlayerList[i].NickName;
            }
        }

        endText.transform.parent.gameObject.SetActive(true);
        endText.text = $"Game Ended \n " +
            $"Current deaths: {(int)propertiesThatChanged["KC"]} \n" +
            $"Winner: <color=green>{_winner}</color> \t Kills: <color=red>{_highestKills}</color>";

        Destroy((GameObject)PhotonNetwork.LocalPlayer.TagObject);

        StartCoroutine(BackToMenuCo());
    }

    IEnumerator BackToMenuCo() {
        yield return new WaitForSeconds(5f);
        PhotonNetwork.Disconnect();
    }

    public override void OnDisconnected(DisconnectCause cause) {
        base.OnDisconnected(cause);

        SceneManager.LoadScene(0);
    }

    private void OnGUI() {
        try {
            if (Input.GetKey(KeyCode.Tab) == false) {
                return;
            }
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "SCOREBOARD");
            style.alignment = TextAnchor.MiddleCenter;
            GUILayout.BeginArea(new Rect(0f, 100f, Screen.width, Screen.height - 100f));
            for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++) {
                string label = $"{PhotonNetwork.PlayerList[i].NickName} - {(int)PhotonNetwork.PlayerList[i].CustomProperties["K"]} " +
                    $"| {(int)PhotonNetwork.PlayerList[i].CustomProperties["D"]}";

                GUILayout.Label(label, style);
            }
            GUILayout.EndArea();
        }
        catch { }
    }
}
