using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TallyMarkDisplay : MonoBehaviour
{
    public GameObject[] tallyPrefabs; // 0 = Tally1, 1 = Tally2, ..., 4 = Tally5
    [SerializeField]private int tallyCount; // Example, you can update this dynamically

    [ContextMenu("Update Tally Display")]
    public void UpdateTallyDisplay(int newTallyCount)
    {
        tallyCount = newTallyCount;
        // Clear previous marks
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        int countLeft = tallyCount;

        while (countLeft > 0)
        {
            int amount = Mathf.Min(countLeft, 10);

            GameObject tallyMark = Instantiate(tallyPrefabs[amount - 1], transform);
            tallyMark.transform.SetParent(transform, false);

            countLeft -= amount;
        }
    }
}
