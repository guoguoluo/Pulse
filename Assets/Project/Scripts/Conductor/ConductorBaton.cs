using UnityEngine;

/// <summary>Shows a temporary baton while the mouse is held in conductor mode.</summary>
public class ConductorBaton : MonoBehaviour
{
    [SerializeField] private Transform batonVisual;

private void Awake()
    {
        SetVisible(false);
    }

private void OnEnable()
    {
        SetVisible(true);
    }

private void OnDisable()
    {
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (batonVisual != null) batonVisual.gameObject.SetActive(visible);
    }
}
