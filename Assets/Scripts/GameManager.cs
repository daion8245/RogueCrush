using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;     // Singleton Instance

    public GameObject panelBackground;      // background panel
    public GameObject panelVictory;         // victory panel
    public GameObject panelDefeat;          // lose panel

    public int goalScore;                   // score needed to win
    public int moves;                       // moves left
    public int points;                      // current score

    public bool isGameEnded;                // is the game ended

    public TMP_Text txt_Points;
    public TMP_Text txt_Moves;
    public TMP_Text txt_Goal;

    public TMP_Text txt_Victory;
    public TMP_Text txt_Lose;
    private void Awake()
    {
        Instance = this;
    }

    public void Initialize(int _moves, int _goal)
    {
        moves = _moves;
        goalScore = _goal;
    }
    // Update is called once per frame
    void Update()
    {
        txt_Points.text = "Points: " + points.ToString();
        txt_Moves.text = "Moves: " + moves.ToString();
        txt_Goal.text = "Goal: " + goalScore.ToString();
    }
    
    public void ProcessTurn(int _pointsToGain , bool _subtractMoves)
    {
        points += _pointsToGain;
        if (_subtractMoves)
        {
            moves--;
        }
        
        if (moves == 0)
        {
            if (points >= goalScore)
            {
                // win game
                isGameEnded = true;
                // display victory panel
                panelBackground.SetActive(true);
                panelVictory.SetActive(true);
                txt_Victory.text = $"Points : {points}";
                FindAnyObjectByType<BoardSystem>().gameObject.SetActive(false);
                return;
            }
            else
            {
                // lose game
                isGameEnded = true;
                // display defeat panel
                panelBackground.SetActive(true);
                panelDefeat.SetActive(true);
                txt_Lose.text = $"Points : {points}\nGoal Points : {goalScore}";
                FindAnyObjectByType<BoardSystem>().gameObject.SetActive(false);
                return;
            }
        }
    }
    public void BackToMenu()
    {
        SceneManager.LoadScene(0);
    }
    public void Retry()
    {
        SceneManager.LoadScene(1);
    }
}
