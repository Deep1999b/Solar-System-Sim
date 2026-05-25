# Scaling And Realism

## Spatial Scale

The project uses:

- `1 Unity unit = 100,000 km`

This is defined in [SolarSystemScale.cs](/F:/Projects/Solar%20System/Assets/Scripts/SolarSystemScale.cs:3).

Planet diameters and orbital distances in the generator are converted from real-world kilometer values into Unity units. That means the relative size-to-distance ratios are intentionally realistic within the chosen compressed scale.

Examples:

- Earth diameter: `12,742 km -> 0.12742 units`
- Earth average distance from Sun: `149,600,000 km -> 1496 units`
- Sun diameter: `1,392,700 km -> 13.927 units`

So visually, the scene is scaled in a physically grounded way.

## What Is Realistic

- Relative body sizes are broadly realistic.
- Relative orbital distances are broadly realistic.
- The overall scale constant is consistent across the simulation.
- Scientific metadata shown in the UI comes from body-specific JSON assets.

## What Is Stylized

- Gravity is not simulated in SI units. The simulation uses tuned values for stability and playability.
- Time is compressed heavily for usability.
- Most major bodies orbit in a simplified shared plane.
- Trails, selection rings, minimap markers, atmosphere shells, and some satellite visuals are exaggerated for readability.
- Lagrange mission behavior is stylized rather than a full high-fidelity mission dynamics model.

## Bottom Line

The project is spatially realistic in ratio, but dynamically stylized for interactive exploration.

That is a good design choice for a portfolio piece: it is believable, readable, and technically explainable without pretending to be a scientific simulator.
