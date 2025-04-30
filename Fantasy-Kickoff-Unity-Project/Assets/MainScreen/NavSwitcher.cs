using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavSwitcher : MonoBehaviour
{
    public GameObject[] panels; // Array to hold references to all UI panels
    public GameObject panelToShow; // The panel to show when this button is clicked

    // Method to switch to a specific panel
    public void SwitchPanel()
    {
        Debug.Log("SwitchPanel method called");

        // Hide all panels
        foreach (GameObject panel in panels)
        {
            Debug.Log("Hiding panel: " + panel.name);
            panel.SetActive(false);
        }

        // Show the selected panel
        if (panelToShow != null)
        {
            Debug.Log("Showing panel: " + panelToShow.name);
            panelToShow.SetActive(true);
        }
        else
        {
            Debug.LogError("Panel to show is not assigned");
        }
    }
}