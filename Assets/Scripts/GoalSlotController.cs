using UnityEngine;
using System.Collections;

/// <summary>
/// This Goal Slot controller class manages the creation of the goal combination of dice, and
/// it provides logic to validate the goal against the dice values.  It supports the following
/// Observer methods:
///     RedemptActionCallback (int score, int currentGoals)
///     ZeroPointNotification () 
/// </summary>
public class GoalSlotController : MonoBehaviour
{

	//=====================================================================
	///<summary>
	/// The types of dice goals
	///<summary>
	enum GoalTyp
	{
		FIVE_OF_A_KIND = 0,
		FOUR_OF_A_KIND,
		FULL_HOUSE,
		STRAIGHT,
		THREE_OF_A_KIND,
		TWO_PAIR,
		FIVE_OR_HIGHER,
		TWO_OR_LOWER,
		// Last element always used for counting
		GOAL_TYP_TOTAL
	};
	
	const int iGOAL_TOTAL = (int)GoalTyp.GOAL_TYP_TOTAL;  // frequent used int const
	bool[] aCheckedGoal;   // marks if a goal is matched
	bool[] aRedemptGoal;   // marks if a goal is claimed
	string[] aGoalName;      // the goal strings
	int[] aScoreMultiplier;  // the multipliers for scoring
	ScoreGoal[] aGoalButton;    // the direct ScoreGoal component to a GoalButton 
	
	public GameObject buttonPrefab;  // the prefab to create goal buttons
	
	// delegate functions
	public System.Action <int, int> RedemptActionCallback;  // <score, currentGoals>
	public System.Action ZeroPointNotification;
	

	/// <summary>
	/// a UITable to manage the layout of goal buttons
	/// </summary>
	UITable buttonTable;
	int diceSum;   // sum of dice values
	
	//=====================================================================
	
	///<summary>
	/// Use this for initialization
	///</summary>
	void Start ()
	{	
		// C# default initialization sets all false
		aCheckedGoal = new bool[ iGOAL_TOTAL ];
		aRedemptGoal = new bool[ iGOAL_TOTAL ];
		aScoreMultiplier = new int[ iGOAL_TOTAL ] {
			20, 10, 5, 4, 3, 2, 1, 1 };
		aGoalName = new string[ iGOAL_TOTAL ] {
			"5 of a Kind",
			"4 of a Kind",
			"Full House",
			"Straight",
			"3 of a Kind",
			"2 Pair",
			"Any 5 or higher",
			"Any 2 or lower" 
		};
		
		aGoalButton = new ScoreGoal[ iGOAL_TOTAL ];
		buttonTable = GetComponent<UITable> ();
		initButtonTable ();
		diceSum = 0;
	}
	
	///<summary>
	/// Set up the veritical table of goal slots
	///</summary>
	void initButtonTable ()
	{
		// Use prefab to create the buttons
		
		//Object pf = Resources.Load("Prefabs/GoalButton");
		
		if (buttonPrefab != null) {
			for (int ii = 0; ii < iGOAL_TOTAL; ii++) {
				GameObject inst = (GameObject)GameObject.Instantiate (buttonPrefab, Vector3.zero, Quaternion.identity);
				if (inst != null) {
					ScoreGoal sg = inst.GetComponent<ScoreGoal> ();		
					sg.Rank = ii;
					sg.LabelText = aGoalName [ii] + " (x" + aScoreMultiplier [ii] + ")";
					sg.Target = this.gameObject;
					sg.ClickCallback = "HandleGoalClick";
					inst.name = "Goal Button " + ii.ToString ();
					inst.transform.parent = this.gameObject.transform;
					inst.transform.localScale = buttonPrefab.transform.localScale;
					inst.transform.localPosition = buttonPrefab.transform.localPosition;
					inst.transform.localRotation = buttonPrefab.transform.localRotation;
					aGoalButton [ii] = sg;
				}
			}
			
			buttonPrefab.SetActive (false);
		}
		
		buttonTable.repositionNow = true;
	}
	
