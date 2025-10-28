
// ============================================
// BenefitInventoryUI.cs (Actualizado)
// ============================================
using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class BenefitInventoryUI : MonoBehaviour
{
    [System.Serializable]
    public class Slot
    {
        public GameObject root;
        public TextMeshProUGUI titulo;
    }

    public int maxSlots = 3;
    public Slot[] slots;

    public void SetBenefits(List<CartaEntry2> lista)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;

            if (i < lista.Count && lista[i] != null)
            {
                if (slots[i].root) slots[i].root.SetActive(true);
                if (slots[i].titulo) slots[i].titulo.text = string.IsNullOrEmpty(lista[i].nombre) ? "Beneficio" : lista[i].nombre;
            }
            else
            {
                if (slots[i].root) slots[i].root.SetActive(false);
                if (slots[i].titulo) slots[i].titulo.text = "";
            }
        }
    }
}