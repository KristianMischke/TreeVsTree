using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public static UIController Instance;
    
    // unity scene references
    [SerializeField] private Text moveText;
    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private GameObject player1Win;
    [SerializeField] private GameObject player2Win;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning("UIController Singleton issue");
        
        Instance = this;
    }
    
    public void ShowRoomCode(string roomCode)
    {
        moveText.text = "Code: " + roomCode;
    }
    
    public void SetTurnText(bool isOurTurn, int remainingMoves)
    {
        if (isOurTurn)
        {
            moveText.text = "Moves left:" + remainingMoves;
        }
        else
        {
            moveText.text = "Waiting...";
        }
    }

    public void ShowWinner(sbyte winnerId)
    {
        gameOverScreen.SetActive(true);
        if(winnerId == 0){
            player1Win.SetActive(true);
        }
        else if(winnerId == 1){
            player2Win.SetActive(true);
        }
    }
}
