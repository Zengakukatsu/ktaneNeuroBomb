using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NeuroConfig
{
    public const string 	GAME = "ktane";
	private const string 	PORT = "ws://127.0.0.1:8000";
	public const float		SELECT_DELAY = 0.25f;
	public const float		ROTATE_DURATION = 0.25f;
	public const string 	MAIN_MENU_CONTEXT = "You are at the KTANE main menu. Start a mission using start_mission.";
	public const string 	MISSION_CONTEXT = "A KTANE mission is active. Use focus to focus on modules. Solve all modules to defuse the bomb!";
    public const string 	RESULT_CONTEXT = "The mission has ended. Use exit_to_menu to return to the main menu, or retry_mission to try again.";
	public const string 	ACTION_NOT_FOUND = "Action does not exist or is stale, removing action.";
	public static string getPort(){
		return PORT;
	}
}
