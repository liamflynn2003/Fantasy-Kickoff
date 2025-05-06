using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class NetworkChecker : MonoBehaviour
{
    public GameObject noInternetPanel;
    private static NetworkChecker instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        DontDestroyOnLoad(gameObject);
        StartCoroutine(CheckConnectionLoop());
    }

    IEnumerator CheckConnectionLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(2f);
            bool isConnected = Application.internetReachability != NetworkReachability.NotReachable;

            noInternetPanel.SetActive(!isConnected);
        }
    }
}
