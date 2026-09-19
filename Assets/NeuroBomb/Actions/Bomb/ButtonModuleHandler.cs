using System.Collections;
using System.Collections.Generic;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;
using UnityEngine;

public class ButtonModuleHandler : BombModuleHandler {

	private readonly ButtonComponent component;
	private int scheduled_release_digit = -1;

	public ButtonModuleHandler(ButtonComponent component)
	{
		this.component = component;
	}

	public override string GetContext()
	{
		return string.Format(
			"The button is {0} and says {1}. Use press_button to tap or hold it.",
			component.ButtonColor.ToString(),
			component.ButtonInstruction.ToString());
	}

	public override string GetStatus()
	{
		if (!component.IsHolding){return GetContext();}

		if (scheduled_release_digit < 0){
			return string.Format(
				"You are holding the button. The strip is {0}. No release has been scheduled.",
				component.IndicatorColor.ToString());}

		return string.Format(
			"You are holding the button. The strip is {0}. The button is scheduled to release the next time the timer contains {1}.",
			component.IndicatorColor.ToString(),
			scheduled_release_digit);
	}

	public override void RegisterActions(ActionWindow window, BombManager manager)
	{
		window
		    .SetContext(GetContext())
			.AddAction(new ActionPressButton(manager, component, this))
			.SetPersistent()
			.Register();
	}

	private static void OpenScheduleWindow(
		BombManager manager,
		ButtonComponent component,
		ButtonModuleHandler handler,
		string context)
	{
		ActionWindow window = ActionWindow.Create(manager.gameObject);

		window
			.SetContext(context)
			.AddAction(new ActionScheduleRelease(manager, component, handler))
			.SetEnd(() =>
				!component.IsHolding ||
				manager.bomb.HasDetonated ||
				manager.bomb.IsSolved())
			.Register();
	}

	private class ActionPressButton : BusyAction<string> {

		private readonly ButtonComponent component;
		private readonly ButtonModuleHandler handler;

		public ActionPressButton(
			BombManager manager,
			ButtonComponent component,
			ButtonModuleHandler handler) : base(manager)
		{
			this.component = component;
			this.handler = handler;
		}

		public override string Name{
			get { return "press_button"; }}
		protected override string Description{
			get { return "Tap the button or begin holding it."; }}
		protected override JsonSchema Schema{
			get{
				return new JsonSchema{
					Type = JsonSchemaType.Object,
					Required = new List<string> { "action" },
					Properties = new Dictionary<string, JsonSchema>{
						{
							"action",
							new JsonSchema{
								Type = JsonSchemaType.String,
								Enum = new List<object> { "tap", "hold" }
							}
						}
					}
				};
			}
		}

		protected override ExecutionResult ValidateAction(
			ActionJData actionData,
			out string action)
		{
			action = null;

			if (component.IsSolved)
				return ExecutionResult.Failure("The module is already solved.");

			if (component.IsHolding)
				return ExecutionResult.Failure("The button is already being held.");

			if (actionData.Data == null || actionData.Data["action"] == null)
				return ExecutionResult.Failure("action was missing.");

			action = actionData.Data["action"].ToString();

			if (action != "tap" && action != "hold")
				return ExecutionResult.Failure("action must be tap or hold.");

			if (component.button.GetComponent<Selectable>() == null)
				return ExecutionResult.Failure("The button is not selectable.");

			return ExecutionResult.Success(action == "tap"
				? "Pressing and immediately releasing the button..."
				: "Holding the button...");
		}

		protected override void Execute(string action)
		{
			if (action == "tap")
				manager.StartCoroutine(TapButton());
			else
				manager.StartCoroutine(HoldButton());
		}

		private IEnumerator TapButton()
		{
			manager.IsBusy = true;

			ResultTracker result = new ResultTracker(component);
			Selectable selectable = component.button.GetComponent<Selectable>();

			yield return manager.StartCoroutine(SelectableHelper.SelectInteract(selectable));
			yield return null;

			Context.Send("The button was tapped. " + result.Read());

			manager.IsBusy = false;
		}

