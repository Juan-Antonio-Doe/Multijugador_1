using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SimplePlayer : MonoBehaviourPun, IPunObservable {

    [SerializeField] private Material localPlayerMat;   // Material para el jugador local.
    private Renderer render; // Para cambiar el material del jugador local.

    [SerializeField] private float speed = 5f;

    [SerializeField] private Rigidbody rb;

    [SerializeField] private GameObject ballPrefab;

    private Vector3 networkPos; // La posición por red para este objeto. Siver para poder interpolar la posicion y que no se vea a trompicones.
    private Quaternion networkRot; // La rotación por red para este objeto.

    private void Awake() {
        render = GetComponent<Renderer>();
        rb = GetComponent<Rigidbody>();
    }

    void Start() {
        if (photonView.IsMine) {
            render.material = localPlayerMat;
        }
        else
            rb.isKinematic = true;  // Desactiva la fisica del jugador externo para que no se mueva.
    }

    private void Update() {
        /*if (photonView.IsMine) {
            Movement();
        }*/

        if (!photonView.IsMine) {
            transform.position = Vector3.MoveTowards(transform.position, networkPos, Time.deltaTime * 5);

            if (networkRot != null)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, networkRot, Time.deltaTime * 5000f);
        }
        else {
            // Jump
            if (Input.GetKeyDown(KeyCode.Space)) {
                rb.AddForce(Vector3.up * 35, ForceMode.Impulse);
            }

            if (Input.GetKeyDown(KeyCode.Keypad0)) {
                transform.position = new Vector3(0, 1, 0);
            }
            if (Input.GetKey(KeyCode.Keypad2)) {
                // Rota la al jugador continuamente mientras se pulsa la tecla.
                transform.rotation *= Quaternion.Euler(10, 10, 10);
            }

            if (Input.GetKeyDown(KeyCode.Keypad7)) {
                ColorChange();
            }
            if (Input.GetKeyDown(KeyCode.Keypad8)) {
                photonView.RPC(nameof(RPC_CreateBall), RpcTarget.All);
            }

            if (Input.GetKeyDown(KeyCode.Escape)) {
                PhotonNetwork.LeaveRoom();
                SceneManager.LoadScene("Menu");
            }
        }
    }

    private void FixedUpdate() {
        if (photonView.IsMine) {
            
            MovementV2();
        }
    }

    void Movement() {
        Vector3 _input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));

        transform.Translate(_input * speed * Time.deltaTime);
    }

    // Mejor manera de manejar el movimiento que incluye Rigidbody.
    void MovementV2() {
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(moveHorizontal, 0.0f, moveVertical);

        rb.AddForce(movement * speed);
    }

    void ColorChange() {
        photonView.RPC(nameof(RPC_ColorChange), RpcTarget.AllBuffered, "#002EFF");
    }

    [PunRPC]
    void RPC_ColorChange(string _hexColor, PhotonMessageInfo info) {
        Color _color = ColorUtility.TryParseHtmlString(_hexColor, out _color) ? _color : new Color(0, 0.1802516f, 1, 0.65f);
        render.material.color = _color;
        //render.material.color = new Color(0, 0.1802516f, 1, 0.65f); // Hex: #002EFF
    }

    [PunRPC]
    void RPC_CreateBall() {
        Instantiate(ballPrefab, transform.position + (transform.forward * 1.5f), ballPrefab.transform.rotation);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {
        if (stream.IsWriting) {
            // Enviamos la informacion al resto de jugadores.
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);

            //Debug.Log($"{photonView.Owner.NickName} envía pos.");
        } else {
            // Recibimos la informacion del resto de jugadores.
            networkPos = (Vector3) stream.ReceiveNext();
            networkRot = (Quaternion) stream.ReceiveNext();

            //Debug.Log($"{photonView.Owner.NickName} recibe pos.");
        }
    }
}
