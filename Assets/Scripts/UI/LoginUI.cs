using System;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoginUI : MonoBehaviour
{
    #region SerializeField
    [SerializeField]
    private TMP_InputField txt_PlayerName;
    [SerializeField]
    private Button Btn_Login;
    #endregion

    private bool _isReady = false;

    private async Task Awake()
    {
        await UnityServices.InitializeAsync();

        if (AuthenticationService.Instance.IsSignedIn)
            AuthenticationService.Instance.SignOut();
        
        if(AuthenticationService.Instance.SessionTokenExists)
            AuthenticationService.Instance.ClearSessionToken();
        
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        Debug.Log($"PlayerId = {AuthenticationService.Instance.PlayerId}");
        _isReady = true;
    }

    private void Start()
    {
        Btn_Login.onClick.AddListener(EnterLogin);
    }

    private void EnterLogin()
    {
        string nickname = txt_PlayerName.text;
        
        if(!_isReady)
            return;
        
        if (!string.IsNullOrEmpty(nickname) && (nickname.Length > 3))
        {
            Debug.Log("플레이어 이름 설정중...");
            try
            {
                if (nickname.Contains(" "))
                {
                    nickname = nickname.Replace(" ", "_");
                }
                
                SetNicknameOnServer(nickname);
                
                SceneManager.LoadScene(0);
            }
            catch (Exception e)
            {
                Console.WriteLine("로그인 실패" + e);
                throw;
            }
        }
    }

    private async void SetNicknameOnServer(string nickname)
    {
        try
        {
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                Debug.LogWarning("아직 로그인되지 않았습니다.");
                return;
            }
        
            string resultName = await AuthenticationService.Instance.UpdatePlayerNameAsync(nickname);
            Debug.Log($"서버에 설정된 PlayerName = {resultName}");
        }
        catch (Exception e)
        {
            Debug.Log("예외 발생" + e);
            return;
        }
    }
}
