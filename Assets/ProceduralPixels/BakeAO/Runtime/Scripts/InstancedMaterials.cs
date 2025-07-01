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
    public class InstancedMaterials : IDisposable
    {
        public Renderer renderer { get; private set; }
        public Material[] originalMaterials { get; private set; }
        public Material[] instancedMaterials { get; private set; }

        private bool isEnabled = false;

        public bool IsEnabled
        {
            get => isEnabled;
            set
            {
                if (value)
                    Enable();
                if (!value)
                    Disable();
            }
        }

        /// <summary>
        /// Creates the copy of the materials of this renderer. Allows modifying the materials.
        /// </summary>
        /// <param name="renderer"></param>
        public InstancedMaterials(Renderer renderer)
        {
            originalMaterials = renderer.sharedMaterials;
            instancedMaterials = renderer.materials;
            this.renderer = renderer;
            isEnabled = true;
        }

        public void Enable()
        {
            if (isEnabled)
                return;

            renderer.sharedMaterials = instancedMaterials;
            isEnabled = true;
        }

        public void Disable()
        {
            if (!isEnabled)
                return;

            renderer.sharedMaterials = originalMaterials;
            isEnabled = false;
        }

        public void Dispose()
        {
            Disable();
            for (int i = 0; i < instancedMaterials.Length; i++)
                SafeDestroy(instancedMaterials[i]);

            renderer = null;
            originalMaterials = null;
            instancedMaterials = null;
        }

        #region Set Property Methods

        public void SetTexture(int propertyID, Texture texture)
        {
            for (int i = 0; i < instancedMaterials.Length; i++)
                if (instancedMaterials[i] != null)
                    instancedMaterials[i].SetTexture(propertyID, texture);
        }

        public void SetFloat(int propertyID, float value)
        {
            for (int i = 0; i < instancedMaterials.Length; i++)
                if (instancedMaterials[i] != null)
                    instancedMaterials[i].SetFloat(propertyID, value);
        }

        #endregion


        /// <summary>
        /// Utility method for safe destroying the objects in editor.
        /// </summary>
        /// <param name="obj"></param>
        private static void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null)
                return;

#if UNITY_EDITOR
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(obj);
            else
                UnityEngine.Object.DestroyImmediate(obj);
#else
                UnityEngine.Object.Destroy(obj);
#endif
        }
    }
}
