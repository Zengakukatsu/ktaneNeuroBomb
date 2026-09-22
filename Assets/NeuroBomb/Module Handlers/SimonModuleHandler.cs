using System.Collections;
using System.Collections.Generic;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;
using UnityEngine;

public class SimonModuleHandler : BombModuleHandler {

	private static readonly string[] colors = {
		"red",
		"blue",
		"green",
		"yellow"
	};

	private readonly SimonComponent component;

	public float glow_check_time;
	public float pause_time = -1f;

	public SimonModuleHandler(SimonComponent component)
	{
		this.component = component;
	}

	public override string GetContext()
	{
		return "The module has red, blue, green, and yellow buttons.";
	}

	public override void RegisterActions(ActionWindow window, BombManager manager)
	{
		glow_check_time = 0f;
		pause_time = -1f;

		window
            .SetContext(GetContext())
			.AddAction(new ActionPressSimonColor(manager, component, this))
			.SetPersistent()
            .Register();

		manager.StartCoroutine(WatchGlows(manager));
	}

	public static string GetColorName(int button_index)
	{
		return colors[button_index];
	}

	private IEnumerator WatchGlows(BombManager manager)
	{
		yield return null;

		while (!component.IsSolved && !manager.mission_ended)
		{
			if (manager.focus != component){yield break;}

			if (glow_check_time > 0f){glow_check_time -= Time.deltaTime;}

			if (pause_time > 0f){pause_time -= Time.deltaTime;}

			if (glow_check_time <= 0f)
			{
				for (int i = 0; i < component.buttons.Length; i++)
				{
					Animator animator = component.buttons[i].GetComponent<Animator>();
					AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

					if (state.shortNameHash != -1749317743) continue;

					Context.Send(
						"The " + colors[i] +
						" button flashed.",
						true);

					glow_check_time = 0.55f;
					pause_time = 1f;
					break;
				}
			}

			if (pause_time <= 0f && pause_time > -1f)
			{
				pause_time = -1f;
				Context.Send("There is a long pause.", true);
			}

			yield return null;
		}
	}
}

public class ActionPressSimonColor : BusyAction<Selectable> {
	private static readonly Dictionary<string, int>
		button_indices =
			new Dictionary<string, int>
			{
				{ "red", 0 },
				{ "blue", 1 },
				{ "green", 2 },
				{ "yellow", 3 }
			};

	private readonly SimonComponent component;
	private readonly SimonModuleHandler handler;

	public ActionPressSimonColor(BombManager manager, SimonComponent component, SimonModuleHandler handler) : base(manager)
	{
		this.component = component;
		this.handler = handler;
	}

	public override string Name {
		get { return "press_color"; }}
	protected override string Description {
		get {return ConfigHelper.Get("FALLBACK - Press one colored button on the Simon Says module.", "action_descriptions", Name);}}
	protected override JsonSchema Schema {
		get {
			return new JsonSchema {
				Type = JsonSchemaType.Object,
				Required = new List<string> {"color"},
				Properties =
					new Dictionary<string, JsonSchema> {
						{
							"color",
							new JsonSchema {
								Type =
									JsonSchemaType.String,
								Enum = new List<object> {
									"red",
									"blue",
									"green",
									"yellow"
								}
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

		if (action_data.Data == null || action_data.Data["color"] == null){
			return ExecutionResult.Failure("color was missing.");}

		string color = action_data.Data["color"].ToString();

		int button_index;

		if (!button_indices.TryGetValue(color, out button_index))
		{
			return ExecutionResult.Failure("color must be red, blue, green, or yellow.");
		}

		selectable = component.buttons[button_index].GetComponent<Selectable>();

		if (selectable == null){
			return ExecutionResult.Failure("That button is not selectable.");}

		return ExecutionResult.Success("Pressing " + color + "...");
	}

	protected override void Execute(Selectable selectable)
	{
		manager.StartCoroutine(PressColor(selectable));
	}

	private IEnumerator PressColor(Selectable selectable)
	{
		manager.IsBusy = true;

		handler.glow_check_time = 1.5f;
		handler.pause_time = -1f;

		SimonButton button = selectable.GetComponent<SimonButton>();
		string color = SimonModuleHandler.GetColorName(button.ButtonIndex);

		ResultTracker result = new ResultTracker(component);

		yield return manager.StartCoroutine(SelectableHelper.SelectInteract(selectable));
		yield return null;

		Context.Send("Pressed " + color + ". " + result.Read());
		manager.IsBusy = false;
	}
}
