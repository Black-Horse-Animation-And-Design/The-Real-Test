/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels.com/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

using System;
using UnityEngine;

namespace ProceduralPixels.BakeAO
{
    internal class InstancedMaterialApplicator : IApplyBakeAO
    {
        private InstancedMaterials instancedMaterials = null;
        private Action<Material> updateAction;

        public InstancedMaterialApplicator(Renderer renderer, Action<Material> updateAction)
        {
            this.instancedMaterials = new InstancedMaterials(renderer); ;
            this.updateAction = updateAction;
        }

        public void Dispose()
        {
            instancedMaterials.Dispose();
        }

        void IApplyBakeAO.ApplyProperties()
        {
            var materials = instancedMaterials.instancedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                var material = materials[i];
                updateAction?.Invoke(material);
            }
        }

        bool IApplyBakeAO.HasInvalidTexture()
        {
            return false;
        }
    }
}
