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
            
            // Track the power activation in debug chain
            DebugChainPrinter.LocalInstance?.TrackLocalAction($"PowerActivated() called for {name} by Player {DeckController.LocalInstance.thisPlayerNumber}");
            DebugChainPrinter.LocalInstance?.TrackMoveChain($"Superpower activation tracked: {name} by P{DeckController.LocalInstance.thisPlayerNumber}");
        }
        
        GameManager.LocalInstance.networkRelay.ShowcaseSuperPowerServerRPC(name);
        DebugChainPrinter.LocalInstance?.TrackNetworkRPC("ShowcaseSuperPowerServerRPC", $"powerName={name}");
    }

}

[CreateAssetMenu(menuName = "SuperPower/UcundanGözAt")]
public class UcundanGözAt : SuperPower
{
    private void OnEnable()
    {
        name = "Ucundan Göz At";
        description = "Rakibin rastgele bir kartını gör";
        rarityMultiplier = 5;
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
        rarityMultiplier = 12;
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
        rarityMultiplier = 7;
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
        description = "Çoktan seçili kartı Sahte Vale'ye dönüştür";
        rarityMultiplier = 15;
    }
    public override void ActivatePower()
    {
        Debug.Log($"[Kapkaç] POWER ACTIVATION START - Player: {DeckController.LocalInstance?.thisPlayerNumber}, Time: {Time.time}");
        Debug.Log($"[Kapkaç] Currently selected card: {(CardInteraction.currentlySelectedCard != null ? CardInteraction.currentlySelectedCard.uniqueCardInstanceID : "NULL")}");
        
        // Check if a card is pre-selected
        if (CardInteraction.currentlySelectedCard == null)
        {
            Debug.LogWarning("[Kapkaç] No card selected! Please select a card before activating this power.");
            return;
        }
        
        Debug.Log($"[Kapkaç] Calling PowerActivated() - This will trigger ShowcaseSuperPowerServerRPC");
        PowerActivated();
        
        Debug.Log($"[Kapkaç] Calling ActivateKapkacOnCardServerRPC directly with selected card");
        int playerNumber = DeckController.LocalInstance.thisPlayerNumber;
        GameManager.LocalInstance.networkRelay.ActivateKapkacOnCardServerRPC(CardInteraction.currentlySelectedCard.uniqueCardInstanceID, playerNumber);
        
        // Reset selection state
        CardInteraction.currentlySelectedCard = null;
        GameManager.LocalInstance.SetCurrentSelectedHandCardNull();
        
        Debug.Log($"[Kapkaç] POWER ACTIVATION COMPLETE - Card transformed immediately");
    }
}

[CreateAssetMenu(menuName = "SuperPower/ValeArar")]
public class ValeArar : SuperPower
{
    private void OnEnable()
    {
        name = "Vale Arar";
        description = "Tur boyunca Valeleri görmeni sağlar";
        rarityMultiplier = 100;
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
        description = "Çoktan seçili kartı kopyalar, bir sonraki seçilen kartın üzerine yapıştırır";
        rarityMultiplier = 20;
    }
    public override void ActivatePower()
    {
        Debug.Log($"[KopyalaYapıştır] POWER ACTIVATION START - Player: {DeckController.LocalInstance?.thisPlayerNumber}, Time: {Time.time}");
        Debug.Log($"[KopyalaYapıştır] Currently selected card: {(CardInteraction.currentlySelectedCard != null ? CardInteraction.currentlySelectedCard.uniqueCardInstanceID : "NULL")}");
        
        // Check if source card is pre-selected
        if (CardInteraction.currentlySelectedCard == null)
        {
            Debug.LogWarning("[KopyalaYapıştır] No source card selected! Please select a source card first, then activate this power to select target card.");
            return;
        }
        
        // Start dual selection mode - store source card and wait for target selection
        Debug.Log($"[KopyalaYapıştır] Starting dual selection mode - source card: {CardInteraction.currentlySelectedCard.uniqueCardInstanceID}");
        GameManager.LocalInstance.StartKopyalaYapistirDualSelection(CardInteraction.currentlySelectedCard);
    }
}

