using System;
using Unity.Entities;

namespace Shooter.ECSwithJobs
{
    [Serializable]
    public struct MoveSpeed : IComponentData
    {
        public float Value;
    }

    public class MoveSpeedComponent : ComponentDataWrapper<MoveSpeed> { }
}
