using UnityEngine;

public class LogicaKobu : MonoBehaviour
{
    public float velocidad = 1.8f;
    public float velocidadCorrer = 2.8f;
    public float velocidadRotacion = 150f;

    private Animator anim;

    public float x, y;

    public Rigidbody rb;
    public float fuerzaDeSalto = 8f;
    public bool puedoSaltar;

    public GameObject particulaPolvo;
    public Transform puntoPies;

    private KobuAttack ataque;
    private PlayerStamina stamina;
    private PlayerDash dash; // NUEVO

    public bool estoyCorriendo;
    private bool estabaEnElAire;

    void Start()
    {
        puedoSaltar = false;
        anim = GetComponent<Animator>();
        ataque = GetComponent<KobuAttack>();
        stamina = GetComponent<PlayerStamina>();
        dash = GetComponent<PlayerDash>(); // NUEVO
    }

    void FixedUpdate()
    {
        // NUEVO: mientras dura el dash, el movimiento normal no se aplica
        if (!ataque.estoyAtacando && (dash == null || !dash.EstaDasheando))
        {
            transform.Rotate(0, x * Time.deltaTime * velocidadRotacion, 0);

            float velocidadActual = estoyCorriendo ? velocidadCorrer : velocidad;
            transform.Translate(0, 0, y * Time.deltaTime * velocidadActual);
        }
    }

    void Update()
    {
        x = Input.GetAxis("Horizontal");
        y = Input.GetAxis("Vertical");

        if (!ataque.estoyAtacando)
        {
            // Solo corre si la stamina lo permite
            bool puedeCorrer = stamina == null || stamina.CanSprint();
            estoyCorriendo = Input.GetKey(KeyCode.LeftShift) && y > 0.1f && puedeCorrer;

            anim.SetFloat("VelX", x);
            anim.SetFloat("VelY", y);
            anim.SetBool("Correr", estoyCorriendo);
        }
        else
        {
            anim.SetFloat("VelX", 0);
            anim.SetFloat("VelY", 0);

            estoyCorriendo = false;
            anim.SetBool("Correr", false);
        }

        if (puedoSaltar)
        {
            if (!ataque.estoyAtacando)
            {
                if (Input.GetKeyDown(KeyCode.Space) && !PauseMenuController.EstaPausado)
                {
                    anim.SetBool("Salto", true);
                    estabaEnElAire = true;
                    rb.AddForce(Vector3.up * fuerzaDeSalto, ForceMode.Impulse);

                    if (particulaPolvo != null && puntoPies != null)
                    {
                        Instantiate(particulaPolvo, puntoPies.position, Quaternion.identity);
                    }
                }
            }

            anim.SetBool("TocoSuelo", true);

            if (estabaEnElAire)
            {
                anim.SetBool("Salto", false);
                estabaEnElAire = false;
            }
        }
        else
        {
            estabaEnElAire = true;
            EstoyCayendo();
        }
    }

    public void EstoyCayendo()
    {
        anim.SetBool("TocoSuelo", false);
    }
}