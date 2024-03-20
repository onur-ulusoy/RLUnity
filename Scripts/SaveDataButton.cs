using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SaveDataButton : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject learningAgent;

    void Start()
    {
        // Add listener for button click
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
        }
        else
        {
            Debug.LogError("StartButton script is not attached to a button.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnButtonClick()
    {
        learningAgent = GameObject.FindGameObjectWithTag("la");
        learningAgent.GetComponent<QLearningAgent>().SaveQTable();
    }

}
