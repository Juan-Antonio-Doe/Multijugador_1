using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

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
    private bool hasGrenade = true; // Para limitar que solo se pueda lanzar una granada.

    [Header("Shoot & Reload")]
    [SerializeField] private int maxAmmo = 30;
    [SerializeField] private int currentAmmo = 30;
    private bool isReloading;
    [SerializeField] private int fireRate = 10;
    private float shootTimer = 0f;

    [SerializeField] private ParticleSystem localShootPS;
    [SerializeField] private ParticleSystem swatShootPS;

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

            // Inicializamos las propiedades del jugador.
            Hashtable _properties = new Hashtable() {
                { "K", 0 }, // Kills
                { "D", 0 }, // Deaths
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(_properties);
        }
        else {
            armsAnim = arms.GetComponent<Animator>();
            gameObject.layer = 0;   // Layer: Default

            currentAmmo = maxAmmo;

            PhotonNetwork.LocalPlayer.TagObject = gameObject;
        }
    }

    void Update() {
        if (photonView.IsMine) {

            if (health > 0) {
                MovementAndRotation();
                Shoot();
            }

            if ((Input.GetKeyDown(KeyCode.G) || Input.GetMouseButtonDown(2)) && hasGrenade) {
                //hasGrenade = false;
                StartCoroutine(ThrowGrenadeCo());
            }

            if (Input.GetKeyDown(KeyCode.BackQuote)) {
                RageQuit();
            }

            #region Cheats
            if (Input.GetKeyDown(KeyCode.Keypad0)) {
                cc.enabled = false;
                transform.position = new Vector3(0, -5, 0);
                cc.enabled = true;
            }
            if (Input.GetKey(KeyCode.Keypad2)) {
                // Rota la al jugador continuamente mientras se pulsa la tecla.
                transform.rotation *= Quaternion.Euler(10, 10, 10);
            }
            if (Input.GetKey(KeyCode.Keypad1)) {
                // Resetea la posición del jugador a la 0,0,0 del padre.
                cc.enabled = false;
                transform.localPosition = Vector3.zero;
                cc.enabled = true;
            }
            if (Input.GetKey(KeyCode.Keypad3)) {
                // Resetea el transform.
                cc.enabled = false;
                transform.position = Vector3.zero;
                transform.rotation = new Quaternion(0, 0, 0, 0);
                cc.enabled = true;

            }
            if (Input.GetKey(KeyCode.Keypad7)) {
                hasGrenade = true;
            }
            #endregion
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
        if (Input.GetMouseButton(0) && (currentAmmo > 0) && Time.time > shootTimer) {
            shootTimer = Time.time + (1f / fireRate);

            Ray _ray = cam.ScreenPointToRay(new Vector3(0.5f, 0.5f, cam.nearClipPlane));

            if (Physics.Raycast(_ray.origin, cam.transform.forward, out RaycastHit _hit, 1000f, damageLayer)) {
                //Debug.Log($"Hit <b>{_hit.collider.gameObject.name}</b> for <b>{damage}</b> damage.");

                if (_hit.collider.gameObject.CompareTag("Player")) {
                    _hit.collider.gameObject.GetComponent<PlayerShooter>().TakeDamage(damage);
                }
            }

            photonView.RPC(nameof(RPC_Shoot), RpcTarget.Others);
            armsAnim.SetTrigger("Shoot");
            localShootPS.Play();

            currentAmmo--;
        }

        if (Input.GetKeyDown(KeyCode.R) && (currentAmmo < maxAmmo) && !isReloading) {
            isReloading = true;
            photonView.RPC(nameof(RPC_Reload), RpcTarget.All);
        }
    }

    [PunRPC]
    void RPC_Shoot() {
        // El resto de jugadores solo tiene que ver la parte visual del disparo (animación, particulas, sonido...).
        swatAnim.SetTrigger("Shoot");
        swatShootPS.Play();
    }

    public void TakeDamage(int _damage) {
        photonView.RPC(nameof(RPC_TakeDamage), photonView.Owner, _damage);
    }

    // Este RPC solo se llama localmente por el jugador que recibe el daño.
    [PunRPC]
    void RPC_TakeDamage(int _damage, PhotonMessageInfo info) {
        if (health > 0) {
            health -= _damage;
            if (health <= 0) {
                /*
                 * Cuando el jugador muere localmente, avisa a todo el mundo de que ha muerto.
                 * Para avisar al jugador que nos ha matado y que se pueda sumar una eliminacion, pasamos
                 * su jugador como parametro del RPC (info.Sender es el jugador que nos envia el RPC).
                 */
                photonView.RPC(nameof(RPC_Die), RpcTarget.All, info.Sender);
                PlayerSpawn.Instance.AddToKillCounter();

                /*Hashtable _properties = new Hashtable() {
                    { "D", (int) PhotonNetwork.LocalPlayer.CustomProperties["D"] + 1 },
                };
                PhotonNetwork.LocalPlayer.SetCustomProperties(_properties);*/

                /// Profesor:
                // Sumamos 1 a nuestra propiedad de muertes.
                int _deaths = (int)PhotonNetwork.LocalPlayer.CustomProperties["D"] + 1;
                PhotonNetwork.LocalPlayer.CustomProperties["D"] = _deaths;
                PhotonNetwork.LocalPlayer.SetCustomProperties(PhotonNetwork.LocalPlayer.CustomProperties);
            }
        }

        //Debug.Log($"Daño: <color=#FF0000><b>{photonView.Owner.NickName}</b></color> tiene <color=#00FF00><b>{health}</b></color> de vida.");
    }

    [PunRPC]
    void RPC_Die(Player _killer) {
        StartCoroutine(RespawnCo());

        if (_killer == PhotonNetwork.LocalPlayer) {
            /*Hashtable _properties = new Hashtable() {
                    { "K", (int) PhotonNetwork.LocalPlayer.CustomProperties["K"] + 1 },
                };
                PhotonNetwork.LocalPlayer.SetCustomProperties(_properties);*/

            /// Profesor:
            // Sumamos 1 a nuestra propiedad de kills.
            int _kills = (int)PhotonNetwork.LocalPlayer.CustomProperties["K"] + 1;
            PhotonNetwork.LocalPlayer.CustomProperties["K"] = _kills;
            PhotonNetwork.LocalPlayer.SetCustomProperties(PhotonNetwork.LocalPlayer.CustomProperties);

            Debug.Log($"<color=red><b>{_killer.NickName}</b></color> me ha matado.");
        }
    }

    IEnumerator RespawnCo() {
        if (photonView.IsMine) {
            arms.gameObject.SetActive(false);
            PlayerSpawn.Instance.Cam.gameObject.SetActive(true);

            cc.enabled = false;
            transform.position = PlayerSpawn.Instance.GetSpawnPosition();
            cc.enabled = true;
        }
        else {
            swatAnim.gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(3f);

        if (photonView.IsMine) {
            arms.gameObject.SetActive(true);
            PlayerSpawn.Instance.Cam.gameObject.SetActive(false);

            health = 100;
            currentAmmo = maxAmmo;
            hasGrenade = true;
        }
        else {
            swatAnim.gameObject.SetActive(true);
        }
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

    IEnumerator ReloadCo() {

        if (photonView.IsMine) {
            armsAnim.SetTrigger("Reload");
            yield return new WaitForSeconds(64f / 30f);
            currentAmmo = maxAmmo;
            isReloading = false;
        }
        else {
            armsAnim.SetTrigger("Reload");
            yield return null;
        }

    }

    [PunRPC]
    void RPC_Reload() {
        StartCoroutine(ReloadCo());
    }

    void RageQuit() {
        PhotonNetwork.Disconnect();
    }

    void OnGUI() {
        if (photonView.IsMine) {
            GUI.color = Color.green;
            GUI.Label(new Rect(10, 10, 100, 20), $"Vida: {health}");

            GUI.color = Color.cyan;
            GUI.Label(new Rect(10, 30, 100, 20), $"Municion: {currentAmmo}/{maxAmmo}");

            GUI.color = Color.red;
            GUI.Label(new Rect((Screen.width / 2) - 20, 14, 100, 50), $"Muertes totales: \n\t {PhotonNetwork.CurrentRoom.CustomProperties["KC"]}");
        }

    }

    void OnDrawGizmos() {
#if UNITY_EDITOR
        GUI.color = Color.green;
        Handles.Label(transform.position + new Vector3(0, 1.8f, 0), gameObject.name);
#endif
    }

    private void OnDestroy() {
        if (gameObject == null)
            return;

        if (photonView.IsMine) {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
}
