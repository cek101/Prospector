using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;


public class Prospector : MonoBehaviour {

	static public Prospector 	S;

	[Header("Set in Inspector")]
	public TextAsset			deckXML;
    public TextAsset            layoutXML;
    // added in 671 - 672
    
    public float                xOffset = 3;
    public float                yOffset = -2.5f; 
    public Vector3              layoutCenter;
    


	[Header("Set Dynamically")]
	public Deck					    deck;
    public Layout                   layout;
    public List<CardProspector>     drawPile;
    // added in 672
    
    public Transform layoutAnchor;
    public CardProspector target;
    public List<CardProspector> tableau;
    public List<CardProspector> discardPile;

    

    void Awake() {
		S = this; // set up a singleton for prospector
	}

	void Start() {
		deck = GetComponent<Deck> (); // get the deck
		deck.InitDeck (deckXML.text); // pass deckxml to it
        Deck.Shuffle(ref deck.cards); // this shffles the deck // i added this line, found it on page 668
        ///

        ///

        layout = GetComponent<Layout>(); // get the layout component
        layout.ReadLayout(layoutXML.text); // pass layout XML to it
        drawPile = ConvertListCardsToListCardProspectors( deck.cards ); // p 670
        //LayoutGame(); // added in 672
	}
    
    List<CardProspector> ConvertListCardsToListCardProspectors (List<Card> lCD) {
        List<CardProspector> lCP = new List<CardProspector>();
        CardProspector tCP; 
        foreach ( Card tCD in lCD) {
            tCP = tCD as CardProspector;
            lCP.Add( tCP );
        }
        return ( lCP );
    }
     // p 672
    //the draw function will pull a single card from the drawPile and return it
    CardProspector Draw () {
        CardProspector cd = drawPile[0]; // pull the 0th CardProspector
        drawPile.RemoveAt(0); // then remove it from List<> drawPile
        return (cd);  // and return it
    }

