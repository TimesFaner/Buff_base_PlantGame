using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public GameObject DefeatPanel;

    // Start is called before the first frame update
    private void Start()
    {
        DefeatPanel.SetActive(false);
    }

    // Update is called once per frame
    private void Update()
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