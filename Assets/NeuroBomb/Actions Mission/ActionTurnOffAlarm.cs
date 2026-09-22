using System.Collections;
using Assets.Scripts.Props;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Websocket;

public class ActionTurnOffAlarm : NeuroAction<Selectable> {

	private readonly BombManager manager;
	private readonly AlarmClock alarm;

	public ActionTurnOffAlarm(BombManager manager, AlarmClock alarm)
	{
		this.manager = manager;
		this.alarm = alarm;
	}

	public override string Name {
		get { return "turn_off_alarm"; }}
	protected override string Description {
		get { return ConfigHelper.Get("FALLBACK - Turn off the annoying alarm clock.", "action_descriptions", Name); }}
	protected override JsonSchema Schema {
		get { return new JsonSchema {Type = JsonSchemaType.Object};}}
	protected override ExecutionResult Validate(ActionJData action_data, out Selectable selectable)
	{
		selectable = null;

		if (manager.mission_ended){
			return ExecutionResult.Failure("The mission has ended.");}

		if (alarm == null || alarm.SnoozeButton == null){
			return ExecutionResult.Failure("The alarm clock button was not found.");}

		selectable =alarm.SnoozeButton.GetComponent<Selectable>();

		if (selectable == null){
			return ExecutionResult.Failure("The alarm clock button is not selectable.");}

		return ExecutionResult.Success("Turning off the alarm clock...");
	}

	protected override void Execute(Selectable selectable)
	{
		manager.StartCoroutine(TurnOff(selectable));
	}	

	private IEnumerator TurnOff(Selectable selectable)
	{
		yield return manager.StartCoroutine(SelectableHelper.SelectInteract(selectable));
		manager.StopAlarmHandling();
	}
}
