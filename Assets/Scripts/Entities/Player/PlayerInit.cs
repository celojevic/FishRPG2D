namespace FishRPG.Entities.Player
{
    using FishNet.Object;
    using UnityEngine;
    using Cinemachine;
    using FishRPG.UI;

    /// <summary>
    /// Inits the player on spawn, such as grabbing the Camera and Canvas, and sets up Rigidbody.
    /// </summary>
    public class PlayerInit : NetworkBehaviour
    {

        public override void OnStartClient(bool isOwner)
        {
            if (isOwner)
            {
                // init camera
                GameObject.FindGameObjectWithTag("MainCamera")
                    .GetComponentInChildren<CinemachineVirtualCamera>()
                    .Follow = transform;

                // init UI
                UiManager.Player = GetComponent<Player>();
            }

            // init rigidbody
            GetComponent<Rigidbody2D>().isKinematic = !isOwner;
        }

    }
}
