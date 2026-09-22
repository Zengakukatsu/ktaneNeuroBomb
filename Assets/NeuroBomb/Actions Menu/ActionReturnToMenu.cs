using System.Collections;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using UnityEngine;

public class ActionReturnToMenu : NeuroAction<Selectable> {

	private readonly PostGameManager manager;
	private readonly ResultPage page;

	public ActionReturnToMenu(PostGameManager post_game_manager,ResultPage result_page)
	{
		manager = post_game_manager;
		page = result_page;
	}

	public override string Name {
		get { return "return_to_menu"; }}
	protected override string Description {
		get { return ConfigHelper.Get("FALLBACK - Return to the mission menu.", "action_descriptions", Name); }}
	protected override JsonSchema Schema {
		get {
			return new JsonSchema {Type = JsonSchemaType.Object};
		}
	}

	protected override ExecutionResult Validate(ActionJData action_data,out Selectable selectable)
	{
		selectable = null;

		if (page == null || page.ContinueButton == null){
			return ExecutionResult.Failure("The return-to-menu button is unavailable.");}

		selectable = page.ContinueButton;

		return ExecutionResult.Success("Returning to the mission menu...");
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
