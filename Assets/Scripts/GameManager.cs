using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        if (points >= goalScore)
        {
            // win game
            isGameEnded = true;
            // display victory panel
            panelBackground.SetActive(true);
            panelVictory.SetActive(true);
            return;
        }
        if (moves == 0)
        {
            // lose game
            isGameEnded = true;
            // display defeat panel
            panelBackground.SetActive(true);
            panelDefeat.SetActive(true);
            return;
        }
    }
    // 
    public void WinGame()
    {
        SceneManager.LoadScene(0);
    }
    public void LoseGame()
    {
        SceneManager.LoadScene(0);
    }
}
