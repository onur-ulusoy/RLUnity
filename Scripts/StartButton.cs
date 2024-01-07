using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class StartButton : MonoBehaviour
{
    public TMP_InputField initialXInput;
    public TMP_InputField initialYInput;
    public TMP_InputField initialZInput;
    public TMP_InputField destinationXInput;
    public TMP_InputField destinationYInput;
    public TMP_InputField destinationZInput;

    public GameObject gameController; // Assign this in the inspector

    private SimController simController;

    void Start()
    {
        // Get the SimController component from the GameController object
        if (gameController != null)
        {
            simController = gameController.GetComponent<SimController>();
        }
        else
        {
            Debug.LogError("GameController is not assigned in the inspector.");
        }

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

    public GameObject QLearningAgent;
    public bool learningActive = false;
    public QLearningAgent qLA;
    public bool started = false;
    void OnButtonClick()
    {
        if (simController == null)
        {
            Debug.LogError("SimController component is not found on the GameController object.");
            return;
        }
        if (!started)
        {
            started = true;

            // Parse input field values for initial position and destination
            if (TryParseVector3Int(initialXInput, initialYInput, initialZInput, out Vector3Int initialPosition) &&
                TryParseVector3Int(destinationXInput, destinationYInput, destinationZInput, out Vector3Int destination))
            {
                // Call the GoToDestination function with the parsed positions
                //simController.GoToDestination(initialPosition, destination);
                qLA.SetVariables(new List<Vector3Int> { simController.structureSize, initialPosition, destination });
                simController.SwitchCubeMaterialAt(initialPosition, "selected");


            }
            else
            {
                Debug.LogError("Invalid input for coordinates.");
            }
        }
        

        learningActive = !learningActive;
        QLearningAgent.SetActive(learningActive);
    }

    private bool TryParseVector3Int(TMP_InputField xField, TMP_InputField yField, TMP_InputField zField, out Vector3Int result)
    {
        result = new Vector3Int();

        if (int.TryParse(xField.text, out int x) &&
            int.TryParse(yField.text, out int y) &&
            int.TryParse(zField.text, out int z))
        {
            result = new Vector3Int(x, y, z);
            return true;
        }

        return false;
    }
}
