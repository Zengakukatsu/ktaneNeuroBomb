using System.Collections;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using UnityEngine;

public class ActionRetryMission : NeuroAction<Selectable> {

	private readonly PostGameManager manager;
	private readonly ResultPage page;

	public ActionRetryMission(PostGameManager post_game_manager, ResultPage result_page)
	{
		manager = post_game_manager;
		page = result_page;
	}

	public override string Name {
		get { return "retry_mission"; }}
	protected override string Description {
		get { return ConfigHelper.Get("FALLBACK - Retry the mission that just ended.", "action_descriptions", Name); }}
	protected override JsonSchema Schema {
		get {
			return new JsonSchema {Type = JsonSchemaType.Object};
		}
	}

	protected override ExecutionResult Validate(ActionJData action_data, out Selectable selectable)
	{
		selectable = null;

		if (page == null || page.RetryButton == null){
			return ExecutionResult.Failure("This mission cannot be retried.");}

		selectable = page.RetryButton;

		return ExecutionResult.Success("Retrying the mission...");
	}

	protected override void Execute(Selectable selectable)
	{
		manager.StartCoroutine(IExecute(selectable));
	}

	private IEnumerator IExecute(Selectable selectable)
	{
		yield return manager.StartCoroutine(SelectableHelper.SelectFocus(selectable));
		Object.Destroy(manager);
	}
}
