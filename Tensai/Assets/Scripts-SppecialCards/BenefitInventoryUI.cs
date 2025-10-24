using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class BenefitInventoryUI : MonoBehaviour
{
    [System.Serializable]
    public class Slot
    {
        public GameObject root;           // Slot1/2/3
        public TextMeshProUGUI titulo;    // Texto dentro del slot
    }

    public int maxSlots = 3;
    public Slot[] slots;                  // tamaño 3 en el inspector

    // Refresca todos los slots con la lista actual
    public void SetBenefits(List<CartaEntry> lista)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;

            if (i < lista.Count && lista[i] != null)
            {
                if (slots[i].root)   slots[i].root.SetActive(true);
                if (slots[i].titulo) slots[i].titulo.text = string.IsNullOrEmpty(lista[i].nombre) ? "Beneficio" : lista[i].nombre;
            }
            else
            {
                if (slots[i].root)   slots[i].root.SetActive(false); // oculta slots vacíos
                if (slots[i].titulo) slots[i].titulo.text = "";
            }
        }
    }
}
