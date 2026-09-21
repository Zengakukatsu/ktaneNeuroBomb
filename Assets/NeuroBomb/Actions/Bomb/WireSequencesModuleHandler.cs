using System.Collections;
using System.Collections.Generic;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;

public class WireSequenceModuleHandler : BombModuleHandler {

	private readonly WireSequenceComponent component;

	public WireSequenceModuleHandler(WireSequenceComponent component)
	{
		this.component = component;
	}

	public override string GetContext()
	{
		return GetPanelDescription(component.currentPage);
	}

	public string GetAllContext()
	{
		List<string> previous_wires = new List<string>();
		List<string> current_wires = new List<string>();
		int current_start = component.currentPage * 3;

		for (int index = 0; index < current_start; index++){
			previous_wires.Add(GetPreviousWireDescription(index));}

		for (int index = current_start; index < current_start + 3; index++){
			current_wires.Add(GetWireDescription(index));}

		string current = "The current panel has: " + string.Join(", ", current_wires.ToArray());

		if (previous_wires.Count == 0){return current;}

		return "The previous panels had: " + string.Join(", ", previous_wires.ToArray()) +". " + current;
	}

	public override void RegisterActions(ActionWindow window, BombManager manager)
	{
		if (StateIsReady()){RegisterWindow(window, manager);}
		else{manager.StartCoroutine(RegisterWhenReady(window, manager));}
	}

	private bool StateIsReady()
	{
		return !component.IsChangingPage && component.CurrentPage != null && component.wireSequence != null;
	}

	private void RegisterWindow(ActionWindow window, BombManager manager)
	{
		window
			.SetContext(GetContext())
			.AddAction(new ActionCutSequenceWire(manager, component))
			.AddAction(new ActionSubmitSequencePanel(manager, component))
			.AddAction(new ActionCheckAllSequenceWires(manager, component, this))
			.SetPersistent()
			.Register();
	}

	private IEnumerator RegisterWhenReady(ActionWindow window, BombManager manager)
	{
		while (!component.IsSolved && !manager.mission_ended && manager.focus == component && manager.module_window == window)
		{
			if (StateIsReady())
			{
				RegisterWindow(window, manager);
				yield break;
			}

			yield return null;
		}
	}

	public string GetPanelDescription(int page)
	{
		List<string> wires = new List<string>();

		int start = page * 3;

		for (int index = start; index < start + 3; index++){
			wires.Add(GetWireDescription(index));}

		return string.Format(
			"This panel shows positions {0}-{1}. {2}.",
			start + 1,
			start + 3,
			string.Join("; ", wires.ToArray()));
	}

	private string GetWireDescription(int index)
	{
		int position = index + 1;

		if (index >= component.wireSequence.Count || component.wireSequence[index].NoWire){
			return position + " is empty";}

		WireSequenceComponent.WireConfiguration wire =component.wireSequence[index];

		return string.Format(
			"{0} is {1} and connected to {2}",
			position,
			wire.Color.ToString().ToLowerInvariant(),
			GetDestination(wire.To));
	}

	private string GetPreviousWireDescription(int index)
	{
		int position = index + 1;

		if (component.wireSequence[index].NoWire){
			return position + " is empty";}

		return position + " is " + component.wireSequence[index].Color.ToString();
	}

	private static string GetDestination(int destination)
	{
		return ((char)('A' + destination)).ToString();
	}
}

public class ActionCutSequenceWire : BusyAction<Selectable> {

	private readonly WireSequenceComponent component;

	public ActionCutSequenceWire(BombManager manager, WireSequenceComponent component) : base(manager)
	{
		this.component = component;
	}

	public override string Name {
		get { return "cut_wire"; }}
	protected override string Description {
		get { return ConfigHelper.Get("FALLBACK - Cut a wire by its numbered position on the current panel.", "action_descriptions", "cut_wire_sequences");}}
	protected override JsonSchema Schema {
		get { return new JsonSchema {
				Type = JsonSchemaType.Object,
				Required = new List<string> {
					"position"
				},
				Properties =
					new Dictionary<string, JsonSchema> {
						{
							"position",
							new JsonSchema {
								Type =
									JsonSchemaType.Integer,
								Minimum = component.currentPage * 3 + 1,
								Maximum = component.currentPage * 3 + 3
							}
						}
					}
			};
		}
	}

	protected override ExecutionResult ValidateAction(ActionJData action_data, out Selectable selectable)
	{
		selectable = null;

		if (action_data.Data == null || action_data.Data["position"] == null){
			return ExecutionResult.Failure("position was missing.");}

		int position = action_data.Data["position"].ToObject<int>();
		int minimum = component.currentPage * 3 + 1;
		int maximum = minimum + 2;

		if (position < minimum || position > maximum){
			return ExecutionResult.Failure(string.Format("position must be between {0} and {1}.", minimum, maximum));}

		int index = position - 1;

		if (index >= component.wireSequence.Count || component.wireSequence[index].NoWire){
			return ExecutionResult.Failure("There is no wire in that position.");}

		WireSequenceComponent.WireConfiguration wire = component.wireSequence[index];

		if (wire.IsSnipped || wire.Wire.Snipped){
			return ExecutionResult.Failure("That wire has already been cut.");}

		selectable = wire.Wire.GetComponent<Selectable>();

		if (selectable == null){
			return ExecutionResult.Failure("That wire is not selectable.");}

		return ExecutionResult.Success("Cutting the wire in position " + position + "...");
	}

