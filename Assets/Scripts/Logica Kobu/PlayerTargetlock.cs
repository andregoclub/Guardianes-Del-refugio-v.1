using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fija a Kobu sobre un enemigo: mientras hay objetivo, Kobu siempre lo mira.
/// Q fija o suelta el objetivo. Tab cambia al siguiente enemigo (de izquierda a derecha).
/// LogicaKobu consulta EstaFijado para mover a Kobu de lado alrededor del enemigo.
/// </summary>
public class TargetLock : MonoBehaviour
{
    [Header("Teclas")]
    [SerializeField] private KeyCode teclaFijar = KeyCode.Q;
    [SerializeField] private KeyCode teclaCambiar = KeyCode.Tab;

    [Header("Búsqueda")]
    [Tooltip("Distancia máxima a la que se puede fijar un enemigo.")]
    [SerializeField] private float radioMaximo = 12f;
    [Tooltip("Si el objetivo se aleja más que esto, se suelta solo.")]
    [SerializeField] private float distanciaSoltar = 16f;
    [Tooltip("Al morir el objetivo, pasa solo al enemigo más conveniente (si no hay, se suelta).")]
    [SerializeField] private bool cambiarAlMorir = true;

    [Header("Giro")]
    [Tooltip("Qué tan rápido gira Kobu hacia el objetivo, en grados por segundo.")]
    [SerializeField] private float velocidadGiro = 540f;

    [Header("Marcador sobre el objetivo")]
    [SerializeField] private Color colorMarcador = new Color(1f, 0.15f, 0.15f, 1f);
    [SerializeField] private float radioMarcador = 0.8f;

    public bool EstaFijado => fijado && objetivo != null;
    public Transform Objetivo => objetivo;

    private bool fijado;
    private Transform objetivo;
    private EnemyHealth objetivoVida;

    private KobuHealth kobuHealth;
    private LineRenderer marcador;

    // ---------- Inicio ----------

    void Awake()
    {
        kobuHealth = GetComponent<KobuHealth>();
        CrearMarcador();
    }

    void OnDestroy()
    {
        if (marcador != null) Destroy(marcador.gameObject);
    }

    // ---------- Input ----------

    void Update()
    {
        if (PauseMenuController.EstaPausado) return;

        // Si Kobu muere, se suelta el objetivo
        if (kobuHealth != null && kobuHealth.EstaMuerto)
        {
            Soltar();
            return;
        }

        if (Input.GetKeyDown(teclaFijar))
        {
            if (EstaFijado) Soltar();
            else FijarMejorObjetivo();
        }
        else if (Input.GetKeyDown(teclaCambiar))
        {
            if (EstaFijado) CambiarAlSiguiente();
            else FijarMejorObjetivo();
        }

        VerificarObjetivo();
    }

    // ---------- Giro hacia el objetivo ----------

    void FixedUpdate()
    {
        if (!EstaFijado) return;

        Vector3 haciaObjetivo = Plano(objetivo.position - transform.position);
        if (haciaObjetivo.sqrMagnitude < 0.0001f) return;

        Quaternion destino = Quaternion.LookRotation(haciaObjetivo, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, destino, velocidadGiro * Time.fixedDeltaTime);
    }

    // ---------- Fijar, soltar y cambiar ----------

    void FijarMejorObjetivo()
    {
        List<EnemyHealth> enemigos = ObtenerEnemigosEnRango();
        if (enemigos.Count == 0) return;

        FijarEn(ElegirMejor(enemigos));
    }

    void CambiarAlSiguiente()
    {
        List<EnemyHealth> enemigos = ObtenerEnemigosEnRango();
        if (enemigos.Count <= 1) return;

        // Ordenados de izquierda a derecha respecto a hacia dónde mira Kobu
        enemigos.Sort((a, b) => AnguloFirmado(a.transform).CompareTo(AnguloFirmado(b.transform)));

        int indiceActual = enemigos.IndexOf(objetivoVida);
        int siguiente = (indiceActual + 1) % enemigos.Count;

        FijarEn(enemigos[siguiente]);
    }

