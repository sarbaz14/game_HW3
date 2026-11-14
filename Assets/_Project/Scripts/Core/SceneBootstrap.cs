using UnityEngine;

namespace ChickenShooter
{
    /// <summary>
    /// Lives on a Bootstrap object in every scene. Guarantees GameManager/AudioManager/ObjectPooler
    /// exist even when a gameplay scene is played directly from the editor.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class SceneBootstrap : MonoBehaviour
    {
        public bool lockCursorWhilePlaying = true;

        private void Awake()
        {
            SystemsBootstrap.Ensure();

            if (lockCursorWhilePlaying)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }

            Time.timeScale = 1f;
        }
    }
}
