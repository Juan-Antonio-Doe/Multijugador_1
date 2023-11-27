using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grenade : MonoBehaviour {

    [SerializeField] private int damage = 40;
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private float timeToExplode = 3f;
    [SerializeField] private LayerMask damageLayer;

    [SerializeField] private GameObject explosionPrefab;    // Prefab del sistema de particulas de la explosion.
	
    IEnumerator Start() {
        yield return new WaitForSeconds(timeToExplode);
        Explode();
    }

    void Explode() {
        if (PhotonNetwork.IsMasterClient) {
            Collider[] _targets = Physics.OverlapSphere(transform.position, explosionRadius, damageLayer);

            for (int i = 0; i < _targets.Length; i++) {
                if (_targets[i].CompareTag("Player")) {
                    _targets[i].GetComponent<PlayerShooter>().TakeDamage(damage);
                }
            }
        }
        
        Destroy(gameObject);
    }

    void OnDrawGizmos() {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }

    void OnDestroy() {
        /*
         * Como todos los jugadores tienen una copia de la granada en su juego, con instanciar localmente la explosion,
         * la vera todo el mundo sin necesidad de sincronizar nada.
         */

        Instantiate(explosionPrefab, transform.position, explosionPrefab.transform.rotation);
    }
}
