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
        public int quantidade = 1;
        public bool empilhavel;
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

    public bool TentarAdicionar(string id, string nome, int largura, int altura, out Item item,
        bool podeRotacionar = true, int quantidade = 1, bool empilhavel = false)
    {
        item = null;
        if (string.IsNullOrWhiteSpace(id) || quantidade < 1 || (!empilhavel && quantidade != 1)) return false;

        Item existente = itens.Find(i => i.id == id);
        if (existente != null)
        {
            if (!empilhavel || !existente.empilhavel || existente.quantidade > int.MaxValue - quantidade) return false;
            existente.quantidade += quantidade;
            item = existente;
            return true;
        }

        largura = Mathf.Max(1, largura); altura = Mathf.Max(1, altura);
        for (int y = 0; y < linhas; y++)
        for (int x = 0; x < colunas; x++)
        {
            Item candidato = new Item { id = id, nome = nome, largura = largura, altura = altura, x = x, y = y,
                podeRotacionar = podeRotacionar, quantidade = quantidade, empilhavel = empilhavel };
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
        if (item == null || string.IsNullOrWhiteSpace(item.id) || Contem(item.id) ||
            item.largura < 1 || item.altura < 1 || item.quantidade < 1 ||
            (!item.empilhavel && item.quantidade != 1) || !Cabe(item, null)) return false;
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

    public int Quantidade(string id)
    {
        Item item = itens.Find(i => i.id == id);
        return item != null ? item.quantidade : 0;
    }

    public bool TentarConsumir(string id, int quantidade = 1)
    {
        Item item = itens.Find(i => i.id == id);
        if (item == null || quantidade < 1 || item.quantidade < quantidade) return false;
        item.quantidade -= quantidade;
        if (item.quantidade == 0) itens.Remove(item);
        return true;
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
