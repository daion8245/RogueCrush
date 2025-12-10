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
    
    Vector2 baseScale;
    private void Awake()
    {
        baseScale = btn_Start.transform.localScale;
        Initialize();
    }

    void Initialize()
    {
        btn_Start.onClick.AddListener(OnClickBtnStart);
        btn_Leaderboard.onClick.AddListener(()=>SceneManager.LoadScene(2));
    }

    void OnClickBtnStart()
    {
        SceneManager.LoadScene(1);
    }

    // btn size smfdjsksmsrj aksemffu goTtmsep dpqkdudtj dksgka
}
