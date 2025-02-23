using UnityEngine;

public class Anvil : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(Player.Tag) || !other.TryGetComponent(out Player plr) || GunRepairUI.Instance.IsActive)
            return;
        
        var gun = plr.ActiveGun;
        if (gun && gun.IsJammed)
        {
            var ui = GunRepairUI.Instance;
            ui.Activate(gun.RepairsNeeded);
            ui.Randomize();
        }
    }
}
