using UnityEngine;
using UnityEngine.SceneManagement;

public class PopUpSystem : MonoBehaviour
{
    public GameObject popUpBoxPlayer;
    public GameObject popUpBoxOthers;

    void Start()
    {
        PlayerPrefs.DeleteAll(); // Clear PlayerPrefs for testing purposes

        // Ocultar todas las pop-ups al iniciar
        popUpBoxPlayer.SetActive(false);
        popUpBoxOthers.SetActive(false);
    }

    public void ShowPopUpPlayer()
    {
        popUpBoxPlayer.SetActive(true);
        popUpBoxOthers.SetActive(false);
        Debug.Log("Mostrando PopUp de Player");
    }

    public void ShowPopUpOthers()
    {
        popUpBoxOthers.SetActive(true);
        popUpBoxPlayer.SetActive(false);
        Debug.Log("Mostrando PopUp de Others");
    }

    public void ClosePopUp()
    {
        // Oculta cualquier pop-up visible
        popUpBoxPlayer.SetActive(false);
        popUpBoxOthers.SetActive(false);
        Debug.Log("PopUp cerrado");
    }

    public void CardButton()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        Debug.Log("Card Button Clicked");
    }
}
