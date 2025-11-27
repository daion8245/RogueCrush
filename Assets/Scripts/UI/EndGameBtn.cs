using UnityEngine;
using UnityEngine.UI;

public class EndGameBtn : MonoBehaviour
{
    public enum BtnType
    {
        Menu,
        Retry
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
    }
}
