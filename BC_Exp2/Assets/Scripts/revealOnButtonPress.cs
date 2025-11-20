
using UnityEngine;

public class revealOnButtonPress : MonoBehaviour
{

        // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void reveal(){

        gameObject.layer = LayerMask.NameToLayer("Default");

        foreach (Transform child in gameObject.GetComponentsInChildren<Transform>())
         {
                 child.gameObject.layer = LayerMask.NameToLayer("Default");
         }

        
        Debug.Log("Revealed");
    }

    public void hide(){
        gameObject.layer = LayerMask.NameToLayer("DepthOnly");
        foreach (Transform child in gameObject.GetComponentsInChildren<Transform>())
         {
                 child.gameObject.layer = LayerMask.NameToLayer("DepthOnly");
         }

        Debug.Log("Hidden");
    }
}
