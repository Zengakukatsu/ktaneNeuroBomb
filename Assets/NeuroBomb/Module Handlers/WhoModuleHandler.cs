using System.Collections;
using System.Collections.Generic;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using System;
using NeuroSdk.Websocket;

public class WhoModuleHandler : BombModuleHandler {

	private static readonly string[] positions = {
		"top-left",
		"top-right",
		"middle-left",
		"middle-right",
		"bottom-left",
		"bottom-right"
	};

	private readonly WhosOnFirstComponent component;

	public WhoModuleHandler(WhosOnFirstComponent component)
	{
		this.component = component;
	}

	public override string GetContext()
	{
		string display = component.DisplayText.text;
		string display_info = string.IsNullOrEmpty(display)
			? "The display is blank."
			: "The display reads \"" + display + "\".";
		List<string> buttons = new List<string>();

		for (int i = 0; i < component.Buttons.Length; i++)
		{
			buttons.Add(positions[i] + " \"" + component.Buttons[i].Text.text + "\"");
		}

		return string.Format(
			"Who's on First is on stage {0} of 3. {1} The buttons are: {2}.",
			component.CurrentStage + 1,
			display_info,
			string.Join(", ", buttons.ToArray()));
	}

	public override void RegisterActions(ActionWindow window, BombManager manager)
	{
		if (StateIsReady()){RegisterWindow(window, manager);}
		else{manager.StartCoroutine(RegisterWhenReady(window, manager));}
	}

	private bool StateIsReady()
	{
		return component.ButtonsEmerged && component.CurrentDisplayTermIndex >= 0;
	}

	private void RegisterWindow(ActionWindow window, BombManager manager)
	{
		window
			.SetContext(GetContext())
			.AddAction(new ActionPressWhoButton(manager, component, this))
			.Register();
	}

	private IEnumerator RegisterWhenReady(ActionWindow window, BombManager manager)
	{
		while (!component.IsSolved && !manager.mission_ended && manager.focus == component && manager.module_window == window)
		{
			if (component.ButtonsEmerged && component.CurrentDisplayTermIndex >= 0)
			{
				RegisterWindow(window, manager);
				yield break;
			}
			yield return null;
		}
	}

	public static string GetPositionName(int index)
	{
		return positions[index];
	}
}

public class ActionPressWhoButton : BusyAction<Selectable> {

	private readonly WhosOnFirstComponent component;
	private readonly WhoModuleHandler handler;

	public ActionPressWhoButton(BombManager manager, WhosOnFirstComponent component, WhoModuleHandler handler) : base(manager)
	{
		this.component = component;
		this.handler = handler;
	}

	public override string Name {
		get { return "press_word"; }}
	protected override string Description {
		get {return ConfigHelper.Get("FALLBACK - Press one of the words currently shown on the module.", "action_descriptions", Name);}}
	protected override JsonSchema Schema {
		get {
			List<object> words = new List<object>();

			for (int i = 0; i < component.Buttons.Length; i++){
				words.Add(component.Buttons[i].Text.text);}

			return new JsonSchema {
				Type = JsonSchemaType.Object,
				Required = new List<string> {
					"word"
				},
				Properties =
					new Dictionary<string, JsonSchema> {
						{
							"word",
							new JsonSchema {
								Type = JsonSchemaType.String,
								Enum = words
							}
						}
					}
			};
		}
	}

protected override ExecutionResult ValidateAction(ActionJData action_data, out Selectable selectable)
{
	selectable = null;

	if (component.IsSolved){
		return ExecutionResult.Failure("The module is already solved.");}

	if (!component.ButtonsEmerged || component.CurrentDisplayTermIndex < 0){
		return ExecutionResult.Failure("The module is changing stages.");}

	if (action_data.Data == null || action_data.Data["word"] == null){
		return ExecutionResult.Failure("word was missing.");}

	string word = action_data.Data["word"].ToString();

	for (int i = 0; i < component.Buttons.Length; i++)
	{
		if (!string.Equals(component.Buttons[i].Text.text, word, StringComparison.OrdinalIgnoreCase)){
			continue;}

		selectable =
			component.Buttons[i].GetComponent<Selectable>();

		if (selectable == null){
			return ExecutionResult.Failure("That button is not selectable.");}

		return ExecutionResult.Success("Pressing \"" + component.Buttons[i].Text.text + "\"...");
	}

	return ExecutionResult.Failure("That word is not present on the module.");
}

	protected override void Execute(Selectable selectable)
	{
		manager.StartCoroutine(PressButton(selectable));
	}

	private IEnumerator PressButton(Selectable selectable)
	{
		manager.IsBusy = true;

		KeypadButton button = selectable.GetComponent<KeypadButton>();

		string position = WhoModuleHandler.GetPositionName(button.ButtonIndex);
		string label = button.Text.text;

		ResultTracker result = new ResultTracker(component);

		yield return manager.StartCoroutine(SelectableHelper.SelectInteract(selectable));

		yield return null;

		string outcome = result.Read();

		bool changing_layout =
			!component.IsSolved &&
			!manager.mission_ended &&
			manager.bomb != null &&
			!manager.bomb.HasDetonated;

		string message = string.Format(
			"Pressed the {0} button labeled \"{1}\". {2}",
			position,
			label,
			outcome);

		if (changing_layout){message += " The buttons on the module are shifting to a new layout.";}

		Context.Send(message);

		if (changing_layout && manager.focus == component){
			manager.MakeModuleWindow();}

		manager.IsBusy = false;
	}
}
