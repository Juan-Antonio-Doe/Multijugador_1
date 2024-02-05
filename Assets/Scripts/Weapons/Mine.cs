using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class Mine : MonoBehaviour, IOnEventCallback {

    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private int damage = 30;
    [SerializeField] private LayerMask damageLayer;
    [SerializeField] private int mineID;    // Para diferenciar las minas entre sí

    [SerializeField] private GameObject explosionPrefab;    // Prefab del sistema de particulas de la explosion.

    private bool exploded;

    private const byte MINE_EXPLODE = 88;

    void Start() {
        PhotonNetwork.AddCallbackTarget(this);
    }

    void OnTriggerEnter(Collider other) {
        if (exploded) return;

        if (other.CompareTag("Player")) {
            PhotonNetwork.RaiseEvent(MINE_EXPLODE, mineID, new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);
        }
    }

    void Explode() {
        if (exploded) 
            return;

        exploded = true;

        // Remove the cache of the spawn mine (This event do nothing on code, only remove the cache).
        Hashtable hashtable = new();
        hashtable.Add("P", transform.position);
        hashtable.Add("ID", mineID);

        /*PhotonNetwork.RaiseEvent(MineSpawner.MINE_SPAWN, hashtable, new RaiseEventOptions { Receivers = ReceiverGroup.All, 
            CachingOption = EventCaching.RemoveFromRoomCache }, SendOptions.SendReliable);*/

        if (PhotonNetwork.IsMasterClient) {
            Collider[] _targets = Physics.OverlapSphere(transform.position, explosionRadius, damageLayer);

            for (int i = 0; i < _targets.Length; i++) {
                if (_targets[i] != null && _targets[i].CompareTag("Player")) {
                    _targets[i].GetComponent<PlayerShooter>().TakeDamage(damage);
                }
            }
        }

        /*
         * Como todos los jugadores tienen una copia de la granada en su juego, con instanciar localmente la explosion,
         * la vera todo el mundo sin necesidad de sincronizar nada.
         */

        Instantiate(explosionPrefab, transform.position, explosionPrefab.transform.rotation);
        Destroy(gameObject);
    }

    void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }

    public void OnEvent(EventData photonEvent) {
        //Debug.Log($"Evento recibido: {photonEvent.Code}");

        if (photonEvent.Code == MINE_EXPLODE) {
            if (mineID == (int)photonEvent.CustomData)
                Explode();
        }
    }

    public void SetID(int spawnID) {
        mineID = spawnID;
    }

    void OnDestroy() {
        if (gameObject == null) 
            return;
    }
}
