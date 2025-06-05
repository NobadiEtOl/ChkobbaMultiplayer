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

[CreateAssetMenu(menuName = "SuperPower/BayaBayaBak")]
public class BayaBayaBak : SuperPower
{
    public override void ActivatePower()
    {
        Debug.Log("BayaBayaBak activated!");
        GameManager.LocalInstance.UseBayaBayaBakPower();
    }
}

[CreateAssetMenu(menuName = "SuperPower/Bomba")]
public class Bomba : SuperPower
{
    public override void ActivatePower()
    {
        Debug.Log("Bomba activated!");
        GameManager.LocalInstance.ActivateBombaPower();
    }
}

[CreateAssetMenu(menuName = "SuperPower/Yapamazsın")]
public class Yapamazsın : SuperPower
{
    public override void ActivatePower()
    {
        Debug.Log("Yapamazsın activated!");
        GameManager.LocalInstance.ActivateYapamazsınPower();
    }
}

[CreateAssetMenu(menuName = "SuperPower/VerZehri")]
public class VerZehri : SuperPower
{
    public override void ActivatePower()
    {
        Debug.Log("VerZehri activated!");
        GameManager.LocalInstance.networkRelay.ActivateVerZehriServerRPC();
    }
}

[CreateAssetMenu(menuName = "SuperPower/KutsalDeste")]
public class KutsalDeste : SuperPower
{
    public override void ActivatePower()
    {
        Debug.Log("KutsalDeste activated!");
        GameManager.LocalInstance.networkRelay.ActivateKutsalDesteServerRPC();
    }
}

[CreateAssetMenu(menuName = "SuperPower/BuDahaİyi")]
public class BuDahaİyi : SuperPower
{
    public override void ActivatePower()
    {
        Debug.Log("BuDahaİyi activated!");
        GameManager.LocalInstance.UseBuDahaIyiPower();
    }
}

[CreateAssetMenu(menuName = "SuperPower/SunuDegisTokus")]
public class SunuDegisTokus : SuperPower
{
    public override void ActivatePower()
    {
        Debug.Log("ŞunuDeğişTokuş activated!");
        GameManager.LocalInstance.ActivateSunuDegisTokusPower();
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
