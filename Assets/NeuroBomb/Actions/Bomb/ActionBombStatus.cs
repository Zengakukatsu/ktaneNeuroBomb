using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;

public class ActionBombStatus : NeuroAction {

	private readonly BombManager manager;

	public ActionBombStatus(BombManager bomb_manager)
	{
		manager = bomb_manager;
	}

	public override string Name {
		get { return "bomb_status"; }}
	protected override string Description {
		get {return "Check the bomb's time, strikes, and module progress.";}}
	protected override JsonSchema Schema {
		get {
			return new JsonSchema {
				Type = JsonSchemaType.Object
			};
		}
	}

	protected override ExecutionResult Validate(
		ActionJData action_data)
	{
		if (manager.bomb == null){
			return ExecutionResult.Failure("The bomb was not found.");}

		TimerComponent timer = manager.bomb.GetTimer();

		if (timer == null){
			return ExecutionResult.Failure("The bomb timer was not found.");}

		int solved = manager.bomb.GetSolvedComponentCount();
		int total = manager.bomb.GetSolvableComponentCount();

		return ExecutionResult.Success(string.Format(
			"Time remaining: {0}. " +
			"Strikes: {1}/{2}. " +
			"Modules solved: {3}/{4}. " +
			"Modules remaining: {5}.",
			
			timer.GetFormattedTime(timer.TimeRemaining, false),
			manager.bomb.NumStrikes,
			manager.bomb.NumStrikesToLose,
			solved,
			total,
			total - solved));
	}

	protected override void Execute()
	{
	}
}