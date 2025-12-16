using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndGameBtn : MonoBehaviour
{
    public enum BtnType
    {
        Menu,
        Retry,
        Leaderboard
    }
    [SerializeField]
    Button btn_EndGame;
    [SerializeField]
    BtnType btnType;
    void Awake()
    {
        btn_EndGame.onClick.AddListener(OnClickEndGameBtn);
    }

    void OnClickEndGameBtn()
    {
        if (btnType == BtnType.Menu)
        {
            GameManager.Instance.BackToMenu();
        }
        else if (btnType == BtnType.Retry)
        {
            GameManager.Instance.Retry();
        }
        else if (btnType == BtnType.Leaderboard)
        {
            GameManager.Instance.MoveSceneLeaderboard();
        }
    }
}
