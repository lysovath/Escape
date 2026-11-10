using UnityEngine;

public class Goal : MonoBehaviour
{
    [SerializeField] private GameObject winningBanner;

    private bool playerHasWon;

    private void Start()
    {
        if (winningBanner != null)
        {
            winningBanner.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        ShowWinningBanner(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        ShowWinningBanner(collision.gameObject);
    }

    private void ShowWinningBanner(GameObject player)
    {
        if (playerHasWon || !player.CompareTag("Player"))
        {
            return;
        }

        playerHasWon = true;

        if (winningBanner != null)
        {
            winningBanner.SetActive(true);
        }
    }
}
