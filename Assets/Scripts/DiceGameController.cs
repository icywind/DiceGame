using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Dice game controller class controlls the overall Scene game logics.
/// </summary>
public class DiceGameController : MonoBehaviour
{

	//----------------------------------------------------------------------
	//   Declarations
	//----------------------------------------------------------------------
	
	// Game objects that are preconnected via editor

	public GameObject[] DiceSpots;       // Spots hold the dice after roll
	public Die[]        myDice;          // The 5 Dice
	public Vector3       myForce;         // Editor defined force values
	
	public GameObject    DiceShowPanel;   // area showing dice
	public GameObject    GoalSlotPanel;   // contain goal buttons
	public GameObject	 UserPanel;   	  // contain control button and helper text
	public GameObject	 EndPanel;    	  // ending dialog
	
	public GameObject    ControlButton;   // The main controll button
	public UILabel       HelpInfoLabel;   // displays help text

	public UILabel	     TurnLabel;       // HUD Label
	public UILabel	     ScoreLabel;      // HUD Label
	public UILabel	     RollsLabel;      // HUD Label
	public UILabel       EndLabel;		  // Ending description
	public UILabel		 GrandScoreLabel; // the final score
	
	UITweener  	     goalSlotTween;   // The GoalSlots can move out of the view
	GoalSlotController   goalController;  // Controls the Goal logics
	
	public int MAX_ROLLS = 3;
	int _myScore = 0;
	int _myTurns = 1;
	int _myCurrentRolls = 0;
	GameObject HelpInfoObject; // parent to HelpInfoLabel
	
	/// <summary>
	/// Enums the States of the game controller	
	/// </summary>
	enum DiceGameState
	{
		AWAITING_ACTION,
		PREPARING_THROW,   // rolling all the dice
		MAKEUP_THROW,      // rolling undetermined dice
		ROLLING_DICE,
		DICE_SETTLE,
		SHOWING_RESULT,
		GAME_ENDING
	};

	/// <summary>
	/// the state of game
	/// </summary>
	DiceGameState gameState;
		
	/// <summary>
	/// Use for checking before and after tweening
	/// </summary>
	bool 	   bResetWaiting = false;
	
	/// <summary>
	/// is there a Makeup throw?
	/// </summary>
	bool	   bMakingUpThrow = false;
	
	/// <summary>
	/// Checks if any die is rolling in the scene
	/// </summary>
	bool bDiceRolling {   
		get {
			bool anyRolling = false;
			foreach (Die die in myDice) {
				if (die) {
					anyRolling |= die.rolling;	
				}
			}
			return anyRolling;
		}
	}
	
	/// <summary>
	/// How many rollings of the dice
	/// </summary>
	int MyRolls {
		get { return _myCurrentRolls; }
		set { 
			_myCurrentRolls = value;
			RollsLabel.text = "Roll:" + _myCurrentRolls.ToString () + "/" + MAX_ROLLS.ToString ();
		}
	}
		
	/// <summary>
	/// Current Score
	/// </summary>
	int MyScore {
		get { return _myScore; }
		set {
			_myScore = value;
			ScoreLabel.text = "Score:" + _myScore.ToString ();
		}
	}
	
	/// <summary>
	/// Current Turn
	/// </summary>
	int MyTurns {
		get { return _myTurns; }
		set { 
			_myTurns = value;
			TurnLabel.text = "Turn:" + _myTurns.ToString ();
		}
	}
	
	/// <summary>
	/// The dice which can be used to roll	
	/// </summary>
	List<Die> dice4Roll;
	
	//----------------------------------------------------------------------
	// Functions
	//----------------------------------------------------------------------
	
	///<summary>
	/// Use this for initialization
	///</summary>
	void Start ()
	{
		print ("Hello, this is game controller Start");
		dice4Roll = new List<Die> ();
		ResetDice ();
		gameState = DiceGameState.AWAITING_ACTION;	
		HelpInfoObject = HelpInfoLabel.transform.parent.gameObject;

		MyRolls = 0; // make sure updated with MAX

		goalSlotTween = GoalSlotPanel.GetComponent<UITweener> ();
		goalController = GoalSlotPanel.GetComponent<GoalSlotController> ();
		goalController.RedemptActionCallback += RedemptNotify;
		goalController.ZeroPointNotification += ZeroPointNotify;	

		NGUITools.SetActive (UserPanel, true);
		NGUITools.SetActive (EndPanel, false);
	}

