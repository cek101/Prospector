using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// an enum to handle all the possible scoring events
public enum eScoreEvent {
    draw, mine, mineGold, gameWin, gameLoss
}

//scoreManager handles all of the scoring
public class ScoreManager : MonoBehaviour {
    static private ScoreManager     S;

    static public int SCORE_FROM_PREV_ROUND = 0;
    static public int HIGH_SCORE = 0;

    [Header("Set Dynamically")]
    //fields to track score info
    public int chain = 0;
    public int scoreRun = 0;
    public int score = 0;
     
	void Awake () {
		if (S == null) {
            S = this; // set the private singleton
        }else {
            Debug.LogError("ERROR: ScoreManager.Awake(): S is already set!");
        }

        // check for a high score in PLayerPrefs
        if (PlayerPrefs.HasKey ("ProespectorHighScore")) {
            HIGH_SCORE = PlayerPrefs.GetInt("ProspectorHighScore");
        }
        // add the score from last round, which will be >0 if it was a win
        score += SCORE_FROM_PREV_ROUND;
        // and reset the SCORE_FROM_PREV_ROUND
        SCORE_FROM_PREV_ROUND = 0;
	}
	
    static public void EVENT(eScoreEvent evt) {
        try { // try-catch stops an error from breaking your program
            S.Event(evt);
        } catch (System.NullReferenceException nre) {
            Debug.LogError("ScoreManager:EVENT() called while S=null.\n" +nre );
        }
    }
	// Update is called once per frame
	void Event (eScoreEvent evt ) {
		switch (evt) {
            // same things need to happen whether it's a draw, a win, or a loss
            case eScoreEvent.draw: // drawing a card
            case eScoreEvent.gameWin: // won the round
            case eScoreEvent.gameLoss: // lost the round 
                chain = 0; // resets the score chain
                score += scoreRun; // add scoreRun to total score
                break;

            case eScoreEvent.mine: // remove a mine card
                chain++; // increase the score chain
                scoreRun += chain; // add score for thsi card to run
                break;
        }

        // this second switch statement handles round wins and losses
        switch (evt) {
            case eScoreEvent.gameWin:
                // if it's a win, add the score to the next round
                // static fields are NOT reset by Scenemanger.LoadScene()
                SCORE_FROM_PREV_ROUND = score;
                print("You won this round! Round score: " + score);
                break;

            case eScoreEvent.gameLoss:
                // if it's a loss, check agaisnt the high score
                if (HIGH_SCORE <= score ) {
                    print("You got the high score! High Score: " + score);
                    HIGH_SCORE = score;
                    PlayerPrefs.SetInt("ProspectorHighScore", score);
                }else {
                    print("Your final score for the game was: " + score);
                }
                break;

            default: 
                print ("score: "+score+" scoreRun:"+scoreRun+" chain:"+chain);
                break;
        }
	}

    static public int CHAIN {  get { return S.chain;  } }
    static public int SCORE {  get { return S.score;  } }
    static public int SCORE_RUN {  get { return S.scoreRun; } }
}
