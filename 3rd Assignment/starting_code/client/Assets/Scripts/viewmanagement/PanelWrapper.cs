using System;
using UnityEngine;
using UnityEngine.UI;

/**
 * Helper around the chat view.
 * 
 * Methods of interest:
 *  OnChatTextEntered += Action<String>
 *  ClearInput()
 *  
 *  @author J.C. Wichman
 */
public class PanelWrapper : MonoBehaviour
{
    //hook up these fields to the correct components in the view
    [SerializeField] private InputField _inputFieldChat = null;

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
    }
}