    void FijarEn(EnemyHealth enemigo)
    {
        objetivoVida = enemigo;
        objetivo = enemigo.transform;
        fijado = true;
    }

    void Soltar()
    {
        fijado = false;
        objetivo = null;
        objetivoVida = null;
    }

    // Suelta el objetivo si murió, desapareció o quedó muy lejos
    void VerificarObjetivo()
    {
        if (!fijado) return;

        bool invalido = objetivo == null
                     || objetivoVida == null
                     || objetivoVida.EstaMuerto
                     || Plano(objetivo.position - transform.position).magnitude > distanciaSoltar;

        if (!invalido) return;

        Soltar();
        if (cambiarAlMorir) FijarMejorObjetivo();
    }

    // ---------- Elegir enemigos ----------

    List<EnemyHealth> ObtenerEnemigosEnRango()
    {
        var lista = new List<EnemyHealth>();
        EnemyHealth[] todos = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);

        foreach (EnemyHealth enemigo in todos)
        {
            if (enemigo.EstaMuerto) continue;

            float distancia = Plano(enemigo.transform.position - transform.position).magnitude;
            if (distancia <= radioMaximo) lista.Add(enemigo);
        }

        return lista;
    }

    // El mejor candidato mezcla cercanía y qué tan de frente está
    EnemyHealth ElegirMejor(List<EnemyHealth> enemigos)
    {
        Vector3 adelante = Plano(transform.forward);
        EnemyHealth mejor = null;
        float mejorPuntaje = float.MaxValue;

        foreach (EnemyHealth enemigo in enemigos)
        {
            Vector3 haciaEnemigo = Plano(enemigo.transform.position - transform.position);

            float angulo = Vector3.Angle(adelante, haciaEnemigo);
            float distancia = haciaEnemigo.magnitude;

            float puntaje = (angulo / 180f) * 0.5f + (distancia / radioMaximo) * 0.5f;

            if (puntaje < mejorPuntaje)
            {
                mejorPuntaje = puntaje;
                mejor = enemigo;
            }
        }

        return mejor;
    }

    float AnguloFirmado(Transform otro)
    {
        Vector3 haciaOtro = Plano(otro.position - transform.position);
        return Vector3.SignedAngle(Plano(transform.forward), haciaOtro, Vector3.up);
    }

    static Vector3 Plano(Vector3 v)
    {
        v.y = 0f;
        return v;
    }

    // ---------- Marcador (anillo en el piso bajo el objetivo) ----------

    void CrearMarcador()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Debug.LogWarning("TargetLock: no se pudo crear el marcador (shader no encontrado).");
            return;
        }

        const int segmentos = 40;

        var go = new GameObject("MarcadorObjetivo");
        marcador = go.AddComponent<LineRenderer>();

        marcador.useWorldSpace = false;
        marcador.loop = true;
        marcador.positionCount = segmentos;
        marcador.widthMultiplier = 0.07f;
        marcador.material = new Material(shader);
        marcador.startColor = colorMarcador;
        marcador.endColor = colorMarcador;
        marcador.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        marcador.receiveShadows = false;

        for (int i = 0; i < segmentos; i++)
        {
            float angulo = (float)i / segmentos * Mathf.PI * 2f;
            marcador.SetPosition(i, new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo)) * radioMarcador);
        }

        go.SetActive(false);
    }

    void LateUpdate()
    {
        if (marcador == null) return;

        bool visible = EstaFijado;
        if (marcador.gameObject.activeSelf != visible)
            marcador.gameObject.SetActive(visible);

        if (!visible) return;

        marcador.transform.position = objetivo.position + Vector3.up * 0.05f;

        // Pequeño pulso para que se note
        float pulso = 1f + 0.08f * Mathf.Sin(Time.time * 6f);
        marcador.transform.localScale = new Vector3(pulso, 1f, pulso);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radioMaximo);
    }
}