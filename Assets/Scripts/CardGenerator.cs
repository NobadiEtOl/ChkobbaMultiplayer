using UnityEngine;
using System.IO;
using UnityEditor;

public class CardGenerator : MonoBehaviour
{
    // Public variables to be set in the Unity Inspector
    public Sprite[] cardFaces; // Array of sprites for the card faces
    public Sprite cardBack;    // The sprite for the card back
    public GameObject cardPrefab; // The card prefab template (you can drag in an empty prefab with the required components)

    void Start()
    {
        CreateCardPrefabs();
    }

    void CreateCardPrefabs()
    {
        // Check if card faces and back are assigned
        if (cardFaces.Length == 0 || cardBack == null)
        {
            Debug.LogError("Card Faces or Card Back not assigned!");
            return;
        }

        int faceCounter=1;
        int kindCounter=1;
        // Loop through each card face
        foreach (Sprite face in cardFaces)
        {
            // Create the card prefab from the template
            GameObject newCard = Instantiate(cardPrefab);

            // Set the card face sprite to the face of the card
            newCard.GetComponent<SpriteRenderer>().sprite = face;

            //To check if all cards are present
            //newCard.transform.position = new Vector3(faceCounter * 10, 0,faceCounter);

            // Create a background card object and set its sprite
            GameObject back = new GameObject("CardBack");
            back.transform.SetParent(newCard.transform); // Attach the card back to the card face
            back.transform.localPosition = new Vector3(0,0,0.0001f); // Position the back correctly behind the face
            back.transform.localRotation = Quaternion.Euler(0, 180, 0);

            // Add a sprite renderer to the back object
            SpriteRenderer backRenderer = back.AddComponent<SpriteRenderer>();
            backRenderer.sprite = cardBack;

            // Optionally, set sorting layer or order to make sure the face is on top of the back
            backRenderer.sortingOrder = 0;  // Set the card back behind the card face
            newCard.GetComponent<SpriteRenderer>().sortingOrder = 0;

            // You can also add other custom components or scripts to the card here if needed
            // E.g., newCard.AddComponent<CardBehavior>();
            
            string kindName="";

            if(faceCounter<=10)kindCounter=1;
            else if(faceCounter<=20)kindCounter=2;
            else if(faceCounter<=30)kindCounter=3;
            else if(faceCounter<=40)kindCounter=4;
            else kindCounter=0;

            if(kindCounter==1)kindName="Carreau";
            else if(kindCounter==2)kindName="Coeur";
            else if(kindCounter==3)kindName="Pique";
            else if(kindCounter==4)kindName="Trefle";
            else kindName="error";

            string temp = (faceCounter%10==0) ? 10.ToString() : (faceCounter%10).ToString();

            newCard.name = kindName + "_" + temp;

            faceCounter++;

        }
    }
}
