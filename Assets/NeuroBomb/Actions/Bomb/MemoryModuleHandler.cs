using System.Collections;
using System.Collections.Generic;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;
using UnityEngine;

public class MemoryModuleHandler : BombModuleHandler {

	private readonly MemoryComponent component;

	public MemoryModuleHandler(MemoryComponent component)
	{
		this.component = component;
	}

	public override string GetContext()
	{
		return GetState();
	}

	public override void RegisterActions(ActionWindow window, BombManager manager)
	{
		window
            .SetContext(GetContext())
			.AddAction(new ActionPressMemoryButton(manager, component, this))
            .SetPersistent()
            .Register();
	}

	public string GetState()
	{
		List<string> labels = new List<string>();

		for (int i = 0; i < component.Buttons.Length; i++){
			labels.Add(component.Buttons[i].Text.text);}

		return string.Format(
			"Memory is on stage {0} of 5. " +
			"The display shows {1}. " +
			"The buttons from left to right are: {2}.",
			component.CurrentStage + 1,
			component.DisplayText.text,
			string.Join(", ", labels.ToArray()));
	}
}

public class ActionPressMemoryButton : BusyAction<Selectable> {

	private readonly MemoryComponent component;
	private readonly MemoryModuleHandler handler;

	public ActionPressMemoryButton(BombManager manager, MemoryComponent component, MemoryModuleHandler handler) : base(manager)
	{
		this.component = component;
		this.handler = handler;
	}

	public override string Name {
		get { return "press_memory_button"; }}
	protected override string Description {
		get {return ConfigHelper.Get("FALLBACK - Press a button by its position from left to right.", "action_descriptions", Name);}}
	protected override JsonSchema Schema {
		get {
			return new JsonSchema {
				Type = JsonSchemaType.Object,
				Required = new List<string> {
					"position"
				},
				Properties =
					new Dictionary<string, JsonSchema> {
						{
							"position",
							new JsonSchema {
								Type = JsonSchemaType.Integer,
								Minimum = 1,
								Maximum = 4
							}
						}
					}
			};
		}
	}

	protected override ExecutionResult ValidateAction(ActionJData action_data, out Selectable selectable)
	{
		selectable = null;

		if (!component.IsInputValid){
			return ExecutionResult.Failure("The module is changing stages. Try again in a moment.");}

		if (action_data.Data == null || action_data.Data["position"] == null)
		{
			return ExecutionResult.Failure("position was missing.");
		}

		int position = action_data.Data["position"].ToObject<int>();

		if (position < 1 || position > 4){
			return ExecutionResult.Failure("position must be between 1 and 4.");}

		selectable = component.Buttons[position - 1].GetComponent<Selectable>();

		if (selectable == null){
			return ExecutionResult.Failure("That button is not selectable.");}

		return ExecutionResult.Success("Pressing the button in position " +position + "...");
	}

	protected override void Execute(Selectable selectable)
	{
		manager.StartCoroutine(PressButton(selectable));
	}

	private IEnumerator PressButton(Selectable selectable)
	{
		manager.IsBusy = true;

		KeypadButton button = selectable.GetComponent<KeypadButton>();

		int position = button.ButtonIndex + 1;
		string label = button.Text.text;

		ResultTracker result = new ResultTracker(component);

		yield return manager.StartCoroutine(SelectableHelper.SelectInteract(selectable));

		yield return null;

		while (!component.IsInputValid && !component.IsSolved && !manager.mission_ended)
		{
			yield return null;
		}

		string message = string.Format(
			"Pressed position {0}, which was labeled {1}. {2}",
			position,
			label,
			result.Read());

		if (!component.IsSolved && !manager.mission_ended){
			message += " " + handler.GetState();}

		Context.Send(message);
		manager.IsBusy = false;
	}
}
