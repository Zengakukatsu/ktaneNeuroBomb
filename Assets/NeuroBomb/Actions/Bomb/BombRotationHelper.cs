using System.Collections;
using Assets.Scripts.Input;
using UnityEngine;

public static class BombRotationHelper {

	// Bomb rotation helper sets transform each frame with a tween
	// to appear somewhat natural.

	// Things like looking at the opposite face require a manual rotation
	// because the focus animation will only go to the currently visible face.
	// This is not an issue for players, but since Neuro can select the back face
	// we need to rotate it for her before the focus selection.

	// The rotation time must be within the highlight delay to prevent some glitchy visuals.

	public static IEnumerator PeekSide(string side)
	{
		SelectableManager selectable_manager = KTInputManager.Instance.SelectableManager;

		if (side == "left" || side == "right")
		{
			float start_spin = selectable_manager.GetZSpin();
			float target_spin = side == "left" ? 270f : 90f;

			yield return BombSetZSpin(target_spin, NeuroConfig.ROTATE_DURATION);

			yield return new WaitForSeconds(0.5f);

			yield return BombSetZSpin(start_spin, NeuroConfig.ROTATE_DURATION);

			yield break;
		}

		if (side == "top" || side == "bottom")
		{
			float start_roll = selectable_manager.GetHeldObjectTiltEulerAngles().x;
			float target_roll = side == "top" ? -90f : 90f;

			yield return BombSetRoll(target_roll, NeuroConfig.ROTATE_DURATION);

			yield return new WaitForSeconds(0.5f);

			yield return BombSetRoll(start_roll, NeuroConfig.ROTATE_DURATION);
		}
	}

	private static IEnumerator BombSetZSpin(
		float target_spin,
		float duration)
	{
		SelectableManager selectable_manager = KTInputManager.Instance.SelectableManager;

		float start_spin = selectable_manager.GetZSpin();

		Vector3 start_tilt = selectable_manager.GetHeldObjectTiltEulerAngles();
		Vector3 end_tilt = start_tilt;

		end_tilt.z -= start_spin - target_spin;

		Quaternion start_rotation = selectable_manager.GetBaseHeldObjectTransform().rotation * Quaternion.Euler(start_tilt);
		Quaternion end_rotation = selectable_manager.GetBaseHeldObjectTransform().rotation * Quaternion.Euler(end_tilt);

		float elapsed = 0f;

		while (elapsed < duration)
		{
			yield return new WaitForEndOfFrame();

			elapsed += Time.deltaTime;
			float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
			selectable_manager.SetControlsRotation(Quaternion.Slerp(start_rotation, end_rotation, t));
		}

		selectable_manager.SetHeldObjectTiltEulerAngles(end_tilt);
		selectable_manager.SetZSpin(target_spin);
		selectable_manager.SetControlsRotation(end_rotation);
		selectable_manager.HandleFaceSelection();
	}

	private static IEnumerator BombSetRoll(float target_roll, float duration)
	{
		SelectableManager selectable_manager = KTInputManager.Instance.SelectableManager;

		Vector3 start_tilt = selectable_manager.GetHeldObjectTiltEulerAngles();
		Vector3 end_tilt = start_tilt;

		end_tilt.x = target_roll;

		Quaternion start_rotation = selectable_manager.GetBaseHeldObjectTransform().rotation * Quaternion.Euler(start_tilt);
		Quaternion end_rotation = selectable_manager.GetBaseHeldObjectTransform().rotation * Quaternion.Euler(end_tilt);

		float elapsed = 0f;

		while (elapsed < duration)
		{
			yield return new WaitForEndOfFrame();

			elapsed += Time.deltaTime;
			float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
			selectable_manager.SetControlsRotation(Quaternion.Slerp(start_rotation, end_rotation, t));
		}
		selectable_manager.SetHeldObjectTiltEulerAngles(end_tilt);
		selectable_manager.SetControlsRotation(end_rotation);
	}
}