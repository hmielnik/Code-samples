using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CheatConsole : UIElement
{
    public static CheatConsole Instance;
    public InputAction[] inputActions;
    public Text cmdHistory;
    public InputField cmdField;
    public Dictionary<string, Vector3> waypoints = new Dictionary<string, Vector3>();

    public void Awake()
    {
        Instance = this;
    }

    public void StartConsole()
    {
        cmdField.text = null;
        cmdField.Select();
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            ExecuteCommand();
        }
    }

    public void ExecuteCommand()
    {
        if (cmdField.text == null)
        {
            cmdField.ActivateInputField();
            return;
        }

        string userInput = cmdField.text.ToLower().ToString();
        cmdHistory.text = userInput + "\n" + cmdHistory.text;
        char[] delimiterCharacters = { ' ' };
        string[] separatedInput = userInput.Split(delimiterCharacters);

        for (int i = 0; i < inputActions.Length; i++)
        {
            InputAction action = inputActions[i];
            if (action.keyWord == separatedInput[0])
            {
                action.InputCommand(separatedInput, FoxCharacter.Instance.gameObject);
            }
        }

        cmdField.text = null;
        cmdField.ActivateInputField();
    }

}
