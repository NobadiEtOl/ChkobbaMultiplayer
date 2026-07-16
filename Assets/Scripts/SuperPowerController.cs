using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public abstract class SuperPower : ScriptableObject
{
    public string name;
    public string description;
    // Cost tier used for mode-based draw weighting. 1/2/3 = selectable tiers, 4 = special (never boosted).
    public int powerCostTier;
    // When false, this power is excluded from the draw pool entirely.
    public bool isPowerEnabled = true;
    public abstract void ActivatePower();
    public void PowerActivated()
    {
        // Track superpower activation
        if (DeckController.LocalInstance != null)
        {
            MoveChainIntegrator.TrackSuperpowerActivation(DeckController.LocalInstance.thisPlayerNumber, name);
            
            // Track the power activation in debug chain
            DebugChainPrinter.LocalInstance?.TrackLocalAction($"PowerActivated() called for {name} by Player {DeckController.LocalInstance.thisPlayerNumber}");
            DebugChainPrinter.LocalInstance?.TrackMoveChain($"Superpower activation tracked: {name} by P{DeckController.LocalInstance.thisPlayerNumber}");
        }
        
        // Showcase is triggered by the server after it accepts the power RPC,
        // so all clients see it at the same time regardless of who activated it.
    }

}

[CreateAssetMenu(menuName = "SuperPower/UcundanGözAt")]
public class UcundanGözAt : SuperPower
{
    private void OnEnable()
    {
        name = "Ucundan Göz At";
        description = "Rakibin rastgele bir kartını gör";
        powerCostTier = 1;
        isPowerEnabled = true;
    }
    public override void ActivatePower()
    {
        // Debug.Log("Ucundan Göz At activated!");
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
        powerCostTier = 1;
        isPowerEnabled = false;
    }
    public override void ActivatePower()
    {
        Debug.Log($"[Oynayamazsın] POWER ACTIVATION START - Player: {DeckController.LocalInstance?.thisPlayerNumber}, Time: {Time.time}");
        Debug.Log($"[Oynayamazsın] Calling PowerActivated() immediately - This will trigger ShowcaseSuperPowerServerRPC");
        PowerActivated();
        Debug.Log($"[Oynayamazsın] Setting up pending block effect - will activate when next card is played");
        GameManager.LocalInstance.ActivateBlockNextPlayerPower();
        Debug.Log($"[Oynayamazsın] POWER ACTIVATION COMPLETE - Block effect is now pending until next card play");
    }
}

[CreateAssetMenu(menuName = "SuperPower/DeğişTokuş")]
public class DeğişTokuş : SuperPower
{
    private void OnEnable()
    {
        name = "Değiş Tokuş";
        description = "Bir kartını rakibin elindeki rastgele bir kartla değiştir";
        powerCostTier = 1;
        isPowerEnabled = true;
    }
    public override void ActivatePower()
    {
        Debug.Log($"[DeğişTokuş] POWER ACTIVATION START - Player: {DeckController.LocalInstance?.thisPlayerNumber}, Time: {Time.time}");
        PowerActivated();
        GameManager.LocalInstance.UseSwapCardWithOpponentPower();
        Debug.Log("[DeğişTokuş] Random swap initiated.");
    }
}

// TODO: PowerDurationTimer — Kapkaç requires a pre-selected hand card before activation.
// Consider adding a card-selection phase timer: when the player activates the token without a card selected,
// enter a "waiting for selection" state, pause the turn timer, and start a power duration timer.
// Cancel the pending selection if time runs out.
[CreateAssetMenu(menuName = "SuperPower/Kapkaç")]
public class Kapkaç : SuperPower
{
    private void OnEnable()
    {
        name = "Kapkaç";
        description = "Değiştirmek için bir kart seç. Seçilen kart Sahte Vale'ye dönüşür";
        powerCostTier = 2;
        isPowerEnabled = true;
    }
    public override void ActivatePower()
    {
        Debug.Log($"[Kapkaç] POWER ACTIVATION START - Player: {DeckController.LocalInstance?.thisPlayerNumber}, Time: {Time.time}");

        Debug.Log($"[Kapkaç] Calling PowerActivated() - This will trigger ShowcaseSuperPowerServerRPC");
        PowerActivated();

        // Enter pending single-card selection mode (activate first, then select a card).
        GameManager.LocalInstance.StartKapkacSelectionPower();

        Debug.Log($"[Kapkaç] POWER ACTIVATION COMPLETE - Waiting for card selection");
    }
}

