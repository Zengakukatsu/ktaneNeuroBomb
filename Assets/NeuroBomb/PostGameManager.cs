using System.Collections;
using System.Text;
using NeuroSdk.Actions;
using UnityEngine;

public class PostGameManager : MonoBehaviour {

	private ActionWindow result_window;
	private ResultPage result_page;

	private void Start()
	{
		StartCoroutine(Init());
	}

	private IEnumerator Init()
	{
		while (result_page == null)
		{
			result_page = FindObjectOfType<ResultPage>();
			yield return null;
		}

		while (KTInputManager.Instance == null ||KTInputManager.Instance.RootSelectable != result_page.ParentSelectable())
		{
			yield return null;
		}

		result_page.ContinueButton.HandleInteract();
		yield return null;

		result_window = ActionWindow.Create(gameObject);
		result_window
			.SetContext(GetResultContext(result_page))
			.AddAction(new ActionRetryMission(this, result_page))
			.AddAction(new ActionReturnToMenu(this, result_page));
		result_window.Register();
	}

	private static string GetResultContext(ResultPage page)
	{
		StringBuilder context = new StringBuilder();

		ResultDefusedPage defused = page as ResultDefusedPage;
		ResultExplodedPage exploded = page as ResultExplodedPage;
		ResultMissionPage mission = page as ResultMissionPage;

		if (defused != null){
			context.Append("The bomb was defused.");
			AppendResult(context, "Time remaining", defused.RemainingTime.FullText);
			AppendResult(context, null, defused.NewBestTime.FullText);}
		else if (exploded != null){
			context.Append("The bomb exploded.");
			AppendResult(context, "Time remaining", exploded.RemainingTime.FullText);
			AppendResult(context, "Cause of explosion", exploded.CauseOfExplosion.FullText);}
		else{
			context.Append("The mission has ended.");}

		if (mission != null){
			AppendResult(context, "Initial time", mission.InitialTime.text);
			AppendResult(context, "Modules", mission.NumModules.text);
			AppendResult(context, "Strikes", mission.NumStrikes.text);}

		context.Append(" You may retry the mission or return to the menu.");

		return context.ToString();
	}

	private static void AppendResult(StringBuilder context, string label, string value)
	{
		if (string.IsNullOrEmpty(value)) return;

		context.Append(" ");

		if (!string.IsNullOrEmpty(label)){
			context.Append(label);
			context.Append(": ");}

		context.Append(value.Trim());

		if (!value.TrimEnd().EndsWith(".")){context.Append(".");}
	}

	private void OnDestroy()
	{
		StopAllCoroutines();

		if (result_window != null){
			result_window.End();
			result_window = null;}
	}
}