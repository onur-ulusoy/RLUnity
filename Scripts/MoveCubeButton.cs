using UnityEngine;
using UnityEngine.UI; // Required for Button
using TMPro; // Required for TextMeshPro elements

public class MoveCubeButton : MonoBehaviour
{
    public GameObject gameController; // Assign the GameController object in the inspector
    public TMP_InputField xInputField;
    public TMP_InputField yInputField;
    public TMP_InputField zInputField;
    public TMP_Dropdown directionDropdown;

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
            Debug.LogError("MoveCubeButton script is not attached to a button.");
        }
    }

    void OnButtonClick()
    {
        if (simController == null)
        {
            Debug.LogError("SimController component is not found on the GameController object.");
            return;
        }

        // Parse input field values for coordinates
        if (int.TryParse(xInputField.text, out int x) && 
            int.TryParse(yInputField.text, out int y) && 
            int.TryParse(zInputField.text, out int z))
        {
            string direction = directionDropdown.options[directionDropdown.value].text;

            // Call the MoveCube function with the parsed coordinates and selected direction
            simController.MoveCube(new Vector3Int(x, y, z), direction);
        }
        else
        {
            Debug.LogError("Invalid input for cube coordinates.");
        }
    }
}
