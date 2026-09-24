using UnityEngine;

/// <summary>
/// Marca um ponto de waypoint (WP) para o Vigia patrulhar.
/// Adiciona este componente a GameObjects vazios na cena para definir o percurso de patrulha.
/// Recomendado: nomear como WP_01, WP_02, etc.
/// </summary>
public class VigilaWaypoint : MonoBehaviour
{
    [Header("Waypoint Settings")]
    [Tooltip("Tempo de espera (segundos) ao chegar a este waypoint. 0 = sem espera.")]
    [Min(0f)]
    public float waitTime = 0f;

    [Tooltip("Raio de chegada: considera chegado quando a distancia for menor que este valor.")]
    [Min(0.1f)]
    public float arrivalRadius = 0.5f;

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);
        Gizmos.DrawSphere(transform.position, arrivalRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, arrivalRadius);
    }

    private void OnDrawGizmosSelected()
    {
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.6f,
            $"WP: {gameObject.name}  (espera: {waitTime}s)",
            new GUIStyle
            {
                normal = { textColor = Color.cyan },
                fontStyle = FontStyle.Bold,
                fontSize = 11
            }
        );
    }
#endif
}
