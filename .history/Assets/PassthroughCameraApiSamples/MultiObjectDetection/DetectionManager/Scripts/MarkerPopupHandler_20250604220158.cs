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

             Transform cam = Camera.main.transform;
        popupWindow.transform.LookAt(cam);
        popupWindow.transform.Rotate(0, 180f, 0); // flip to face forward

            Debug.Log("[Popup] Popup shown for: " + info);

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
