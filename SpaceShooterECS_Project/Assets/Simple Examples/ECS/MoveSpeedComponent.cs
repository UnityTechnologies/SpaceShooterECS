using System;
using Unity.Entities;

namespace Shooter.ECS
{
    [Serializable]
    public struct MoveSpeed : IComponentData
    {
        public float Value;
    }

    public class MoveSpeedComponent : ComponentDataWrapper<MoveSpeed> { }
}
