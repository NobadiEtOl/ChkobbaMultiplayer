###General Rules
    # Always write a summary comment on top of every uncommented function or method.
    # Use clear and descriptive variable names.
    # Prefer early returns for error handling.
    # Consider TODO and Documentation for each new functions and method so that future features are easier to implement
    # When adding new features, tell me how to update this README if needed.
    # Always tell me if my promt confilicts with the README.txt so that I can either change my promt or README
    # You should make recommendations if you think there is a better approach according to README

###TO DO
    ##Problems
        #Bu daha iyi kullandıktan sonra ele gelen kartın scale i doğru değil
        #Showcase açıkken bi anda rakibin tüm kartları face up oldu
        (Done)#Kopyala yapıştır kartı koypalayıp yapıştırıyor ama üstündeki efektleri(kapkaç) etkilemiyor
    ##Audio
        #Sound Effects
            //Captures
            //Card movement
            //Super powers
                /Unique for each power
        #BackGround musics
            //Main music
            //Game music
        #Screen Effects
            //Win
            //Lose
        #Sound Adjustments
    ##Animations
        #Card Animations
            //More interesting movement animations
            //Bounce animation for stops
        #Better Power Animations
            //May be unique animations
        #Better Card Placements
            //Make them look more like Uno
        #FeedBack animations
            //Animations for wrong moves
            //Animations for valid moves
    ##UI
        #Better Screens
        #Return Back Buttons
        #Tutorial Screen
        #Player info display
    ##GamePlay
        #Add Bots
        #Turn timers and skips
        #Restrict the cards you can select(anything goes rn for debugging reasons)
    ##Networking & Multiplayer
        #Host migration
        #Reconnect 
        #Player info
            //Name
            //Profile Pic
            //Gold
    ##Super Power System
        #More powers
            //Powers about powers
        #Power balancing
            //Rarity
            //Points on disposal
        #Power sprites
        #Power restrictions checks
    ##Code Quality & Maintenance
        #More comments
        #Refactoring and Decoupling
        #Unit tests
        #System tests
        #Remove unused code and assets

####Documentation
--------------------------------------------------------------------------------------
### Project Structure Overview

- **GameManager.cs**  
  Central script for game state, player turns, card events, and super power activation. Handles UI updates, round resets, and communicates with NetworkRelay for multiplayer events.

- **DeckController.cs**  
  Manages card prefab instantiation, card dealing, card movement/animations, and hand/pool/center layouts. Handles both 2-player and 4-player logic. Generally provides methods used for card objects' movements, rotation and scaling.

- **CardInteraction.cs**  
  Attached to each card GameObject. Handles selection, drag/drop, indicator visuals, and unique card IDs. Supports card peeking, swapping, and power interactions. Maintains a static lookup for all cards.

- **Server.cs**  
  Authoritative game logic for multiplayer. Tracks all card locations, player hands/pools, and round logic. Handles scoring, power effects, and win condition checks. Communicates with clients via NetworkRelay.

- **NetworkRelay.cs**  
  Handles all ClientRPC and ServerRPC calls for multiplayer synchronization. Forwards moves, card state changes, super power activations, and UI events between server and clients.

- **SuperPowerController.cs & SuperPowerToken.cs**  
  ScriptableObject-based system for super powers. Each power is a class with its own activation logic. Tokens are spawned and can be clicked to activate powers. Power activation is networked and triggers UI feedback.

- **SuperPowerSpawner.cs**  
  Manages spawning, displaying, and removing super power tokens. Handles UI for power info and activation buttons.

---

### Game Flow

1. **Game Start**
   - GameManager initializes all references and UI.
   - DeckController creates and shuffles cards, deals to players and center.
   - Server tracks all card IDs and player hands.

2. **Player Turn**
   - Player selects a card (CardInteraction).
   - GameManager checks move legality and sends move to Server via NetworkRelay.
   - Server updates game state and notifies all clients of changes.
   - The player's turn always ends after a card is played

3. **Card Movement**
   - DeckController animates card movement to center, pools, or hands.
   - CardInteraction handles selection indicators and drag/drop.

4. **Super Powers**
   - SuperPowerTokens are spawned by SuperPowerSpawner.
   - Clicking a token shows info and allows activation if requirements are met.
   - Power activation is networked and may affect cards, players, or UI.
   - GameManager and Server apply power effects and update state.
   - Any power can be played in the player's own turn (if not said otherwise)

5. **Round End**
   - Server calculates points and determines winner.
   - GameManager shows win screen and updates points.

---

### Networking

