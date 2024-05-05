using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PHealthUI : MonoBehaviour
{
    public Image healthctrl;
    public TextMeshProUGUI healthText;
    public static float healthCurrent;
    public static float healthMax;
    
     void Start()
        {
         
            
           
        }

     public void Gethealth(float currenthealth, float maxhealth)
     {
         healthMax = maxhealth;
         healthCurrent = currenthealth;
         
     }
        // Update is called once per frame
        void Update()
        {
    
            healthctrl.fillAmount= healthCurrent / healthMax;
    
            healthText.text= healthCurrent.ToString()+"/"+healthMax.ToString();
        }
}
