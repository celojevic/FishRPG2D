using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FishRPG.Entities.Player
{
    public class PlayerFloatingWeapon : NetworkBehaviour
    {

        [Tooltip("The visual object of the equipped weapon.")]
        [SerializeField] private Transform _weaponTransform = null;

        // TODO make dependent on weapon, bigger axes = slower (~7f)
        [SerializeField] private float _slerpSpeed = 10f;

        private Player _player;

        private void Awake()
        {
            _player = GetComponent<Player>();
        }

        private void Update()
        {
            // idea: hold left click to start drawing a pattern. like a pentagram that when completed,
            //          will trigger aoe effect, or extra blade that travel the same pattern

            Vector3 mouseDir = 
                ((Vector3)_player.Input.WorldMousePosition - _player.transform.position).normalized;

            _weaponTransform.position = Vector3.Slerp(_weaponTransform.position,
                _player.transform.position + mouseDir + Vector3.up / 2f, Time.deltaTime * _slerpSpeed);
                

            _weaponTransform.rotation = 
                Quaternion.Euler(0, 0, Mathf.Atan2(mouseDir.y, mouseDir.x) * Mathf.Rad2Deg - 90f);

        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            Gizmos.DrawRay(
                transform.position,
                (_player.Input.WorldMousePosition - (Vector2)_player.transform.position).normalized
            );
        }

    }
}