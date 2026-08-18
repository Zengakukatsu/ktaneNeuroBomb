
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
			string context = GetMissionListContext(page);

			// This window allows Neuro to press prev/next or select a mission on the mission select page.
			window = ActionWindow.Create(gameObject);
			window
				.SetContext(context)
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
			string context = GetMissionDetailContext(page);

			window = ActionWindow.Create(gameObject);
			window
				.SetContext(context)
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

	// I need to make the Menu Manager use the global helpers instead of these Select things.

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

	private List<MissionTableOfContentsMissionEntry> GetAvailableMissions(MissionTableOfContentsPage page)
	{
		List<MissionTableOfContentsMissionEntry> missions = new List<MissionTableOfContentsMissionEntry>();
		foreach (MissionTableOfContentsEntry entry in page.Entries)
		{
			MissionTableOfContentsMissionEntry missionEntry = entry as MissionTableOfContentsMissionEntry;

			if (missionEntry == null) continue;

			Mission mission = MissionManager.Instance.GetMission(missionEntry.MissionID);

			if (mission != null && !mission.IsTutorial && missionEntry.IsUnlocked){
				missions.Add(missionEntry);}
		}
		return missions;
	}

	private string GetMissionDetailContext(MissionDetailPage page)
	{
		List<string> details = new List<string>();

		string title = "";
		if (page.TextTitle != null){
			if (page.TextTitle.SingleLine != null && page.TextTitle.SingleLine.gameObject.activeInHierarchy){
				title = page.TextTitle.SingleLine.text;}
			else if (page.TextTitle.DoubleLine != null){
				title = page.TextTitle.DoubleLine.text;}
		}
		
		if (!string.IsNullOrEmpty(title)){
			details.Add("Mission: " + title + ".");}

		if (page.TextDescription != null && !string.IsNullOrEmpty(page.TextDescription.text)){
			details.Add("Description: " + page.TextDescription.text.Replace("\n", " ").Replace("\r", ""));}

		if (page.TextTime != null && !string.IsNullOrEmpty(page.TextTime.text)){
			details.Add("Time: " + page.TextTime.text.Replace("\n", " ").Replace("\r", "") + ".");}

		if (page.TextModuleCount != null &&!string.IsNullOrEmpty(page.TextModuleCount.text)){
			details.Add(page.TextModuleCount.text.Replace("\n", " ").Replace("\r", "") + ".");}

		if (page.TextStrikes != null &&!string.IsNullOrEmpty(page.TextStrikes.text)){
			details.Add(page.TextStrikes.text.Replace("\n", " ").Replace("\r", "") + ".");}

		if (page.TextBestTime != null &&!string.IsNullOrEmpty(page.TextBestTime.text)){
			details.Add("Best time: " + page.TextBestTime.text.Replace("\n", " ").Replace("\r", "") + ".");}

		LeaderboardPage leaderboard = bomb_binder.LeaderboardPage;

		if (leaderboard != null){
			if (leaderboard.Subtitle != null && !string.IsNullOrEmpty(leaderboard.Subtitle.text)){
				details.Add("Leaderboard: " + leaderboard.Subtitle.text.Replace("\n", " ").Replace("\r", "") + ".");}

			List<string> entries = new List<string>();

			if (leaderboard.DisplayEntries != null){
				foreach (BombBinderLeaderboardEntry entry in leaderboard.DisplayEntries){
					if (entry == null) continue;
					if (entry.LeaderboardSelectable == null) continue;
					if (entry.LeaderboardSelectable.Entry == null) continue;
					if (entry.Rank == null || entry.Name == null || entry.Time == null) continue;

					entries.Add(string.Format(
						"{0}: {1}, {2}",
						entry.Rank.text,
						entry.Name.text,
						entry.Time.text));
				}
			}

			if (entries.Count > 0){
				details.Add(
					"Leaderboard entries: " +
					string.Join("; ", entries.ToArray()) +
					".");}
			else{
				details.Add("No leaderboard entries are currently available.");}
		}
		details.Add("You may start this mission or return to the mission list.");

		return string.Join(" ", details.ToArray());
	}

	private string GetMissionListContext(MissionTableOfContentsPage page)
	{
		List<string> available = new List<string>();
		int locked_count = 0;
		int mission_count = 0;

		foreach (MissionTableOfContentsEntry entry in page.Entries){
			MissionTableOfContentsMissionEntry mission_entry = entry as MissionTableOfContentsMissionEntry;

			if (mission_entry == null) continue;

			Mission mission = MissionManager.Instance.GetMission(mission_entry.MissionID);

			if (mission == null || mission.IsTutorial) continue;

			mission_count++;

			if (!mission_entry.IsUnlocked){
				locked_count++;
				continue;}

			string mission_name = mission_entry.EntryText.text;

			bool completed = mission_entry.CheckStamp != null && mission_entry.CheckStamp.activeSelf;

			if (completed){mission_name += " (completed)";}
			available.Add(mission_name);
		}

		List<string> context = new List<string>();

		context.Add("The binder is open with several pages of missions. ");

		if (available.Count > 0){
			context.Add("Available missions on this page: " + string.Join(", ", available.ToArray()) + ".");}

		if (locked_count > 0){
			context.Add(string.Format(
				"{0} mission{1} on this page {2} locked.",
				locked_count,
				locked_count == 1 ? "" : "s",
				locked_count == 1 ? "is" : "are"));}

		if (mission_count > 0 && locked_count == mission_count){
			context.Add(
				"All available missions are on previous pages.");}
		else{
			context.Add(
				"Select a listed mission to view its details, or flip the page to browse more missions.");}

		return string.Join(" ", context.ToArray());
	}
}
