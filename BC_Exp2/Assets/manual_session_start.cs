using UnityEngine;
using UXF;
using System.Collections.Generic;
using System.IO;


public class manual_session_start : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {   
        var session = GetComponent<Session>();

        string path = Path.Combine(Application.streamingAssetsPath, "interception_expansion.json");
        string json = File.ReadAllText(path);

        var settingsDict = MiniJSON.Json.Deserialize(json) as Dictionary<string, object>;
        UXF.Settings settings = new UXF.Settings(settingsDict);
    
        // Start the session manually
        session.Begin("Interception_Stereopsis_Exp", "test", 1, null,  settings);

        Debug.Log("Manual session started with settings from interception_expansion.json");
    }

}
