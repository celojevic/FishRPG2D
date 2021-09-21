namespace FishRPG.UI
{
    using UnityEngine;

    public class UIEquipmentSlot : UISlot
    {

        [SerializeField] private EquipmentSlot _mySlot;

        protected override void HandleRightClick()
        {
            if (UiManager.Player.Equipment.Equipment[(int)_mySlot] != null)
                UiManager.Player.Equipment.CmdUnequip(_mySlot);
        }

        protected override void HandlePointerEnter()
        {
            if (_mySlot < 0 || _mySlot >= EquipmentSlot.Count) return;

            var equip = UiManager.Player?.Equipment?.Equipment[(int)_mySlot];
            if (equip != null)
                UITooltip.Show(equip.BuildString());
        }

        private void OnValidate()
        {
            if (_mySlot == EquipmentSlot.Count)
            {
                Debug.LogWarning($"Slot cannot be the Count. Assign a valid slot to {name}.");
                _mySlot = EquipmentSlot.Weapon;
            }
        }

    }
}
