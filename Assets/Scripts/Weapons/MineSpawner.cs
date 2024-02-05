using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class MineSpawner : MonoBehaviour, IOnEventCallback {

    [SerializeField] private Mine minePrefab;
    [SerializeField] private Collider spawnArea;
    [SerializeField] private Vector2Int timeToSpawn = new Vector2Int(5, 10);

    [SerializeField] private float checkRadius = 0.5f;
    [SerializeField] private LayerMask obstacleLayer;

    [SerializeField] public const byte MINE_SPAWN = 27;    // Constante para identificar el evento de crear minas

    private int spawnID;    // Para diferenciar las minas entre sí
	
    void Start() {
        PhotonNetwork.AddCallbackTarget(this);  // Si queremos que el objeto pueda recibir eventos de Photon, debemos añadirlo a la lista de callback targets
        
        StartCoroutine(SpawnMinesCo());
    }

    IEnumerator SpawnMinesCo() {
        while (true) {
            // Tiempo aleatorio usando los dos valores del vector2Int
            int _randomTime = Random.Range(timeToSpawn.x, timeToSpawn.y);

            yield return new WaitForSeconds(_randomTime);
            
            if (PhotonNetwork.IsMasterClient)
                SpawnMine();
        }
    }

    void SpawnMine() {
        // Calculamos posición aleatoria usando los bounds del collider para que siempre elija un punto dentro del área
        Vector3 _randomPos = new Vector3(Random.Range(-spawnArea.bounds.extents.x, spawnArea.bounds.extents.x), 0,
            Random.Range(-spawnArea.bounds.extents.z, spawnArea.bounds.extents.z));

        // Comprobamos que no haya obstáculos en la posición aleatoria
        if (Physics.CheckSphere(_randomPos, checkRadius, obstacleLayer)) {
            SpawnMine();    // Si hay obstáculos, volvemos a llamar a la función para que calcule otra posición
            return;
        }

        //object[] _parameters = new object[] { _randomPos, spawnID };    // Creamos un array de objetos para enviar los datos que queramos
        Hashtable _parameters = new Hashtable();    // Creamos un hashtable para enviar los datos que queramos
        _parameters.Add("P", _randomPos);
        _parameters.Add("ID", spawnID);

        PhotonNetwork.RaiseEvent(MINE_SPAWN, _parameters, new RaiseEventOptions { Receivers = ReceiverGroup.All, 
            CachingOption = EventCaching.AddToRoomCache }, SendOptions.SendReliable);
    }

    public void OnEvent(EventData photonEvent) {
        //Debug.Log($"Evento recibido: {photonEvent.Code}");

        if (photonEvent.Code == MINE_SPAWN) {
            //object[] _parameters = (object[])photonEvent.CustomData;    // Recuperamos los datos enviados en el evento
            Hashtable _parameters = (Hashtable)photonEvent.CustomData;    // Recuperamos los datos enviados en el evento

            /*// Los datos se recuperan en el mismo orden en el que se enviaron
            Vector3 _spawnPos = (Vector3)_parameters[0];
            spawnID = (int)_parameters[1];  // Actualizamos el ID de la mina que recibimos del master client*/

            Vector3 _spawnPos = (Vector3)_parameters["P"];
            spawnID = (int)_parameters["ID"];

            Mine _mine = Instantiate(minePrefab, _spawnPos, Quaternion.Euler(0, 0, 0));
            //_mine.transform.SetParent(spawnArea.transform);
            _mine.SetID(spawnID);

            spawnID++;  // Incrementamos el ID de la mina para que la siguiente tenga un ID diferente
        }
    }
}
