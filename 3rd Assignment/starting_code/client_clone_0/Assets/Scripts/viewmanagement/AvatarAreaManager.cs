using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

/**
 * The AvatarAreaManager class is the main facade to the whole visual avatar area you see in your scene.
 * It allows you to create an AvatarView mapped to an ID, and provides information after you clicked
 * on the avatar area in a certain range around its center.
 * 
 * The AvatarAreaManager creates the avatar views by instantiating an AvatarView prefab, 
 * which is a gameobject that wraps both a speechbubble and a skin. 
 * See AvatarView for more information.
 * 
 * How to use? 
 * -----------------------------------------------------------------
 * Register for important events like this:
 *  _avatarAreaManager.OnAvatarAreaClicked += onAvatarAreaClicked;
 *  _panelWrapper.OnChatTextEntered += onChatTextEntered;
 *  
 * And call important functionality like this:
 *  _avatarAreaManager.AddAvatarView(int pId) : AvatarView
 *  _avatarAreaManager.GetAvatarView (int pAvatarId) : AvatarView
 *  _avatarAreaManager.RemoveAvatarView (int pAvatarId) : void
 *  _avatarAreaManager.GetAllAvatarIds () : List<int> 
 *  
 *  _avatarView.Move(Vector3 pEndPosition)
 *  _avatarView.SetSkin (int pSkin)
 *  _avatarView.Say (string pText)
 *  
 *  You can inspect the details of the classes for more information, but it is not required to finish the course. 
 * 
 * @author J.C. Wichman
 */
public class AvatarAreaManager : MonoBehaviour
{
    [Tooltip("The prefab that is instantiated for each avatar")]
    [SerializeField] private AvatarView _avatarViewPrefab = null;

    [Tooltip("The radius around our own world center in which on screen raycasts on the floor plane are accepted.")]
    public float radius = 20;

    //map of integer id's to actual avatarviews
    private Dictionary<int, AvatarView> _avatarViews = new Dictionary<int, AvatarView>();

    //event callback for when someone clicks on our avatar area
    public Action<Vector3> OnAvatarAreaClicked = delegate { };

    /**
     * Creates a new AvatarView with the given id.
     * If a view already exists for the given id an exception will be thrown.
     */
    public AvatarView AddAvatarView(int pId)
    {
        if (HasAvatarView(pId))
        {
            throw new ArgumentException($"Cannot add AvatarView with id {pId}, already exists.");
        }

        //create a new view with ourselves as the transform parent
        AvatarView avatarView = Instantiate<AvatarView>(_avatarViewPrefab, transform);
        _avatarViews[pId] = avatarView;
        return avatarView;
    }

    /**
     * Returns the AvatarView with the given id or an exception if it doesn't exist.
     */
    public AvatarView GetAvatarView (int pAvatarId)
    {
        if (!HasAvatarView(pAvatarId))
        {
            throw new Exception($"Avatar with key {pAvatarId} not found.");
        } 

        return _avatarViews[pAvatarId];
    }

    /**
     * Tells you whether an AvatarView for the given id exists.
     */
    public bool HasAvatarView(int pAvatarId)
    {
        return _avatarViews.ContainsKey(pAvatarId);
    }

    /**
     * Removes the AvatarView with the given id or an exception if the given id does not exist.
     */
    public void RemoveAvatarView (int pAvatarId)
    {
        AvatarView avatarView = GetAvatarView(pAvatarId);
        avatarView.Remove();
        _avatarViews.Remove(pAvatarId);
    }

    /**
     * Returns a list of all of the current avatar ids we are managing.
     * This allows for easy updating/comparing with another list of avatar ids.
     * 
     * e.g. given another list of avatar ids you could:
     * - add an avatar if it is in the other list but not in this one
     * - update an avatar if it is in both lists
     * - remove an avatar if it is in this list but not in the other one (the left-overs so to speak)
     */
    public List<int> GetAllAvatarIds ()
    {
        return _avatarViews.Keys.ToList<int>();
    }

    private void Update()
    {
        doRayCast(); 
    }

    /**
     * Raycasts if your mouse is on the screen and not over a UI element,
     * and calls OnAvatarAreaClicked if you hit something.
     */
    private void doRayCast()
    {
        if (Input.mousePosition.x < 0 || Input.mousePosition.x > Screen.width) return;
        if (Input.mousePosition.y < 0 || Input.mousePosition.y > Screen.height) return;

        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            Plane plane = new Plane(Vector3.up, 0);
            
            float hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (plane.Raycast(ray, out hit))
            {
                Vector3 point = ray.GetPoint(hit);
                if ((point - transform.position).magnitude < radius) OnAvatarAreaClicked(point);
            }
        }
    }

}