- Uses Unity Netcode, Relay, and Lobby services.
- All game state changes are sent to Server, which validates and broadcasts to all clients.
- NetworkRelay.cs contains all RPC methods for syncing state and UI.
- Game uses P2P with Host-Client relations so that it is free to build with unity relay.
- But this P2P structure does not allow for host migration easily.


---

### Card & Power System

- Each card has a unique ID and is tracked in CardInteraction.cardLookup and Server.allCardLookup.
- Powers can modify card values, swap cards, block moves, or trigger animations.
- Some powers require card selection or specific board state (checked in SuperPowerSpawner).
- Changed card do not reset each round but they do reset in a new game.
- Players can only play their own cards and powers.
- Normally players can only interact with their own cards or powers. However some powers may allow the player to interact with 
other players (or center or deck) cards and powers (for example: şunu değiş tokuş or şunu değiş bunu tokuş).


---

### UI & Effects

- UI screens and panels are found and managed by GameManager.
- Super power activations are shown with fade-in/out text and optional animations.
- Card and power animations are handled by DeckController and CardInteraction.
- Only the cards in the player's own hands should autoRotate

---

### Notes

- Many methods use coroutines for animations and delays.
- Most logic is split between GameManager (client-side), Server (authoritative), and DeckController (visuals).
- Power activation and card changes are always networked to keep all clients in sync.

---

### Pişti Rules 

- There can be 2 or 4 players for 1v1 or 2v2.
- 4 cards are dealt to the center, last face up.
- Each player is dealt 4 cards.
- Players take turn playing one card at a time.
- Round ends when there are no cards left to play
- Points are calculated at the end of each round.
    - Aces (value=1) and Jacks (value=11) are worth 1 point each.
    - 2 of clubs (kind=1, value=2) is 2 points.
    - 10 of diamonds (kind=2, value=10) is 3 points.
    - Player (or team) with the most cards in their pool gets 3 points.
    - Normal piştis are worth 10 points and Jack piştis are worth 20.
        -Pişti happens if there is only one card in the center and it gets captured with the same valued card. 
    - Under normal curcimstances and without pişti there are 16 points to get each round.
- Cards are captured if the player plays a same valued card or a Jack.
- If there is no capture the card is added to the center.
- Players turn end when they play a card.

---

### Game Spesific Rules

- Playing a card instantly ends players turn.
- 3 powers are given to the player when they are dealt card.
- Players can hold 5 powers max.
- Players can play powers in their turn before they play a card.
- Some powers may grant additional points to the player directly(Zafer puanı) or inderectly(Kutsal deste).
- Some powers can alter the appearance and value of the cards.
- Changes in the cards stays between rounds but gets reset between games.
- 1v1 is individual mode.
- 2v2 is a team mode. Player is teamed with the player sitting opposite of them (0-2,1-3 are teams).

---

### Known Issues / Limitations

- Beacuse the game does not have a dedicated server host migration is not possible.
- No logic for reconnection.
- I dont know what will happen if the host or the clients closes the game or takes it to the background.
- No disconnect detection.
- No bot implimentation
- No gameState tracking in case a clients is needed to be synced up with the current gameState in local view.
- No turn timers
- The code for card movements are wonky and too nested, probably needs refactoring.
- Constructing the local view for clients is based on relative indexes because it is simple but I dont know if there is any weak point in the code because of that.


### Things to test

-Check if kopyalayapıştır is working for different effects both for addition and reset. Values are set correctly,
need to check if visuals are updated correctly.
-Check if updatecurrentlayout is called after necessary powers (mainly swaps).


//Turun kimde olduğu UI
//Ortaya gelen kartlar birbirine cliplenebiliyor, ortaya geldikten sonar rotasyon 0la
//Sırtını bıçakla gücü
//BattıBalıkYanGider sen rakibini oyna
//Oyundan çıkıca geri girişinde bağlantı kopuk
//CloseInfoBoxı invokedan çıkar
//2 player logicleri 4 e geçir
//Kartlar hareket ederken DOKill lazım. Bazı swaplarda kart animasyonunu bitirmemişse kötü oluyor
//ŞunuDeğişBunuTokuşta swap sonrası rotasyonlar yanlış ayarlı
//Çok hızlı oynarsan kartlar orijinal halleri ile ortaya geliyor

/////DOtweenler kart ortaya konurken hala bitmemiş olursa kartın scalei farklı kalıyor.
Bir şekilde bğtğn aktif tweenleri kart hareketi başlamadan bitirmeli ya da tweenler bitene kadar beklemeli.