using System.Collections;
using System.Collections.Generic;
using System.Text;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;

public class WiresModuleHandler : BombModuleHandler
{
    private readonly WireSetComponent component;

    public WiresModuleHandler(WireSetComponent component)
    {
        this.component = component;
    }

	public override string GetContext(){
		StringBuilder context = new StringBuilder("The wires top to bottom are: ");

		for (int i = 0; i < component.WireCount; i++){
			if (i > 0) context.Append(", ");
			context.Append(component.wires[i].GetColor().ToString());
		}
		context.Append(". Use cut_wire to cut a wire at an index (1-" + component.WireCount + ")");

		return context.ToString();
	}

    public override void RegisterActions(ActionWindow window, BombManager manager)
    {
        window
            .SetContext(GetContext())
            .AddAction(new ActionCutWire(manager, component))
            .SetPersistent()
            .Register();
    }
}

public class ActionCutWire : BusyAction<Selectable>
{
    private readonly WireSetComponent component;

    public ActionCutWire(BombManager manager, WireSetComponent component) : base(manager)
    {
        this.component = component;
    }

    public override string Name{
        get { return "cut_wire"; }
    }

    protected override string Description{
        get{return "Cut a wire by its number from top to bottom.";}
    }

    protected override JsonSchema Schema{
        get{
            return new JsonSchema{
                Type = JsonSchemaType.Object,
                Required = new List<string> { "index" },
                Properties =
                    new Dictionary<string, JsonSchema>
                    {
                        {
                            "index",
                            new JsonSchema
                            {
                                Type = JsonSchemaType.Integer,
                                Minimum = 1,
                                Maximum = component.WireCount
                            }
                        }
                    }
            };
        }
    }

    protected override ExecutionResult ValidateAction(ActionJData actionData, out Selectable selectable)
    {
        selectable = null;

        if (actionData.Data == null || actionData.Data["index"] == null){
            return ExecutionResult.Failure(
                "index was missing.");
        }

        int wireNumber =
            actionData.Data["index"].ToObject<int>();

        if (wireNumber < 1 || wireNumber > component.WireCount){
            return ExecutionResult.Failure(
                string.Format(
                    "index must be between 1 and {0}.",
                    component.WireCount));
        }

        SnippableWire wire = component.wires[wireNumber - 1];

        if (wire.Snipped){
            return ExecutionResult.Failure(
                string.Format(
                    "Wire {0} has already been cut.",
                    wireNumber));
        }

        selectable = wire.GetComponent<Selectable>();

        if (selectable == null){
            return ExecutionResult.Failure(
        		"That wire is not selectable.");
        }

        return ExecutionResult.Success(
            string.Format(
                "Cutting wire {0} ({1})...",
                wireNumber,
                wire.GetColor().ToString()));
    }

    protected override void Execute(Selectable selectable)
    {
        manager.StartCoroutine(CutWire(selectable));
    }

    private IEnumerator CutWire(Selectable selectable)
    {
        manager.IsBusy = true;
        ResultTracker result =new ResultTracker(component);

        yield return manager.StartCoroutine(SelectableHelper.SelectInteract(selectable));
        yield return null;

        Context.Send("Wire has been cut. " + result.Read());
        manager.IsBusy = false;
    }
}