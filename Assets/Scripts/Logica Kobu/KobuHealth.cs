using UnityEngine;
using Unity.Cinemachine;

public class KobuHealth : MonoBehaviour
{
    [SerializeField] private int vidaActual = 0;

    public int VidaActual
    {
        get { return vidaActual; }
    }

    [SerializeField] private int vidaTotal = 100;

    public bool EstaMuerto
    {
        get { return vidaActual <= 0; }
    }

    public int VidaTotal
    {
        get { return vidaTotal; }
    }

    [Header("Referencias")]

    [SerializeField] private Animator anim;
    [SerializeField] private LogicaKobu logicaKobu;
    [SerializeField] private KobuAttack kobuAttack;
    [SerializeField] private PlayerRespawn playerRespawn;
    [SerializeField] private PlayerDash playerDash; // NUEVO

    [Header("Feedback de daño recibido")]

    [SerializeField] private PlayerFlash playerFlash;
    [SerializeField] private CinemachineImpulseSource impulseSource;


    void Start()
    {
        vidaActual = vidaTotal;

        if (anim == null)
            anim = GetComponent<Animator>();

        if (logicaKobu == null)
            logicaKobu = GetComponent<LogicaKobu>();

        if (kobuAttack == null)
            kobuAttack = GetComponent<KobuAttack>();

        if (playerRespawn == null)
            playerRespawn = GetComponent<PlayerRespawn>();

        if (playerDash == null) // NUEVO
            playerDash = GetComponent<PlayerDash>();

        if (playerFlash == null)
            playerFlash = GetComponent<PlayerFlash>();

        if (impulseSource == null)
            impulseSource = GetComponent<CinemachineImpulseSource>();
    }


    public void RecibirDaño(int daño)
    {
        // NUEVO: si ya está muerto, los golpes siguientes se ignoran
        if (EstaMuerto) return;

        // NUEVO: mientras dura el dash, Kobu esquiva el golpe
        if (playerDash != null && playerDash.EsInvulnerable) return;

        vidaActual -= daño;

        // Flash del modelo.
        if (playerFlash != null)
        {
            playerFlash.Flash();
        }

        // Viñeta roja en pantalla.
        if (DamageVignette.Instancia != null)
        {
            DamageVignette.Instancia.Mostrar();
        }

        // Camera shake.
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse();
        }

        if (vidaActual <= 0)
        {
            vidaActual = 0;

            Morir();
        }
    }


    public void RestaurarVida()
    {
        vidaActual = vidaTotal;

        // NUEVO: el dash se reactiva junto con el resto
        if (playerDash != null)
            playerDash.enabled = true;
    }


    public void Curar(int cantidad)
    {
        vidaActual += cantidad;

        if (vidaActual > vidaTotal)
        {
            vidaActual = vidaTotal;
        }
    }


    private void Morir()
    {
        Debug.Log("Kobu ha muerto");

        anim.SetTrigger("Death");

        logicaKobu.enabled = false;
        kobuAttack.enabled = false;

        // NUEVO: tampoco se puede dashear estando muerto
        if (playerDash != null)
            playerDash.enabled = false;

        // NUEVO: pantalla de Game Over (aparece tras una breve espera)
        if (GameOverController.Instancia != null)
            GameOverController.Instancia.Mostrar();
        else
            Debug.LogWarning("No hay un GameOverController en la escena.");
    }
}