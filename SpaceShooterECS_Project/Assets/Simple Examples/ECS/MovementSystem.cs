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
		
		protected override void OnUpdate()
		{
			using (var enemies = enemyGroup.ToEntityArray(Allocator.TempJob))
			{
				foreach (var enemy in enemies)
				{
					Position position	= EntityManager.GetComponentData<Position>(enemy);
					quaternion rotation = EntityManager.GetComponentData<Rotation>(enemy).Value;
					float speed			= EntityManager.GetComponentData<MoveSpeed>(enemy).Value;

					position.Value += Time.deltaTime * speed * math.forward(rotation);

					if (position.Value.z < GameManager.GM.bottomBound)
						position.Value.z = GameManager.GM.topBound;

					EntityManager.SetComponentData(enemy, position);
				}

			}
		}
    }
}
