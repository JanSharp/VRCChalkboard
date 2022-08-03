
<!-- cSpell:ignore ulongs -->

- [x] only apply changes to the texture if the player is near the board
- [x] late joiner syncing
  - [x] handle unexpected ownership transfer
  - [x] update chalk action structure to match chalkboard
  - [x] use 3 or 4 ulongs and sync less frequently. it's significantly more efficient in terms of total synced byte count
- [x] add progress bars just for fun
- [x] improve queue logic for chalks
- [x] scale previews based on board size and resolution
- [x] adjust update rates depending on resolution
- [x] use transparency for background
- [x] test planes
- [ ] looking at the boards from the side makes the lines disappear, this is most likely related to both them now being transparent and on planes in font of the board
- [ ] maybe test interpolation for less jagged drawing in VR. May depend on the user, their setup and general usage such as distance from the board, speed of writing
- [x] set grip depending on if the user is in VR
- [x] reduce allowed distance from the board in VR
- [x] move board up and down
- [ ] you being the only single person in the world is going to break late joiner syncing for the first other person joining
- [ ] drawing a whole bunch with one chalk takes too long to catch up with incremental syncing causing you using another chalk too quickly being potentially overdrawn by the old chalk for all other clients, so in other words it's a de-sync.
