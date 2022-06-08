
# VFX Target Gun

## Done

- when held show target locally
- when clicked, play effect at target location
  - if it is looped, star it, unless it was already looping, in which case disable (return to default state)
  - if it isn't looped, just play it. If all particle systems are already playing, create a new one, therefore allowing for multiple of the same effect to be active
- define effects using effect descriptors which all are a child of a specific GameObject
- interact on top of the gun to show a UI
  it's basically a grid of buttons that have been instantiated the first time you picked it up, where the currently active is indicated somehow. Currently looping ones are also marked somehow. Upon click of one of those buttons, change active effect to the clicked one and close the UI immediately
- indicate active state of both looped and non looped effects outside of the UI
- allow disabling of currently active looped effect without having to point at the ground
- disable target indicator if the current loop effect is active
- add a toggle in the UI to keep it open to allow for quick testing of the different effects, especially in VR where you have 2 hands. Yea I like this idea

## Todo

- maybe add stop button for currently active looped effects in the UI
- show name of selected effect outside of UI
- The GUI also needs a close button to not confuse people
- color legend for what they mean
- local visibility and grab-able toggle, global for all guns
- sync non looping effects when they are played
- sync looping effects when they are played
- sync looping effects for late joiners
- sync controller state to allow giving another DM your gun
  - the synced state must not care about the UI, because the UI may not even exist yet. Well, the buttons.
    - when creating the buttons, adjust them to match the current state
  - sync selected effect