[CreateAssetMenu(menuName = "SuperPower/ValeArar")]
public class ValeArar : SuperPower
{
    private void OnEnable()
    {
        name = "Vale Arar";
        description = "Tur boyunca Valeleri görmeni sağlar";
        powerCostTier = 3;
        isPowerEnabled = true;
    }
    [ContextMenu("Vale Arar")]
    public override void ActivatePower()
    {
        // Debug.Log("ValeArar activated!");
        PowerActivated();
        GameManager.LocalInstance.ActivateValeArarPower();
    }


}

// TODO: PowerDurationTimer — KopyalaYapıştır uses dual card selection (source + target).
// Add PauseTurnTimerForPowerServerRPC + StartPowerDurationTimerServerRPC inside StartKopyalaYapistirDualSelection()
// and handle CancelDualSelectionPower() equivalent to reset state if the timer expires.
[CreateAssetMenu(menuName = "SuperPower/KopyalaYapistir")]
public class KopyalaYapistir : SuperPower
{
    private void OnEnable()
    {
        name = "Kopyala Yapıştır";
        description = "Bir kartı kopyala, başka bir kartın üzerine yapıştır";
        powerCostTier = 3;
        isPowerEnabled = true;
    }
    public override void ActivatePower()
    {
        Debug.Log($"[KopyalaYapıştır] POWER ACTIVATION START - Player: {DeckController.LocalInstance?.thisPlayerNumber}, Time: {Time.time}");
        // Phase 1: enter source-selection mode (no pre-selected card required)
        GameManager.LocalInstance.StartKopyalaYapistirDualSelection(null);
    }
}

[CreateAssetMenu(menuName = "SuperPower/BayaBayaBak")]
public class BayaBayaBak : SuperPower
{
    private void OnEnable()
    {
        name = "Baya Baya Bak";
        description = "Bir rakibin tüm kartlarını gör";
        powerCostTier = 2;
        isPowerEnabled = true;
    }
    public override void ActivatePower()
    {
        Debug.Log($"[BayaBayaBak] POWER ACTIVATION START - Player: {DeckController.LocalInstance?.thisPlayerNumber}, Time: {Time.time}");
        
        // Track the power activation
        DebugChainPrinter.LocalInstance?.TrackLocalAction($"BayaBayaBak power activation started by Player {DeckController.LocalInstance?.thisPlayerNumber}");
        DebugChainPrinter.LocalInstance?.TrackPowerUsage("BayaBayaBak", DeckController.LocalInstance?.thisPlayerNumber ?? 0, "Power activation initiated");
        
        Debug.Log($"[BayaBayaBak] Calling PowerActivated() - This will trigger ShowcaseSuperPowerServerRPC and track activation");
        PowerActivated();
        
        Debug.Log($"[BayaBayaBak] Calling UseBayaBayaBakPower() - This will call UseBayaBayaBakServerRPC");
        GameManager.LocalInstance.UseBayaBayaBakPower();
        
        Debug.Log($"[BayaBayaBak] POWER ACTIVATION COMPLETE");
        DebugChainPrinter.LocalInstance?.TrackLocalAction("BayaBayaBak power activation completed");
    }
}

