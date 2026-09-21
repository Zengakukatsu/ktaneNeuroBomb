using System.Collections;
using UnityEngine;

public static class SelectableHelper {

	// Interacts with a Selectable through SelectableManager to
	// set it as the games focused Selectable.
	public static IEnumerator SelectFocus(Selectable selectable)
	{
		selectable.HandleSelect(true);
		yield return new WaitForSeconds(ConfigHelper.Get(0.25f, "timing", "highlight_duration_seconds"));

		KTInputManager.Instance.SelectableManager.Select(selectable, false);
		KTInputManager.Instance.SelectableManager.HandleInteract();

		selectable.HandleDeselect(null);
	}

	// Interacts with a Selectable through it's HandleInteract()
	// without changing the games focused Selectable.
	public static IEnumerator SelectInteract(Selectable selectable)
	{
		selectable.HandleSelect(true);
		yield return new WaitForSeconds(ConfigHelper.Get(0.25f, "timing", "highlight_duration_seconds"));

		selectable.HandleInteract();

		selectable.HandleDeselect(null);
	}
}
