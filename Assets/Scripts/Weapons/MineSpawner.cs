using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MineSpawner : MonoBehaviour, IOnEventCallback {

    [SerializeField] private Mine minePrefab;
    [SerializeField] private Vector2 spawnRange = new Vector2(5f, 5f);
    [SerializeField] private Vector2Int timeToSpawn = new Vector2Int(5, 10);
    [SerializeField] private const byte MINE_SPAWN = 27;    // Constante para identificar el evento de crear minas

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
        // Calculamos posición aleatoria usando los dos valores del vector2
        Vector3 _randomPos = new Vector3(Random.Range(-spawnRange.x, spawnRange.x), 0f, Random.Range(-spawnRange.y, spawnRange.y));

        object[] _parameters = new object[] { _randomPos, spawnID };    // Creamos un array de objetos para enviar los datos que queramos

        PhotonNetwork.RaiseEvent(MINE_SPAWN, _parameters, new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);
    }

    public void OnEvent(EventData photonEvent) {
        //Debug.Log($"Evento recibido: {photonEvent.Code}");

        if (photonEvent.Code == MINE_SPAWN) {
            object[] _parameters = (object[])photonEvent.CustomData;    // Recuperamos los datos enviados en el evento

            // Los datos se recuperan en el mismo orden en el que se enviaron
            Vector3 _spawnPos = (Vector3)_parameters[0];
            spawnID = (int)_parameters[1];  // Actualizamos el ID de la mina que recibimos del master client

            Mine _mine = Instantiate(minePrefab, _spawnPos, Quaternion.Euler(0, 0, 0));
            _mine.transform.SetParent(transform);
            _mine.SetID(spawnID);

            spawnID++;  // Incrementamos el ID de la mina para que la siguiente tenga un ID diferente
        }
    }
}
