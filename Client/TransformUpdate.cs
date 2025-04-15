using UnityEngine;

public class TransformUpdate
{
    // Create public getters for the TransformUpdates tick & position
    public ushort Tick { get; private set; }
    public Vector3 Position { get; private set; }

    // Assign the tick & position based off given tick & position
    public TransformUpdate(ushort tick, Vector3 position)
    {
        Tick = tick;
        Position = position;
    }
}
