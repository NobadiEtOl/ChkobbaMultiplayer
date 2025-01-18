using UnityEngine;
using System.IO;
using UnityEditor;

public class CardGenerator : MonoBehaviour
{
    public Sprite[] cardFaces; // Array of sprites for the card faces
    public Sprite cardBack;    // The sprite for the card back
    public GameObject cardPrefab; // The card prefab template
    public GameObject selectedCardIndicatorPrefab; // Prefab for the selected card indicator

    void Start()
    {
        CreateCardPrefabs();
    }

    void CreateCardPrefabs()
    {
        // Check if card faces, back, and selected card indicator are assigned
        if (cardFaces.Length == 0 || cardBack == null || selectedCardIndicatorPrefab == null)
        {
            Debug.LogError("Card Faces, Card Back, or Selected Card Indicator not assigned!");
            return;
        }

        int kindCounter = 1;

        for (int faceCounter = 1; faceCounter <= cardFaces.Length; faceCounter++)
        {
            // Create the card prefab from the template
            GameObject newCard = Instantiate(cardPrefab);

            // Set the card face sprite to the face of the card
            newCard.GetComponent<SpriteRenderer>().sprite = cardFaces[faceCounter - 1];

            // Create a background card object and set its sprite
            GameObject back = new GameObject("CardBack");
            back.transform.SetParent(newCard.transform); // Attach the card back to the card face
            back.transform.localPosition = new Vector3(0, 0, 0.1f); // Position the back correctly behind the face
            back.transform.localRotation = Quaternion.Euler(0, 180, 0);

            // Add a sprite renderer to the back object
            SpriteRenderer backRenderer = back.AddComponent<SpriteRenderer>();
            backRenderer.sprite = cardBack;

            // Set sorting layer or order to ensure correct layering
            backRenderer.sortingOrder = 0;  // Card back behind the card face
            newCard.GetComponent<SpriteRenderer>().sortingOrder = 0;

            // Add the selected card indicator as a child
            GameObject selectedIndicator = Instantiate(selectedCardIndicatorPrefab, newCard.transform);
            selectedIndicator.name = "SelectedCardIndicator";
            selectedIndicator.transform.localPosition = new Vector3(0, 0, 0.2f);  // Position it relative to the card
            selectedIndicator.SetActive(false); // Initially make it inactive

            // Determine kind (suit) and number
            string kindName = "";
            if (faceCounter % 10 == 1 && faceCounter != 1)
            {
                kindCounter++;
            }

            switch (kindCounter)
            {
                case 1:
                    kindName = "Carreau";
                    break;
                case 2:
                    kindName = "Coeur";
                    break;
                case 3:
                    kindName = "Pique";
                    break;
                case 4:
                    kindName = "Trefle";
                    break;
                default:
                    kindName = "error";
                    break;
            }

            string temp = (faceCounter % 10 == 0) ? "10" : (faceCounter % 10).ToString();

            // Assign name, tag, and layer
            newCard.name = kindName + "_" + temp;
            newCard.tag = kindCounter + "_" + temp;
            newCard.layer = 3;
        }
    }
}
