using System.Collections;
using Assets.Scripts.Platform;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;
using UnityEngine;

public class ActionSpinChair : BusyAction {

	public ActionSpinChair(BombManager bomb_manager) : base(bomb_manager)
	{
	}

	public override string Name {
		get { return "spin_in_chair"; }}
	protected override string Description {
		get { return ConfigHelper.Get("FALLBACK - Spin all the way around in the chair. It is fun!", "action_descriptions", Name); }}
	protected override JsonSchema Schema {
		get {
			return new JsonSchema {
				Type = JsonSchemaType.Object
			};
		}
	}

	protected override ExecutionResult ValidateAction(ActionJData action_data)
	{
		if (AbstractPlatformUtil.Instance == null || AbstractPlatformUtil.Instance.PlayerController == null){
			return ExecutionResult.Failure("You cannot spin right now.");}

		return ExecutionResult.Success("Spinning around in the chair...");
	}

	protected override void Execute()
	{
		manager.StartCoroutine(Spin());
	}

	private IEnumerator Spin()
	{
		manager.IsBusy = true;

		PlayerController player = AbstractPlatformUtil.Instance.PlayerController;

		Transform player_transform = player.transform;
		Quaternion start_rotation = player_transform.localRotation;

		float duration = ConfigHelper.Get(1.5f, "timing", "chair_spin_duration");
		float elapsed = 0f;

		while (elapsed < duration && !manager.mission_ended)
		{
			elapsed += Time.deltaTime;

			float progress = Mathf.Clamp01(elapsed / duration);
			float angle = 360f * Mathf.SmoothStep(0f, 1f, progress);

			player_transform.localRotation = start_rotation * Quaternion.AngleAxis(angle, Vector3.up);

			yield return null;
		}

		if (player_transform != null){player_transform.localRotation = start_rotation;}

		Context.Send("You finished spinning around in the chair.");

		manager.IsBusy = false;
	}
}
