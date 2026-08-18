using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Nucleo puro da maleta: cada operacao so altera o estado depois de validar todos os espacos.</summary>
[Serializable]
public sealed class InventarioGrade
{
    [Serializable]
    public sealed class Item
    {
        public string id;
        public string nome;
        public int largura;
        public int altura;
        public int x;
        public int y;
        public bool rotacionado;
        public bool podeRotacionar = true;
        public int LarguraAtual { get { return rotacionado ? altura : largura; } }
        public int AlturaAtual { get { return rotacionado ? largura : altura; } }
    }

    public readonly int colunas;
    public readonly int linhas;
    private readonly List<Item> itens = new List<Item>();
    public IList<Item> Itens { get { return itens.AsReadOnly(); } }

    public InventarioGrade(int colunas, int linhas)
    {
        this.colunas = Mathf.Max(1, colunas);
        this.linhas = Mathf.Max(1, linhas);
    }

    public bool TentarAdicionar(string id, string nome, int largura, int altura, out Item item, bool podeRotacionar = true)
    {
        item = null;
        largura = Mathf.Max(1, largura); altura = Mathf.Max(1, altura);
        for (int y = 0; y < linhas; y++)
        for (int x = 0; x < colunas; x++)
        {
            Item candidato = new Item { id = id, nome = nome, largura = largura, altura = altura, x = x, y = y, podeRotacionar = podeRotacionar };
            if (!Cabe(candidato, null)) continue;
            itens.Add(candidato); item = candidato; return true;
        }
        return false;
    }

    public bool TentarMover(Item item, int x, int y)
    {
        if (item == null || !itens.Contains(item)) return false;
        int anteriorX = item.x, anteriorY = item.y;
        item.x = x; item.y = y;
        if (Cabe(item, item)) return true;
        item.x = anteriorX; item.y = anteriorY; return false;
    }

    public bool TentarRotacionar(Item item)
    {
        if (item == null || !item.podeRotacionar || !itens.Contains(item)) return false;
        item.rotacionado = !item.rotacionado;
        if (Cabe(item, item)) return true;
        item.rotacionado = !item.rotacionado; return false;
    }

    public bool TentarRestaurar(Item item)
    {
        if (item == null || !Cabe(item, null)) return false;
        itens.Add(item); return true;
    }

    public bool Contem(string id)
    {
        return itens.Exists(i => i.id == id);
    }

    public bool Remover(Item item)
    {
        return item != null && itens.Remove(item);
    }

    private bool Cabe(Item candidato, Item ignorar)
    {
        if (candidato.x < 0 || candidato.y < 0 || candidato.x + candidato.LarguraAtual > colunas || candidato.y + candidato.AlturaAtual > linhas) return false;
        foreach (Item outro in itens)
        {
            if (outro == ignorar) continue;
            if (candidato.x < outro.x + outro.LarguraAtual && candidato.x + candidato.LarguraAtual > outro.x &&
                candidato.y < outro.y + outro.AlturaAtual && candidato.y + candidato.AlturaAtual > outro.y) return false;
        }
        return true;
    }
}