		private IEnumerator HoldButton()
		{
			manager.IsBusy = true;
			handler.scheduled_release_digit = -1;

			Selectable selectable = component.button.GetComponent<Selectable>();

			selectable.HandleSelect(true);

			yield return new WaitForSeconds(NeuroConfig.SELECT_DELAY);

			selectable.HandleInteract();

			while (!component.IsHolding && !manager.bomb.HasDetonated && !manager.bomb.IsSolved())
			{
				yield return null;
			}

			if (!component.IsHolding)
			{
				Context.Send(manager.bomb.HasDetonated
					? "Bomb exploded!"
					: "Bomb defused!");

				manager.IsBusy = false;
				yield break;
			}

			string context = string.Format(
				"You are holding the button. The strip is {0}. " +
				"Wait for the manual user to determine which digit " +
				"must be present on the timer. Then use " +
				"schedule_release with that digit. The button will " +
				"remain held until the timer contains it.",
				component.IndicatorColor.ToString());

			OpenScheduleWindow(manager, component, handler, context);
		}
	}

	private class ActionScheduleRelease : NeuroAction<int> {

		private readonly BombManager manager;
		private readonly ButtonComponent component;
		private readonly ButtonModuleHandler handler;

		public ActionScheduleRelease(BombManager manager, ButtonComponent component, ButtonModuleHandler handler)
		{
			this.manager 	= manager;
			this.component 	= component;
			this.handler 	= handler;
		}

		public override string Name{
			get { return "schedule_release"; }}
		protected override string Description{
			get{
				return
					"Schedule the held button to release the next " +
					"time the timer display contains the chosen " +
					"digit. Only use this after the manual user " +
					"tells you the required digit.";
			}
		}
		protected override JsonSchema Schema{
			get{
				return new JsonSchema{
					Type = JsonSchemaType.Object,
					Required = new List<string> { "digit" },
					Properties =
						new Dictionary<string, JsonSchema>{
							{
								"digit",
								new JsonSchema{
									Type =
										JsonSchemaType.Integer,
									Minimum = 0,
									Maximum = 9
								}
							}
						}
				};
			}
		}

		protected override ExecutionResult Validate(ActionJData actionData, out int digit)
		{
			digit = -1;

			if (manager.mission_ended)
				return ExecutionResult.Failure("The mission has ended.");

			if (component.IsSolved)
				return ExecutionResult.Failure("The module is already solved.");

			if (!component.IsHolding)
				return ExecutionResult.Failure("The button is not being held.");

			if (actionData.Data == null || actionData.Data["digit"] == null)
				return ExecutionResult.Failure("digit was missing.");

			digit = actionData.Data["digit"].ToObject<int>();

			if (digit < 0 || digit > 9)
				return ExecutionResult.Failure("digit must be between 0 and 9.");

			return ExecutionResult.Success(string.Format(
				"Scheduling the button to release the next " +
				"time the timer contains {0}...",
				digit));
		}

		protected override void Execute(int digit)
		{
			handler.scheduled_release_digit = digit;
			manager.StartCoroutine(ReleaseOnDigit(digit));
		}

		private IEnumerator ReleaseOnDigit(int digit)
		{
			TimerComponent timer = manager.bomb.GetTimer();

			string initialDisplay = timer.GetFormattedTime(timer.TimeRemaining, false);
			string targetDigit = digit.ToString();

			while (component.IsHolding && !manager.bomb.HasDetonated && !manager.bomb.IsSolved())
			{
				string displayedTime = timer.GetFormattedTime(timer.TimeRemaining, false);

				if (displayedTime != initialDisplay && displayedTime.Contains(targetDigit))
					break;

				yield return null;
			}

			if (manager.bomb.HasDetonated || manager.bomb.IsSolved())
				yield break;

			if (!component.IsHolding){
				manager.IsBusy = false;
				yield break;}

			ResultTracker result = new ResultTracker(component);

			Selectable selectable = component.button.GetComponent<Selectable>();

			string releaseTime =timer.GetFormattedTime(timer.TimeRemaining, false);

			selectable.HandleDeselect(null);
			handler.scheduled_release_digit = -1;

			yield return null;

			Context.Send(string.Format(
				"The timer contained {0}. Released the button " +
				"when it displayed {1}. {2}",
				digit,
				releaseTime,
				result.Read()));

			manager.IsBusy = false;
		}
	}
}