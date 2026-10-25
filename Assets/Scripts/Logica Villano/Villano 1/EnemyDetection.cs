using UnityEngine;
using System.Collections;

public class EnemyDetection : MonoBehaviour
{
    [Header("Referencias")]

    [SerializeField] private Transform jugador;

    [SerializeField] private EnemyMovement enemyMovement;
    [SerializeField] private KobuHealth kobuHealth;
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private EnemyPatrol enemyPatrol;   // NUEVO

    [Header("Detección")]

    [SerializeField] private float radioDeteccion = 5f;
    [SerializeField] private float distanciaAtaque = 2f;
    [SerializeField] private EnemyAttack enemyAttack;

    [Header("Sonido de detección")]

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sonidoDeteccion;
    [Range(0f, 1f)]
    [SerializeField] private float volumenDeteccion = 0.6f;

    // Tiempo de espera entre una reproducción y la siguiente.
    [SerializeField] private float intervaloEntreSonidos = 3f;

    private Coroutine sonidoCoroutine;


    private void Awake()
    {
        if (enemyMovement == null)
            enemyMovement = GetComponent<EnemyMovement>();

        if (enemyAttack == null)
            enemyAttack = GetComponent<EnemyAttack>();

        if (enemyPatrol == null)   // NUEVO
            enemyPatrol = GetComponent<EnemyPatrol>();

        if (kobuHealth == null && jugador != null)
        {
            kobuHealth = jugador.GetComponent<KobuHealth>();
        }

        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealth>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
        {
            audioSource.clip = sonidoDeteccion;
            audioSource.loop = false; // manejamos el loop nosotros, con intervalo
            audioSource.volume = volumenDeteccion;
            audioSource.playOnAwake = false;
        }
    }


    private void Update()
    {
        if (enemyHealth != null && enemyHealth.EstaMuerto)
        {
            enemyMovement.Detener();
            DetenerSonido();
            return;
        }

        if (kobuHealth != null && kobuHealth.EstaMuerto)
        {
            enemyMovement.Detener();
            DetenerSonido();
            return;
        }

        if (jugador == null)
        {
            DetenerSonido();
            return;
        }

        float distancia = Vector3.Distance(transform.position, jugador.position);

        if (distancia > radioDeteccion)
        {
            DetenerSonido();

            // NUEVO: si tiene ruta, patrulla; si no, se queda quieto como antes
            if (enemyPatrol != null && enemyPatrol.TieneRuta)
                enemyPatrol.Actualizar();
            else
                enemyMovement.Detener();

            return;
        }

        ReproducirSonido();

        if (distancia <= distanciaAtaque)
        {
            enemyMovement.Detener();
            enemyAttack.Atacar();
        }
        else
        {
            enemyMovement.Perseguir(jugador);
        }
    }


    private void ReproducirSonido()
    {
        if (audioSource == null || sonidoDeteccion == null)
            return;

        // Si ya está corriendo el ciclo de reproducción, no arrancamos otro en paralelo.
        if (sonidoCoroutine == null)
        {
            sonidoCoroutine = StartCoroutine(CicloDeSonido());
        }
    }


    private IEnumerator CicloDeSonido()
    {
        while (true)
        {
            audioSource.PlayOneShot(sonidoDeteccion, volumenDeteccion);

            // Esperamos la duración del clip + el intervalo configurado antes de repetir.
            yield return new WaitForSeconds(sonidoDeteccion.length + intervaloEntreSonidos);
        }
    }


    private void DetenerSonido()
    {
        if (sonidoCoroutine != null)
        {
            StopCoroutine(sonidoCoroutine);
            sonidoCoroutine = null;
        }

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radioDeteccion);
    }
}