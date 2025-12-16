using System.Xml;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleUI : MonoBehaviour
{
    [SerializeField]
    Button btn_Start;
    [SerializeField]
    private Button btn_Leaderboard;
    [SerializeField]
    private Button btn_Exit;

    private void Awake()
    {
        Initialize();
    }

    void Initialize()
    {
        btn_Start.onClick.AddListener(OnClickBtnStart);
        btn_Leaderboard.onClick.AddListener(()=>SceneManager.LoadScene(3));
        btn_Exit.onClick.AddListener(()=>Application.Quit());
    }

    void OnClickBtnStart()
    {
        SceneManager.LoadScene(2);
    }
}
