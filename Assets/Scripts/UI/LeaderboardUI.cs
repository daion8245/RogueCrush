using System;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Services.Core;

namespace UI
{
    public class LeaderboardUI : MonoBehaviour
    {
        #region SerializeField
    
        [SerializeField]
        private Button backButton;

        [SerializeField]
        private Text leaderboardTxt;
    
        #endregion

        private string[] _topPlayer = new string[5];
        
        public static LeaderboardUI Instance;

        private void Awake()
        {
            //싱글톤 패턴 구현
            if (Instance == null)
                Instance = this;
            else
                //이미 인스턴스가 존재하면 자신을 파괴
                Destroy(gameObject);
            
            UnityServicesReset();
        }

        private async void UnityServicesReset()
        {
            try
            {
                await UnityServices.InitializeAsync();
                Debug.Log("Unity Services initialized successfully!");
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }
            }
            catch (ServicesInitializationException exception)
            {
                Debug.LogError($"Unity Services failed to initialize: {exception.Message}");
            }
        }

        private void Start()
        {
            backButton.onClick.AddListener(()=>SceneManager.LoadScene(0)); //뒤로가기 버튼 구현
            GetScore(5);
        }

        public static async void LeaderboardSubmitScore(int score)
        {
            try
            {
                await LeaderboardsService.Instance.AddPlayerScoreAsync("best_Player_Leaderboard", score);
                Debug.Log("플레이어 스코어 리더보드에 기록됨.");
            }
            catch (Exception e)
            {
                Debug.LogError("점수 제출 오류: " + e.Message);
            }
        }

        private async void GetScore(int topN)
        {
            try
            {
                var topScores = await LeaderboardsService.Instance.GetScoresAsync
                ("best_Player_Leaderboard", new GetScoresOptions
                {
                    Limit = topN
                });

                if (topScores != null && topScores.Results.Count > 0)
                {
                    for (int i = 0; i < topScores.Results.Count; i++)
                    {
                        string str = null;
                        var score = topScores.Results[i];
                        Debug.Log($"순위 {i + 1}: 유저 이름 = {score.PlayerName}, 점수 = {score.Score}");
                        
                        switch (i)
                        {
                            case 0:
                                break;
                            case 1:
                                break;
                            case 2:
                                break;
                        }

                        str = $"{i + 1} : {score.PlayerName} \t score({score.Score}) \n";

                        _topPlayer[i] = str;
                    }
                    
                    SetLeaderboardText();
                    
                    Debug.Log("모든 순위가 표시되었습니다.");
                }
                else
                {
                    Debug.Log("리더보드에 데이터가 없습니다.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"리더보드 데이터를 가져오는 중 오류가 발생했습니다: {e.Message}");
            }
        }

        private void SetLeaderboardText()
        {
            leaderboardTxt.text = "";
            foreach (string player in _topPlayer)
            {
                leaderboardTxt.text += player;
            }
        }
        
    }
}