	///<summary>
	/// Not use Update, since it does not have to be every single frame
	///</summary>
	void FixedUpdate ()
	{
		if (gameState == DiceGameState.ROLLING_DICE) {
			if (!bDiceRolling) {
				SettleDice ();
			}
		}
		
		if( Input.GetKey( KeyCode.Escape ) ) {
			Application.Quit();	
		}
	}

	///<summary>
	/// Reset the dice roll list and the physics on the dice
	///</summary>
	void ResetDice ()
	{
		dice4Roll.Clear ();	
		int count = 0;
		GameObject startPivot = GameObject.Find ("StartPivotPoint");
		Vector3 pos = startPivot.transform.position;

		foreach (Die die in myDice) {
			UIButtonMessage uimsg = die.GetComponent<UIButtonMessage> ();
			if (uimsg != null) {
				uimsg.target = this.gameObject;
				uimsg.functionName = "OnDiePressed";
			}
			dice4Roll.Add (die);
			die.tag = "Untagged";
			// dieOnFloat( die.gameObject, count );  // put back position
			die.gameObject.rigidbody.useGravity = true;  // drop to the table
			die.gameObject.rigidbody.isKinematic = true;  // drop to the table
			die.transform.position = new Vector3 (pos.x + 0.3f * count, pos.y, pos.z);
			count++;
		}	

		ClearHolders ();
	}
	
	
	///<summary>
	/// Clear out the dice holders
	///</summary>
	void ClearHolders ()
	{
		UISprite sprite;
		foreach (GameObject holder in DiceSpots) {
			sprite = holder.GetComponentInChildren<UISprite> ();
			sprite.alpha = 0.5f;
		}
	}
	
	///<summary>
	/// After long press, dice are floating and waiting to be thrown out
	///</summary>
	void ShowFloatingDice ()
	{
		int count = 0;
		GameObject containRoom = GameObject.Find ("ContainRoom");
		foreach (Die die in dice4Roll) {
			die.transform.parent = containRoom.transform;
			Debug.Log ("Die(" + die.gameObject.name + ")");	
			dieOnFloat (die.gameObject, count);
			count++;
		}
	}
	
	///<summary>
	/// Called by ShowFloatingDice, handle each individual die
	/// Predefined location of the spawning points are used.
	///</summary>
	void dieOnFloat (GameObject dieObject, int index)
	{
		Vector4 angleVelocity = new Vector4 (0, 0, 0, 0);
		// destroy current gallery die if we have one
		GameObject sp = GameObject.Find ("SpawnPoint" + index.ToString ());
		if (sp != null) {

			// Set to the starting position
			dieObject.transform.position = sp.transform.position;
			// disable rigidBody gravity
			dieObject.rigidbody.useGravity = false;
			dieObject.rigidbody.isKinematic = false;
			// add saved angle and angle velocity or torque impulse
			if (angleVelocity.x == 0 && angleVelocity.y == 0 && angleVelocity.z == 0)
				dieObject.rigidbody.AddTorque (new Vector3 (0, -.4F, .4f), ForceMode.Impulse);
			else
				dieObject.rigidbody.angularVelocity = angleVelocity;
		} else {
			Debug.LogError ("Can't find spawn point of name SpawnPoint" + index.ToString ());	
		}
	}	
	
	///<summary>
	/// Calling this function means the die is already in a Dice Holder
	///</summary>
	void LockDie (Die die, bool lockIt)
	{
		GameObject holder = die.gameObject.transform.parent.gameObject;
		UISprite sprite = holder.GetComponentInChildren<UISprite> ();
		
		if (lockIt) {
			die.gameObject.tag = "Finish";
			sprite.alpha = 1f;		
		} else {
			die.gameObject.tag = "Untagged";
			sprite.alpha = 0.5f;		
		}
	}
	
	
	///<summary>
	/// The dice stop rolling, let them sit on the table until user tap
	///</summary>
	void SettleDice ()
	{
		gameState = DiceGameState.DICE_SETTLE;	
		HelpInfoObject.SetActive (true);
		HelpInfoLabel.text = "Tap table collect and sort dice.";
	}
	
	///<summary>
	/// Press the still die to lock it
	///</summary>
	void OnDiePressed (GameObject dieObject)
	{
		Die die = dieObject.GetComponent<Die> ();
		Debug.Log (die.name + "Die pressed.");
		if (die.rolling || gameState != DiceGameState.AWAITING_ACTION)
			return;   // don't do any thing if die is not still
		
		if (die.tag == "Untagged") {
			dice4Roll.Remove (die);
			LockDie (die, true);
		} else {
			if (!dice4Roll.Contains (die)) {
				dice4Roll.Add (die);	
				LockDie (die, false);
			}
		}
	
		//float dieScaleZ = dieObject.transform.localScale.z;
		//	dieObject.transform.localPosition += new Vector3(0,0, dieScaleZ/2);
		
	}
	
