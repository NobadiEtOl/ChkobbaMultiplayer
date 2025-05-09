using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class ProfileScript : MonoBehaviour
{
    [SerializeField]private Player player;
    [SerializeReference] private Sprite[] cardBackSprite;
    [SerializeField]private GameObject cardBackShowcase;
    // Start is called before the first frame update
    void Start()
    {
        cardBackShowcase.GetComponent<Image>().sprite = cardBackSprite[player.cardBackIndex];
    }

    // Update is called once per frame
    void Update()
    {
        
    }

   private int counter = 0;

    public void OnCardBackchangeNext()
    {
        // Increment the counter and loop back to 0 if it exceeds the array length
        counter = (counter + 1) % cardBackSprite.Length;

        // Update the card back showcase with the new sprite
        cardBackShowcase.GetComponent<Image>().sprite = cardBackSprite[(player.cardBackIndex + counter) % cardBackSprite.Length];
    }

    public void OnCardBackchangePrevious()
    {
        // Decrement the counter and loop back to the last index if it goes below 0
        counter = (counter - 1 + cardBackSprite.Length) % cardBackSprite.Length;

        // Update the card back showcase with the new sprite
        cardBackShowcase.GetComponent<Image>().sprite = cardBackSprite[(player.cardBackIndex + counter + cardBackSprite.Length) % cardBackSprite.Length];
    }

    public void OnSaveConfirm()
    {
        // Update the player's cardBackIndex with the new value
        player.cardBackIndex = (player.cardBackIndex + counter) % cardBackSprite.Length;

        // Update the player's card back in the game
        player.UpdateCardBack();

        // Reset the counter to 0
        counter = 0;
    }
}
