using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PHealthUI : MonoBehaviour
{
    public static float healthCurrent;
    public static float healthMax;
    public Image healthctrl;
    public TextMeshProUGUI healthText;

    private void Start()
    {
    }

    // Update is called once per frame
    private void Update()
    {
        healthctrl.fillAmount = healthCurrent / healthMax;

        healthText.text = healthCurrent + "/" + healthMax;
    }

    public void Gethealth(float currenthealth, float maxhealth)
    {
        healthMax = maxhealth;
        healthCurrent = currenthealth;
    }
}