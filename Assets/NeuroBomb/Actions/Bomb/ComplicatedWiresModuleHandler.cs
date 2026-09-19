using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Components.VennWire;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;

public class ComplicatedWiresModuleHandler : BombModuleHandler {

	private readonly VennWireComponent component;

	public ComplicatedWiresModuleHandler(VennWireComponent component)
	{
		this.component = component;
	}

	public override string GetContext()
	{
		List<string> wires = new List<string>();

		for (int i = 0; i < component.ActiveWires.Length; i++)
		{
			wires.Add(GetWireDescription(component.ActiveWires[i], i + 1));
		}
		return "There are " + component.ActiveWires.Length + " wires from left to right " + string.Join("; ", wires.ToArray()) + ".";
	}

	public override void RegisterActions(ActionWindow window, BombManager manager)
	{
		window
			.SetContext(GetContext())
			.AddAction(new ActionCutComplicatedWire(manager, component))
			.SetPersistent()
			.Register();
	}

	public static string GetColorName(VennSnippableWire wire)
	{
		List<string> colors = new List<string>();

		if ((wire.Color & VennWireColor.Red) != 0){colors.Add("red");}
		if ((wire.Color & VennWireColor.Blue) != 0){colors.Add("blue");}
		if ((wire.Color & VennWireColor.White) != 0){colors.Add("white");}

		return colors.Count == 0
			? "uncolored"
			: string.Join(" and ", colors.ToArray());
	}

	private static string GetWireDescription(VennSnippableWire wire, int position)
	{
		return string.Format(
			"wire {0} is {1}, its LED is {2}, {3}",
			position,
			GetColorName(wire),
			wire.IsLEDOn ? "lit" : "unlit",
			wire.HasSymbol ? "it has a star. " : "it has no star. ");
	}
}

public class ActionCutComplicatedWire : BusyAction<Selectable> {

	private readonly VennWireComponent component;

	public ActionCutComplicatedWire(BombManager manager, VennWireComponent component) : base(manager)
	{
		this.component = component;
	}

	public override string Name {
		get { return "cut_wire"; }}
	protected override string Description {
		get {return "Cut an uncut wire by its numbered position from left to right.";}}
	protected override JsonSchema Schema {
		get {
			List<object> available_wires =
				new List<object>();

			for (int i = 0;
				i < component.ActiveWires.Length;
				i++)
			{
				if (!component.ActiveWires[i].Snipped){
					available_wires.Add(i + 1);}
			}

			return new JsonSchema {
				Type = JsonSchemaType.Object,
				Required = new List<string> {
					"index"
				},
				Properties =
					new Dictionary<string, JsonSchema> {
						{
							"index",
							new JsonSchema {
								Type = JsonSchemaType.Integer,
								Enum = available_wires
							}
						}
					}
			};
		}
	}

	protected override ExecutionResult ValidateAction(ActionJData action_data, out Selectable selectable)
	{
		selectable = null;

		if (action_data.Data == null || action_data.Data["index"] == null){
			return ExecutionResult.Failure("index was missing.");}

		int index = action_data.Data["index"].ToObject<int>();

		if (index < 1 || index > component.ActiveWires.Length){
			return ExecutionResult.Failure("That wire does not exist.");}

		VennSnippableWire wire = component.ActiveWires[index - 1];

		if (wire.Snipped){
			return ExecutionResult.Failure("That wire has already been cut.");}

		selectable = wire.GetComponent<Selectable>();

		if (selectable == null){
			return ExecutionResult.Failure("That wire is not selectable.");}

		return ExecutionResult.Success("Cutting wire " + index + "...");
	}

	protected override void Execute(Selectable selectable)
	{
		manager.StartCoroutine(CutWire(selectable));
	}

	private IEnumerator CutWire(Selectable selectable)
	{
		manager.IsBusy = true;

		VennSnippableWire wire = selectable.GetComponent<VennSnippableWire>();

		int position = wire.WireIndex + 1;

		string color = ComplicatedWiresModuleHandler.GetColorName(wire);

		ResultTracker result = new ResultTracker(component);

		yield return manager.StartCoroutine(SelectableHelper.SelectInteract(selectable));

		yield return null;

		Context.Send(string.Format(
			"Cut wire {0}, which was {1}. {2}",
			position,
			color,
			result.Read()));

		manager.IsBusy = false;
	}
}