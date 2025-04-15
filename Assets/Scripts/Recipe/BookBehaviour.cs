using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class BookBehaviour : MonoBehaviour
{
    MeshRenderer meshRenderer;

    [Header("List of all pages - Put materials here for each page")]
    public List<Material> pages = new List<Material>();

    [Header("Colliders - Put colliders here for each page")]
    public XRSimpleInteractable leftPoke;
    public XRSimpleInteractable rightPoke;

    [Header("How many pages the book has - This is handled by code")]
    public int maxPages;
    public int minPages;

    [Header("Current state of book - This is handled by code")]
    public int DisplayNumber = 0;
    public Material page1;
    public Material page2;

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        page1 = pages[0];
        page2 = pages[1];

        // Initialize the materials array properly
        Material[] materials = meshRenderer.materials;
        materials[2] = page1;
        materials[3] = page2;
        meshRenderer.materials = materials;

        maxPages = pages.Count;
        minPages = 0;

        // Subscribe to interaction events
        if (leftPoke != null)
        {
            leftPoke.selectEntered.AddListener(OnLeftPoke);
        }
        else
        {
            Debug.LogWarning("Left poke interactor is not assigned.");
        }

        if (rightPoke != null)
        {
            rightPoke.selectEntered.AddListener(OnRightPoke);
        }
        else
        {
            Debug.LogWarning("Right poke interactor is not assigned.");
        }

        SwitchPage(DisplayNumber);
    }

    public void SwitchPage(int pageNumber)
    {
        if (pageNumber < 0 || pageNumber + 1 >= pages.Count)
        {
            Debug.LogWarning("Invalid page number!");
            return;
        }

        DisplayNumber = pageNumber;

        page1 = pages[pageNumber];
        page2 = pages[pageNumber + 1];

        // Modify the materials array and reassign it
        Material[] materials = meshRenderer.materials;
        materials[2] = page1;
        materials[3] = page2;
        meshRenderer.materials = materials;
    }

    private void OnLeftPoke(SelectEnterEventArgs args)
    {
        Debug.Log("Left poke triggered.");
        SwitchPage(DisplayNumber - 2);
    }

    private void OnRightPoke(SelectEnterEventArgs args)
    {
        Debug.Log("Right poke triggered.");
        SwitchPage(DisplayNumber + 2);
    }

    private void OnDestroy()
    {
        // Unsubscribe from interaction events to avoid memory leaks
        if (leftPoke != null)
        {
            leftPoke.selectEntered.RemoveListener(OnLeftPoke);
        }

        if (rightPoke != null)
        {
            rightPoke.selectEntered.RemoveListener(OnRightPoke);
        }
    }
}