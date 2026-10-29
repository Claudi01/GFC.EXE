using UnityEngine;
using UnityEngine.SceneManagement;

public enum SuperficieAudio
{
    Areia = 0,
    TerraGrama = 1,
    Metal = 2,
    Madeira = 3,
    ConcretoPedra = 4,
    AguaRasa = 5
}

/// <summary>Identifica a superficie sob os pes, incluindo a textura pintada no Terrain.</summary>
public static class DetectorSuperficieAudio
{
    public static SuperficieAudio Identificar(Collider collider, Vector3 ponto)
    {
        if (collider == null) return PadraoDaCena();
        // O piso de madeira precisa existir na colisao sob os pes; o grupo Tent inteiro nao e madeira.
        if (NomePisoCabana(collider.name)) return SuperficieAudio.Madeira;

        TerrainCollider terreno = collider as TerrainCollider;
        if (terreno != null && terreno.terrainData != null)
        {
            SuperficieAudio tipo;
            if (TentarTerrain(terreno, ponto, out tipo)) return tipo;
        }

        // O material do collider e mais especifico que os nomes dos grupos da cena.
        SuperficieAudio superficie;
        if (collider.sharedMaterial != null &&
            TentarNome(collider.sharedMaterial.name, out superficie)) return superficie;

        Renderer renderer = collider.GetComponent<Renderer>();
        if (renderer == null) renderer = collider.GetComponentInParent<Renderer>();
        if (renderer != null)
        {
            foreach (Material material in renderer.sharedMaterials)
                if (material != null && TentarNome(material.name, out superficie)) return superficie;
        }

        Transform atual = collider.transform;
        while (atual != null)
        {
            if (TentarNome(atual.name, out superficie)) return superficie;
            atual = atual.parent;
        }

        return PadraoDaCena();
    }

    private static bool TentarTerrain(TerrainCollider terreno, Vector3 ponto, out SuperficieAudio superficie)
    {
        superficie = SuperficieAudio.TerraGrama;
        TerrainLayer[] camadas;
        float[,,] pesos;
        if (!AmostrarTerrain(terreno, ponto, out camadas, out pesos)) return false;
        int dominante = 0;
        for (int i = 1; i < pesos.GetLength(2); i++)
            if (pesos[0, 0, i] > pesos[0, 0, dominante]) dominante = i;
        if (dominante >= camadas.Length || camadas[dominante] == null) return false;

        TerrainLayer camada = camadas[dominante];
        if (TentarNome(camada.name, out superficie)) return true;
        return camada.diffuseTexture != null && TentarNome(camada.diffuseTexture.name, out superficie);
    }

    /// <summary>Peso real das camadas de areia pintadas no Terrain, independente de props sobre o piso.</summary>
    public static float PesoAreiaNoTerrain(TerrainCollider terreno, Vector3 ponto)
    {
        TerrainLayer[] camadas;
        float[,,] pesos;
        if (!AmostrarTerrain(terreno, ponto, out camadas, out pesos)) return 0f;
        bool praia = terreno.gameObject.scene.name.Equals("Praia", System.StringComparison.OrdinalIgnoreCase);
        float total = 0f;
        for (int i = 0; i < pesos.GetLength(2) && i < camadas.Length; i++)
        {
            TerrainLayer camada = camadas[i];
            if (camada == null) continue;
            if (NomeAreia(camada.name, praia) ||
                (camada.diffuseTexture != null && NomeAreia(camada.diffuseTexture.name, praia)))
                total += pesos[0, 0, i];
        }
        return Mathf.Clamp01(total);
    }

    private static bool AmostrarTerrain(TerrainCollider terreno, Vector3 ponto,
        out TerrainLayer[] camadas, out float[,,] pesos)
    {
        camadas = null;
        pesos = null;
        if (terreno == null || terreno.terrainData == null) return false;
        TerrainData dados = terreno.terrainData;
        camadas = dados.terrainLayers;
        if (camadas == null || camadas.Length == 0 || dados.alphamapLayers == 0 ||
            dados.alphamapWidth <= 0 || dados.alphamapHeight <= 0 || dados.size.x <= 0f || dados.size.z <= 0f) return false;
        Vector3 local = terreno.transform.InverseTransformPoint(ponto);
        if (local.x < 0f || local.z < 0f || local.x > dados.size.x || local.z > dados.size.z) return false;
        int x = Mathf.Clamp(Mathf.FloorToInt(local.x / dados.size.x * dados.alphamapWidth), 0, dados.alphamapWidth - 1);
        int z = Mathf.Clamp(Mathf.FloorToInt(local.z / dados.size.z * dados.alphamapHeight), 0, dados.alphamapHeight - 1);
        pesos = dados.GetAlphamaps(x, z, 1, 1);
        return pesos.GetLength(2) > 0;
    }

    public static bool TentarNome(string nome, out SuperficieAudio superficie)
    {
        string texto = (nome ?? string.Empty).ToLowerInvariant();
        if (Contem(texto, "agua", "água", "water", "puddle", "poça", "poca") || NomeDoMar(texto))
            superficie = SuperficieAudio.AguaRasa;
        else if (NomeAreia(texto, SceneManager.GetActiveScene().name.ToLowerInvariant().Contains("praia")))
            superficie = SuperficieAudio.Areia;
        else if (Contem(texto, "grama", "grass", "terra", "dirt", "soil", "mud", "folha", "leaf", "ground"))
            superficie = SuperficieAudio.TerraGrama;
        else if (NomePisoCabana(texto) || Contem(texto, "madeira", "wood", "plank", "bote", "lifeboat"))
            superficie = SuperficieAudio.Madeira;
        else if (Contem(texto, "metal", "ferro", "iron", "steel", "chapa", "container", "navio", "ship", "deck", "cabinet", "aco", "aço"))
            superficie = SuperficieAudio.Metal;
        else if (Contem(texto, "concreto", "concrete", "cimento", "pedra", "stone", "rock", "asphalt", "tijolo", "brick"))
            superficie = SuperficieAudio.ConcretoPedra;
        else
        {
            superficie = SuperficieAudio.TerraGrama;
            return false;
        }
        return true;
    }

    private static bool NomeAreia(string nome, bool praia)
    {
        string texto = (nome ?? string.Empty).ToLowerInvariant();
        return Contem(texto, "areia", "sand", "beach") ||
            (praia && texto.Contains("dirt_lighted_down"));
    }

    private static bool NomePisoCabana(string nome)
    {
        string texto = (nome ?? string.Empty).Trim().ToLowerInvariant();
        return texto == "tent_floor" || texto.StartsWith("tent_floor (") || texto.StartsWith("tent_floor.");
    }

    private static bool NomeDoMar(string texto)
    {
        // Nomes usados na Praia; nao usar Contains("mar"), que tambem casa com "armario".
        return texto == "mar" || texto.StartsWith("mar (") ||
            texto == "mat_marvolkov" || texto.StartsWith("mat_marvolkov (");
    }

    private static bool Contem(string texto, params string[] termos)
    {
        foreach (string termo in termos) if (texto.Contains(termo)) return true;
        return false;
    }

    private static SuperficieAudio PadraoDaCena()
    {
        return SceneManager.GetActiveScene().name.ToLowerInvariant().Contains("praia")
            ? SuperficieAudio.TerraGrama : SuperficieAudio.ConcretoPedra;
    }
}
