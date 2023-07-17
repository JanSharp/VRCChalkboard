
# Chalkboard

A chalkboard with the aim of always being in sync, including for late joiners. Visuals and performance are of course also kept in mind, and while those could be better, it's acceptable as it is right now.

# Installing

Head to my [VCC Listing](https://jansharp.github.io/vrc/vcclisting.xhtml) and follow the instructions there.

# Features

- Chalk
  - Each chalk has exactly one color of your choosing
  - There's 2 modes, chalk and sponge. The only difference between the 2 is the drawn shape
  - Every chalk can draw an every board
  - Fully synced both for current players and for late joiners
- Chalkboard
  - Handles multiple chalks drawing on the same board at the same time
  - Also fully synced (technically the board is the one doing late joiner syncing, the chalk isn't)
- ChalkboardManager
  - There must be exactly one manager at the root of the scene with the name `ChalkboardManager`. Its editor scripting ends up assigning ids to chalks and boards.

-- TODO: Add proper documentation for how to use the chalk and chalkboard

# Ideas

I believe there's a way for me to change how the chalks work in order to allow the item (the object with the vrc pickup) to be disabled without breaking the chalk's syncing. The general idea is to move the chalk script outside of the item, but I feel like I remember there being some issue with that. Best would be if the script just does it automatically on start, like moving itself to be a child of the manager, but I'm not sure.

Use Textures created at runtime instead of requiring the user to create them for each board in the editor.

Use a render texture and a custom shader for both better performance while drawing on a board and faster response time, plus a potential for splines. But I can sense that this is quite the complex shader, and I don't even know if it's truly possible, since the script should only pass in new points to the GPU and the shader should keep the previous data for all other pixels. Maybe that's possible, who knows.
