# TODO List

## Completed
- [x] Fix server hand tracking: Remove played cards from playersHandCardsIDs in GetMove method
- [x] Add card removal when player captures (in capture branch of GetMove)
- [x] Add card removal when player adds to center (in AddCardIDToCenter)
- [x] Test that saves now capture actual current game state, not initial dealt state
- [x] Fix center cards being dealt face-down during game state loading
- [x] Implement bombed cards tracking for cards outside normal game flow
- [x] Implement superpower state persistence for save/load
- [x] Implement card power effect persistence (Kapkaç, Yandım Anam value changes)

## In Progress
- [ ] Test the card power effect persistence with save/load

## Pending
- [ ] Add any additional debugging tools if needed
- [ ] Consider other special card locations (if any) that need tracking
- [ ] Test points persistence during save/load
- [ ] Consider adding visual indicators for active superpowers after load
- [ ] Test that card sprites correctly reflect value changes after load
