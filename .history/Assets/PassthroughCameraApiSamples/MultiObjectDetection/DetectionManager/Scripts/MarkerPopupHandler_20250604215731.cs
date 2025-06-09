using UnityEngine;
using TMPro;

public class MarkerPopupHandler : MonoBehaviour
{
    public GameObject popupWindow;
    public TextMeshProUGUI infoText; // optional

    private bool isPopupVisible = false;

    void Start()
    {
        if (popupWindow != null)
            popupWindow.SetActive(false);
    }

    public void ShowPopup(string info)
    {
        if (!isPopupVisible && popupWindow != null)
        {
            popupWindow.SetActive(true);
            if (infoText != null) infoText.text = info;

            Debug.Log("[] Popup shown for: " + info);

            isPopupVisible = true;
        }
    }

    public void HidePopup()
    {
        if (isPopupVisible && popupWindow != null)
        {
            popupWindow.SetActive(false);
            isPopupVisible = false;
        }
    }
}
