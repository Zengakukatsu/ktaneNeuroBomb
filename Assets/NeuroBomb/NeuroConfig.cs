using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// A lot of this is placeholder from my last version of the mod.

public static class NeuroConfig
{
    public const string 	GAME = "ktane";
	private const string 	PORT = "ws://127.0.0.1:8000";
	public const float		SELECT_DELAY = 0.25f;
	public const float		ROTATE_DURATION = 0.25f;
	public const string 	MAIN_MENU_CONTEXT = "You are at the KTANE main menu. Start a mission using start_mission.";
	public const string 	MISSION_CONTEXT = "A KTANE mission is active. Work with the manual user to defuse the bomb. Tell them what you see, ask them what to do, and follow their instructions. Use focus_module to choose a module. A mistake will give you a strike, and 3 strikes will make the bomb explode. Solve all modules to defuse the bomb!";
    public const string 	RESULT_CONTEXT = "The mission has ended. Use exit_to_menu to return to the main menu, or retry_mission to try again.";
	public const string 	ACTION_NOT_FOUND = "Action does not exist or is stale, removing action.";
	public static string getPort(){
		return PORT;
	}
}