	///<summary>
	/// This call back function handles clicks from the Goal buttons
	///</summary>
	void HandleGoalClick (GameObject srcGO)
	{
		ScoreGoal goal = srcGO.GetComponent<ScoreGoal> ();
		Debug.Log (goal.LabelText + " was click. Rank is " + goal.Rank);
		
		aRedemptGoal [goal.Rank] = true;
		goal.State = ScoreGoal.GoalButtonState.USED;
		int redempted = 0;
		foreach (ScoreGoal sg in aGoalButton) {
			if (sg.State == ScoreGoal.GoalButtonState.HIGHLIGHTED) {
				sg.State = ScoreGoal.GoalButtonState.NORMAL;	
			} else if (sg.State == ScoreGoal.GoalButtonState.USED) {
				redempted++;
			}
		}
		
		if (RedemptActionCallback != null) {
			
			RedemptActionCallback (diceSum * aScoreMultiplier [goal.Rank], iGOAL_TOTAL - redempted);
		}
	}
	
	///<summary>
	/// Resets Data Structure 
	/// Note that RedemptGaol are reset on new Scene (new game) only
	///</summary>
	public void ResetData ()
	{
		for (int ii = 0; ii < iGOAL_TOTAL; ii++) {
			aCheckedGoal [ii] = false;
		}
		diceSum = 0;
		
		for (int ii = 0; ii < iGOAL_TOTAL; ii++) {
			// undo any previous highlights
			if (aGoalButton [ii].State == ScoreGoal.GoalButtonState.HIGHLIGHTED) {
				aGoalButton [ii].State = ScoreGoal.GoalButtonState.NORMAL;
			}
		}
	}
	
	//---------------------------------------------------------------------- 
	///<summary>
	/// Receive dice value and validate result, the input array is sorted.
	///</summary>
	public void ValidateGoals (int [] diceVal)
	{
		ResetData ();
		int [] valCount = new int[6];
		diceSum = 0;
		
		aCheckedGoal [(int)GoalTyp.STRAIGHT] = true; // try to prove in the loop
		for (int ii = 0; ii < diceVal.Length; ii++) {
			valCount [diceVal [ii] - 1]++;
			
			if (diceVal [ii] <= 2) {
				aCheckedGoal [(int)GoalTyp.TWO_OR_LOWER] = true;
			}
			
			if (diceVal [ii] >= 5) { 
				aCheckedGoal [(int)GoalTyp.FIVE_OR_HIGHER] = true;
			}
			
			if (ii > 0) {
				if (diceVal [ii] != diceVal [ii - 1] + 1) {
					aCheckedGoal [(int)GoalTyp.STRAIGHT] = false;	
				}
			}
			
			diceSum += diceVal [ii];
		} // end of diceVal loop
		
		
		bool OnePairFound = false;
		for (int ii = 0; ii < 6; ii++) {
			if (valCount [ii] == 5) {
				aCheckedGoal [(int)GoalTyp.FIVE_OF_A_KIND] = true;
				aCheckedGoal [(int)GoalTyp.FULL_HOUSE] = true;
				aCheckedGoal [(int)GoalTyp.TWO_PAIR] = true;
			}
				
			if (valCount [ii] >= 4) {
				aCheckedGoal [(int)GoalTyp.FOUR_OF_A_KIND] = true;
				aCheckedGoal [(int)GoalTyp.TWO_PAIR] = true;
			}
			
			if (valCount [ii] >= 3) {
				aCheckedGoal [(int)GoalTyp.THREE_OF_A_KIND] = true;
				if (OnePairFound) {
					// there is exactly one pair exist, so..	
					aCheckedGoal [(int)GoalTyp.FULL_HOUSE] = true;
					aCheckedGoal [(int)GoalTyp.TWO_PAIR] = true;
				} else {
					OnePairFound = true;
				}
			}
			
			if (valCount [ii] == 2) {
				if (aCheckedGoal [(int)GoalTyp.THREE_OF_A_KIND]) {
					aCheckedGoal [(int)GoalTyp.FULL_HOUSE] = true;
				} else if (OnePairFound) {
					// a pair found in previous iteration	
					aCheckedGoal [(int)GoalTyp.TWO_PAIR] = true;
				}
				OnePairFound = true;
			}
		} // end of valCount loop
		
		bool zeroPoint = true;
		for (int ii = 0; ii < iGOAL_TOTAL; ii++) {
			
			if (aCheckedGoal [ii] && !aRedemptGoal [ii]) {
				Debug.Log (aGoalName [ii] + " is good for Redemption.");
				aGoalButton [ii].State = ScoreGoal.GoalButtonState.HIGHLIGHTED;	
				zeroPoint = false;
			}
		}
		
		// Notify we don't have any available goal match in this throw
		if (zeroPoint) {
			if (ZeroPointNotification != null) {
				ZeroPointNotification ();
			}
		}
	}  // end of ValidateGoals
	//---------------------------------------------------------------------- 

} // End of Class definition (GoalSlotController)
