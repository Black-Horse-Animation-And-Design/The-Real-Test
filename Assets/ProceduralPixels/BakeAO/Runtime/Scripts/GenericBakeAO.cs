/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels.com/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

using System;
using System.Collections.Generic;
using System.Dynamic;
using UnityEngine;

namespace ProceduralPixels.BakeAO
{
    [HelpURL("https://proceduralpixels.com/BakeAO/Documentation/BakeAOComponent")]
    [ExecuteAlways, DisallowMultipleComponent]
    public abstract class GenericBakeAO : MonoBehaviour
    {
        [Tooltip("How to apply Bake AO properties into the renderer. Default option will fallback to the default apply mode set in the Project Settings.")]
        [SerializeField] internal BakeAOApplyMode mode = BakeAOApplyMode.Default;

        [Tooltip("Ambient occlusion texture that should be applied to the model.")]
        [SerializeField] internal Texture2D ambientOcclusionTexture;

        [Tooltip("Occlusion Strength parameter controls the ambient occlusion application strength. Default value is 1.0.")]
        [SerializeField][Range(0.0f, 2.0f)] internal float occlusionStrength = 1.0f;

        [Tooltip("UV set of a model that will be used to sample ambient occlusion texture. It should be a UV Set that the texture was baked for.")]
        [SerializeField] internal UVChannel occlusionUVSet = UVChannel.UV0;

        [Tooltip("Decides if the ambient occlusion texture should be applied also into a diffuse texture. By default AO texture is applied only to the ambient lighting.")]
        [SerializeField] internal bool applyOcclusionIntoDiffuse = false;

        public BakeAOApplyMode ApplyMode
        {
            get
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                {
                    if (mode == BakeAOApplyMode.Default)
                        return BakeAORuntimeSettings.Instance.applyMode;
                    else
                        return mode; 
                }
                else
                    return BakeAOApplyMode.MaterialPropertyBlock;
#else
                if (mode == BakeAOApplyMode.Default)
                    return BakeAORuntimeSettings.Instance.applyMode;
                else
                    return mode;
#endif
            }
            set
            {
                mode = value;
                UpdateAmbientOcclusionProperties();
            }
        }


        public Texture2D AmbientOcclusionTexture
        {
            get => ambientOcclusionTexture;
            set
            {
                ambientOcclusionTexture = value;
                UpdateAmbientOcclusionProperties();
            }
        }

        public float OcclusionStrength
        {
            get => occlusionStrength;
            set
            {
                occlusionStrength = value;
                UpdateAmbientOcclusionProperties();
            }
        }

        public UVChannel OcclusionUVSet
        {
            get => occlusionUVSet;
            set
            {
                occlusionUVSet = value;
                UpdateAmbientOcclusionProperties();
            }
        }

        public bool ApplyOcclusionIntoDiffuse
        {
            get => applyOcclusionIntoDiffuse;
            set
            {
                applyOcclusionIntoDiffuse = value;
                UpdateAmbientOcclusionProperties();
            }
        }

        [HideInInspector] public Renderer rendererRef;

        IApplyBakeAO applicator;

        public static readonly int occlusionMapStandardID = Shader.PropertyToID("_OcclusionMap");
        public static readonly int occlusionStrengthStandardID = Shader.PropertyToID("_OcclusionStrength");
        public static readonly int occlusionUVSetStandardID = Shader.PropertyToID("_AOTextureUV");
        public static readonly int applyOcclusionToDiffuseStandardID = Shader.PropertyToID("_MultiplyAlbedoAndOcclusion");


        protected void Awake()
        {
            if (rendererRef == null)
                TryGetComponent(out rendererRef);
        }

        protected void OnEnable()
        {
#if UNITY_EDITOR
            if (!Application.IsPlaying(this))
            {
                applicator = new MaterialPropertyBlockApplicator(rendererRef, SetupPropertyBlock);
            }
            else
            {
                switch (ApplyMode)
                {
                    case BakeAOApplyMode.MaterialPropertyBlock:
                        applicator = new MaterialPropertyBlockApplicator(rendererRef, SetupPropertyBlock);
                        break;
                    case BakeAOApplyMode.MaterialInstance:
                        applicator = new InstancedMaterialApplicator(rendererRef, SetupMaterialProperties);
                        break;
                    case BakeAOApplyMode.Default:
                        applicator = new MaterialPropertyBlockApplicator(rendererRef, SetupPropertyBlock);
                        break;
                }
            }
#else
            switch (ApplyMode)
            {
                case BakeAOApplyMode.MaterialPropertyBlock:
                    applicator = new MaterialPropertyBlockApplicator(rendererRef, SetupPropertyBlock);
                    break;
                case BakeAOApplyMode.MaterialInstance:
                    applicator = new InstancedMaterialApplicator(rendererRef, SetupMaterialProperties);
                    break;
                case BakeAOApplyMode.Default:
                    applicator = new MaterialPropertyBlockApplicator(rendererRef, SetupPropertyBlock);
                    break;
            }
#endif 
            UpdateAmbientOcclusionProperties();
        }

        protected void OnDisable()
        {
            applicator?.Dispose();
            applicator = null;
        }

        public void UpdateAmbientOcclusionProperties()
        {
            if (!enabled)
                return;

            if (rendererRef == null || ambientOcclusionTexture == null)
                return;

            if (applicator == null)
                return;

            applicator.ApplyProperties();
        }

        public abstract void SetupPropertyBlock(MaterialPropertyBlock propertyBlock);
        public abstract void SetupMaterialProperties(Material material);

        /// <summary>
        /// This method forces to do a complete update of the Bake AO component. Use it after you changed many properties using the code.
        /// </summary>
        public void Refresh()
        {
            OnDisable();
            if (isActiveAndEnabled)
                OnEnable();
        }

        protected void OnValidate()
        {
            if (rendererRef == null)
                TryGetComponent(out rendererRef);

            if (ambientOcclusionTexture == null)
                OnDisable();

            UpdateAmbientOcclusionProperties();
        }

#if UNITY_EDITOR
        // Used in editor scripts to check if the texture is broken, which can happen when user modifies/deletes the texture from the assets.
        public bool HasInvalidTexture()
        {
            if (applicator == null)
                return false;

            return applicator.HasInvalidTexture();
        }
#endif
    }
}
