using System.Collections;
using System.Collections.Generic;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;

public class ActionFocusModule : BusyAction<Selectable> {

	private readonly Dictionary<string, BombComponent> available_modules;

	public ActionFocusModule(BombManager bomb_manager) : base(bomb_manager)
	{
		available_modules = new Dictionary<string, BombComponent>();

		foreach (ModuleInfo info in bomb_manager.modules)
		{
			if (info.component == bomb_manager.focus) continue;
			available_modules[info.name] = info.component;
		}
	}

	public override string Name { get { return "focus_module"; } }
	protected override string Description { get { return "Look at and focus a module on the bomb. Allows you to perform that module's actions."; } }
	protected override JsonSchema Schema{
		get{
			List<object> names = new List<object>();
			foreach (string name in available_modules.Keys) names.Add(name);

			return new JsonSchema{
				Type = JsonSchemaType.Object,
				Required = new List<string> { "module_name" },
				Properties = new Dictionary<string, JsonSchema>{
					{ "module_name", new JsonSchema { Type = JsonSchemaType.String, Enum = names } }}
			};
		}
	}

	protected override ExecutionResult ValidateAction(ActionJData action_data, out Selectable selectable)
	{
		selectable = null;
		string module_name = null;

		if (action_data.Data != null && action_data.Data["module_name"] != null){
			module_name = action_data.Data["module_name"].ToString();}

		if (string.IsNullOrEmpty(module_name)){
			return ExecutionResult.Failure("module_name was null.");}

		BombComponent component;

		if (!available_modules.TryGetValue(module_name, out component)){
			return ExecutionResult.Failure(string.Format("Module: {0} was not available.", module_name));}

		selectable = component.GetComponent<Selectable>();

		if (selectable == null){
			return ExecutionResult.Failure(string.Format("Module: {0} isn't currently selectable.", module_name));}

		return ExecutionResult.Success(string.Format("Focusing {0}...", module_name));
	}

	protected override void Execute(Selectable selectable)
	{
		manager.StartCoroutine(IExecute(selectable));
	}

	private IEnumerator IExecute(Selectable selectable)
	{
		manager.IsBusy = true;
	
		manager.focus_window.End();
		if(manager.module_window != null){
			manager.module_window.End();
		}

		yield return manager.StartCoroutine(SelectableHelper.SelectFocus(selectable));

		manager.focus = selectable.GetComponent<BombComponent>();

		manager.MakeFocusWindow();
		manager.MakeModuleWindow();

		manager.IsBusy = false;
	}
}
