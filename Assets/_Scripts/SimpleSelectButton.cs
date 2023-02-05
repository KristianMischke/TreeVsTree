using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class SimpleSelectButton : MonoBehaviour
{
    private Button _button;

    public GameLogic.GameParameters Parameters;
    // Start is called before the first frame update
    void Start()
    {
        _button = GetComponent<Button>();
        
        _button.onClick.AddListener(OnClicked);
    }
    
    private void OnClicked()
    {
        NetworkGameController.Instance.CreateRoomWithSettings(Parameters);
    }
}
