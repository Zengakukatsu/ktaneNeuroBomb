using System.Collections.Generic;
using System.Collections;
using System.Text;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;
using NeuroSdk.Messages.Outgoing;

public class WiresModuleHandler : IBombModuleHandler {

	private readonly WireSetComponent component;

	public WiresModuleHandler(WireSetComponent component)
	{
		this.component = component;
	}

	public string GetContext()
	{
		return "This action is a placeholder for testing Registry and Focus Module.";
	}

	public void RegisterActions(ActionWindow window, BombManager manager)
	{
		window.AddAction(new ActionCutWire(manager, component));
		window.SetPersistent();
	}
}

public class ActionCutWire : BusyAction<Selectable> {

	private readonly WireSetComponent component;

	public ActionCutWire(BombManager bombManager, WireSetComponent component) : base(bombManager)
	{
		this.component = component;
	}

	public override string Name{
		get { return "cut_wire"; }}
	protected override string Description{
		get { return "Cut the wire at the given index."; }}
	protected override JsonSchema Schema{
		get{
			return new JsonSchema{
				Type = JsonSchemaType.Object,
				Required = new List<string> { "index" },
				Properties = new Dictionary<string, JsonSchema>{
					{
						"index",
						new JsonSchema
						{
							Type = JsonSchemaType.Integer,
							Minimum = 0,
							Maximum = component.WireCount - 1
						}
					}
				}
			};
		}
	}

	protected override ExecutionResult ValidateAction(ActionJData actionData, out Selectable selectable)
	{
		selectable = null;

		if (actionData.Data == null || actionData.Data["index"] == null)
		{
			return ExecutionResult.Failure("index was missing.");
		}

		int index = actionData.Data["index"].ToObject<int>();

		if (index < 0 || index >= component.WireCount){
			return ExecutionResult.Failure(string.Format("index must be between 0 and {0}.", component.WireCount - 1));}

		selectable = component.wires[index].GetComponent<Selectable>();

		return ExecutionResult.Success(string.Format(
			"Cutting wire {0} ({1})...",
			index,
			component.GetColorOfWireIndex(index).ToString().ToLowerInvariant()));
	}

	protected override void Execute(Selectable selectable)
	{
		manager.StartCoroutine(IExecute(selectable));
	}

	private IEnumerator IExecute(Selectable selectable)
	{
		manager.IsBusy = true;

		yield return manager.StartCoroutine(SelectableHelper.SelectInteract(selectable));
		Context.Send("Wire has been cut.");

		manager.IsBusy = false;
	}
}