using UnityEngine;

namespace ProceduralPixels.BakeAO
{

    /// <summary>
    /// Attaching this component anywhere on the scene will initialize the <see cref="BakeAORuntimeSettings"/>. Bake AO should do this automatically via scene processor. You don't need to manually attach this component!
    /// <see cref="BakeAORuntimeSettings"/> must be initialized by this component to make the Bake AO work properly. 
    /// </summary>
    public class BakeAORuntimeSettingsInitializer : MonoBehaviour
    {
        // This may be confusing but the settingsReference field is serialized so Unity will load the settings into memory before calling the Awake method.
        // Loading the asset into memory will make it initialize and register as a singleton.
        public BakeAORuntimeSettings settingsReference = null;

        private void Awake()
        {
#if UNITY_EDITOR
            BakeAORuntimeSettings.Instance.RefreshApplyMode();
#endif
            Destroy(gameObject); // Runtime settings were initialized by the engine, so the game object can be destroyed asap. BakeAORuntimeSettings is already properly initialized.
        }
    }

}