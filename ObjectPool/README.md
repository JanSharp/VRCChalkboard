
This was going to be an object pool system where objects get instantiated at runtime. Except that's impossible because those instantiated items cannot have any synchronizing behaviours on them. There is a better idea, see [ItemSync/PerformanceOptions.md](../ItemSync/PerformanceOptions.md) at the bottom.
