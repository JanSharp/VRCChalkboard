
# VFX Target Gun

when held, show target locally
when clicked, play effect at target location
- if it is looped, indicate it is placed. if it was already looping, disable and indicate as such (return to default state)
- if it isn't looped, just play it. maybe even also indicate active state, could probably use the same system
define effects using an array of an effect descriptor. The descriptor is also going to handle all effect specific things

interact on top of the gun to show a UI
it's basically a grid of buttons that have been instantiated the first time you picked it up, where the currently active is indicated somehow. Currently looping ones are also marked somehow. Upon click of one of those buttons, change active effect to the clicked one and close the UI immediately

The GUI also needs a close button to not confuse people, and if i'm using colors it'll need a legend for what they mean

maybe add a toggle in the UI to keep it open to allow for quick testing of the different effects, especially in VR where you have 2 hands. Yea I like this idea

indication text for which one is currently selected, outside of the gui.
