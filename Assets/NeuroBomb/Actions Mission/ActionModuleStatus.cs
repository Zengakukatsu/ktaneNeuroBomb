using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;

public class ActionModuleStatus : NeuroAction {

	private readonly BombManager manager;

	public ActionModuleStatus(BombManager manager)
	{
		this.manager = manager;
	}

	public override string Name {
		get { return "check_current_module_status"; }}
	protected override string Description {
		get { return ConfigHelper.Get("FALLBACK - Check the current state of the focused module.", "action_descriptions", Name); }}
	protected override JsonSchema Schema {
		get { return new JsonSchema {Type = JsonSchemaType.Object};}}

	protected override ExecutionResult Validate(ActionJData action_data)
	{
		if (manager.mission_ended){
			return ExecutionResult.Failure("The mission has ended.");}

		if (manager.focus == null){
			return ExecutionResult.Failure("No module is currently focused.");}

		foreach (ModuleInfo info in manager.modules){
			if (info.component != manager.focus) continue;

			string status = info.component.IsSolved
				? "The focused module, " + info.name + ", is solved."
				: info.handler.GetStatus();

			return ExecutionResult.Success(status);
		}

		return ExecutionResult.Failure("The focused module could not be found.");
	}

	protected override void Execute()
	{

	}
}
