using System.Collections;
using System.Collections.Generic;
using System.Text;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;

public class PasswordModuleHandler : BombModuleHandler {

	private readonly PasswordComponent component;

	public PasswordModuleHandler(PasswordComponent component)
	{
		this.component = component;
	}

	public override string GetContext()
	{
		return string.Format(
			"The password module has five letter positions numbered 1 through 5 from left to right. " +
			"It currently displays {0}.",
			GetCurrentWord(component));
	}

	public override void RegisterActions(ActionWindow window, BombManager manager)
	{
		window
			.SetContext(GetContext())
			.AddAction(new ActionCheckPasswordLetters(manager, component))
			.AddAction(new ActionSetPasswordLetter(manager, component))
			.AddAction(new ActionSubmitPassword(manager, component))
			.SetPersistent()
			.Register();
	}

	public static string GetCurrentWord(PasswordComponent component)
	{
		StringBuilder word = new StringBuilder();
		foreach (CharSpinner spinner in component.Spinners){word.Append(spinner.GetCurrentChar());}
		return word.ToString();
	}

	public static string GetAvailableLetters(CharSpinner spinner)
	{
		List<string> letters = new List<string>();
		foreach (char letter in spinner.Options){letters.Add(letter.ToString());}
		return string.Join(", ", letters.ToArray());
	}
}

public class ActionCheckPasswordLetters : BusyAction<CharSpinner> {

	private readonly PasswordComponent component;

	public ActionCheckPasswordLetters(BombManager manager, PasswordComponent component) : base(manager)
	{
		this.component = component;
	}

	public override string Name {
		get { return "check_letters"; }}
	protected override string Description {
		get { return "Cycle through every available letter at one password position.";}}
	protected override JsonSchema Schema {
		get { return new JsonSchema {
				Type = JsonSchemaType.Object,
				Required = new List<string> { "position" },
				Properties = new Dictionary<string, JsonSchema> {
					{
						"position",
						new JsonSchema {
							Type = JsonSchemaType.Integer,
							Minimum = 1,
							Maximum = 5
						}
					}
				}
			};
		}
	}

	protected override ExecutionResult ValidateAction(ActionJData action_data, out CharSpinner spinner)
	{
		spinner = null;

		if (action_data.Data == null || action_data.Data["position"] == null){
			return ExecutionResult.Failure("position was missing.");}

		int position = action_data.Data["position"].ToObject<int>();

		if (position < 1 || position > component.Spinners.Count){
			return ExecutionResult.Failure("position must be between 1 and 5.");}

		spinner = component.Spinners[position - 1];

		if (spinner.DownButton.GetComponent<Selectable>() == null){
			return ExecutionResult.Failure("That password spinner is not selectable.");}

		return ExecutionResult.Success("Checking every letter at position " + position + "...");
	}

	protected override void Execute(CharSpinner spinner)
	{
		manager.StartCoroutine(CheckLetters(spinner));
	}

	private IEnumerator CheckLetters(CharSpinner spinner)
	{
		manager.IsBusy = true;

		int position = component.Spinners.IndexOf(spinner) + 1;

		Selectable selectable = spinner.DownButton.GetComponent<Selectable>();

		List<string> letters = new List<string>();

		letters.Add(spinner.GetCurrentChar().ToString());

		for (int i = 1; i < spinner.Options.Count; i++)
		{
			yield return manager.StartCoroutine(SelectableHelper.SelectInteract(selectable));
			letters.Add(spinner.GetCurrentChar().ToString());
		}

		yield return manager.StartCoroutine(SelectableHelper.SelectInteract(selectable));

		Context.Send(string.Format(
			"Position {0} cycled through these letters: {1}. " +
			"It returned to {2}. The complete display is {3}.",
			position,
			string.Join(", ", letters.ToArray()),
			spinner.GetCurrentChar(),
			PasswordModuleHandler.GetCurrentWord(component)));

		manager.IsBusy = false;
	}
}