	protected override void Execute(Selectable selectable)
	{
		manager.StartCoroutine(CutWire(selectable));
	}

	private IEnumerator CutWire(Selectable selectable)
	{
		manager.IsBusy = true;

		WireSequenceWire wire = selectable.GetComponent<WireSequenceWire>();

		WireSequenceComponent.WireConfiguration configuration = new WireSequenceComponent.WireConfiguration();

		for (int i = 0; i < component.wireSequence.Count; i++)
		{
			if (component.wireSequence[i].Wire != wire){continue;}
			configuration = component.wireSequence[i];
			break;
		}

		ResultTracker result = new ResultTracker(component);

		yield return manager.StartCoroutine(SelectableHelper.SelectInteract(selectable));

		yield return null;

		Context.Send(string.Format(
			"Cut the {0} wire connected to {1}. {2}",
			configuration.Color
				.ToString()
				.ToLowerInvariant(),
			((char)('A' + configuration.To)).ToString(),
			result.Read()));

		manager.IsBusy = false;
	}
}

public class ActionSubmitSequencePanel : BusyAction<Selectable> {

	private readonly WireSequenceComponent component;

	public ActionSubmitSequencePanel(BombManager manager, WireSequenceComponent component) : base(manager)
	{
		this.component = component;
	}

	public override string Name {
		get { return "submit_panel"; }}
	protected override string Description {
		get {return ConfigHelper.Get("FALLBACK - Submit the current panel and attempt to move to the next panel.", "action_descriptions", Name);}}
	protected override JsonSchema Schema {
		get {return new JsonSchema {
				Type = JsonSchemaType.Object
			};
		}
	}

	protected override ExecutionResult ValidateAction(ActionJData action_data, out Selectable selectable)
	{
		selectable = component.DownButton;

		if (selectable == null){
			return ExecutionResult.Failure("The panel cannot be submitted right now.");}

		return ExecutionResult.Success("Submitting panel " + (component.currentPage + 1) + "...");
	}

	protected override void Execute(Selectable selectable)
	{
		manager.StartCoroutine(SubmitPanel(selectable));
	}

	private IEnumerator SubmitPanel(Selectable selectable)
	{
		manager.IsBusy = true;

		int previous_page = component.currentPage;

		ResultTracker result = new ResultTracker(component);

		yield return manager.StartCoroutine( SelectableHelper.SelectInteract(selectable));

		bool advanced = component.currentPage != previous_page;
		bool replace_window =advanced && !component.IsSolved && !manager.mission_ended && manager.focus == component;

		if (replace_window){manager.module_window.End();}

		while (component.IsChangingPage && !manager.mission_ended){
			yield return null;}

		Context.Send(string.Format(
			"Submitted panel {0}. {1}",
			previous_page + 1,
			result.Read()));

		if (replace_window){manager.MakeModuleWindow();}

		manager.IsBusy = false;
	}
}

public class ActionCheckAllSequenceWires : BusyAction {

	private readonly WireSequenceComponent component;
	private readonly WireSequenceModuleHandler handler;

	public ActionCheckAllSequenceWires(BombManager manager, WireSequenceComponent component, WireSequenceModuleHandler handler) : base(manager)
	{
		this.component = component;
		this.handler = handler;
	}

	public override string Name {
		get { return "check_all_wires"; }}
	protected override string Description {
		get {return ConfigHelper.Get("FALLBACK - Review all wire panels that have already been reached.", "action_descriptions", Name);}}
	protected override JsonSchema Schema {
		get {return new JsonSchema {
				Type = JsonSchemaType.Object
			};
		}
	}

	protected override ExecutionResult ValidateAction(ActionJData action_data)
	{
		return ExecutionResult.Success("Checking all previously seen wire panels...");
	}

	protected override void Execute()
	{
		manager.StartCoroutine(CheckAllWires());
	}

	private IEnumerator CheckAllWires()
	{
		manager.IsBusy = true;

		int current_page = component.currentPage;

		for (int i = 0; i < current_page * 2 && !manager.mission_ended; i++)
		{
			Selectable button = i < current_page
				? component.UpButton
				: component.DownButton;

			yield return manager.StartCoroutine(SelectableHelper.SelectInteract(button));

			yield return null;

			while (component.IsChangingPage && !manager.mission_ended){
				yield return null;}
		}

		if (!manager.mission_ended)
		{
			string message = handler.GetAllContext() + ".";

			if (current_page >= 2){message += " That took a while. Try to remember the previous wires next time.";}

			Context.Send(message);
		}

		manager.IsBusy = false;
	}
}
