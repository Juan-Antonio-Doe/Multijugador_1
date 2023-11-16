using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerShooter : MonoBehaviourPun, IPunObservable {

    [SerializeField] private CharacterController cc;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float rotationSpeed = 200f;
    [SerializeField] private Camera cam;

    [Header("Combat Stats")]
    [SerializeField] private int health = 100;
    [SerializeField] private int damage = 10;

    [SerializeField] private LayerMask damageLayer;

    [Header("Grenade Throw")]
    [SerializeField] private Rigidbody grenadePrefab;
    [SerializeField] private Transform grenadeSpawnPoint;
    [SerializeField] private int throwForce = 6;

    [SerializeField] private Transform arms;    // El objeto con los brazos y la camara del personaje.
    private float armsRotationX = 0f;    // La rotación en X de los brazos.

    [SerializeField] private Animator armsAnim;   // El animator con el modelo del personaje que se mostrará al jugador.    
    [SerializeField] private Animator swatAnim;   // El animator con el modelo del personaje que se mostrará a otros jugadores.

    private Vector3 networkPos; // La posición por red para este objeto. Siver para poder interpolar la posicion y que no se vea a trompicones.
    private Quaternion networkRot; // La rotación por red para este objeto.
    private float netXRot; // Para interpolar la rotación en eje X de los brazos.
    private int netSwatMovement;    // Para sincronizar la animación de movimiento del swat.

    void Start() {
        TryGetComponent(out cc);
        cam = GetComponentInChildren<Camera>();
        gameObject.name = photonView.Owner.NickName;

        if (!photonView.IsMine) {
            swatAnim.gameObject.SetActive(true);
            arms.gameObject.SetActive(false);
        }
        else {
            armsAnim = arms.GetComponent<Animator>();
            gameObject.layer = 0;   // Layer: Default
        }
    }

    void Update() {
        if (photonView.IsMine) {
            MovementAndRotation();
            Shoot();

            if (Input.GetKeyDown(KeyCode.G) || Input.GetMouseButtonDown(2)) {
                StartCoroutine(ThrowGrenadeCo());
            }

            if (Input.GetKeyDown(KeyCode.Keypad0)) {
                transform.position = new Vector3(0, -5, 0);
            }
            if (Input.GetKey(KeyCode.Keypad2)) {
                // Rota la al jugador continuamente mientras se pulsa la tecla.
                transform.rotation *= Quaternion.Euler(10, 10, 10);
            }
        }
        else {
            SyncOtherPlayers();
        }
    }

    void MovementAndRotation() {
        Vector3 _input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));

        _input = transform.TransformDirection(_input);

        cc.Move(_input.normalized * speed * Time.deltaTime);
        transform.Rotate(0, Input.GetAxisRaw("Mouse X") * rotationSpeed * Time.deltaTime, 0);

        armsRotationX -= Input.GetAxisRaw("Mouse Y") * rotationSpeed * Time.deltaTime;
        armsRotationX = Mathf.Clamp(armsRotationX, -90f, 90f);
        arms.localEulerAngles = new Vector3(armsRotationX, 0, 0);

        // Activamos la animación de movimiento en función al vector del input.
        armsAnim.SetFloat("Speed", _input.sqrMagnitude);

        // Sincronizamos la animación de movimiento del swat.
        SwatMovementAnimation(_input);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {
        if (stream.IsWriting) {
            // Enviamos la posición y la rotación del jugador.
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);

            /* Como la rotación en el eje Y para el swat se hace mediante animación, transformamos el valor de la rotación a un valor que vaya
             * de -1 (abajo) a 1 (arriba).
             */
            stream.SendNext(armsRotationX / -90f);   

            stream.SendNext(netSwatMovement);
        }
        else {
            // Recibimos la posición y la rotación del jugador.
            networkPos = (Vector3)stream.ReceiveNext();
            networkRot = (Quaternion)stream.ReceiveNext();
            netXRot = (float)stream.ReceiveNext();
            netSwatMovement = (int)stream.ReceiveNext();

        }
    }

    void SyncOtherPlayers() {
        transform.position = Vector3.MoveTowards(transform.position, networkPos, Time.deltaTime * 5);

        if (networkRot != null)
            transform.rotation = Quaternion.RotateTowards(transform.rotation, networkRot, Time.deltaTime * 1000f);

        // Sincronizamos e interpolamos la rotación en el eje X de los brazos.
        armsRotationX = Mathf.Lerp(swatAnim.GetFloat("Aiming"), netXRot, Time.deltaTime * 10f);
        swatAnim.SetFloat("Aiming", armsRotationX);

        // Sincronizamos la animación de movimiento del swat.
        swatAnim.SetFloat("Movement", netSwatMovement);
    }

    void Shoot() {
        if (Input.GetMouseButton(0)) {
            Ray _ray = cam.ScreenPointToRay(new Vector3(0.5f, 0.5f, cam.nearClipPlane));

            if (Physics.Raycast(_ray.origin, cam.transform.forward, out RaycastHit _hit, 1000f, damageLayer)) {
                //Debug.Log($"Hit <b>{_hit.collider.gameObject.name}</b> for <b>{damage}</b> damage.");

                if (_hit.collider.gameObject.CompareTag("Player")) {
                    _hit.collider.gameObject.GetComponent<PlayerShooter>().TakeDamage(damage);
                }
            }

            photonView.RPC(nameof(RPC_Shoot), RpcTarget.Others);
            swatAnim.SetTrigger("Shoot");
        }
    }

    [PunRPC]
    void RPC_Shoot() {
        //photonView.RPC(nameof(RPC_Shoot), RpcTarget.Others);
        swatAnim.SetTrigger("Shoot");
    }

    public void TakeDamage(int _damage) {
        photonView.RPC(nameof(RPC_TakeDamage), photonView.Owner, _damage);
    }

    [PunRPC]
    void RPC_TakeDamage(int _damage) {
        if (health > 0)
            health -= _damage;

        Debug.Log($"Daño: <b>{photonView.Owner.NickName}</b> tiene <b>{health}</b> de vida.");
    }

    void SwatMovementAnimation(Vector3 _input) {

        // Parado
        if (_input == Vector3.zero)
            netSwatMovement = 0;

        if (_input.x > 0.1f)           // Derecha
            netSwatMovement = 3;
        else if (_input.x < -0.1f)      // Izquierda
            netSwatMovement = 4;

        if (_input.z > 0.1f)           // Adelante
            netSwatMovement = 1;
        else if (_input.z < -0.1f)      // Atrás
            netSwatMovement = 2;

    }

    [PunRPC]
    void RPC_ThrowGrenade(Vector3 _spawnPos, Vector3 _force) {
        Rigidbody _grenade = Instantiate(grenadePrefab, _spawnPos, Quaternion.identity);

        _grenade.AddForce(_force, ForceMode.Impulse);
    }

    IEnumerator ThrowGrenadeCo() {
        armsAnim.SetTrigger("ThrowGrenade");
        yield return new WaitForSeconds(0.3f);  // animation frame = 9, frameRate = 30fps, 9/30 = 0.3f

        photonView.RPC(nameof(RPC_ThrowGrenade), RpcTarget.All, grenadeSpawnPoint.position, cam.transform.forward * throwForce);
    }

    void OnGUI() {
        if (photonView.IsMine)
            GUI.Label(new Rect(10, 10, 100, 20), $"Vida: {health}");
    }
}
