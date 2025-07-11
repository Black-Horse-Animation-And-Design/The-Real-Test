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
    internal class MaterialPropertyBlockApplicator : IApplyBakeAO
    {
        private MaterialPropertyBlock propertyBlock = null;
        private Renderer renderer = null;
        private Action<MaterialPropertyBlock> updateAction;

        public MaterialPropertyBlockApplicator(Renderer renderer, Action<MaterialPropertyBlock> updateAction)
        {
            propertyBlock = new MaterialPropertyBlock();
            this.renderer = renderer;
            this.updateAction = updateAction;
        }

        public void Dispose()
        {
            propertyBlock.Clear();
            renderer.SetPropertyBlock(null);

            updateAction = null;
            renderer = null;
            propertyBlock.Clear();
        }

        void IApplyBakeAO.ApplyProperties()
        {
            renderer.GetPropertyBlock(propertyBlock);
            updateAction?.Invoke(propertyBlock);
            renderer.SetPropertyBlock(propertyBlock);
        }

        bool IApplyBakeAO.HasInvalidTexture()
        {
            if (propertyBlock != null)
            {
                var aoTexture = propertyBlock.GetTexture(GenericBakeAO.occlusionMapStandardID);
                return aoTexture == null;
            }

            return true;
        }
    }
}
