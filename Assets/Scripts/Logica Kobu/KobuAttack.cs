using UnityEngine;

public class KobuAttack : MonoBehaviour
{
    [Header("Fluidez del combate")]
    [Tooltip("Un clic hecho durante un golpe se recuerda este tiempo y se ejecuta apenas termina.")]
    [SerializeField] private float tiempoBuffer = 0.25f;

    private Animator anim;

    public bool estoyAtacando;

    private bool siguienteGolpeDerecho = false;
    private float ultimoClic = -999f;

    void Start()
    {
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        // No atacar mientras el juego está en pausa
        if (PauseMenuController.EstaPausado) return;

        // Cada clic se recuerda unos instantes (buffer)
        if (Input.GetKeyDown(KeyCode.Mouse0))
            ultimoClic = Time.time;

        bool hayClicPendiente = Time.time - ultimoClic <= tiempoBuffer;

        if (hayClicPendiente && !estoyAtacando)
        {
            ultimoClic = -999f; // el clic ya se usó
            Atacar();
        }
    }

    void Atacar()
    {
        if (siguienteGolpeDerecho)
        {
            anim.SetTrigger("Rightpunch");
        }
        else
        {
            anim.SetTrigger("golpe");
        }

        siguienteGolpeDerecho = !siguienteGolpeDerecho;
        estoyAtacando = true;
    }

    // Animation Event: se llama al final de la animación
    public void DejaDeGolpear()
    {
        estoyAtacando = false;
    }
}