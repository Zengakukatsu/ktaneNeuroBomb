
using NeuroSdk.Actions;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Missions;

public class MenuManager : MonoBehaviour {

	private ActionWindow 	window;
	public	BombBinder		bomb_binder;

	void Start (){
    	StartCoroutine(Init());
	}

    private IEnumerator Init()
    {
        while (bomb_binder == null)
        {
            bomb_binder = FindObjectOfType<BombBinder>();
            yield return null;
        }
        Debug.Log("[NeuroBomb] Found BombBinder: " + bomb_binder.name);
        ShowPage();
	}

	public void ShowPage(){

        if (bomb_binder == null){
		    Debug.Log("[NeuroBomb] bomb_binder became null unexpectedly.");
			return;
		}

		// If binder is closed
		// Registers: open_binder
		if (!IsBinderOpen())
		{
			window = ActionWindow.Create(gameObject);
			window
				.SetContext("You are in an office at a large wooden desk. There are several scattered binders and folders. To your right is a whiteboard and to your left is a metal cabinet. The room has a few boxes scattered on the floor. The bomb binder in front of you contains bomb defusal missions.")
				.AddAction(new ActionOpenBinder(this, bomb_binder));
			window.Register();
			return;
		}

		// If mission select page is open
		// Registers: select_mission, flip_page
        if (bomb_binder.MissionTableOfContentsPageManager != null && bomb_binder.MissionTableOfContentsPageManager.gameObject.activeSelf)
        {
			MissionTableOfContentsPage page = bomb_binder.MissionTableOfContentsPageManager.CurrentToCPage;
			List<MissionTableOfContentsMissionEntry> availableMissions = GetAvailableMissions(page);
			ActionSelectMission selectMission = new ActionSelectMission(this, availableMissions);

			// This window allows Neuro to press prev/next or select a mission on the mission select page.
			window = ActionWindow.Create(gameObject);
			window
				.SetContext(
					"Inside the binder are multiple pages of missions to select. " + 
					"select_mission for more details, or flip_page to browse more Missions!")
				.AddAction(new ActionFlipPage(this, page));
				// Only register select_mission if the schema is not empty.
				if (availableMissions.Count > 0)
				{
					window.AddAction(selectMission);
				}
			window.Register();
            return;
        }

		// If Mission detail page is open
		// Registers: start_mission, return_to_list
		if (bomb_binder.MissionDetailPage != null && bomb_binder.MissionDetailPage.gameObject.activeSelf)
		{
			MissionDetailPage page = bomb_binder.MissionDetailPage;

			// This window allows Neuro to start a mission from the mission detail page or return to mission select.
			window = ActionWindow.Create(gameObject);
			window
				.SetContext(
					"You are viewing mission details. You may start the mission or return to the mission list.")
				.AddAction(new ActionStartMission(this, page))
				.AddAction(new ActionReturnToList(this, page));
			window.Register();
			return;
		}

		// Recover from an unexpected binder state by releasing the current binder selection.
		// ShowPage() will then expose open_binder for the resulting state.
		KTInputManager.Instance.LetGo();
		RefreshPage();
		return;
	}

	public void RefreshPage()
	{
		// Wait one frame so the action result is sent before the next window is registered.
		StartCoroutine(RefreshPageNextFrame());
	}

	private IEnumerator RefreshPageNextFrame()
	{
		yield return null;
		ShowPage();
	}

	//====================
	//      Helpers     ||
	//====================

	public void Select(Selectable selectable, bool refreshPage)
	{
		StartCoroutine(SelectCoroutine(selectable, refreshPage));
	}

	private IEnumerator SelectCoroutine(Selectable selectable, bool refreshPage)
	{
		selectable.HandleSelect(true);
		yield return new WaitForSeconds(NeuroConfig.SELECT_DELAY);

		KTInputManager.Instance.SelectableManager.Select(selectable,false);
		KTInputManager.Instance.SelectableManager.HandleInteract();

		selectable.HandleDeselect(null);

		if (refreshPage)RefreshPage();
	}

	private bool IsBinderOpen()
	{
		return bomb_binder != null &&
			bomb_binder.FrontCoverOccluder != null &&
			bomb_binder.FrontCoverOccluder.gameObject.activeSelf;
	}

	private List<MissionTableOfContentsMissionEntry> GetAvailableMissions(
		MissionTableOfContentsPage page)
	{
		List<MissionTableOfContentsMissionEntry> missions = new List<MissionTableOfContentsMissionEntry>();
		foreach (MissionTableOfContentsEntry entry in page.Entries)
		{
			MissionTableOfContentsMissionEntry missionEntry = entry as MissionTableOfContentsMissionEntry;

			if (missionEntry == null) continue;

			Mission mission = MissionManager.Instance.GetMission(missionEntry.MissionID);

			if (mission != null &&
				!mission.IsTutorial &&
				missionEntry.IsUnlocked)
			{
				missions.Add(missionEntry);
			}
		}
		return missions;
	}
}
