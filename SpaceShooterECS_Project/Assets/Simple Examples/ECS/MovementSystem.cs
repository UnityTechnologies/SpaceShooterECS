using Unity.Collections;
using Unity.Entities;
using Unity.Burst;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Shooter.ECS
{
	
    public class MovementSystem : ComponentSystem 
	{
		struct EnemyGroup
		{
			public ComponentDataArray<Position> positions;
			[ReadOnly] public ComponentDataArray<Rotation> rotations;
			[ReadOnly] public ComponentDataArray<MoveSpeed> moveSpeeds;
			public readonly int Length;
		}
		[Inject]
		EnemyGroup enemies;

		protected override void OnUpdate()
		{
			for (int i = 0; i < enemies.Length; i++)
			{
				Position position = enemies.positions[i];
				Rotation rotation = enemies.rotations[i];
				MoveSpeed speed = enemies.moveSpeeds[i];

				position.Value += Time.deltaTime * speed.Value * math.forward(rotation.Value);

				if (position.Value.z < GameManager.GM.bottomBound)
					position.Value.z = GameManager.GM.topBound;

				enemies.positions[i] = position;
			}
		}
    }
}
