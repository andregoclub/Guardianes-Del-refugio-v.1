using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Ruta de patrullaje: el enemigo camina de punto en punto, hace una pausa en cada uno y vuelve a empezar.
/// EnemyDetection llama a Actualizar() cada frame mientras Kobu no esté a la vista.
/// EnemyMovement es quien mueve al agente.
/// </summary>
public class EnemyPatrol : MonoBehaviour
{
    [Header("Ruta")]
    [Tooltip("Puntos por los que camina, en orden. Crealos como objetos vacíos FUERA del enemigo.")]
    [SerializeField] private Transform[] puntos;

    [Tooltip("Segundos que se queda quieto al llegar a cada punto.")]
    [SerializeField] private float tiempoEspera = 2f;

    [Tooltip("Si no llega a un punto en este tiempo, sigue con el siguiente (evita que se trabe).")]
    [SerializeField] private float tiempoMaxPorPunto = 20f;

    [Header("Referencias")]
    [SerializeField] private EnemyMovement movimiento;

    public bool TieneRuta => posiciones != null && posiciones.Length > 0;

    private Vector3[] posiciones;   // posiciones de los puntos, ajustadas al NavMesh
    private int indice;             // punto al que se dirige
    private bool destinoAsignado;
    private bool esperando;
    private float finEspera;
    private float inicioTramo;
    private int ultimoFrame = -10;

    // ---------- Inicio ----------

    void Awake()
    {
        if (movimiento == null)
            movimiento = GetComponent<EnemyMovement>();

        GuardarPosiciones();
    }

    // Se guardan las posiciones al iniciar y se acercan al NavMesh por si algún punto quedó un poco corrido
    void GuardarPosiciones()
    {
        var lista = new List<Vector3>();

        if (puntos != null)
        {
            foreach (Transform punto in puntos)
            {
                if (punto == null) continue;

                Vector3 posicion = punto.position;

                if (NavMesh.SamplePosition(posicion, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                    posicion = hit.position;
                else
                    Debug.LogWarning($"{name}: el punto '{punto.name}' está lejos del NavMesh.");

                lista.Add(posicion);
            }
        }

        posiciones = lista.ToArray();
    }

    // ---------- Patrullaje ----------

    /// <summary>Llamar cada frame mientras el enemigo debe patrullar.</summary>
    public void Actualizar()
    {
        if (!TieneRuta) return;

        // Si pasó algún frame sin llamarse (persiguió, atacó, etc.), retoma desde el punto más cercano
        if (Time.frameCount - ultimoFrame > 1)
            Reanudar();
        ultimoFrame = Time.frameCount;

        // Pausa al llegar a un punto
        if (esperando)
        {
            movimiento.Detener();

            if (Time.time >= finEspera)
            {
                esperando = false;
                indice = (indice + 1) % posiciones.Length;
            }
            return;
        }

        // Ir hacia el punto actual
        if (!destinoAsignado)
        {
            destinoAsignado = movimiento.IrA(posiciones[indice]);
            inicioTramo = Time.time;
            return;
        }

        // ¿Llegó, o tardó demasiado?
        bool llego = movimiento.HaLlegado();
        bool tardoDemasiado = Time.time - inicioTramo > tiempoMaxPorPunto;

        if (llego || tardoDemasiado)
        {
            esperando = true;
            destinoAsignado = false;
            finEspera = Time.time + tiempoEspera;
        }
    }

    void Reanudar()
    {
        esperando = false;
        destinoAsignado = false;
        indice = PuntoMasCercano();
    }

    int PuntoMasCercano()
    {
        int mejor = 0;
        float mejorDistancia = float.MaxValue;

        for (int i = 0; i < posiciones.Length; i++)
        {
            Vector3 diferencia = posiciones[i] - transform.position;
            diferencia.y = 0f;

            float distancia = diferencia.sqrMagnitude;
            if (distancia < mejorDistancia)
            {
                mejorDistancia = distancia;
                mejor = i;
            }
        }

        return mejor;
    }

    // ---------- Ruta visible en la escena ----------

    void OnDrawGizmosSelected()
    {
        if (puntos == null) return;

        Transform anterior = null;
        Transform primero = null;

        foreach (Transform punto in puntos)
        {
            if (punto == null) continue;

            if (primero == null) primero = punto;

            // El primer punto va en verde, los demás en amarillo
            Gizmos.color = (punto == primero) ? Color.green : Color.yellow;
            Gizmos.DrawSphere(punto.position, 0.25f);

            if (anterior != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(anterior.position, punto.position);
            }

            anterior = punto;
        }

        // Cierra el bucle: del último punto vuelve al primero
        if (anterior != null && primero != null && anterior != primero)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(anterior.position, primero.position);
        }
    }
}