	///<summary>
	/// The Action Control button is pressed (a long-press gesture)
	///</summary>
	void OnActionPress ()
	{
		bool okToThrow = false;
		
		if (bResetWaiting)
			return;
		
		if (gameState == DiceGameState.AWAITING_ACTION && MyRolls < MAX_ROLLS) {
			okToThrow = true;
		} else if (gameState == DiceGameState.MAKEUP_THROW && MyRolls <= MAX_ROLLS) {
			okToThrow = true;
		}
			
		if (okToThrow) {
			HelpInfoLabel.text = "Release the button to throw dice.";
			PrepareThrow ();
			ShowFloatingDice ();
		}
	}
	
	///<summary>
	/// Tapping/clicking the Control button; not used 
	///</summary>
//	void OnActionTap() {
//		if( bResetWaiting ) {		
//			ResetPanelPosition();
//		}
//	}
	
	
	///<summary>
	/// Release of the Control button
	///</summary>
	void OnActionRelease ()
	{
		if (gameState == DiceGameState.PREPARING_THROW) {
			ThrowDice ();	
		}
	}
	
	///<summary>
	/// Getting ready to throw dice 
	///</summary>
	void PrepareThrow ()
	{
		gameState = DiceGameState.PREPARING_THROW;
		goalSlotTween.enabled = true;
		goalSlotTween.Play (true);
		goalSlotTween.eventReceiver = this.gameObject;
		goalSlotTween.callWhenFinished = "GoalTweeningFinish";
		DiceShowPanel.SetActive (false);
	}
	
	///<summary>
	/// Function to calculate force to be used
	///</summary>
	Vector3 Force (GameObject spawnPoint)
	{
		Vector3 rollTarget = Vector3.zero + new Vector3 (2 + 7 * Random.value, .5F + 4 * Random.value, -2 - 3 * Random.value);
		return Vector3.Lerp (spawnPoint.transform.position, rollTarget, 1).normalized * (-35 - Random.value * 20);
	}
    
	///<summary>
	/// throw the dice out
	///</summary>
	void ThrowDice ()
	{
		if (gameState == DiceGameState.PREPARING_THROW && !bMakingUpThrow) {
			MyRolls++;	
		}
		gameState = DiceGameState.ROLLING_DICE;

		ControlButton.SetActive (false);	
		//Vector3 vForce = new Vector3( -0.5f, 0.5f, 0.1f);

		foreach (Die die in dice4Roll) {
			Vector3 vForce = myForce; //Force ( die.gameObject );
			float fMag = -50 * die.transform.localScale.magnitude;
			die.gameObject.rigidbody.AddForce (vForce);
			die.gameObject.rigidbody.AddTorque (new Vector3 (fMag * Random.value, fMag * Random.value, fMag * Random.value), ForceMode.Impulse);
			die.gameObject.rigidbody.useGravity = true;
			die.gameObject.rigidbody.isKinematic = false;
		}
	}
	
	///<summary>
	/// Call back from the tweening object (Goal Slots)
	///</summary>
	void GoalTweeningFinish ()
	{
		bResetWaiting = !bResetWaiting; 		
	}
	
	///<summary>
	/// Reset Panel Position 
	///</summary>
	void ResetGoalPosition ()
	{
		bResetWaiting = false;
		goalSlotTween.callWhenFinished = "GoalResultShown";
		goalSlotTween.Play (false);
	}
	
	///<summary>
	/// Callback after the tween of reset poistion finished
	///</summary>
	void GoalResultShown ()
	{
		if (bMakingUpThrow) {
			gameState = DiceGameState.MAKEUP_THROW;
		} else {
			gameState = DiceGameState.AWAITING_ACTION;	
		}
	}
	
