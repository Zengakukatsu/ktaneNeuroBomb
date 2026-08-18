using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Input;
using Assets.Scripts.Missions;
using NeuroSdk.Actions;
using NeuroSdk.Messages.Outgoing;
using UnityEngine;

public class BombManager : MonoBehaviour {

	public Bomb bomb { get; private set; }
	public bool IsBusy { get; set; }

	public List<ModuleInfo> modules { get; private set; }
	public ActionWindow focus_window;
	public ActionWindow module_window;
	public ActionWindow global_window;
	public BombComponent focus;
	private bool missionEnded;

	private void Start()
	{
		StartCoroutine(Init());
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

		Populate();
		SendBombContext();
		MakeFocusWindow();
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
			.AddAction(new ActionCheckSides(this))
			.AddAction(new ActionBombStatus(this))
			.Register();
	}

	public void MakeFocusWindow()
	{
		focus_window = ActionWindow.Create(gameObject);
		focus_window
			.SetContext(NeuroConfig.MISSION_CONTEXT)
			.AddAction(new ActionFocusModule(this))
			.Register();
	}

	public void MakeModuleWindow()
	{
		foreach (ModuleInfo info in modules){
			if (info.component != focus) continue;

			module_window = ActionWindow.Create(gameObject);
			module_window.SetContext(info.handler.GetContext());
			info.handler.RegisterActions(module_window, this);
			module_window.Register();
			return;
		}
	}

	private void SendBombContext()
	{
		List<string> names = new List<string>();
		foreach (ModuleInfo info in modules) names.Add(info.name);
		Context.Send("The bomb has these modules: " + string.Join(", ", names.ToArray()) + ".");
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
