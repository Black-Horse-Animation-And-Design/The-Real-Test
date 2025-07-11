/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels.com/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

namespace ProceduralPixels.BakeAO
{
    /// <summary>
    /// How to apply Bake AO component
    /// </summary>
    public enum BakeAOApplyMode
    {
        /// <summary>
        /// Pick the application mode automatically. For built-in render pipeline it is MaterialPropertyBlock, for SRP it is MaterialInstance.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Use material property block in the renderer.
        /// </summary>
        MaterialPropertyBlock = 1,

        /// <summary>
        /// Create material instances and modify Bake AO properties of created materials
        /// </summary>
        MaterialInstance = 2,
    }
}
