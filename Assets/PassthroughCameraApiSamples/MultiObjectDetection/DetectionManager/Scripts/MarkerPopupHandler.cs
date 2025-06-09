using UnityEngine;
using TMPro;

public class MarkerPopupHandler : MonoBehaviour
{
    public GameObject popupWindow;
    public TextMeshProUGUI infoText; // optional

    private bool isPopupVisible = false;

    public int classId;
    public string className;

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
            if (infoText != null) infoText.text = $"Class ID: {classId}\nName: {className}";;

            Transform cam = Camera.main.transform;
            popupWindow.transform.LookAt(cam);
            popupWindow.transform.Rotate(0, 180f, 0); // flip to face forward

            Debug.Log($"[Popup] Show info: ID={classId}, Name={className}");
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
