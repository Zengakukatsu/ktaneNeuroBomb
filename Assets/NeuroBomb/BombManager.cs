using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Input;
using Assets.Scripts.Missions;
using NeuroSdk.Actions;
using NeuroSdk.Messages.Outgoing;
using UnityEngine;
using Assets.Scripts.Props;
using Events;

public class BombManager : MonoBehaviour {

	public Bomb bomb { get; private set; }
	public bool IsBusy { get; set; }

	public List<ModuleInfo> modules { get; private set; }
	public ActionWindow module_window;
	public ActionWindow global_window;
	public BombComponent focus;
	public bool mission_ended;

	private AlarmClock alarm_clock;
	private bool alarm_is_on;

	private ActionWindow alarm_window;
	private Coroutine alarm_coroutine;

	private void Start()
	{
		StartCoroutine(Init());
	}

	private void Update()
	{
		if (mission_ended || bomb == null) return;
		if (!bomb.HasDetonated && !bomb.IsSolved()) return;

		mission_ended = true;
		IsBusy = true;

		EndAllWindows();
		Destroy(GetComponent<PostGameManager>());

		Context.Send(bomb.HasDetonated
			? "The bomb exploded. The mission is over."
			: "The bomb was defused. The mission is complete.");

		if (GetComponent<PostGameManager>() == null){gameObject.AddComponent<PostGameManager>();}
	}

	private IEnumerator Init()
	{
		GameplayState gameplayState = null;

		while (gameplayState == null){
			gameplayState = FindObjectOfType<GameplayState>();
			yield return null;}
		while (!gameplayState.RoundStarted){
			yield return null;}

		bomb = gameplayState.Bomb;
		Debug.Log("[NeuroBomb] Found Bomb: " + bomb.name);

		FloatingHoldable holdable = bomb.GetComponent<FloatingHoldable>();
		KTInputManager.Instance.SelectableManager.Hold(holdable);

		alarm_clock = FindObjectOfType<AlarmClock>();
		EnvironmentEvents.OnAlarmClockChange += AlarmClockChanged;

		Populate();
		SendBombContext();
		MakeGlobalWindow();

		Debug.Log("[NeuroBomb] Bomb populated with " + bomb.BombComponents.Count + " component(s).");
	}

	private void Populate()
	{
		modules = new List<ModuleInfo>();

		List<BombComponent> ordered = new List<BombComponent>();
		Dictionary<string, int> name_counts = new Dictionary<string, int>();
		foreach (BombComponent component in bomb.BombComponents){
			if (component.ComponentType == ComponentTypeEnum.Timer) continue;
			if (component.ComponentType == ComponentTypeEnum.Empty) continue;

			ordered.Add(component);

			string base_name = component.GetModuleDisplayName().ToLowerInvariant();
			int count;
			name_counts.TryGetValue(base_name, out count);
			name_counts[base_name] = count + 1;
		}

		Dictionary<string, int> seen = new Dictionary<string, int>();
		foreach (BombComponent component in ordered){
			string base_name = component.GetModuleDisplayName().ToLowerInvariant();
			string name = base_name;

			if (name_counts[base_name] > 1){
				int index;
				seen.TryGetValue(base_name, out index);
				index += 1;
				seen[base_name] = index;
				name = string.Format("{0}_{1}", base_name, index);}

			IBombModuleHandler handler = ModuleHandlerRegistry.Create(component);
			modules.Add(new ModuleInfo(component, handler, name));
		}
	}

	public void MakeGlobalWindow()
	{
		global_window = ActionWindow.Create(gameObject);
		global_window
			.SetPersistent()
			.SetContext(NeuroConfig.MISSION_CONTEXT)
			.AddAction(new ActionSpinChair(this))
			.AddAction(new ActionCheckSides(this))
			.AddAction(new ActionBombStatus(this))
			.AddAction(new ActionModuleStatus(this))
			.AddAction(new ActionFocusModule(this))
			.Register();
	}

	public void MakeModuleWindow()
	{
		foreach (ModuleInfo info in modules){
			if (info.component != focus) continue;

			module_window = ActionWindow.Create(gameObject);
			info.handler.RegisterActions(module_window, this);
			return;
		}
	}

	private void SendBombContext()
	{
		List<string> names = new List<string>();
		foreach (ModuleInfo info in modules) names.Add(info.name);
		Context.Send("The bomb has these modules: " + string.Join(", ", names.ToArray()) + ".");
	}

	private void OnDestroy()
	{
		EnvironmentEvents.OnAlarmClockChange -= AlarmClockChanged;
		EndAllWindows();
	}

	private void EndAllWindows()
	{
		foreach (ActionWindow window in GetComponents<ActionWindow>()){
			window.End();}

		module_window = null;
		global_window = null;
	}

	private void AlarmClockChanged(bool on)
	{
		alarm_is_on = on;

		if (on){
			StartAlarmHandling();}
		else{
			StopAlarmHandling();}
	}

	private IEnumerator AlarmLoop()
	{
		while (alarm_is_on && !mission_ended){
			Context.Send("There is an annoying alarm going off!");
			yield return new WaitForSeconds(ConfigHelper.Get(2.0f, "timing", "alarm_context_frequency"));}

		alarm_coroutine = null;
	}

	public void StopAlarmHandling()
	{
		if (alarm_coroutine != null){
			StopCoroutine(alarm_coroutine);
			alarm_coroutine = null;}

		if (alarm_window != null){
			alarm_window.End();
			alarm_window = null;}
	}

	public void StartAlarmHandling()
	{
		if (alarm_window == null){
			alarm_window = ActionWindow.Create(gameObject);
			alarm_window
				.AddAction(new ActionTurnOffAlarm(
					this,
					alarm_clock))
				.Register();}

		if (alarm_coroutine == null){alarm_coroutine = StartCoroutine(AlarmLoop());}
	}
}

public struct ModuleInfo {

	public readonly BombComponent component;
	public readonly IBombModuleHandler handler;
	public readonly string name;

	public ModuleInfo(BombComponent Component, IBombModuleHandler Handler, string Name)
	{
		component = Component;
		handler = Handler;
		name = Name;
	}
}
