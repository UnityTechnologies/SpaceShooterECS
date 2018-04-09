using System;
using UnityEngine;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Rendering;

namespace UnityEngine.ECS.Rendering
{
    [System.Serializable]
	public struct EntityInstanceRenderer : ISharedComponentData
	{
        public Mesh                 mesh;
        public Material[]           materials;

        public ShadowCastingMode    castShadows;
        public bool                 receiveShadows;
	}

	public class EntityRendererComponent : SharedComponentDataWrapper<EntityInstanceRenderer>
    {
        private void OnDrawGizmos()
        {
            EntityInstanceRenderer renderData = Value;
            Gizmos.color = Color.white;
            //Gizmos.DrawMesh(renderData.mesh, transform.position, transform.rotation, transform.localScale);

            for(int i = 0; i < renderData.mesh.subMeshCount; i++)
            {
                renderData.materials[i].SetPass(0);
                Graphics.DrawMeshNow(renderData.mesh, transform.position, transform.rotation, i);
            }

        }

        void OnDrawGizmosSelected()
        {

        }
    }
}
