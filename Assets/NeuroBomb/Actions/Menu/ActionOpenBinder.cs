using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NeuroSdk.Actions;
using NeuroSdk.Json;
using NeuroSdk.Messages.Outgoing;
using NeuroSdk.Websocket;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class ActionOpenBinder : NeuroAction{

    private readonly MenuManager manager;
    private readonly BombBinder binder;
    private Selectable selectable;

    public ActionOpenBinder(MenuManager menuManager, BombBinder bombBinder)
    {
        manager = menuManager;
        binder = bombBinder;
    }

	public override string Name{
		get { return "open_binder"; }}
    protected override string Description{
		get { return "Opens the mission binder."; }}
    protected override JsonSchema Schema{
        get{
            return new JsonSchema{Type = JsonSchemaType.Object};
        }
    }

    protected override ExecutionResult Validate(ActionJData actionData)
    {
        if (binder == null)
        {
            Debug.LogError("[NeuroBomb] No BombBinder found.");
            return ExecutionResult.Failure("There is no binder.");
        }

        selectable = binder.Selectable;

        if (selectable == null)
        {
            Debug.LogError("[NeuroBomb] Binder Selectable is null.");
            return ExecutionResult.Failure("The binder is not selectable.");
        }

        return ExecutionResult.Success();
    }
	protected override void Execute()
	{
        manager.Select(selectable, true);
	}
}