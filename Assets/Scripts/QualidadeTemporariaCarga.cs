using UnityEngine;

/// <summary>
/// Reduz o custo de sombras e detalhes somente enquanto a cena de carga estiver ativa.
/// Os valores originais do nivel de qualidade sao restaurados ao sair da cena.
/// </summary>
public sealed class QualidadeTemporariaCarga : MonoBehaviour
{
    [SerializeField, Min(0f)] private float distanciaMaximaDeSombras = 70f;
    [SerializeField, Range(0, 4)] private int maximoDeCascatas = 2;
    [SerializeField, Min(0.1f)] private float lodBiasMaximo = 1f;

    private int nivelOriginal;
    private float distanciaSombrasOriginal;
    private int cascatasOriginais;
    private float lodBiasOriginal;
    private bool aplicado;

    private void Awake()
    {
        if (!Application.isPlaying) return;

        nivelOriginal = QualitySettings.GetQualityLevel();
        distanciaSombrasOriginal = QualitySettings.shadowDistance;
        cascatasOriginais = QualitySettings.shadowCascades;
        lodBiasOriginal = QualitySettings.lodBias;

        QualitySettings.shadowDistance = Mathf.Min(distanciaSombrasOriginal, distanciaMaximaDeSombras);
        QualitySettings.shadowCascades = Mathf.Min(cascatasOriginais, maximoDeCascatas);
        QualitySettings.lodBias = Mathf.Min(lodBiasOriginal, lodBiasMaximo);
        aplicado = true;
    }

    private void OnDisable() => RestaurarQualidade();

    private void OnDestroy() => RestaurarQualidade();

    private void RestaurarQualidade()
    {
        if (!aplicado) return;

        // Se o jogador trocou a qualidade pelo menu durante a cena, respeita a nova escolha.
        if (QualitySettings.GetQualityLevel() == nivelOriginal)
        {
            QualitySettings.shadowDistance = distanciaSombrasOriginal;
            QualitySettings.shadowCascades = cascatasOriginais;
            QualitySettings.lodBias = lodBiasOriginal;
        }

        aplicado = false;
    }
}