    //layoutGame() positions the initial tableau of cards, a.k.a. the "mine"
    void LayoutGame() {
        // create an empyt GameObject to serve as an anchor for the tableau 
        if (layoutAnchor == null) {
            GameObject tGO = new GameObject("_LayoutAnchor");
            // ^ create an empty GameObject named _LayoutAnchor in the Hierarchy
            layoutAnchor = tGO.transform; // grab its transform
            layoutAnchor.transform.position = layoutCenter; // position it
        }

        // moves the current target to the discardPile
        void MoveToDiscard(CardProspector cd) {
            // set the state of the card to discard
            cd.state = eCardState.discard;
            discardPile.Add(cd); // add it to the discardPile List<>
            cd.transform.parent = layoutAnchor; // update its transform parent

            // position this card on the discardPile
            cd.transform.localPosition = new Vector3(
            layout.multiplier.x * layout.discardPile.x,
                layout.multiplier.y * layout.discardPile.y,
                -layout.discardPile.layerID + 0.5f);
            cd.FaceUp = true;
            // place it on top of the pile for depth sorting
            cd.SetSortingLayerName(layout.discardPile.layerName);
            cd.SetSortOrder(-100 + discardPile.Count);
        }

        //make cd the new target card
        void MoveToTarget(CardProspector cd) {
            // if there is currently a target card move it to discardPile
            if (target != null) MoveToDiscard(target);
            target = cd; // cd is the new target
            cd.state = eCardState.target;
            cd.transform.parent = layoutAnchor;
            //move to the target position
            cd.transform.localPosition = new Vector3(
                layout.multiplier.x * layout.discardPile.x,
                layout.multiplier.y * layout.discardPile.y,
                -layout.discardPile.layerID);

            cd.FaceUp = true;// mae it face-up
            cd.SetSortingLayerName(layout.discardPile.layerName);
            cd.SetSortOrder(0);
        }

        // arranges all the cards of the drawPile to show how many are left
        void UpdateDrawPile() {
            CardProspector cd;
            // go through all the cards of the drawPile
            for (int i = 0; i < drawPile.Count; i++) {
                cd = drawPile[i];
                cd.transform.parent = layoutAnchor;

                //position it correctly with the layout.drawPile.stagger
                Vector2 dpStagger = layout.drawPile.stagger;
                cd.transform.localPosition = new Vector3(
                    layout.multiplier.x * layout.discardPile.x,
                    layout.multiplier.y * layout.discardPile.y,
                    -layout.discardPile.layerID + 0.1f * i);

                cd.FaceUp = false;  // make them all face-down
                cd.state = eCardState.drawpile;
                // set depth sorting
                cd.SetSortingLayerName(layout.drawPile.layerName);
                cd.SetSortOrder(-10 * i);
            }
        }

        // cardclicked is called any time a card in the game is clicked
        public void CardClicked(CardProspector cd) {
            // the reaction is determined by the state of the clicked card
            switch (cd.state) {
                case eCardState.target:
                    // clicking the target card does nothing
                    break;

                case eCardState.drawpile:
                    // clicking any card in the drawPile will draw the next card
                    MoveToDiscard(target); // moves the target to the discardPile
                    MoveToTarget(Draw()); // Moves the next drawn card to the target
                    UpdateDrawPile(); // restacks the drawPile
                    break;

                case eCardState.tableau:
                    // clicking a card in the tableau will check if it's a valid play
                    bool validMatch = true;
                    if (!cd.FaceUp) {
                        // if the card is face-down, it's not valid
                        validMatch = false;
                    }
                    if (!AdjacentRank(cd, target)) {
                        // if it's not an adjacent rank, it's not valid
                        validMatch = false;
                    }
                    if (!validMatch) return; // return if not valid

                    // if we got here, then: Yay! it's a valid card.
                    tableau.Remove(cd); // remove it from the tableau list
                    MoveToTarget(cd); // make it the target card
                    SetTableauFaces(); // update the tableau card face ups
                    break;
            }
            // check to see whether the game is over or not
            CheckForGameOver();
        }

        // test whether the game is over 
        void CheckForGameOver() {
            // if the tableau is empty, the game is over 
            if (tableau.Count == 0) {
                // call GameOver() with a win
                GameOver(true);
                return;
            }

            // if there are still cards in the draw pile, the game's not over
            if (drawPile.Count > 0) {
                return;
            }

            // check for remaining valid plays
            foreach (CardProspector cd in tableau) {
                if (AdjacentRank(cd, target)) {
                    // if there is a valid play, the game's not over
                    return;
                }
            }


            // since there are no valid plays, the game is over
            // call GameOver with a loss
            GameOver(false);
        }

        // called when the game is over.  simple for now, but expandable 
        void GameOver(bool won) {
            if (won)    {
                print("Game Over.  You Won! :)");
            }else {
                print("Game Over.  You Lost! :)");
            }
            // reload the scene, resetting the game
            SceneManager.LoadScene("_Prospector_Scene_0");
        }

        // return true if the two cards are adjacent in rank ( A & K wrap around)
        public bool AdjacentRank(CardProspector c0, CardProspector c1) {
            // if either card is face-down, it's not adjacent.
            if (!c0.FaceUp || !c1.FaceUp) return (false);

            // if they are 1 apart, they are adjacent
            if (Mathf.Abs(c0.rank - c1.rank) == 1) {
                return (true);
            }
            // if one is Ace and the other King, they are adjacent
            if (c0.rank == 1 && c1.rank == 13) return (true);
            if (c0.rank == 13 && c1.rank == 1) return (true);

            // otherwise, return false
            return (false);
        }
        CardProspector cp;
        // follow the layout
        foreach (SlotDef tSD in layout.slotDefs) {
            // ^ Iterate through all the SLotDefs in the layout.slotDefs as tSD
            cp = Draw(); // pull a card from the top (beginning) of the draw pile
            cp.FaceUp = tSD.faceUp; // set its faceUp to the value in SlotDef
            cp.transform.parent = layoutAnchor; // make its parent layoutAnchor
            // this replaces the previous parent: deck.deckANchor, which 
            // appears as _Deck in the Hierarchy when the scene is playing.
            cp.transform.localPosition = new Vector3(
                layout.multiplier.x * tSD.x,
                layout.multiplier.y * tSD.y,
                -tSD.layerID);
            // ^ set the localPOsition of the card based on slotDef
            cp.layoutID = tSD.id;
            cp.slotDef = tSD;
            // cardProspectors in the tableau have the state CardState.tableau
            cp.state = eCardState.tableau;
            // cardProspectors in the tableau have the state CardState.tableau
            cp.SetSortingLayerName(tSD.layerName); // Set the sorting layers

            tableau.Add(cp); // add this Card Prospector to the List<> tableau
        }
        //set up the initial target card
        MoveToTarget(Draw());

        // set up the draw pile 
        UpdateDrawPile();

    }
    
}
