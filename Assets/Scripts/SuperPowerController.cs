using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public abstract class SuperPower : ScriptableObject
{
    public string name;
    public string description;
    public int rarityMultiplier;
    public abstract void ActivatePower();
    public void PowerActivated()
    {
        // Track superpower activation
        if (DeckController.LocalInstance != null)
        {
            MoveChainIntegrator.TrackSuperpowerActivation(DeckController.LocalInstance.thisPlayerNumber, name);
        }
        
        GameManager.LocalInstance.networkRelay.ShowcaseSuperPowerServerRPC(name);
    }

}

[CreateAssetMenu(menuName = "SuperPower/UcundanGözAt")]
public class UcundanGözAt : SuperPower
{
    private void OnEnable()
    {
        name = "Ucundan Göz At";
        description = "Rakibin rastgele bir kartını gör";
        rarityMultiplier = 10;
    }
    public override void ActivatePower()
    {
        Debug.Log("Ucundan Göz At activated!");
        PowerActivated();
        GameManager.LocalInstance.UsePeekOpponentCardPower();
    }
}

[CreateAssetMenu(menuName = "SuperPower/Oynayamazsın")]
public class Oynayamazsın : SuperPower
{
    private void OnEnable()
    {
        name = "Oynayamazsın";
        description = "Oynanan kartı bir tur için kapatılamaz yap";
        rarityMultiplier = 3;
    }
    public override void ActivatePower()
    {
        Debug.Log("Oynayamazsın activated!");
        PowerActivated();
        GameManager.LocalInstance.ActivateBlockNextPlayerPower();
    }
}

[CreateAssetMenu(menuName = "SuperPower/DeğişTokuş")]
public class DeğişTokuş : SuperPower
{
    private void OnEnable()
    {
        name = "Değiş Tokuş";
        description = "Rakip ile rastgele bir kart değiş tokuş";
        rarityMultiplier = 9;
    }
    public override void ActivatePower()
    {
        Debug.Log("Değiş Tokuş activated!");
        PowerActivated();
        GameManager.LocalInstance.UseSwapCardWithOpponentPower();
    }
}

[CreateAssetMenu(menuName = "SuperPower/Kapkaç")]
public class Kapkaç : SuperPower
{
    private void OnEnable()
    {
        name = "Kapkaç";
        description = "Bir sonraki seçtiğin kartını Sahte Vale'ye dönüştür";
        rarityMultiplier = 5;
    }
    public override void ActivatePower()
    {
        Debug.Log("Kapkaç activated!");
        PowerActivated();
        GameManager.LocalInstance.ActivateKapkacPower();
    }
}

[CreateAssetMenu(menuName = "SuperPower/ValeArar")]
public class ValeArar : SuperPower
{
    private void OnEnable()
    {
        name = "Vale Arar";
        description = "Tur boyunca Valeleri görmeni sağlar";
        rarityMultiplier = 1;
    }
    [ContextMenu("Vale Arar")]
    public override void ActivatePower()
    {
        Debug.Log("ValeArar activated!");
        PowerActivated();
        GameManager.LocalInstance.ActivateValeArarPower();
    }


}

[CreateAssetMenu(menuName = "SuperPower/KopyalaYapistir")]
public class KopyalaYapistir : SuperPower
{
    private void OnEnable()
    {
        name = "Kopyala Yapıştır";
        description = "Çoktan seçili kartı kopyalar, bir sonraki seçilen kartın üzerine kopyalar";
        rarityMultiplier = 4;
    }
    public override void ActivatePower()
    {
        Debug.Log("KopyalaYapıstır activated!");
        PowerActivated();
        GameManager.LocalInstance.ActivateKopyalaYapistirPower();
    }
}

[CreateAssetMenu(menuName = "SuperPower/BayaBayaBak")]
public class BayaBayaBak : SuperPower
{
    private void OnEnable()
    {
        name = "Baya Baya Bak";
        description = "Bir rakibin tüm kartlarını gör";
        rarityMultiplier = 4;
    }
    public override void ActivatePower()
    {
        Debug.Log("BayaBayaBak activated!");
        PowerActivated();
        GameManager.LocalInstance.UseBayaBayaBakPower();
    }
}

