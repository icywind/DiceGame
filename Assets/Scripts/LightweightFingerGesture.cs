using UnityEngine;
using System.Collections;


/*
 *  Customized lightweight tapping gesture recognizer which supports
 * 	  - single tap 
 * 	  - long press
 *    - swipe left
 *    - swipe right
 * 
 *  Date: Sep 5, 2012
 */
public class LightweightFingerGesture : MonoBehaviour {
	
	public GameObject gestureEventReceiver;

	public string OnLongPressInvoke = "";
	public string OnSingleTapInvoke = "";
	public string OnSwipeLeftInvoke = "";
	public string OnSwipeRightInvoke = "";
	public string OnDraggingInvoke = "";
	public string OnDragReleased = "";
	public string OnLongPressUp  = "";
	public float longPressThreshold = 0.7f;
	
	Vector2 lastTouchLocation;
	float lastTouchTime;
	
	void Start() {
		if (gestureEventReceiver == null) {
			gestureEventReceiver = gameObject;
		}
	}
	
	FSMState fsmState = FSMState.NOTPRESSED;
	
	private enum FSMState {
		NOTPRESSED,
		PRESSED,
		DRAGGING,
		LONGPRESSED,
		SIGNLETAP,
		SWIPE
	}
	
	void OnPressDown() {
		if (fsmState == FSMState.NOTPRESSED || fsmState == FSMState.DRAGGING) {
			fsmState = FSMState.PRESSED;
			lastTouchLocation = UICamera.currentTouch.pos;
			lastTouchTime = Time.time;
			Invoke("OnLongPressTimeout", longPressThreshold);
		}
	}
	
	private bool isTouchPositionChanged(Vector2 touch1, Vector2 touch2) {
		return Vector2.Distance(touch1, touch2) > 30;	
	}
	
	void OnDrag (Vector2 delta)  {
		fsmState = FSMState.DRAGGING;
		if(OnDraggingInvoke.Length > 0) gestureEventReceiver.SendMessage(OnDraggingInvoke);	
	}
	
	void OnPressUp() {
		if (fsmState == FSMState.PRESSED) {
			if (! isTouchPositionChanged(lastTouchLocation, UICamera.currentTouch.pos)) {
				fsmState = FSMState.SIGNLETAP;
				OnSingleTapDetected();		// raise SIGNLETAP;
				fsmState = FSMState.NOTPRESSED;
			} 
		} else if (fsmState == FSMState.DRAGGING) {
			if (Time.time - lastTouchTime < longPressThreshold) {    // TODO: might be better to have its own threshold
				fsmState = FSMState.SWIPE;
				Vector2 delta = UICamera.currentTouch.pos - lastTouchLocation;
				if (delta.x < 0) {
					OnSwipeLeftDetected();  // raise SWIPE;
				} else {
					OnSwipeRightDetected();
				}
				fsmState = FSMState.NOTPRESSED;
			} else {
				OnDragged();
			}
				
		} else if(fsmState == FSMState.LONGPRESSED) {
			OnLongPressUpReturn();
			fsmState = FSMState.NOTPRESSED;
		}
	}
	
	void OnDragged() 
	{
		// mike: added sender parameter so we know who got dropped
		if(OnDragReleased.Length > 0) 
			gestureEventReceiver.SendMessage(OnDragReleased, this.gameObject );	
	}
	
	void OnLongPressTimeout() {
		if (fsmState == FSMState.PRESSED) {
			fsmState = FSMState.LONGPRESSED;
			OnLongPressDetected();     // raise long pressed event
			//fsmState = FSMState.NOTPRESSED;  // reset
		}
	}
	
	void OnLongPressUpReturn() {
		if (OnLongPressUp.Length > 0)  
			gestureEventReceiver.SendMessage(OnLongPressUp, gameObject, SendMessageOptions.DontRequireReceiver);
	}
	
	void OnSingleTapDetected() {
		if (OnSingleTapInvoke.Length > 0) 
			gestureEventReceiver.SendMessage(OnSingleTapInvoke, gameObject, SendMessageOptions.DontRequireReceiver);
	}
	
	void OnLongPressDetected() {
		if (OnLongPressInvoke.Length > 0) 
			gestureEventReceiver.SendMessage(OnLongPressInvoke, gameObject, SendMessageOptions.DontRequireReceiver);
	}
	
	void OnSwipeLeftDetected() {
		if (OnSwipeLeftInvoke.Length > 0) 
			gestureEventReceiver.SendMessage(OnSwipeLeftInvoke, SendMessageOptions.DontRequireReceiver);
	}
	
	void OnSwipeRightDetected() {
		if (OnSwipeRightInvoke.Length > 0) 
			gestureEventReceiver.SendMessage(OnSwipeRightInvoke, SendMessageOptions.DontRequireReceiver);
	}
	
	public void OnPress(bool isPressed) {
		//Debug.Log("OnPress pressed=" + isPressed);
		if (isPressed) {
			OnPressDown(); 
		} else {
			OnPressUp();
		}
	}
}
