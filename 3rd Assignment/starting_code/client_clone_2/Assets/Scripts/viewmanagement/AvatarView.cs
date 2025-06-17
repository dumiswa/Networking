using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/**
 * The AvatarView class is a wrapper around both a skin and speechbubble with a couple of simple methods:
 * 
 *  Move        -   moves anywhere you tell it do 
 *  SetSkin     -   takes an id, automatically mods/clamps it to a valid index in a list of skin prefabs and instantiates it
 *  Say         -   passes your text on the SpeechBubble, you can safely 'say' all incoming messages, 
 *                  the SpeechBubble auto queues and displays it.
 * 
 * For instantaneous positioning, just set the worldposition directly (probably only needed on spawning).
 * 
 * @author J.C. Wichman
 */
public class AvatarView : MonoBehaviour
{
    [Tooltip("How fast does this avatar move to the given target location")]
    public float moveSpeed = 0.05f;

    [Tooltip("This list of skin prefabs this avatar can use. If it has an animator, we also call SetBool(IsWalking, ...) on it.")]
    [SerializeField] private List<GameObject> prefabs = null;
    private GameObject _skin = null;    //the current chosen skin
    private int _skinId = -1;           //the skin id so we can prevent setting duplicates

    private bool _moving = false;       //are we currently moving?
    private Vector3 _target;            //if so where?

    private SpeechBubble _speechBubble; //reference to the speech bubble so we can say stuff
    private Animator _animator = null;  //if present a reference to the animator so we can check if we are walking

    private void Awake()
    {
        //this should always be present
        _speechBubble = GetComponentInChildren<SpeechBubble>();
        //this needs to be retrieved on a per skin basis
        //_animator = ...
    }

    private void Update()
    {
        //if we are moving, rotate towards the target and move towards it
        if (_moving)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_target - transform.position, Vector3.up);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, 6f * Time.deltaTime);
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, _target, moveSpeed * Time.deltaTime);
            _moving = Vector3.Distance(transform.localPosition, _target) > 0.01f;
            //if we stopped moving, update the animator
            if (!_moving) updateAnimator();
        }
    }

    /**
     * Initiate move the avatar towards the endposition with the default movement speed.
     */
    public void Move(Vector3 pEndPosition)
    {
        _target = pEndPosition;
        _moving = true;
        updateAnimator();
    }

    /**
     * Changes the current skin by replacing the current skin gameobject with a new one.
     * At this point for simplicity the AvatarView expects a certain prefab height,
     * a skin pivot point aligned at the feet of the skin and 
     * an animator with an IsWalking parameter.
     * Please check one of the provided skin prefabs.
     */
    public void SetSkin (int pSkin)
    {
        //'normalize' the skin id so we will never crash
        if (pSkin % prefabs.Count == _skinId) return;
        _skinId = Mathf.Clamp(pSkin % prefabs.Count, 0, prefabs.Count);

        //bye bye current one if one exists
        if (_skin != null) Destroy(_skin);

        //create the new one and get its animator
        _skin = Instantiate(prefabs[_skinId], transform);
        _animator = _skin.GetComponent<Animator>();
        updateAnimator();

        //throw some scaling effect in there
        _skin.transform.DOScale(1, 1).From(0.01f, true).SetEase(Ease.OutElastic);
    }

    /**
     * Queue the given text into the speechbubble.
     */
    public void Say(string pText)
    {
        _speechBubble.Say(pText);
    }

	public void Remove()
	{
        _skin.transform.DOScale(0, 0.5f).SetEase(Ease.InBack);
        _speechBubble.Clear();
        Destroy(gameObject, 0.6f);
        enabled = false;
    }

	/**
     * Set the animator to walking and update its speed if required.
     */
	private void updateAnimator()
    {
        if (_animator == null) return;
        _animator.SetBool("IsWalking", _moving);
        _animator.speed = _moving ? moveSpeed : 1;
    }

}