[CreateAssetMenu(menuName = "SuperPower/Bomba")]
public class Bomba : SuperPower
{
    private void OnEnable()
    {
        name = "Bomba";
        description = "Ortadaki kartların hepsini patlat";
        rarityMultiplier =3;
    }
    public override void ActivatePower()
    {
        Debug.Log("Bomba activated!");
        PowerActivated();
        if (GameManager.LocalInstance.centerCards.Count != 0) GameManager.LocalInstance.ActivateBombaPower();
    }
}

[CreateAssetMenu(menuName = "SuperPower/Yapamazsın")]
public class Yapamazsın : SuperPower
{
    private void OnEnable()
    {
        name = "Yapamazsın";
        description = "Bir sonraki oynanan süper gücü gizlice engelle";
        rarityMultiplier = 5;
    }
    public override void ActivatePower()
    {
        Debug.Log("Yapamazsın activated!");
        PowerActivated();
        GameManager.LocalInstance.ActivateYapamazsınPower();
    }
}

[CreateAssetMenu(menuName = "SuperPower/VerZehri")]
public class VerZehri : SuperPower
{
    private void OnEnable()
    {
        name = "Ver Zehri";
        description = "Ortayı zehirle. Zehirli desteyi alan taraf ortadaki kart sayısı kadar puan kaybeder";
        rarityMultiplier = 5;
    }
    public override void ActivatePower()
    {
        Debug.Log("VerZehri activated!");
        PowerActivated();
        GameManager.LocalInstance.networkRelay.ActivateVerZehriServerRPC();
    }
}

[CreateAssetMenu(menuName = "SuperPower/KutsalDeste")]
public class KutsalDeste : SuperPower
{
    private void OnEnable()
    {
        name = "Kutsal Deste";
        description = "Ortayı kutsa. Zehirli desteyi alan taraf ortadaki kart sayısı kadar puan kazanır";
        rarityMultiplier = 5;
    }
    public override void ActivatePower()
    {
        Debug.Log("KutsalDeste activated!");
        PowerActivated();
        GameManager.LocalInstance.networkRelay.ActivateKutsalDesteServerRPC();
    }
}

[CreateAssetMenu(menuName = "SuperPower/BuDahaİyi")]
public class BuDahaİyi : SuperPower
{
    private void OnEnable()
    {
        name = "Bu Daha İyi";
        description = "Çoktan seçili kart ile ortadaki kartı değiştir";
        rarityMultiplier = 5;
    }
    public override void ActivatePower()
    {
        Debug.Log("BuDahaİyi activated!");
        PowerActivated();
        GameManager.LocalInstance.UseBuDahaIyiPower();
    }
}

[CreateAssetMenu(menuName = "SuperPower/SunuDegisTokus")]
public class SunuDegisTokus : SuperPower
{
    private void OnEnable()
    {
        name = "Şunu Değiş Tokuş";
        description = "Çoktan seçilmiş kartınla rakibin istediğin kartını değiş tokuş";
        rarityMultiplier = 4;
    }
    public override void ActivatePower()
    {
        Debug.Log("ŞunuDeğişTokuş activated!");
        PowerActivated();
        GameManager.LocalInstance.ActivateSunuDegisTokusPower();
    }
}

[CreateAssetMenu(menuName = "SuperPower/SunuDegisBunuTokus")]
public class SunuDegisBunuTokus : SuperPower
{
    private void OnEnable()
    {
        name = "Şunu Değiş Bunu Tokuş";
        description = "Tüm karlarını sırayla değiş tokuş";
        rarityMultiplier = 2;
    }
    public override void ActivatePower()
    {
        Debug.Log("ŞunuDeğişBunuTokuş activated!");
        PowerActivated();
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
        description = "Tur sonuna kadar elinde Zafer Puanı tutarsan 5 puan kazan";
        rarityMultiplier = 3;
    }

    public override void ActivatePower()
    {
        // No active effect
        Debug.Log("Zafer Puanı has no active effect.");
        PowerActivated();
    }
}

[CreateAssetMenu(menuName = "SuperPower/YandımAnam")]
public class YandımAnam : SuperPower
{
    private void OnEnable()
    {
        name = "Yandım Anam";
        description = "Seçilen bir kartı yak.";
        rarityMultiplier = 5;
    }
    public override void ActivatePower()
    {
        Debug.Log("Yandım Anam activated!");
        PowerActivated();
        GameManager.LocalInstance.ActivateYandimAnamPower();
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
