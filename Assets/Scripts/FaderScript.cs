using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FaderScript : MonoBehaviour
{
    private Image imagemPreta;
    public float velocidadeFade = 1.5f; // Quanto maior, mais rápido o fade

    void Awake()
    {
        imagemPreta = GetComponent<Image>();
        // Garante que a imagem começa preta e transparente (alpha 0)
        Color c = imagemPreta.color;
        c.a = 0;
        imagemPreta.color = c;
    }

    // Função que o script do diário vai chamar
    public IEnumerator FazerFade(bool escurecer)
    {
        float alvoAlpha = escurecer ? 1 : 0; // Se escurecer, alvo é 1 (preto total). Se clarear, alvo é 0 (transparente).
        Color c = imagemPreta.color;
        float alphaAtual = c.a;

        // Enquanto o alpha não chegar no alvo...
        while (Mathf.Abs(alphaAtual - alvoAlpha) > 0.01f)
        {
            // Move suavemente o alpha em direção ao alvo
            alphaAtual = Mathf.MoveTowards(alphaAtual, alvoAlpha, velocidadeFade * Time.deltaTime);
            c.a = alphaAtual;
            imagemPreta.color = c;
            yield return null; // Espera o próximo frame
        }

        // Garante que o valor final seja exato
        c.a = alvoAlpha;
        imagemPreta.color = c;
    }
}