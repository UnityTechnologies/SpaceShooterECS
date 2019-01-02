using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Shooter.ECS
{
	
    public class MovementSystem : ComponentSystem 
	{
		ComponentGroup enemyGroup;

		protected override void OnCreateManager()
		{
			enemyGroup = GetComponentGroup(typeof(Position), typeof(Rotation), typeof(MoveSpeed));
		}
		//Right now we're stuck until the release gets updated.
		protected override void OnUpdate()
		{
			using (var enemies = enemyGroup.ToEntityArray())
			{ }
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
