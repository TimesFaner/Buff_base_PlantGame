using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public GameObject DefeatPanel;
    // Start is called before the first frame update
    void Start()
    {
        DefeatPanel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ReStart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ShowPanel(GameObject _panel)
    {
        _panel.SetActive(true);
    }
}