public class ActionSetPasswordLetter :
	BusyAction<PasswordLetterSelection> {

	private readonly PasswordComponent component;

	public ActionSetPasswordLetter(BombManager manager, PasswordComponent component) : base(manager)
	{
		this.component = component;
	}

	public override string Name {
		get { return "set_letter"; }}
	protected override string Description {
		get { return "Rotate one password position to a desired available letter.";}}
	protected override JsonSchema Schema {
		get { return new JsonSchema {
				Type = JsonSchemaType.Object,
				Required = new List<string> {
					"position",
					"letter"
				},
				Properties =
					new Dictionary<string, JsonSchema> {
						{
							"position",
							new JsonSchema {
								Type = JsonSchemaType.Integer,
								Minimum = 1,
								Maximum = 5
							}
						},
						{
							"letter",
							new JsonSchema {
								Type = JsonSchemaType.String,
								MinLength = 1,
								MaxLength = 1,
								Pattern = "^[A-Z]$"
							}
						}
					}
			};
		}
	}

	protected override ExecutionResult ValidateAction(ActionJData action_data, out PasswordLetterSelection selection)
	{
		selection = null;

		if (action_data.Data == null || action_data.Data["position"] == null){
			return ExecutionResult.Failure("position was missing.");}

		if (action_data.Data["letter"] == null){
			return ExecutionResult.Failure("letter was missing.");}

		int position = action_data.Data["position"].ToObject<int>();

		if (position < 1 || position > component.Spinners.Count){
			return ExecutionResult.Failure("position must be between 1 and 5.");}

		string letter_text = action_data.Data["letter"].ToString().Trim();

		if (letter_text.Length != 1){
			return ExecutionResult.Failure("letter must be one character.");}

		char requested_letter = char.ToUpperInvariant(letter_text[0]);

		CharSpinner spinner = component.Spinners[position - 1];

		int target_index = -1;

		for (int i = 0; i < spinner.Options.Count; i++){
			if (char.ToUpperInvariant(spinner.Options[i]) == requested_letter){
				target_index = i;
				break;
			}
		}

		if (target_index < 0){
			return ExecutionResult.Failure(
				string.Format(
					"Position {0} cannot display {1}. " +
					"Its available letters are: {2}.",
					position,
					requested_letter,
					PasswordModuleHandler
						.GetAvailableLetters(spinner)));}

		char letter = spinner.Options[target_index];

		int current_index = spinner.Options.IndexOf(spinner.GetCurrentChar());

		int next_steps = (target_index - current_index + spinner.Options.Count) % spinner.Options.Count;
		int previous_steps = (current_index - target_index + spinner.Options.Count) % spinner.Options.Count;

		selection = new PasswordLetterSelection {
			Presses =
				next_steps <= previous_steps
					? next_steps
					: previous_steps,
			Selectable =
				next_steps <= previous_steps
					? spinner.DownButton.GetComponent<Selectable>()
					: spinner.UpButton.GetComponent<Selectable>()
		};

		if (selection.Presses > 0 && selection.Selectable == null){
			return ExecutionResult.Failure("That password spinner is not selectable.");}

		return ExecutionResult.Success(
			string.Format("Setting position {0} to {1}...", position, letter));
	}

	protected override void Execute(PasswordLetterSelection selection)
	{
		manager.StartCoroutine(SetLetter(selection));
	}

	private IEnumerator SetLetter(PasswordLetterSelection selection)
	{
		manager.IsBusy = true;

		for (int i = 0; i < selection.Presses; i++){
			yield return manager.StartCoroutine(SelectableHelper.SelectInteract(selection.Selectable));
		}

		yield return null;

		KeypadButton button =selection.Selectable.GetComponent<KeypadButton>();

		CharSpinner spinner = button == null ? null : button.ParentComponent as CharSpinner;

		if (spinner == null)
		{
			Context.Send(
				"The letter was changed, but its slot could not be identified. " +
				"The password now displays " +
				PasswordModuleHandler.GetCurrentWord(component) +
				".");

			manager.IsBusy = false;
			yield break;
		}

		int slot = component.Spinners.IndexOf(spinner) + 1;

		Context.Send(string.Format(
			"Letter {0} selected in slot {1}. " +
			"The password now displays {2}.",
			spinner.GetCurrentChar(),
			slot,
			PasswordModuleHandler.GetCurrentWord(component)));

		manager.IsBusy = false;
	}
}

public class ActionSubmitPassword : BusyAction<Selectable> {

	private readonly PasswordComponent component;

	public ActionSubmitPassword(BombManager manager, PasswordComponent component) : base(manager)
	{
		this.component = component;
	}

	public override string Name {
		get { return "submit_password"; }}
	protected override string Description {
		get { return "Submit the currently displayed password."; }}
	protected override JsonSchema Schema {
		get { return new JsonSchema { Type = JsonSchemaType.Object }; }}

	protected override ExecutionResult ValidateAction(ActionJData action_data, out Selectable selectable)
	{
		selectable = null;

		selectable = component.SubmitButton.GetComponent<Selectable>();

		if (selectable == null){
			return ExecutionResult.Failure("The submit button is not selectable.");}

		return ExecutionResult.Success("Submitting " + PasswordModuleHandler.GetCurrentWord(component) + "...");
	}

	protected override void Execute(Selectable selectable)
	{
		manager.StartCoroutine(Submit(selectable));
	}

	private IEnumerator Submit(Selectable selectable)
	{
		manager.IsBusy = true;

		string word = PasswordModuleHandler.GetCurrentWord(component);
		ResultTracker result = new ResultTracker(component);

		yield return manager.StartCoroutine(SelectableHelper.SelectInteract(selectable));
		yield return null;

		Context.Send("Submitted " + word + ". " + result.Read());

		manager.IsBusy = false;
	}
}

public class PasswordLetterSelection {
	public Selectable Selectable;
	public int Presses;
}