[CreateAssetMenu(menuName = "SuperPower/Bomba")]
public class Bomba : SuperPower
{
    private void OnEnable()
    {
        name = "Bomba";
        description = "Ortadaki kartların hepsini patlat";
        powerCostTier = 1;
        isPowerEnabled = true;
    }
    public override void ActivatePower()
    {
        // Debug.Log("Bomba activated!");
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
        powerCostTier = 1;
        isPowerEnabled = false;
    }
    public override void ActivatePower()
    {
        // Debug.Log("Yapamazsın activated!");
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
        description = "Ortayı zehirle. Zehirli desteyi kapatan taraf ortadaki kart sayısı kadar puan kaybeder";
        powerCostTier = 2;
        isPowerEnabled = true;
    }
    public override void ActivatePower()
    {
        Debug.Log($"[VerZehri] POWER ACTIVATION START - Player: {DeckController.LocalInstance?.thisPlayerNumber}, Time: {Time.time}");
        Debug.Log($"[VerZehri] Center cards count: {GameManager.LocalInstance.centerCards.Count}");
        Debug.Log($"[VerZehri] Calling PowerActivated() immediately - This will trigger ShowcaseSuperPowerServerRPC");
        PowerActivated();
        Debug.Log($"[VerZehri] Setting up pending poison effect - will activate when next card is played");
        GameManager.LocalInstance.networkRelay.ActivateVerZehriServerRPC();
        Debug.Log($"[VerZehri] POWER ACTIVATION COMPLETE - Poison effect is now pending until next card play");
    }
}

[CreateAssetMenu(menuName = "SuperPower/KutsalDeste")]
public class KutsalDeste : SuperPower
{
    private void OnEnable()
    {
        name = "Kutsal Deste";
        description = "Ortayı kutsa. Zehirli desteyi kapatan taraf ortadaki kart sayısı kadar puan kazanır";
        powerCostTier = 3;
        isPowerEnabled = true;
    }
    public override void ActivatePower()
    {
        Debug.Log($"[KutsalDeste] POWER ACTIVATION START - Player: {DeckController.LocalInstance?.thisPlayerNumber}, Time: {Time.time}");
        Debug.Log($"[KutsalDeste] Center cards count: {GameManager.LocalInstance.centerCards.Count}");
        Debug.Log($"[KutsalDeste] Calling PowerActivated() immediately - This will trigger ShowcaseSuperPowerServerRPC");
        PowerActivated();
        Debug.Log($"[KutsalDeste] Setting up pending holy effect - will activate when next card is played");
        GameManager.LocalInstance.networkRelay.ActivateKutsalDesteServerRPC();
        Debug.Log($"[KutsalDeste] POWER ACTIVATION COMPLETE - Holy effect is now pending until next card play");
    }
}

[CreateAssetMenu(menuName = "SuperPower/BuDahaİyi")]
public class BuDahaİyi : SuperPower
{
    private void OnEnable()
    {
        name = "Bu Daha İyi";
        description = "Bir kart seç ve ortadaki kartla değiştir";
        powerCostTier = 2;
        isPowerEnabled = true;
    }
    public override void ActivatePower()
    {
        Debug.Log($"[BuDahaİyi] POWER ACTIVATION START - Player: {DeckController.LocalInstance?.thisPlayerNumber}, Time: {Time.time}");
        Debug.Log($"[BuDahaİyi] Center cards count: {GameManager.LocalInstance.centerCards.Count}");

        Debug.Log($"[BuDahaİyi] Calling PowerActivated() - This will trigger ShowcaseSuperPowerServerRPC");
        PowerActivated();

        // Enter pending single-card selection mode (activate first, then pick hand card).
        GameManager.LocalInstance.StartBuDahaIyiSelectionPower();

        Debug.Log($"[BuDahaİyi] POWER ACTIVATION COMPLETE - Waiting for hand card selection");
    }
}

[CreateAssetMenu(menuName = "SuperPower/SunuDegisTokus")]
public class SunuDegisTokus : SuperPower
{
    private void OnEnable()
    {
        name = "Şunu Değiş Tokuş";
        description = "Adım 1: herhangi bir el kartı seç. Adım 2: farklı bir el kartı seç. Seçtiğin iki kart yer değiştirir";
        powerCostTier = 2;
        isPowerEnabled = true;
    }
    public override void ActivatePower()
    {
        Debug.Log($"[ŞunuDeğişTokuş] POWER ACTIVATION START - Player: {DeckController.LocalInstance?.thisPlayerNumber}, Time: {Time.time}");
        GameManager.LocalInstance.StartSunuDegisTokusDualSelection(null, "Şunu Değiş Tokuş");
        Debug.Log("[ŞunuDeğişTokuş] Selection mode started. Select any two different hand cards.");
    }
}

