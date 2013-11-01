using UnityEngine;
using System.Collections;

//==========================================================================
/// <summary>
/// This class provides data storage and logic for a goal button.
/// </summary>
public class ScoreGoal : MonoBehaviour {

	public enum GoalButtonState {
		NORMAL 		= 0,
		HIGHLIGHTED = 1,
		USED		= 2
	};
	
	UILabel buttonLabel;
	GoalButtonState _myState;
	Color [] backGroundColor;
	Color [] textColor;	
	
	public UISprite BackgroundSprite;
	public int Rank {get;set;}  
	public string ClickCallback;
	public GameObject Target;
	
	public GoalButtonState State {
		get { return _myState; }
		set {
			_myState = value;
			UpdateAttribute();
		}
	}
	
	string _labelText = string.Empty;
	public string LabelText {
		get { 
			if( initialized ) return buttonLabel.text; 
			else return _labelText;
		}
		set { 
			_labelText = value;
			if( initialized ) buttonLabel.text = _labelText; 
		}
	}
	
	bool initialized = false;
	
	/// <summary>
	/// Start this instance.
	/// </summary>
	void Start () {			
		backGroundColor = new Color[3];
		backGroundColor[(int)GoalButtonState.NORMAL] = new Color( 217/255f, 230/255f, 18/255f, 142/255f);
		backGroundColor[(int)GoalButtonState.HIGHLIGHTED] = new Color( 1f,1f,1f, 200/255f);
		backGroundColor[(int)GoalButtonState.USED] = new Color( 92/255f, 85/255f, 225/255f, 100/255f);
		
		textColor = new Color[3];
		textColor[(int)GoalButtonState.NORMAL] = new Color( 188/255f, 170/255f, 170/255f, 200/255f);
		textColor[(int)GoalButtonState.HIGHLIGHTED] = new Color( 1f,1f,1f,1f);
		textColor[(int)GoalButtonState.USED] = new Color( 40/255f, 40/255f, 40/255f, 1f);
		
		buttonLabel = GetComponentInChildren<UILabel>();
		initialized = true;
		if( _labelText != string.Empty ) {
			buttonLabel.text = _labelText;
		}
		
		State = GoalButtonState.NORMAL;
	}
	
	/// <summary>
	/// Updates the button background and label color and style according to state.
	/// </summary>
	void UpdateAttribute () {
		UIButton btn;
		btn = GetComponent <UIButton> ();
		
		BackgroundSprite.color = backGroundColor[(int)_myState];
		buttonLabel.color      = textColor[(int)_myState];
		
		switch( _myState ) {
		case GoalButtonState.NORMAL:
			buttonLabel.effectStyle = UILabel.Effect.None;
			break;
		case GoalButtonState.HIGHLIGHTED:
			buttonLabel.effectStyle = UILabel.Effect.Outline;
			break;
		case GoalButtonState.USED:
			buttonLabel.effectStyle = UILabel.Effect.None;
			btn.enabled = false;
			break;
		}
	}
	
	/// <summary>
	/// Raises the click event.
	/// </summary>
	void OnClick() {
		if( Target && State == GoalButtonState.HIGHLIGHTED ) {
			if( ClickCallback.Length > 0 ) {
				Target.SendMessage( ClickCallback, this.gameObject, SendMessageOptions.DontRequireReceiver );
			}
		}
	}
}
