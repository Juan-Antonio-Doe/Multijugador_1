using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

public class Enemy : MonoBehaviour, IOnEventCallback {

    [field: SerializeField] private Transform[] points { get; set; }
    [field: SerializeField] private NavMeshAgent agent { get; set; }
    [field: SerializeField] private float range { get; set; } = 4f;
    [field: SerializeField] private Transform target { get; set; }
    [field: SerializeField] private LayerMask targetMasks { get; set; } = 1 << 0 | 1 << 6;

    public const int ENEMY_SYNC_POSITION = 44, ENEMY_SYNC_TARGET = 45;
	
    void Start() {
        PhotonNetwork.AddCallbackTarget(this);

        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        InvokeRepeating(nameof(PickPoint), 10f, 10f);
    }

    void Update() {
        if (target == null) {
            if (PhotonNetwork.IsMasterClient) {
                DetectTarget();
            }
        } else {
            agent.SetDestination(target.position);
        }
    }

    void PickPoint() {
        Vector3 _position = points[Random.Range(0, points.Length)].position;

        if (PhotonNetwork.IsMasterClient) {
            PhotonNetwork.RaiseEvent(ENEMY_SYNC_POSITION, _position, new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);
        }
    }

    void DetectTarget() {
        Collider[] _targets = Physics.OverlapSphere(transform.position, range, targetMasks);
        target = null; // Reset the attack target

        foreach (Collider target in _targets) {
            if (target.CompareTag("Player")) {
                this.target = target.gameObject.transform;
                int _viewID = this.target.GetComponent<PhotonView>().ViewID;

                if (PhotonNetwork.IsMasterClient) {
                    PhotonNetwork.RaiseEvent(ENEMY_SYNC_TARGET, _viewID, new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);
                }

                return; // Stop the loop once the first valid target is found
            }
        }
    }

    public void OnEvent(EventData photonEvent) {
        if (photonEvent.Code == ENEMY_SYNC_POSITION) {
            agent.SetDestination((Vector3)photonEvent.CustomData);
        }

        if (photonEvent.Code == ENEMY_SYNC_TARGET) {
            target = PhotonView.Find((int)photonEvent.CustomData).transform;
        }
    }
}