// TODO: PowerDurationTimer — ŞunuDeğişBunuTokuş is a multi-step sequential swap (N cards).
// When the swap loop starts in ActivateSunuDegisBunuTokusPower(), pause the turn timer and start a cumulative power duration timer.
// On each individual swap complete, optionally reset the per-swap timer. Cancel all remaining swaps if the timer expires.
[CreateAssetMenu(menuName = "SuperPower/SunuDegisBunuTokus")]
public class SunuDegisBunuTokus : SuperPower
{
    private void OnEnable()
    {
        name = "Şunu Değiş Bunu Tokuş";
        description = "Tüm karlarını sırayla değiş tokuş";
        powerCostTier = 3;
        isPowerEnabled = true;
    }
    public override void ActivatePower()
    {
        Debug.Log($"[ŞunuDeğişBunuTokuş] POWER ACTIVATION START - Player: {DeckController.LocalInstance?.thisPlayerNumber}, Time: {Time.time}");
        
        // Check if player has cards in hand
        if (GameManager.LocalInstance.myCards == null || GameManager.LocalInstance.myCards.Count == 0)
        {
            Debug.LogWarning("[ŞunuDeğişBunuTokuş] No cards in hand! Cannot activate this power.");
            return;
        }
        
        Debug.Log($"[ŞunuDeğişBunuTokuş] Starting multi-swap mode - {GameManager.LocalInstance.myCards.Count} cards to swap");
        // NOTE: PowerActivated() will be called AFTER all swaps are complete
        GameManager.LocalInstance.ActivateSunuDegisBunuTokusPower();
        Debug.Log($"[ŞunuDeğişBunuTokuş] Multi-swap mode started - PowerActivated() will be called when all swaps are done");
    }
}

[CreateAssetMenu(menuName = "SuperPower/ZaferPuani")]
public class ZaferPuani : SuperPower
{
    [SerializeField] public int points = 5; // Adjustable in Inspector

    private void OnEnable()
    {
        name = "Zafer Puanı";
        description = "Kullanınca 5 puan kazan";
        powerCostTier = 4;
        isPowerEnabled = true;
    }

    public override void ActivatePower()
    {
        Debug.Log($"[ZaferPuani] POWER ACTIVATION START - Player: {DeckController.LocalInstance?.thisPlayerNumber}, Points: {points}, Time: {Time.time}");
        
        // Call PowerActivated() first for visual effects
        PowerActivated();
        
        // DIRECT POINT ADDITION: Add points immediately to the player/team
        int playerNumber = DeckController.LocalInstance.thisPlayerNumber;
        GameManager.LocalInstance.PowerProcessor?.ExecuteZaferPuani(points);
        
        Debug.Log($"[ZaferPuani] POWER ACTIVATION COMPLETE - Added {points} points immediately to player {playerNumber}");
    }
}

// TODO: PowerDurationTimer — Yandım Anam requires a pre-selected card before activation.
// Consider adding a card-selection phase timer: when the player activates the token without a card selected,
// enter a "waiting for selection" state, pause the turn timer, and start a power duration timer.
// Cancel the pending selection if time runs out.
[CreateAssetMenu(menuName = "SuperPower/YandımAnam")]
public class YandımAnam : SuperPower
{
    private void OnEnable()
    {
        name = "Yandım Anam";
        description = "Değiştirmek için bir kart seç. Seçilen kart yanar ve sadece başka yanık kartlarca kapatılabilir.";
        powerCostTier = 1;
        isPowerEnabled = true;
    }
    public override void ActivatePower()
    {
        Debug.Log($"[YandımAnam] POWER ACTIVATION START - Player: {DeckController.LocalInstance?.thisPlayerNumber}, Time: {Time.time}");

        Debug.Log($"[YandımAnam] Calling PowerActivated() - This will trigger ShowcaseSuperPowerServerRPC");
        PowerActivated();

        // Enter pending single-card selection mode (activate first, then select a card).
        GameManager.LocalInstance.StartYandimAnamSelectionPower();

        Debug.Log($"[YandımAnam] POWER ACTIVATION COMPLETE - Waiting for card selection");
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
