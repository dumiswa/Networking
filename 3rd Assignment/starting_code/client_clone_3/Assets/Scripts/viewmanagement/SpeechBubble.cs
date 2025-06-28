using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/**
 * SpeechBubble allows you to queue text that will automatically be displayed in pages,
 * including tween in and out animations and sound effects. The speech bubble grows and shrinks automatically
 * within certain limits and the delay in showing the next 'page' is related to the length of the text.
 * In addition you can click on the text to go to the next 'page'.
 * 
 * @author J.C. Wichman
 */ 
public class SpeechBubble : MonoBehaviour
{
    [Tooltip("Global scale setting for each bubble (if the text on your screen is too small, increase this value)")]
    public float globalScaleFactor = 1;
    [Tooltip("Text delay is multiplied with the text height to get a final wait value before the next piece of text is displayed")]
    public float waitMultiplier = 4;

    //using for tweening in the tooltip as it appears and disappears, combined with the global scale factor
    private float _localScaleFactor = 0;
    
    //used to adjust tooltip scale as speechbubble is moving closer or away from the camera
    private float _startingDistance;
    private Camera _camera;

    //the seperate elements of the speechbubble
    private SpriteRenderer _background;
    private TextMeshPro _textfield;

    //the list of text items we need to show. Each item is basically added by one call to Say.
    //We queue them, but additionally have to take into account that one item might not fit into
    //the speech bubble in one go, so we use the paging mechanism from textmesh pro
    private List<string> _backLog = new List<string>();
    //used to keep track of whether we have to animate in/out
    private bool _textShowing = false;
    //contains the audio effect we play as we go from _textShowing false to true
    private AudioSource _audioSource;
    //if you set this to true the chat sound will be played everytime a new page is displayed
    [SerializeField] private bool _playSoundEveryTime = false;

    //when we show a piece of text, we queue a callback to show the next piece or hide the bubble all together
    private Coroutine _lastRoutine = null;
    
    private void Awake()
    {
        _textfield = GetComponentInChildren<TextMeshPro>();
        _textfield.text = "";

        _background = GetComponentInChildren<SpriteRenderer>();

        _camera = Camera.main;
        _startingDistance = Vector3.Distance(_camera.transform.position, transform.position);

        _audioSource = GetComponent<AudioSource>();
        
        //trigger scale immediately to avoid weird glitches
        Update();
    }

    void Update()
    {
        transform.rotation = Quaternion.LookRotation(_camera.transform.forward, _camera.transform.up);

        float distanceBasedScaleAdjustment = Vector3.Distance(_camera.transform.position, transform.position) / _startingDistance;
        transform.localScale = Vector3.one *
                                globalScaleFactor *
                                _localScaleFactor *
                                distanceBasedScaleAdjustment;
    }

    /**
     * Queue text for immediate or future display (based on whether something is already being displayed)
     */
    public void Say(string pText)
    {
        if (pText == null || pText.Length == 0) return;
        _backLog.Add(pText);
        //currently not showing anything? Do the display immediately.
        if (!_textShowing) processNextBacklogItem();
    }

    public void Clear()
	{
        _backLog.Clear();
        if (_lastRoutine != null) StopCoroutine(_lastRoutine);
        hideSpeechBubble();
	}

    /**
     * Checks if there is more text to display in pages, otherwise hides the speech bubble.
     */
    private void processNextBacklogItem()
    {
        //is there stuff in the backlog? Ok show it
        if (_backLog.Count > 0)
        {
            showNextBacklogItem();
        } else //scale to zero and mark us as not displaying anything
        {
            hideSpeechBubble();
        }
    }

    /**
     * Grabs next string from the backlog for paged display.
     */
    private void showNextBacklogItem()
    {
        //get the backlog item, reset the current page to 1 in case something else messed with it
        _textfield.text = _backLog[0];
        _backLog.RemoveAt(0);
        //for the love of god, this should be 0, but through code the first page is 1 .... this took me hours :))!
        _textfield.pageToDisplay = 1;   
        updateSpeechBubbleSize();
        
        //transition in the bubble
        if (!_textShowing)
        {
            DOTween.To(() => _localScaleFactor, x => _localScaleFactor = x, 1, 1).SetEase(Ease.OutElastic, 0.1f);
        }

        if (!_textShowing || _playSoundEveryTime) _audioSource.Play();
        _textShowing = true;

        //queue timeout for next page
        queueShowNextPage();
    }

    private void showNextPage()
    {
        TMP_TextInfo textInfo = _textfield.textInfo;

        if (_textfield.pageToDisplay < textInfo.pageCount)
        {
            _textfield.pageToDisplay = _textfield.pageToDisplay + 1;
            if (_playSoundEveryTime) _audioSource.Play();
            updateSpeechBubbleSize();
            queueShowNextPage();
        } else
        {
            processNextBacklogItem();
        }
    }

    private void hideSpeechBubble()
    {
        DOTween.To(() => _localScaleFactor, x => _localScaleFactor = x, 0, 0.5f).SetEase(Ease.InQuad);
        _textShowing = false;
    }

    private void OnMouseUp()
    {
        if (_textShowing) showNextPage();
    }

    /////////////////////////////////////////////////////////////////////////
    /// Helper methods for delayed calls
    /////////////////////////////////////////////////////////////////////////

    private void queueShowNextPage ()
    {
        if (_lastRoutine != null) StopCoroutine(_lastRoutine);
        _lastRoutine = StartCoroutine(callLaterRoutine(showNextPage, _textfield.bounds.size.y * waitMultiplier));
    }

    private IEnumerator callLaterRoutine (Action pAction, float pDelay)
    {
        yield return new WaitForSeconds(pDelay);
        pAction();
    }


    private void updateSpeechBubbleSize()
    {
        //first calculate the correct mesh for the current page
        _textfield.ForceMeshUpdate();

        //the bubble size is equal to the text size with a little padding
        float backgroundHeight = Mathf.Max(_textfield.textBounds.size.y + 1, 1.2f);
        _background.size = new Vector2(2, backgroundHeight);

        //now we have to find the new center of the scaled sprite, which is using 9 sliced scaling with the pivot in the bottom
        float backgroundBottomSliceSize = _background.sprite.border.y / _background.sprite.pixelsPerUnit;
        float backgroundTopSliceSize = _background.sprite.border.w / _background.sprite.pixelsPerUnit;
        float backgroundCenterSize = backgroundHeight - backgroundBottomSliceSize - backgroundTopSliceSize;
        float backgroundCenterY = backgroundBottomSliceSize + backgroundCenterSize / 2;

        //now we take the background center, align top of textfield with the center and then deduct half of the actual text size
        float textCenterY = backgroundCenterY - _textfield.rectTransform.sizeDelta.y/2 + (_textfield.textBounds.size.y/2);
        _textfield.transform.localPosition = new Vector3(1, textCenterY, -0.1f);
    }

}
