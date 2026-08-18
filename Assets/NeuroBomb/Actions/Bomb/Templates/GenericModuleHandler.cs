using System.Collections.Generic;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;

// Handler used for modules not found in the registry.
// Basically deprecated, kinda just used it to see how children worked.

public class GenericModuleHandler : IBombModuleHandler {

	private readonly BombComponent component;

	public GenericModuleHandler(BombComponent component)
	{
		this.component = component;
	}

	private int GetChildCount()
	{
		Selectable selectable = component.GetComponent<Selectable>();
		return (selectable != null && selectable.Children != null) ? selectable.Children.Length : 0;
	}

	public string GetContext()
	{
		return string.Format(
			"This is a {0} module. There is no specific support for it yet, but it has {1} selectable part(s); use select_child to try interacting with one by index (0-based).",
			component.GetModuleDisplayName(),
			GetChildCount());
	}

	public void RegisterActions(ActionWindow window, BombManager manager)
	{
		int childCount = GetChildCount();

		if (childCount > 0)
		{
			window.AddAction(new ActionSelectChild(manager, component, childCount));
		}
	}
}

public class ActionSelectChild : BusyAction<int> {

	private readonly BombComponent component;
	private readonly int childCount;

	public ActionSelectChild(BombManager bombManager, BombComponent component, int childCount) : base(bombManager)
	{
		this.component = component;
		this.childCount = childCount;
	}

	public override string Name{
		get { return "select_child"; }}
	protected override string Description{
		get { return "Select and interact with one of this module's parts by index (0-based)."; }}
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
							Maximum = childCount - 1
						}
					}
				}
			};
		}
	}

	protected override ExecutionResult ValidateAction(
		ActionJData actionData,
		out int index)
	{
		index = -1;

		if (actionData.Data == null || actionData.Data["index"] == null)
		{
			return ExecutionResult.Failure("index was missing.");
		}

		int parsedIndex = actionData.Data["index"].ToObject<int>();

		if (parsedIndex < 0 || parsedIndex >= childCount)
		{
			return ExecutionResult.Failure(
				string.Format("index must be between 0 and {0}.", childCount - 1));
		}

		index = parsedIndex;
		return ExecutionResult.Success(string.Format("Selecting child {0}...", index));
	}

	protected override void Execute(int index)
	{
		Selectable selectable = component.GetComponent<Selectable>();
		Selectable target = selectable.Children[index];
	}
}
