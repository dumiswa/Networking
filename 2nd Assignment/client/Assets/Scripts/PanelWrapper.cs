using System;
using UnityEngine;
using UnityEngine.UI;

/**
 * Wrapper around the chat view.
 * 
 * Methods of interest:
 *  OnChatTextEntered += Action<String>
 *  AddOutput(string)
 *  ClearInput()
 *  
 *  @author J.C. Wichman
 */
public class PanelWrapper : MonoBehaviour
{
    //hook up these fields to the correct components in the view
    [SerializeField] private InputField _inputFieldChat = null;
    [SerializeField] private Text _textOutput = null;
    [SerializeField] private ScrollRect _scrollRect = null;

    private bool _focusedRequested = false;
    private bool _scrollRequested = false;

    public event Action<string> OnChatTextEntered = delegate { };

    private void Start()
    {
        //setup chat input listener to trigger on enter
        _inputFieldChat.onEndEdit.AddListener(
            (value) => {
                if (Input.GetKeyDown(KeyCode.Return)) OnChatTextEntered(value);
            }
         );
    }

    public void ClearInput()
    {
        _inputFieldChat.text = "";
        _focusedRequested = true;
    }

    public void ClearOutput()
    {
        _textOutput.text = "";
        _scrollRequested = true;
    }

    public void AddOutput(string pOutput)
    {
        _textOutput.text += pOutput + "\n";
        _scrollRequested = true;
    }

    private void Update()
    {
        checkFocus();
    }

    private void checkFocus()
    {
        if (_focusedRequested)
        {
            _inputFieldChat.ActivateInputField();
            _inputFieldChat.Select();
            _focusedRequested = false;
        }

        if (_scrollRequested)
        {
            _scrollRect.verticalNormalizedPosition = 0;
            _scrollRequested = false;
        }
    }

}
