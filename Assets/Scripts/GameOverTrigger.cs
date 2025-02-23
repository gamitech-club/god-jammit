using UnityEngine;
using UnityEngine.Assertions;

public class GameOverTrigger : MonoBehaviour
{
    private void Start()
    {
        Assert.IsNotNull(Player.Instance, $"[{name}] Player instance not found in scene");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out EnemyMovement enemy))
            return;
        
        var plr = Player.Instance;

        if (!plr.IsGameOver)
        {
            plr.GameOver();
        }
    }
}
