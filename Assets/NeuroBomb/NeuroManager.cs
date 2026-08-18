using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NeuroManager : MonoBehaviour {

    private MenuManager menu_manager;
    private BombManager bomb_manager;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

	private void Start () 
	{
	}

    private void OnDestroy()
    {
    }
	
	private void Update ()
	{
	}

    void OnLevelWasLoaded(int scene){
        BuildManagerForScene(scene);
    }

	private void BuildManagerForScene(int scene){
        if(scene == 3){

            //Scene 3 is the office. MenuManager is created.
            Debug.Log("[NeuroBomb] Creating MenuManager.");
            menu_manager = gameObject.AddComponent<MenuManager>();
        }
        if(scene == 4){

            //Scene 4 is the mission. BombManager is created.
            Debug.Log("[NeuroBomb] Creating BombManager.");
            bomb_manager = gameObject.AddComponent<BombManager>();
        }
    }
}
