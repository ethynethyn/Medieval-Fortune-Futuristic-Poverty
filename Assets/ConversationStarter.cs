using UnityEngine;
using DialogueEditor;

public class ConversationStarter : MonoBehaviour
{
    public GameObject interactionImage; // Drag your UI Image here in the Inspector
    public NPCConversation myConversation;

    private bool playerInTrigger = false;

    private void Start()
    {
        if (interactionImage != null)
            interactionImage.SetActive(false);
    }

    private void Update()
    {
        if (playerInTrigger &&
            ConversationManager.Instance != null &&
            !ConversationManager.Instance.IsConversationActive)
        {
            if (interactionImage != null)
                interactionImage.SetActive(true);

            if (Input.GetKeyDown(KeyCode.E))
            {
                ConversationManager.Instance.StartConversation(myConversation);
            }
        }
        else
        {
            if (interactionImage != null)
                interactionImage.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTrigger = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTrigger = false;

            if (ConversationManager.Instance != null &&
                ConversationManager.Instance.IsConversationActive)
            {
                ConversationManager.Instance.EndConversation();
            }

            if (interactionImage != null)
                interactionImage.SetActive(false);
        }
    }
}
