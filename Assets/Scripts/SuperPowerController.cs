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
        GameManager.LocalInstance.ActivateBlockNextPlayerPower();
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

[CreateAssetMenu(menuName = "SuperPower/Kapkaç")]
public class Kapkaç : SuperPower
{
    public override void ActivatePower()
    {
        Debug.Log("Kapkaç activated!");
        GameManager.LocalInstance.ActivateKapkacPower();
    }
}

[CreateAssetMenu(menuName = "SuperPower/ValeArar")]
public class ValeArar : SuperPower
{
    public override void ActivatePower()
    {
        Debug.Log("ValeArar activated!");
        GameManager.LocalInstance.ActivateValeArarPower();
    }
}

[CreateAssetMenu(menuName = "SuperPower/KopyalaYapistir")]
public class KopyalaYapistir : SuperPower
{
    public override void ActivatePower()
    {
        Debug.Log("KopyalaYapıstır activated!");
        GameManager.LocalInstance.ActivateKopyalaYapistirPower();
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
