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
    private void OnEnable()
    {
        name = "Ucundan Göz At";
        description = "Peek at an opponent's card.";
        rarityMultiplier = 1;
    }
    public override void ActivatePower()
    {
        Debug.Log("Ucundan Göz At activated!");
        GameManager.LocalInstance.UsePeekOpponentCardPower();
    }
}

[CreateAssetMenu(menuName = "SuperPower/Oynayamazsın")]
public class Oynayamazsın : SuperPower
{
    private void OnEnable()
    {
        name = "Oynayamazsın";
        description = "Peek at an opponent's card.";
        rarityMultiplier = 1;
    }
    public override void ActivatePower()
    {
        Debug.Log("Oynayamazsın activated!");
        GameManager.LocalInstance.ActivateBlockNextPlayerPower();
    }
}

[CreateAssetMenu(menuName = "SuperPower/DeğişTokuş")]
public class DeğişTokuş : SuperPower
{
    private void OnEnable()
    {
        name = "Değiş Tokuş";
        description = "Peek at an opponent's card.";
        rarityMultiplier = 1;
    }
    public override void ActivatePower()
    {
        Debug.Log("Değiş Tokuş activated!");
        GameManager.LocalInstance.UseSwapCardWithOpponentPower();
    }
}

[CreateAssetMenu(menuName = "SuperPower/Kapkaç")]
public class Kapkaç : SuperPower
{
    private void OnEnable()
    {
        name = "Kapkaç";
        description = "Peek at an opponent's card.";
        rarityMultiplier = 1;
    }
    public override void ActivatePower()
    {
        Debug.Log("Kapkaç activated!");
        GameManager.LocalInstance.ActivateKapkacPower();
    }
}

[CreateAssetMenu(menuName = "SuperPower/ValeArar")]
public class ValeArar : SuperPower
{
    private void OnEnable()
    {
        name = "Vale Arar";
        description = "Peek at an opponent's card.";
        rarityMultiplier = 100;
    }
    [ContextMenu("Vale Arar")]
    public override void ActivatePower()
    {
        Debug.Log("ValeArar activated!");
        GameManager.LocalInstance.ActivateValeArarPower();
    }


}

[CreateAssetMenu(menuName = "SuperPower/KopyalaYapistir")]
public class KopyalaYapistir : SuperPower
{
    private void OnEnable()
    {
        name = "Kopyala Yapıştır";
        description = "Peek at an opponent's card.";
        rarityMultiplier = 1;
    }
    public override void ActivatePower()
    {
        Debug.Log("KopyalaYapıstır activated!");
        GameManager.LocalInstance.ActivateKopyalaYapistirPower();
    }
}

[CreateAssetMenu(menuName = "SuperPower/BayaBayaBak")]
public class BayaBayaBak : SuperPower
{
    private void OnEnable()
    {
        name = "Baya Baya Bak";
        description = "Peek at an opponent's card.";
        rarityMultiplier = 1;
    }
    public override void ActivatePower()
    {
        Debug.Log("BayaBayaBak activated!");
        GameManager.LocalInstance.UseBayaBayaBakPower();
    }
}

[CreateAssetMenu(menuName = "SuperPower/Bomba")]
public class Bomba : SuperPower
{
    private void OnEnable()
    {
        name = "Bomba";
        description = "Peek at an opponent's card.";
        rarityMultiplier = 1;
    }
    public override void ActivatePower()
    {
        Debug.Log("Bomba activated!");
        GameManager.LocalInstance.ActivateBombaPower();
    }
}

[CreateAssetMenu(menuName = "SuperPower/Yapamazsın")]
public class Yapamazsın : SuperPower
{
    private void OnEnable()
    {
        name = "Yapamazsın";
        description = "Peek at an opponent's card.";
        rarityMultiplier = 1;
    }
    public override void ActivatePower()
    {
        Debug.Log("Yapamazsın activated!");
        GameManager.LocalInstance.ActivateYapamazsınPower();
    }
}

[CreateAssetMenu(menuName = "SuperPower/VerZehri")]
public class VerZehri : SuperPower
{
    private void OnEnable()
    {
        name = "Ver Zehri";
        description = "Peek at an opponent's card.";
        rarityMultiplier = 1;
    }
    public override void ActivatePower()
    {
        Debug.Log("VerZehri activated!");
        GameManager.LocalInstance.networkRelay.ActivateVerZehriServerRPC();
    }
}

[CreateAssetMenu(menuName = "SuperPower/KutsalDeste")]
public class KutsalDeste : SuperPower
{
    private void OnEnable()
    {
        name = "Kutsal Deste";
        description = "Peek at an opponent's card.";
        rarityMultiplier = 1;
    }
    public override void ActivatePower()
    {
        Debug.Log("KutsalDeste activated!");
        GameManager.LocalInstance.networkRelay.ActivateKutsalDesteServerRPC();
    }
}

[CreateAssetMenu(menuName = "SuperPower/BuDahaİyi")]
public class BuDahaİyi : SuperPower
{
    private void OnEnable()
    {
        name = "Bu Daha İyi";
        description = "Peek at an opponent's card.";
        rarityMultiplier = 1;
    }
    public override void ActivatePower()
    {
        Debug.Log("BuDahaİyi activated!");
        GameManager.LocalInstance.UseBuDahaIyiPower();
    }
}

[CreateAssetMenu(menuName = "SuperPower/SunuDegisTokus")]
public class SunuDegisTokus : SuperPower
{
    private void OnEnable()
    {
        name = "Şunu Değiş Tokuş";
        description = "Peek at an opponent's card.";
        rarityMultiplier = 1;
    }
    public override void ActivatePower()
    {
        Debug.Log("ŞunuDeğişTokuş activated!");
        GameManager.LocalInstance.ActivateSunuDegisTokusPower();
    }
}

[CreateAssetMenu(menuName = "SuperPower/SunuDegisBunuTokus")]
public class SunuDegisBunuTokus : SuperPower
{
    private void OnEnable()
    {
        name = "Şunu Değiş Bunu Tokuş";
        description = "Peek at an opponent's card.";
        rarityMultiplier = 1;
    }
    public override void ActivatePower()
    {
        Debug.Log("ŞunuDeğişBunuTokuş activated!");
        GameManager.LocalInstance.ActivateSunuDegisBunuTokusPower();
    }
}

[CreateAssetMenu(menuName = "SuperPower/ZaferPuani")]
public class ZaferPuani : SuperPower
{
    [SerializeField] public int points = 5; // Adjustable in Inspector

    private void OnEnable()
    {
        name = "Zafer Puanı";
        description = $"Round bonus: If you hold this at the end of the round, your team gets {points} points!";
        rarityMultiplier = 100;
    }

    public override void ActivatePower()
    {
        // No active effect
        Debug.Log("Zafer Puanı has no active effect.");
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