[CreateAssetMenu(menuName = "SuperPower/BayaBayaBak")]
public class BayaBayaBak : SuperPower
{
    private void OnEnable()
    {
        name = "Baya Baya Bak";
        description = "Bir rakibin tüm kartlarını gör";
        rarityMultiplier = 17;
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
        rarityMultiplier =18;
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
        rarityMultiplier = 22;
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
        rarityMultiplier = 25;
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
        rarityMultiplier = 25;
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
        description = "Çoktan seçili kart ile ortadaki kartı değiştir";
        rarityMultiplier = 20;
    }
    public override void ActivatePower()
    {
        Debug.Log($"[BuDahaİyi] POWER ACTIVATION START - Player: {DeckController.LocalInstance?.thisPlayerNumber}, Time: {Time.time}");
        Debug.Log($"[BuDahaİyi] Currently selected card: {(GameManager.LocalInstance.GetCurrentSelectedHandCard() != null ? GameManager.LocalInstance.GetCurrentSelectedHandCard() : "NULL")}");
        Debug.Log($"[BuDahaİyi] Center cards count: {GameManager.LocalInstance.centerCards.Count}");
        
        // Check if a card is pre-selected and center has cards
        if (GameManager.LocalInstance.GetCurrentSelectedHandCard() == null)
        {
            Debug.LogWarning("[BuDahaİyi] No hand card selected! Please select a card from your hand before activating this power.");
            return;
        }
        
        if (GameManager.LocalInstance.centerCards.Count == 0)
        {
            Debug.LogWarning("[BuDahaİyi] No center cards available to swap with!");
            return;
        }
        
        Debug.Log($"[BuDahaİyi] Calling PowerActivated() - This will trigger ShowcaseSuperPowerServerRPC");
        PowerActivated();
        
        Debug.Log($"[BuDahaİyi] Calling UseBuDahaIyiPower() - This will trigger UseBuDahaIyiServerRPC");
        GameManager.LocalInstance.UseBuDahaIyiPower();
        
        Debug.Log($"[BuDahaİyi] POWER ACTIVATION COMPLETE - Server will handle the card swap");
    }
}

[CreateAssetMenu(menuName = "SuperPower/SunuDegisTokus")]
public class SunuDegisTokus : SuperPower
{
    private void OnEnable()
    {
        name = "Şunu Değiş Tokuş";
        description = "Önce kendi elinden bir kart seç, sonra rakibin elinden bir kart seç. Seçtiğin kartları birbirleriyle değiş tokuş yap";
        rarityMultiplier = 14;
    }
    public override void ActivatePower()
    {
        Debug.Log($"[ŞunuDeğişTokuş] POWER ACTIVATION START - Player: {DeckController.LocalInstance?.thisPlayerNumber}, Time: {Time.time}");
        GameManager.LocalInstance.StartSunuDegisTokusDualSelection(null, "Şunu Değiş Tokuş");
        Debug.Log("[ŞunuDeğişTokuş] Selection mode started. Select own card first, then opponent card.");
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
        rarityMultiplier = 22;
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
        rarityMultiplier = 67;
    }

    public override void ActivatePower()
    {
        Debug.Log($"[ZaferPuani] POWER ACTIVATION START - Player: {DeckController.LocalInstance?.thisPlayerNumber}, Points: {points}, Time: {Time.time}");
        
        // Call PowerActivated() first for visual effects
        PowerActivated();
        
        // DIRECT POINT ADDITION: Add points immediately to the player/team
        int playerNumber = DeckController.LocalInstance.thisPlayerNumber;
        GameManager.LocalInstance.networkRelay.ActivateZaferPuaniServerRPC(playerNumber, points);
        
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
        description = "Seçilen bir kartı yak. Yanık karları sadece başka yanık kartlar kapatabilir.";
        rarityMultiplier = 19;
    }
    public override void ActivatePower()
    {
        Debug.Log($"[YandımAnam] POWER ACTIVATION START - Player: {DeckController.LocalInstance?.thisPlayerNumber}, Time: {Time.time}");
        Debug.Log($"[YandımAnam] Currently selected card: {(CardInteraction.currentlySelectedCard != null ? CardInteraction.currentlySelectedCard.uniqueCardInstanceID : "NULL")}");
        
        // Check if a card is pre-selected
        if (CardInteraction.currentlySelectedCard == null)
        {
            Debug.LogWarning("[YandımAnam] No card selected! Please select a card before activating this power.");
            return;
        }
        
        Debug.Log($"[YandımAnam] Calling PowerActivated() - This will trigger ShowcaseSuperPowerServerRPC");
        PowerActivated();
        
        Debug.Log($"[YandımAnam] Calling ActivateYandimAnamOnCardServerRPC directly with selected card");
        GameManager.LocalInstance.networkRelay.ActivateYandimAnamOnCardServerRPC(CardInteraction.currentlySelectedCard.uniqueCardInstanceID);
        
        // Reset selection state
        CardInteraction.currentlySelectedCard = null;
        GameManager.LocalInstance.SetCurrentSelectedHandCardNull();
        
        Debug.Log($"[YandımAnam] POWER ACTIVATION COMPLETE - Card burned immediately");
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
