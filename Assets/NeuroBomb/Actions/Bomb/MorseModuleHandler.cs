using System.Collections;
using NeuroSdk.Actions;
using NeuroSdk.Messages.Outgoing;
using UnityEngine;
using System.Collections.Generic;
using NeuroSdk.Json;
using NeuroSdk.Websocket;

public class MorseModuleHandler : BombModuleHandler {

	private readonly MorseCodeComponent component;

	public MorseModuleHandler(MorseCodeComponent component)
	{
		this.component = component;
	}

	public override string GetContext()
	{
		return "The module has a flashing light. Dots, dashes, short pauses between letters, and long reset pauses between repetitions will be reported as they occur.";
	}

	public override void RegisterActions(ActionWindow window,BombManager manager)
	{
		window
			.SetContext(GetContext())
			.AddAction(new ActionSetMorseFrequency(manager, component))
			.AddAction(new ActionTransmitMorse(manager, component))
			.SetPersistent()
			.Register();

		window.StartCoroutine(WatchSignal(manager));
	}

	private IEnumerator WatchSignal(BombManager manager)
	{
		bool was_lit = component.LEDLit.activeSelf;
		float state_started = Time.time;
		bool complete_state = false;

		float signal_threshold = component.DotLength * 2f;
		float letter_threshold = component.DotLength * 2f;
		float reset_threshold = component.DotLength * (component.LetterSpaceDurationInUnits + component.WordSpaceDurationInUnits) / 2f;

		while (!component.IsSolved && !manager.mission_ended)
		{
			if (manager.focus != component){yield break;}

			bool is_lit = component.LEDLit.activeSelf;

			if (is_lit != was_lit)
			{
				float duration = Time.time - state_started;

				if (complete_state)
				{
					if (was_lit)
					{
						Context.Send(duration >= signal_threshold ? "You see a long flash. Dash." : "You see a short flash. Dot.", true);
					}
					else if (duration >= reset_threshold)
					{
						Context.Send("Long reset pause.",true);
					}
					else if (duration >= letter_threshold)
					{
						Context.Send("Short pause.",true);
					}
				}
				was_lit = is_lit;
				state_started = Time.time;
				complete_state = true;
			}
			yield return null;
		}
	}
}

public class MorseFrequencySelection {

	public Selectable Selectable;
	public int Presses;
}

public class ActionSetMorseFrequency : BusyAction<MorseFrequencySelection> {

	private static readonly string[] frequencies = {
		"3.505", "3.515", "3.522", "3.532",
		"3.535", "3.542", "3.545", "3.552",
		"3.555", "3.565", "3.572", "3.575",
		"3.582", "3.592", "3.595", "3.600"
	};

	private readonly MorseCodeComponent component;

	public ActionSetMorseFrequency(BombManager manager, MorseCodeComponent component) : base(manager)
	{
		this.component = component;
	}

	public override string Name {
		get { return "set_frequency"; }}
	protected override string Description {
		get { return "Adjust the displayed frequency without transmitting."; }}
	protected override JsonSchema Schema {
		get {
			return new JsonSchema {
				Type = JsonSchemaType.Object,
				Required = new List<string> {
					"frequency"
				},
				Properties =
					new Dictionary<string, JsonSchema> {
						{
							"frequency",
							new JsonSchema {
								Type = JsonSchemaType.String,
								Enum = new List<object> {
									"3.505", "3.515", "3.522", "3.532",
									"3.535", "3.542", "3.545", "3.552",
									"3.555", "3.565", "3.572", "3.575",
									"3.582", "3.592", "3.595","3.600"
								}
							}
						}
					}
			};
		}
	}

	protected override ExecutionResult ValidateAction(ActionJData action_data, out MorseFrequencySelection selection)
	{
		selection = null;

		if (action_data.Data == null || action_data.Data["frequency"] == null){
			return ExecutionResult.Failure("frequency was missing.");}

		string requested = action_data.Data["frequency"].ToString();
		int target_index = System.Array.IndexOf(frequencies, requested);

		if (target_index < 0){
			return ExecutionResult.Failure("That frequency is not available.");}

		int difference = target_index - component.CurrentFrequencyIndex;

		selection = new MorseFrequencySelection {
			Presses = Mathf.Abs(difference),
			Selectable = difference > 0
				? component.UpButton.GetComponent<Selectable>()
				: difference < 0
					? component.DownButton.GetComponent<Selectable>()
					: null
		};

		if (selection.Presses > 0 && selection.Selectable == null){
			return ExecutionResult.Failure("The frequency control is not selectable.");}

		return ExecutionResult.Success("Setting the frequency to " + requested + " MHz...");
	}

	protected override void Execute(MorseFrequencySelection selection)
	{
		manager.StartCoroutine(SetFrequency(selection));
	}

	private IEnumerator SetFrequency(MorseFrequencySelection selection)
	{
		manager.IsBusy = true;

		for (int i = 0; i < selection.Presses; i++)
		{
			yield return manager.StartCoroutine(SelectableHelper.SelectInteract(selection.Selectable));
		}

		yield return null;
		Context.Send("The displayed frequency is now " + frequencies[component.CurrentFrequencyIndex] + " MHz.");

		manager.IsBusy = false;
	}
}

public class ActionTransmitMorse : BusyAction<Selectable> {

	private readonly MorseCodeComponent component;

	public ActionTransmitMorse(BombManager manager, MorseCodeComponent component) : base(manager)
	{
		this.component = component;
	}

	public override string Name {
		get { return "transmit"; }}
	protected override string Description {
		get { return "Transmit the selected frequency. Only use when you have set the module to the correct frequency."; }}
	protected override JsonSchema Schema {
		get { return new JsonSchema { Type = JsonSchemaType.Object }; }
	}

	protected override ExecutionResult ValidateAction(ActionJData action_data, out Selectable selectable)
	{
		selectable = component.TransmitButton.GetComponent<Selectable>();

		if (selectable == null){
			return ExecutionResult.Failure("The transmit button is not selectable.");}

		return ExecutionResult.Success("Transmitting...");
	}

	protected override void Execute(Selectable selectable)
	{
		manager.StartCoroutine(Transmit(selectable));
	}

	private IEnumerator Transmit(Selectable selectable)
	{
		manager.IsBusy = true;

		ResultTracker result = new ResultTracker(component);

		yield return manager.StartCoroutine(SelectableHelper.SelectInteract(selectable));
		yield return null;
		Context.Send("Transmitted. " + result.Read());

		manager.IsBusy = false;
	}
}