using System;
using System.Collections.Generic;
using Unity.Services.Leaderboards;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Services.Core;

namespace UI
{
    public struct PlayerScore
    {
        public string playerName;
        public int playerScore;
    }

    public class LeaderboardUI : MonoBehaviour
    {
        #region PrivateVariable

        private List<PlayerScore> playerScores = new List<PlayerScore>();

        #endregion
    
        #region SerializeField
    
        [SerializeField]
        private Button backButton;
    
        #endregion
        
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
            }
            catch (ServicesInitializationException exception)
            {
                Debug.LogError($"Unity Services failed to initialize: {exception.Message}");
            }
        }

        private void Start()
        {
            backButton.onClick.AddListener(()=>SceneManager.LoadScene(0)); //뒤로가기 버튼 구현
            GetScore(10);
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
                var topScores = await LeaderboardsService.Instance.GetScoresAsync("test_Leaderboard", new GetScoresOptions
                {
                    Limit = topN
                });

                if (topScores != null && topScores.Results.Count > 0)
                {
                    for (int i = 0; i < topScores.Results.Count; i++)
                    {
                        var score = topScores.Results[i];
                        Debug.Log($"순위 {i + 1}: 유저 ID = {score.PlayerId}, 점수 = {score.Score}");
                    }
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
    }
}