
<!-- cSpell:ignore ulongs -->

- [ ] only apply changes to the texture if the player is near the board
- [x] late joiner syncing
  - [x] handle unexpected ownership transfer
  - [x] update chalk action structure to match chalkboard
  - [x] use 3 or 4 ulongs and sync less frequently. it's significantly more efficient in terms of total synced byte count
- [ ] add progress bars just for fun
- [ ] improve queue logic for chalks
- [ ] scale previews based on board size and resolution
- [ ] adjust update rates depending on resolution
- [ ] use transparency for background
- [ ] test planes
- [ ] maybe test interpolation for less jagged drawing in VR. May depend on the user, their setup and general usage such as distance from the board, speed of writing
- [ ] set grip depending on if the user is in VR
- [ ] reduce allowed distance from the board in VR
- [ ] move board up and down
