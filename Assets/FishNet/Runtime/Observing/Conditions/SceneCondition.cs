﻿using FishNet.Connection;
using FishNet.Object;
using FishNet.Observing;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FishNet.Managing
{

    [CreateAssetMenu(menuName = "FishNet/Observers/Scene Condition", fileName = "New Scene Condition")]
    public class SceneCondition : ObserverCondition
    {
        #region Serialized.
        /// <summary>
        /// True to synchronize which scene the object was spawned in to clients. When true this object will be moved to the clients equivelant of the scene it was spawned in on the server. This setting does not continously move this object to the same scene.
        /// </summary>
        [Tooltip("True to synchronize which scene the object was spawned in to clients. When true this object will be moved to the clients equivelant of the scene it was spawned in on the server. This setting does not continously move this object to the same scene.")]
        [SerializeField]
        private bool _synchronizeScene = false;
        #endregion

        public void ConditionConstructor(bool synchronizeScene)
        {
            _synchronizeScene = synchronizeScene;
        }

        /// <summary>
        /// Returns if the object which this condition resides should be visible to connection.
        /// </summary>
        /// <param name="connection"></param>
        public override bool ConditionMet(NetworkConnection connection)
        {
            /* If this objects connection is valid then check if
             * connection and this objects owner shares any scenes.
             * Don't check if the object resides in the same scene
             * because thats not reliable as server might be moving
             * objects. */
            if (base.NetworkObject.OwnerIsValid)
            {
                foreach (Scene s in base.NetworkObject.Owner.Scenes)
                {
                    //Scenes match.
                    if (connection.Scenes.Contains(s))
                        return true;
                }

                //Fall through, no scenes shared.
                return false;
            }
            else
            {
                /* When there is no owner only then is the gameobject
                 * scene checked. That's the only way to know at this point. */
                return connection.Scenes.Contains(base.NetworkObject.gameObject.scene);
            }
        }

        /// <summary>
        /// True if the condition requires regular updates.
        /// </summary>
        /// <returns></returns>
        public override bool Timed()
        {
            return false;
        }


        /// <summary>
        /// Clones referenced ObserverCondition. This must be populated with your conditions settings.
        /// </summary>
        /// <returns></returns>
        public override ObserverCondition Clone()
        {
            SceneCondition copy = ScriptableObject.CreateInstance<SceneCondition>();
            copy.ConditionConstructor(_synchronizeScene);
            return copy;
        }

    }
}
