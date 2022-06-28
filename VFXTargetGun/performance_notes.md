
# Syncing

TODO: organize and summarize notes

10: 456
9: 424
8: 384
7: 352
6: 312
5: 280

10-9: 32
9-8: 40
8-7: 32
7-6: 40
6-5: 32
5-4: 40
4-3: 32
3-2: 40
2-1: 32
1-0: 40

don't ask me why but this is what we get
it should be 44 for every effect if there was zero overhead
but instead we get less?
are they actually doing compression as they should be?
if yes, how is it still so large after that?
also if yes, how is it consistent? I mean sure I've only tested 5 values, but they have a pattern.
It's just weird, but let's just take the average:
36 per effect

late joiner sync byte count for effects:
96 per gun with any active effects + 36 per effect

so you can now have about 657.7777777777778 active effects spread accross 5 guns for it to get as bad as it was yesterday with 12 effects (although the fact that it was 12 yesterday doesn't really matter, it was all of the accumulated overhead of arrays, really)



raw values:
(
    old system, first join
    steam:
    25224
    proton caller:
    0

    old system, second join
    steam:
    49384
    proton caller:
    0

    new system, first join for real this time
    steam:
    1112
    proton caller:
    0

    new system, second join for real this time
    steam:
    1568
    proton caller:
    0
)


the first join is bigger because everything that requested serialization ends up syncing twice. This won't happen in the real world because there are pretty much always at least 2 people in the map already.
the second join is what matters

test with 6, 3, 1 active effects

old system:
(first join: 25224)
second join: 24160

new system:
(first join: 1112)
second join: 456

GG! Not done yet, need to clean up, but it's break time :D
