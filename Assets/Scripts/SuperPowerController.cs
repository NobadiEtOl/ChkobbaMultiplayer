using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public abstract class SuperPower : ScriptableObject
{
    public string name;
    public string description;
    public int rarityMultiplier;
    public abstract void ActivatePower();
}

[CreateAssetMenu(menuName = "SuperPower/UcundanGözAt")]
public class UcundanGözAt : SuperPower
{
    public override void ActivatePower()
    {
        Debug.Log("Ucundan Göz At activated!");
        GameManager.LocalInstance.UsePeekOpponentCardPower();
    }
}

[CreateAssetMenu(menuName = "SuperPower/Oynayamazsın")]
public class Oynayamazsın : SuperPower
{
    public override void ActivatePower()
    {
        Debug.Log("Oynayamazsın activated!");
    }
}

[CreateAssetMenu(menuName = "SuperPower/DeğişTokuş")]
public class DeğişTokuş : SuperPower
{
    public override void ActivatePower()
    {
        Debug.Log("Değiş Tokuş activated!");
        GameManager.LocalInstance.UseSwapCardWithOpponentPower();
    }
}

public class SuperPowerController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