	//---------------------------------------------------------------------- 
	/// <summary>
	/// After dice stop rolling, transition to new state
	/// Show the Results.
	/// </summary>
	void ShowRollResult ()
	{
		gameState = DiceGameState.SHOWING_RESULT;
            
		ResetGoalPosition ();
		DiceShowPanel.SetActive (true);
		ControlButton.SetActive (true);

		// construct a list of dice that lay flat, if value = 0, then it is undetermined
		List <Die> diceList = new List<Die> ();
		dice4Roll.Clear ();
		foreach (Die die in myDice) {
			if (die.value > 0) {
				diceList.Add (die);
			} else {
				dice4Roll.Add (die);  
			}
		}
		SortDiceList (diceList);
		PrintDiceList (diceList); // debug

		// Move the dice to the holder spots
		ClearHolders ();
		int index = 0;
		foreach (Die die in diceList) {
			if (DiceSpots [index]) {
				die.transform.parent = DiceSpots [index].transform;
				die.transform.localPosition = new Vector3 (0, 0, die.transform.localPosition.z);
				// if previously locked
				if (die.tag == "Finish") {
					Debug.Log (" Relock die ");
					LockDie (die, true);	
				}
			}
			index++;
		}


		// dice4Roll now holds the undetermined dice
		if (dice4Roll.Count > 0) {
			gameState = DiceGameState.MAKEUP_THROW;
			bMakingUpThrow = true;  // need this since state change after tweening
		} else { 
			// all dice has value
			gameState = DiceGameState.AWAITING_ACTION;
			bMakingUpThrow = false;

			int [] diceVal = new int[5];
			int cnt = 0;
			foreach (Die die in diceList) {
				diceVal [cnt] = die.value;   // construct the array for validation
				cnt++;

				// since dice4Roll has 0 element, put the dice back to this list if not locked
				if (die.tag == "Untagged") {
					dice4Roll.Add (die);
				}
			}		

			// validate the result, a callback maybe called from goalController
			goalController.ValidateGoals (diceVal);
		}
	} // end of ShowRollResult
	//---------------------------------------------------------------------- 
	
	///<summary>
	/// Handle taps on the table cloth
	///</summary>
	void OnPlaneClick ()
	{
		if (gameState == DiceGameState.DICE_SETTLE) {
			ShowRollResult ();
		}
		
		if (gameState == DiceGameState.AWAITING_ACTION) {
			HelpInfoLabel.text = "Press on the die to lock/unlock it for next turn or choose your goal prize.";
		} else if (gameState == DiceGameState.MAKEUP_THROW) {
			HelpInfoLabel.text = "Reroll the undertermined dice";
		}
	}
	
	///<summary>
	/// Callback: redemption, add the score to total and reset for next turn
	///</summary>
	void RedemptNotify (int score, int goalsLeft)
	{
		ResetDice ();
		goalController.ResetData ();
		MyScore += score;		
		MyRolls = 0;
		MyTurns++;
		if (goalsLeft == 0) {
			ShowEnding (true);	
		}
	}
	
	///<summary>
	/// Callback: If the dice has no points in outcome
	///</summary>
	void ZeroPointNotify ()
	{
		Debug.Log ("Uh-oh, this throw does not match any goal points :(");	
		if (MyRolls >= MAX_ROLLS) {
			ShowEnding (false);	
		}
	}
	
	///<summary>
	/// Handle the end of the game.
	///</summary>
	void ShowEnding (bool winning)
	{
		gameState = DiceGameState.GAME_ENDING;

		NGUITools.SetActive (UserPanel, false);
		NGUITools.SetActive (EndPanel, true);
	
		GrandScoreLabel.text = "Total Score: " + MyScore.ToString();

		if (winning) {
			EndLabel.text = "Congratulations!!\n You just clear the game!! \nWant to Play again?";
		} else {
			EndLabel.text = "Hmmm...\n You did great but did not beat the game.\nWant to Play again?";
		}
	}
	
	///<summary>
	///  User wants to restart
	///</summary>
	void OnOKRestart ()
	{
		Debug.Log ("Reload game...");
		Application.LoadLevel ("MainScene");	
	}
	
	///<summary>
	///  User wants to quit
	///</summary>
	void OnQuit ()
	{
		Debug.Log ("Quiting game...");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;	
#else
		Application.Quit ();		
#endif
	}
	
	//================================================================
	// UTIL Functions
	//================================================================
	///<summary>
	/// Sorting
	///</summary>
	void SortDiceList (List<Die> diceList)
	{
		diceList.Sort (delegate(Die x, Die y) {
			return x.value.CompareTo (y.value); 
		});
	}
	
	///<summary>
	/// Printing
	///</summary>
	void PrintDiceList (List<Die> diceList)
	{
		int count = 0;
		foreach (Die die in diceList) {
			Debug.Log ("Dice " + count.ToString () + " = " + die.value);
			count++;
		}
	}
	
}  // End of Class definition (DiceGameController)
